using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Aurora.Modules;
using Aurora.Modules.ProcessMonitor;
using Aurora.Settings;
using Aurora.Settings.Controls;
using Aurora.Utils;
using Serilog.Core;
using Constants = Common.Constants;
using MessageBox = System.Windows.MessageBox;

namespace Aurora;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static bool Closing { get; private set; }
    private static readonly Mutex Mutex = new(false, "{C88D62B0-DE49-418E-835D-CE213D58444C}");

    public static bool IsSilent { get; private set; }

    private static readonly PluginsModule PluginsModule = new();
    private static readonly IpcListenerModule IpcListenerModule = new();
    private static readonly HttpListenerModule HttpListenerModule = new();
    private static readonly ProcessesModule ProcessesModule = new();
    private static readonly RazerSdkModule RazerSdkModule = new(LightingStateManagerModule.LightningStateManager);
    private static readonly DevicesModule DevicesModule = new(RazerSdkModule.RzSdkManager);
    private static readonly LightingStateManagerModule LightingStateManagerModule = new(
        PluginsModule.PluginManager, IpcListenerModule.IpcListener, HttpListenerModule.HttpListener,
        DevicesModule.DeviceManager, ProcessesModule.ActiveProcessMonitor, ProcessesModule.RunningProcessMonitor
    );
    private static readonly OnlineSettings OnlineSettings = new(DevicesModule.DeviceManager, ProcessesModule.RunningProcessMonitor);
    private static readonly LayoutsModule LayoutsModule = new(RazerSdkModule.RzSdkManager, OnlineSettings.LayoutsUpdate);

    private readonly List<AuroraModule> _modules =
    [
        new UpdateModule(),
        new UpdateCleanup(),
        new InputsModule(),
        new MediaInfoModule(),
        new AudioCaptureModule(),
        new PointerUpdateModule(),
        new HardwareMonitorModule(),
        PluginsModule,
        IpcListenerModule,
        HttpListenerModule,
        ProcessesModule,
        new LogitechSdkModule(ProcessesModule.RunningProcessMonitor),
        RazerSdkModule, //depends LSM
        DevicesModule,  //depends Chroma
        LightingStateManagerModule, //depends DeviceManager
        OnlineSettings,
        LayoutsModule,
        new PerformanceMonitor(ProcessesModule.RunningProcessMonitor)
    ];

    private static readonly AuroraControlInterface ControlInterface = new(DevicesModule.DeviceManager, IpcListenerModule.IpcListener);
    private readonly AuroraTrayIcon _trayIcon = new(ControlInterface);

    private static readonly SemaphoreSlim PreventShutdown = new(0);

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Global.Initialize();
        UseArgs(e);

        CheckRunningProcesses();

        new UserSettingsBackup().BackupIfNew();
        var systemInfo = SystemUtils.GetSystemInfo();
        Global.logger.Information("{Sys}", systemInfo);

        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AppendPrivatePath("x64");
        if (!Global.isDebug)
            currentDomain.UnhandledException += CurrentDomain_UnhandledException;

        //Load config
        Global.logger.Information("Loading Configuration");
        Global.Configuration = ConfigManager.Load();
        Global.DeviceConfiguration = ConfigManager.LoadDeviceConfig();

        Global.effengine = new Effects(DevicesModule.DeviceManager);

        if (Global.Configuration.HighPriority)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        }

        WindowListener.Instance = new WindowListener();
        var initModules = _modules.Select(async m => await m.InitializeAsync())
            .Where(t => t!= null)
            .ToArray();

        await ControlInterface.Initialize();
        _trayIcon.DisplayWindow += TrayIcon_OnDisplayWindow;
        var configUi = await CreateWindow();

        Global.logger.Information("Waiting for modules...");
        await Task.WhenAll(initModules);
        MainWindow = configUi;
        Global.logger.Information("Modules initiated");
        if (!IsSilent)
        {
            await DisplayWindow();
        }

        //move this to ProcessModule
        WindowListener.Instance.StartListening();

        //Debug Windows on Startup
        if (Global.Configuration.BitmapWindowOnStartUp)
            Window_BitmapView.Open();
        if (Global.Configuration.HttpWindowOnStartUp)
            Window_GSIHttpDebug.Open(HttpListenerModule.HttpListener);

        SessionEnding += (_, sessionEndingParams) =>
        {
            Global.logger.Information("Session ending. Reason: {Reason}", sessionEndingParams.ReasonSessionEnding);
            Shutdown();
            PreventShutdown.Wait();
        };
    }

    private async Task DisplayWindow()
    {
        if (MainWindow is not ConfigUI mainWindow)
        {
            var configUi = await CreateWindow();
            MainWindow = configUi;
            configUi.Display();
            return;
        }
        mainWindow.Display();
    }

    private async Task<ConfigUI> CreateWindow()
    {
        Global.logger.Information("Loading ConfigUI...");
        var stopwatch = Stopwatch.StartNew();
        var configUi = new ConfigUI(RazerSdkModule.RzSdkManager, PluginsModule.PluginManager, LayoutsModule.LayoutManager,
            HttpListenerModule.HttpListener, IpcListenerModule.IpcListener, DevicesModule.DeviceManager,
            LightingStateManagerModule.LightningStateManager, ControlInterface);
        Global.logger.Debug("new ConfigUI() took {Elapsed} milliseconds", stopwatch.ElapsedMilliseconds);
        
        stopwatch.Restart();
        await configUi.Initialize();
        Global.logger.Debug("configUi.Initialize() took {Elapsed} milliseconds", stopwatch.ElapsedMilliseconds);
        stopwatch.Stop();

        return configUi;
    }

    private void CheckRunningProcesses()
    {
        try
        {
            if (Mutex.WaitOne(TimeSpan.FromMilliseconds(0), true)) return;
            try
            {
                var client = new NamedPipeClientStream(
                    ".", Constants.AuroraInterfacePipe, PipeDirection.Out, PipeOptions.Asynchronous);
                client.Connect(2);
                if (!client.IsConnected)
                    throw new InvalidOperationException();
                var command = "restore"u8.ToArray();
                client.Write(command, 0, command.Length);
                client.Close();
            }
            catch
            {
                MessageBox.Show("Aurora is already running.\r\nExiting.", "Aurora - Error");
                ForceShutdownApp(0);
            }

            Closing = true;
            Environment.Exit(0);
        }
        catch (AbandonedMutexException)
        {
            /* Means previous instance closed anyway */
        }
    }

    [DoesNotReturn]
    internal static void ForceShutdownApp(int exitCode)
    {
        PreventShutdown.Release();
        Environment.ExitCode = exitCode;
        Environment.Exit(exitCode);
    }

    private void UseArgs(StartupEventArgs e)
    {
        for (var i = 0; i < e.Args.Length; i++)
        {
            var arg = e.Args[i];

            switch (arg)
            {
                case "-debug":
                    Global.isDebug = true;
                    Global.logger.Information("Program started in debug mode");
                    break;
                case "-restart":
                    var pid = int.Parse(e.Args[++i]);
                    try
                    {
                        var previousAuroraProcess = Process.GetProcessById(pid);
                        previousAuroraProcess.WaitForExit();
                    }
                    catch (ArgumentException) { /* process doesn't exist */ }
                    break;
                case "-minimized":
                case "-silent":
                    IsSilent = true;
                    Global.logger.Information("Program started with '-silent' parameter");
                    break;
                case "-ignore_update":
                    new UpdateModule().IgnoreUpdate = true;
                    Global.logger.Information("Program started with '-ignore_update' parameter");
                    break;
                case "-delay":
                    if (i + 1 >= e.Args.Length || !int.TryParse(e.Args[i++], out var delayTime))
                        delayTime = 5000;
                    Global.logger.Information("Program started with '-delay' parameter with delay of {DelayTime} ms", delayTime);
                    Thread.Sleep(delayTime);
                    break;
            }
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (Closing)
        {
            return;
        }
        Closing = true;
        base.OnExit(e);

        if (Global.Configuration != null)
            ConfigManager.Save(Global.Configuration, Configuration.ConfigFile);

        var tasks = _modules.Select(async m =>
        {
            try
            {
                await m.DisposeAsync();
            }
            catch (Exception moduleException)
            {
                Global.logger.Fatal(moduleException,"Failed closing module {@Module}", m);
            }
        });
        
        var forceExitTimer = StartForceExitTimer();

        await Task.WhenAll(tasks);
        forceExitTimer.GetApartmentState(); //statement just to keep referenced
        (Global.logger as Logger)?.Dispose();
        _trayIcon.Dispose();

        Mutex.ReleaseMutex();
        Mutex.Dispose();

        PreventShutdown.Release();
    }

    private Thread StartForceExitTimer()
    {
        var thread = new Thread(() =>
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Thread.Sleep(6000);
            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                ForceShutdownApp(0);
            }
        })
        {
            IsBackground = true,
            Name = "Exit timer"
        };
        thread.Start();
        return thread;
    }

    private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exc = (Exception)e.ExceptionObject;
        if (exc is COMException { Message: "0x88890004" })
        {
            return;
        }
        Global.logger.Fatal(exc, "Fatal Exception caught");

        if (!e.IsTerminating || Current == null || Closing)
        {
            return;
        }
        if (exc is SEHException sehException && sehException.CanResume())
        {
            return;
        }

        QuitFromError(exc);
    }

    private void App_DispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var exc = e.Exception;
        if (exc is COMException { Message: "0x88890004" })
        {
            e.Handled = true;
            return;
        }
        Global.logger.Fatal(exc, "Fatal Exception caught");

        if (!Global.isDebug)
            e.Handled = true;
        else
            throw exc;

        QuitFromError(exc);
    }

    private static void QuitFromError(Exception exc)
    {
        if (!Global.Configuration?.CloseProgramOnException ?? false) return;
        if (Closing) return;
        MessageBox.Show("Aurora fatally crashed. Please report the follow to author: \r\n\r\n" + exc,
            "Aurora has stopped working");
        //Perform exit operations
        Current?.Shutdown();
        (Global.logger as Logger)?.Dispose();
    }

    private async void TrayIcon_OnDisplayWindow(object? sender, EventArgs e)
    {
        await DisplayWindow();
    }
}
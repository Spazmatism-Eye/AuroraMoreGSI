using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Amib.Threading;
using AuroraRgb.EffectsEngine;
using AuroraRgb.Modules;
using AuroraRgb.Modules.ProcessMonitor;
using AuroraRgb.Profiles;
using AuroraRgb.Settings.Layers.Ambilight;
using AuroraRgb.Settings.Layers.Controls;
using AuroraRgb.Settings.Overrides;
using AuroraRgb.Utils;
using Common.Utils;
using Newtonsoft.Json;
using PropertyChanged;
using Point = System.Drawing.Point;

namespace AuroraRgb.Settings.Layers;

#region Enums
public enum AmbilightType
{
    [Description("Default")]
    Default = 0,

    [Description("Average color")]
    AverageColor = 1
}

public enum AmbilightCaptureType
{
    [Description("Coordinates")]
    Coordinates = 0,

    [Description("Entire Monitor")]
    EntireMonitor = 1,

    [Description("Foreground Application")]
    ForegroundApp = 2,

    [Description("Specific Process")]
    SpecificProcess = 3
}

public enum AmbilightFpsChoice
{
    [Description("Lowest")]
    VeryLow = 0,

    [Description("Low")]
    Low,

    [Description("Medium")]
    Medium,

    [Description("High")]
    High,

    [Description("Highest")]
    Highest,
}

#endregion

public class AmbilightLayerHandlerProperties : LayerHandlerProperties2Color<AmbilightLayerHandlerProperties>
{
    [JsonIgnore]
    private AmbilightType? _ambilightType;

    [JsonProperty("_AmbilightType")]
    public AmbilightType AmbilightType
    {
        get => Logic._ambilightType ?? _ambilightType ?? AmbilightType.Default;
        set => SetFieldAndRaisePropertyChanged(out _ambilightType, value);
    }

    [JsonIgnore]
    private AmbilightCaptureType? _ambilightCaptureType;

    [JsonProperty("_AmbilightCaptureType")]
    public AmbilightCaptureType AmbilightCaptureType
    {
        get => Logic._ambilightCaptureType ?? _ambilightCaptureType ?? AmbilightCaptureType.EntireMonitor;
        set => SetFieldAndRaisePropertyChanged(out _ambilightCaptureType, value);
    }

    [JsonIgnore]
    private int? _ambilightOutputId;

    [JsonProperty("_AmbilightOutputId")]
    public int AmbilightOutputId
    {
        get => Logic._ambilightOutputId ?? _ambilightOutputId ?? 0;
        set => _ambilightOutputId = value;
    }

    [JsonIgnore]
    private AmbilightFpsChoice? _ambiLightUpdatesPerSecond;

    [JsonProperty("_AmbiLightUpdatesPerSecond")]
    public AmbilightFpsChoice AmbiLightUpdatesPerSecond
    {
        get => Logic._ambiLightUpdatesPerSecond ?? _ambiLightUpdatesPerSecond ?? AmbilightFpsChoice.Medium;
        set => SetFieldAndRaisePropertyChanged(out _ambiLightUpdatesPerSecond, value);
    }

    [JsonIgnore]
    private string? _specificProcess;
        
    [JsonProperty("_SpecificProcess")]
    public string SpecificProcess
    {
        get => Logic._specificProcess ?? _specificProcess ?? string.Empty;
        set => SetFieldAndRaisePropertyChanged(out _specificProcess, value);
    }

    [JsonIgnore]
    private Rectangle? _coordinates;

    [JsonProperty("_Coordinates")]
    [LogicOverridable("Coordinates")] 
    public Rectangle Coordinates
    {
        get => Logic._coordinates ?? _coordinates ?? Rectangle.Empty;
        set => SetFieldAndRaisePropertyChanged(out _coordinates, value);
    }

    [JsonIgnore]
    private bool? _brightenImage;

    [JsonProperty("_BrightenImage")]
    public bool BrightenImage
    {
        get => Logic._brightenImage ?? _brightenImage ?? false;
        set => SetFieldAndRaisePropertyChanged(out _brightenImage, value);
    }

    [JsonIgnore]
    private float? _brightnessChange;

    [JsonProperty("_BrightnessChange")]
    public float BrightnessChange
    {
        get => Logic._brightnessChange ?? _brightnessChange ?? 0.0f;
        set => SetFieldAndRaisePropertyChanged(out _brightnessChange, value);
    }

    [JsonIgnore]
    private bool? _saturateImage;

    [JsonProperty("_SaturateImage")]
    public bool SaturateImage
    {
        get => Logic._saturateImage ?? _saturateImage ?? false;
        set => SetFieldAndRaisePropertyChanged(out _saturateImage, value);
    }

    [JsonIgnore]
    private float? _saturationChange;

    [JsonProperty("_SaturationChange")]
    public float SaturationChange
    {
        get => Logic._saturationChange ?? _saturationChange ?? 0.0f;
        set => SetFieldAndRaisePropertyChanged(out _saturationChange, value);
    }

    [JsonIgnore] private bool? _flipVertically;

    [JsonProperty("_FlipVertically")]
    public bool FlipVertically
    {
        get => Logic._flipVertically ?? _flipVertically ?? false;
        set => SetFieldAndRaisePropertyChanged(out _flipVertically, value);
    }

    [JsonIgnore]
    private bool? _experimentalMode;

    [JsonProperty("_ExperimentalMode")]
    public bool ExperimentalMode
    {
        get => Logic._experimentalMode ?? _experimentalMode ?? false;
        set => SetFieldAndRaisePropertyChanged(out _experimentalMode, value);
    }

    [JsonIgnore] private bool? _hueShiftImage;

    [JsonProperty("_HueShiftImage")]
    public bool HueShiftImage
    {
        get => Logic._hueShiftImage ?? _hueShiftImage ?? false;
        set => SetFieldAndRaisePropertyChanged(out _hueShiftImage, value);
    }

    [JsonIgnore]
    private float? _hueShiftAngle;
    [JsonProperty("_HueShiftAngle")]
    public float HueShiftAngle
    {
        get => Logic._hueShiftAngle ?? _hueShiftAngle ?? 0.0f;
        set => SetFieldAndRaisePropertyChanged(out _hueShiftAngle, value);
    }

    public AmbilightLayerHandlerProperties()
    { }

    public AmbilightLayerHandlerProperties(bool assignDefault = false) : base(assignDefault) { }

    public override void Default()
    {
        base.Default();
        _ambilightOutputId = 0;
        _ambiLightUpdatesPerSecond = AmbilightFpsChoice.Medium;
        _ambilightType = AmbilightType.Default;
        _ambilightCaptureType = AmbilightCaptureType.EntireMonitor;
        _specificProcess = "";
        _coordinates = new Rectangle(0, 0, 0, 0);
        _brightenImage = false;
        _brightnessChange = 1.0f;
        _saturateImage = false;
        _saturationChange = 1.0f;
        _flipVertically = false;
        _experimentalMode = false;
        _hueShiftImage = false;
        _hueShiftAngle = 0.0f;
        _Sequence = new KeySequence(Effects.Canvas.WholeFreeForm);
    }
}

[LogicOverrideIgnoreProperty("_PrimaryColor")]
[LogicOverrideIgnoreProperty("_SecondaryColor")]
[LogicOverrideIgnoreProperty("_Sequence")]
[DoNotNotify]
public partial class AmbilightLayerHandler : LayerHandler<AmbilightLayerHandlerProperties>
{
    private IScreenCapture? _screenCapture;

    private readonly SmartThreadPool _captureWorker;
    private readonly WorkItemCallback _screenshotWork;

    private Brush _screenBrush = Brushes.Transparent;
    private IntPtr _specificProcessHandle = IntPtr.Zero;
    private Rectangle _cropRegion = Rectangle.Empty;
    private ImageAttributes _imageAttributes = new();

    private bool _invalidated; //properties changed
    private bool _brushChanged = true;

    private readonly Stopwatch _captureStopwatch = new();
    private DateTime _lastProcessDetectTry = DateTime.UtcNow;

    public IEnumerable<string> Displays => _screenCapture?.GetDisplays() ?? ImmutableList<string>.Empty;

    public AmbilightLayerHandler() : base("Ambilight Layer")
    {
        var stpStartInfo = new STPStartInfo
        {
            ApartmentState = ApartmentState.STA,
            IdleTimeout = 1000
        };

        _captureWorker = new SmartThreadPool(stpStartInfo)
        {
            MaxThreads = 1,
        };
        _screenshotWork = TakeScreenshot;
    }

    protected override async Task Initialize()
    {
        await base.Initialize();
        _screenCapture?.Dispose();
        _screenCapture = Properties.ExperimentalMode ? new DxScreenCapture() : new GdiScreenCapture();
        _screenCapture.ScreenshotTaken += ScreenshotAction;
    }

    public override EffectLayer Render(IGameState gamestate)
    {
        if (Properties.Sequence.GetAffectedRegion().IsEmpty)
            return EffectLayer.EmptyLayer;

        if (_captureWorker.WaitingCallbacks < 1)
        {
            _captureWorker.QueueWorkItem(_screenshotWork);
        }

        //This is needed to prevent the layer from disappearing
        //for a frame when the user alt-tabs with the foregroundapp option selected
        if (TryGetCropRegion(out var newCropRegion))
            _cropRegion = newCropRegion;
        else if (DateTime.UtcNow - _lastProcessDetectTry > TimeSpan.FromSeconds(2))
        {
            UpdateSpecificProcessHandle(Properties.SpecificProcess);
            _lastProcessDetectTry = DateTime.UtcNow;
        }
        //and because of that, this should never happen 
        if (_cropRegion.IsEmpty)
            return EffectLayer;

        if (!_brushChanged)
        {
            return EffectLayer;
        }

        if (_invalidated)
        {
            EffectLayer.Clear();
            _invalidated = false;
        }

        lock (_screenBrush)
        {
            EffectLayer.DrawTransformed(Properties.Sequence,
                m =>
                {
                    if (!Properties.FlipVertically) return;
                    m.Scale(1, -1, MatrixOrder.Prepend);
                    m.Translate(0, -_cropRegion.Height, MatrixOrder.Prepend);
                },
                g =>
                {
                    try
                    {
                        g.Clear(Color.Transparent);
                        g.FillRectangle(_screenBrush, 0, 0, _cropRegion.Width, _cropRegion.Height);
                    }
                    catch
                    {
                        //rarely matrix
                    }
                },
                _cropRegion with {X = 0, Y = 0}
            );
        }
        _brushChanged = false;

        return EffectLayer;
    }

    protected override System.Windows.Controls.UserControl CreateControl()
    {
        return new Control_AmbilightLayer(this);
    }

    private object? TakeScreenshot(object? sender)
    {
        try
        {
            TryTakeScreenshot();
        }
        catch(Exception e) {
            Global.logger.Error(e, "Ambilight Screenshot Error");
            Thread.Sleep(2000);
        }
        return null;
    }

    private void TryTakeScreenshot()
    {
        _screenCapture?.Capture(_cropRegion);
        WaitTimer(_captureStopwatch.Elapsed);
        _captureStopwatch.Restart();
    }

    private void ScreenshotAction(object? sender, Bitmap bitmap)
    {
        CreateScreenBrush(bitmap, _cropRegion);
    }

    private void WaitTimer(TimeSpan elapsed)
    {
        var screenshotInterval = GetIntervalFromFps(Properties.AmbiLightUpdatesPerSecond);
        if (screenshotInterval > elapsed)
            Thread.Sleep(screenshotInterval - elapsed);
        else
            Thread.Sleep(screenshotInterval);
    }

    //B, G, R, A
    private static readonly long[] ColorData = {0L, 0L, 0L, 0L};
    private void CreateScreenBrush(Bitmap screenCapture, Rectangle cropRegion)
    {
        switch (Properties.AmbilightType)
        {
            case AmbilightType.Default:
                lock (_screenBrush)
                {
                    _screenBrush.Dispose();
                    _screenBrush = new TextureBrush(screenCapture, new Rectangle(0, 0, screenCapture.Width, screenCapture.Height), _imageAttributes);
                }
                _brushChanged = true;
                break;
            case AmbilightType.AverageColor:
                var average = BitmapUtils.GetRegionColor(screenCapture, new Rectangle(Point.Empty, cropRegion.Size), ColorData);

                if (Properties.BrightenImage)
                    average = CommonColorUtils.ChangeBrightness(average, Properties.BrightnessChange);
                if (Properties.SaturateImage)
                    average = CommonColorUtils.ChangeSaturation(average, Properties.SaturationChange);
                if (Properties.HueShiftImage)
                    average = CommonColorUtils.ChangeHue(average, Properties.HueShiftAngle);

                if (average is { R: 0, G: 0, B: 0 })
                {
                    return;
                }

                lock (_screenBrush)
                {
                    _screenBrush.Dispose();
                    _screenBrush = new SolidBrush(average);
                }
                _brushChanged = true;
                break;
        }
    }

    protected override async void PropertiesChanged(object? sender, PropertyChangedEventArgs args)
    {
        base.PropertiesChanged(sender, args);
            
        Initialize().Wait();

        var mtx = BitmapUtils.GetEmptyColorMatrix();
        if (Properties.BrightenImage)
            mtx = BitmapUtils.ColorMatrixMultiply(mtx, BitmapUtils.GetBrightnessColorMatrix(Properties.BrightnessChange));
        if (Properties.SaturateImage)
            mtx = BitmapUtils.ColorMatrixMultiply(mtx, BitmapUtils.GetSaturationColorMatrix(Properties.SaturationChange));
        if (Properties.HueShiftImage)
            mtx = BitmapUtils.ColorMatrixMultiply(mtx, BitmapUtils.GetHueShiftColorMatrix(Properties.HueShiftAngle));
        _imageAttributes = new ImageAttributes();
        _imageAttributes.SetColorMatrix(new ColorMatrix(mtx));
        _imageAttributes.SetWrapMode(WrapMode.Clamp);

        await ClearEvents();
        switch (Properties.AmbilightCaptureType)
        {
            case AmbilightCaptureType.SpecificProcess when !string.IsNullOrWhiteSpace(Properties.SpecificProcess):
                UpdateSpecificProcessHandle(Properties.SpecificProcess);
                
                WindowListener.Instance.WindowCreated += WindowsChanged;
                WindowListener.Instance.WindowDestroyed += WindowsChanged;
                break;
            case AmbilightCaptureType.ForegroundApp:
                _specificProcessHandle = User32.GetForegroundWindow();
                (await ProcessesModule.ActiveProcessMonitor).ActiveProcessChanged += ProcessChanged;
                break;
            case AmbilightCaptureType.Coordinates:
            case AmbilightCaptureType.EntireMonitor:
            default:
                break;
        }

        _invalidated = true;
    }

    private async Task ClearEvents()
    {
        (await ProcessesModule.ActiveProcessMonitor).ActiveProcessChanged -= ProcessChanged;
        WindowListener.Instance.WindowCreated -= WindowsChanged;
        WindowListener.Instance.WindowDestroyed -= WindowsChanged;
    }

    private void ProcessChanged(object? sender, EventArgs e)
    {
        _specificProcessHandle = User32.GetForegroundWindow();
    }

    private void WindowsChanged(object? sender, int e)
    {
        if (!WindowListener.Instance.ProcessWindowsMap.TryGetValue(Properties.SpecificProcess, out var windows)) return;
        var targetWindow = windows.First();
        _specificProcessHandle = new IntPtr(targetWindow.WindowHandle);
    }

    #region Helper Methods
    /// <summary>
    /// Gets the region to crop based on user preference and current display.
    /// Switches display if the desired coordinates are offscreen.
    /// </summary>
    /// <returns></returns>
    private bool TryGetCropRegion(out Rectangle crop)
    {
        crop = new Rectangle();

        switch (Properties.AmbilightCaptureType)
        {
            case AmbilightCaptureType.EntireMonitor:
                //we're using the whole screen, so we don't crop at all
                crop = Screen.AllScreens[Properties.AmbilightOutputId].Bounds;
                break;
            case AmbilightCaptureType.Coordinates:
                var screenBounds = Screen.AllScreens[Properties.AmbilightOutputId].Bounds;
                crop = new Rectangle(
                    Properties.Coordinates.Left - screenBounds.Left,
                    Properties.Coordinates.Top - screenBounds.Top,
                    Properties.Coordinates.Width,
                    Properties.Coordinates.Height);
                break;
            case AmbilightCaptureType.SpecificProcess:
            case AmbilightCaptureType.ForegroundApp:
                var handle = _specificProcessHandle;
                if (handle == IntPtr.Zero)
                    return false;//happens when alt tabbing

                var appRect = GetWindowRectangle(handle);

                crop = new Rectangle(
                    appRect.Left,
                    appRect.Top,
                    appRect.Right - appRect.Left,
                    appRect.Bottom - appRect.Top);

                break;
        }

        return crop is { Width: > 4, Height: > 4 };
    }

    /// <summary>
    /// Returns an interval in ms usign the AmbilightFpsChoice enum.
    /// Higher values result in lower intervals
    /// </summary>
    /// <param name="fps"></param>
    /// <returns></returns>
    private static TimeSpan GetIntervalFromFps(AmbilightFpsChoice fps) => new(0, 0,0,  0, 1000 / (10 + 5 * (int)fps));

    /// <summary>
    /// Updates the handle of the window used to crop the screenshot
    /// </summary>
    /// <param name="process"></param>
    public void UpdateSpecificProcessHandle(string process)
    {
        var processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(process));
        try
        {
            if (processes.Length == 0)
            {
                return;
            }
            var targetProcess = Array.Find(
                processes,
                p => p.MainWindowHandle != IntPtr.Zero
            );
            if (targetProcess != null)  //target process is there but doesn't have window yet
            {
                _specificProcessHandle = targetProcess.MainWindowHandle;
            }
        }
        finally
        {
            foreach (var p in processes)
            {
                p.Close();
            }
        }
    }

    #endregion

    #region DWM

    [LibraryImport("dwmapi.dll")]
    private static partial void DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [Flags]
    private enum DwmWindowAttribute : uint
    {
        DwmwaNcrenderingEnabled = 1,
        DwmwaNcrenderingPolicy = 2,
        DwmwaTransitionsForcedisabled = 4,
        DwmwaAllowNcpaint = 8,
        DwmwaCaptionButtonBounds = 16,
        DwmwaNonclientRtlLayout = 32,
        DwmwaForceIconicRepresentation = 64,
        DwmwaFlip3DPolicy = 128,
        DwmwaExtendedFrameBounds = 256,
        DwmwaHasIconicBitmap = 512,
        DwmwaDisallowPeek = 1024,
        DwmwaExcludedFromPeek = 2048,
        DwmwaCloak = 4096,
        DwmwaCloaked = 8192,
        DwmwaFreezeRepresentation = 16384,
        DwmwaLast = 32768
    }

    private static Rect GetWindowRectangle(IntPtr hWnd)
    {
        var size = Marshal.SizeOf(typeof(Rect));
        DwmGetWindowAttribute(hWnd, (int)DwmWindowAttribute.DwmwaExtendedFrameBounds, out var rect, size);

        return rect;
    }

    #endregion
}

internal interface IScreenCapture : IDisposable
{
    /// <summary>
    /// Captures a screenshot of the full screen, returning a full resolution bitmap
    /// </summary>
    /// <returns></returns>
    void Capture(Rectangle desktopRegion);

    event EventHandler<Bitmap> ScreenshotTaken;

    IEnumerable<string> GetDisplays();
}
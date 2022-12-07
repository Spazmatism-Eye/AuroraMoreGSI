﻿using System;
using System.Linq;
using Aurora.Utils;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Aurora.Modules.AudioCapture;

/// <summary>
/// Utility class to make it easier to manage dealing with audio devices and input.
/// Will handle the creation of devices if required. If another AudioDevice is using that device, they will share the same reference.
/// Can be hot-swapped to a different device, moving all events to the newly selected device.
/// </summary>
public sealed class AudioDeviceProxy : IDisposable, NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
{
    private readonly MMDeviceEnumerator _deviceEnumerator = new();

    public event EventHandler<EventArgs> DeviceChanged;

    // Stores event handlers added to the proxy, so they can easily be added and removed from the MMDevice when it changes without
    // needing to rely on the consumer manually removing and re-adding the events.
    private EventHandler<WaveInEventArgs> _waveInDataAvailable;

    // ID of currently selected device.
    private string _deviceId;
    private bool _defaultDeviceChanged;

    /// <summary>Creates a new reference to the default audio device with the given flow direction.</summary>
    public AudioDeviceProxy(DataFlow flow) : this(AudioDevices.DefaultDeviceId, flow)
    {
    }

    /// <summary>Creates a new reference to the audio device with the given ID with the given flow direction.</summary>
    public AudioDeviceProxy(string deviceId, DataFlow flow)
    {
        Flow = flow;
        DeviceId = deviceId ?? AudioDevices.DefaultDeviceId;
        _deviceEnumerator.RegisterEndpointNotificationCallback(this);
    }

    /// <summary>Indicates recorded data is available on the selected device.</summary>
    /// <remarks>This event is automatically reassigned to the new device when it is swapped.</remarks>
    public event EventHandler<WaveInEventArgs> WaveInDataAvailable
    {
        add
        {
            _waveInDataAvailable += value; // Update stored event listeners
            if (WaveIn != null) WaveIn.DataAvailable += value; // If the device is valid, pass the event handler on
        }
        remove
        {
            _waveInDataAvailable -= value; // Update stored event listeners
            if (WaveIn != null) WaveIn.DataAvailable -= value; // If the device is valid, pass the event handler on
        }
    }

    public MMDevice Device { get; private set; }
    public WasapiCapture WaveIn { get; private set; }
    public string DeviceName { get; private set; }

    /// <summary>Gets the currently assigned direction of this device.</summary>
    private DataFlow Flow { get; }

    /// <summary>Gets or sets the ID of the selected device.</summary>
    public string DeviceId
    {
        get => _deviceId;
        set
        {
            value ??= AudioDevices.DefaultDeviceId; // Ensure not-null (if null, assume default device)
            if (_deviceId == value && !(_defaultDeviceChanged && _deviceId == AudioDevices.DefaultDeviceId)) return;
            _defaultDeviceChanged = false;
            _deviceId = value;
            UpdateDevice();
        }
    }

    /// <summary>Gets a new MMDevice and wave in based on the current <see cref="DeviceId"/> and <see cref="Flow"/></summary>
    private void UpdateDevice()
    {
        // Release the current device (if any), removing any events as required
        if (WaveIn != null)
            WaveIn.DataAvailable -= _waveInDataAvailable;
        DisposeCurrentDevice();

        // Get a new device with this ID and flow direction
        var mmDevice = _deviceId == AudioDevices.DefaultDeviceId
            ? _deviceEnumerator.GetDefaultAudioEndpoint(Flow, Role.Multimedia) // Get default if no ID is provided
            : _deviceEnumerator.EnumerateAudioEndPoints(Flow, DeviceState.Active)
                .FirstOrDefault(d => d.ID == DeviceId); // Otherwise, get the one with this ID
        if (mmDevice == null) return;
        SetDevice(mmDevice);
    }

    private AudioClient _audioClient;   //just keep a reference!
    private void SetDevice(MMDevice mmDevice)
    {
        var _ = mmDevice.AudioMeterInformation?.MasterPeakValue; //"Activate" device
        _audioClient = mmDevice.AudioClient;
        mmDevice.AudioSessionManager.RefreshSessions();
        Device = mmDevice;

        // Get a WaveIn from the device and start it, adding any events as requied
        WaveIn = Flow == DataFlow.Render ? new WasapiLoopbackCapture(mmDevice) : new WasapiCapture(mmDevice);
        WaveIn.DataAvailable += _waveInDataAvailable;
        WaveIn.StartRecording();

        DeviceName = Device.FriendlyName;
        DeviceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Disposes and clears the current <see cref="Device"/> and <see cref="WaveIn"/>.</summary>
    private void DisposeCurrentDevice()
    {
        Device = null;

        WaveIn?.StopRecording();
        WaveIn = null;
        DeviceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (DeviceId != deviceId)
            return;

        switch (newState)
        {
            case DeviceState.Active:
                DisposeCurrentDevice();
                var mmDevice = _deviceEnumerator.GetDevice(DeviceId);
                SetDevice(mmDevice);
                break;
            case DeviceState.Disabled:
            case DeviceState.Unplugged:
            case DeviceState.NotPresent:
                DisposeCurrentDevice();
                break;
        }
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        if (pwstrDeviceId == DeviceId)
        {
            var mmDevice = _deviceEnumerator.GetDevice(pwstrDeviceId);
            SetDevice(mmDevice);
        }
    }

    public void OnDeviceRemoved(string deviceId)
    {
        if (Device.ID == DeviceId)
        {
            DisposeCurrentDevice();
        }
    }

    /// <summary>
    /// Update the device when changed by the system.
    /// </summary>
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (Flow != flow || !AudioDevices.DefaultDeviceId.Equals(DeviceId)) return;
        DisposeCurrentDevice();
        var mmDevice = _deviceEnumerator.GetDevice(defaultDeviceId);
        if (mmDevice.State == DeviceState.Active)
        {
            SetDevice(mmDevice);
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        //unused
    }

    #region IDisposable Implementation

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeCurrentDevice();
        _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
    }

    #endregion
}
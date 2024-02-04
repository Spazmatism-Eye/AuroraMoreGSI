﻿using Common;
using Common.Devices;
using HidLibrary;

namespace AuroraDeviceManager.Devices.UnifiedHID
{
    internal abstract class UnifiedBase
    {
        protected HidDevice device = null;

        public Dictionary<DeviceKeys, Func<byte, byte, byte, bool>> DeviceFuncMap { get; protected set; } = new Dictionary<DeviceKeys, Func<byte, byte, byte, bool>>();
        public Dictionary<DeviceKeys, SimpleColor> DeviceColorMap { get; protected set; } = new();

        public virtual bool IsConnected => device?.IsOpen ?? false;
        public virtual string PrettyName => "DeviceBase";
        public virtual string PrettyNameFull => PrettyName + $" (VendorID={device.Attributes.VendorHexId}, ProductID={device.Attributes.ProductHexId})";

        public abstract bool Connect();

        protected bool Connect(int vendorID, int[] productIDs, short usagePage)
        {
            IEnumerable<HidDevice> devices = HidDevices.Enumerate(vendorID, productIDs);

            if (devices.Count() > 0)
            {
                try
                {
                    device = devices.First(dev => dev.Capabilities.UsagePage == usagePage);
                    device.OpenDevice();

                    if (IsConnected)
                    {
                        Global.Logger.Information("[UnifiedHID] connected to device {Name}", PrettyNameFull);

                        DeviceColorMap.Clear();

                        foreach (var key in DeviceFuncMap)
                        {
                            // Set black as default color
                            DeviceColorMap.Add(key.Key, SimpleColor.Black);
                        }
                    }
                    else
                    { 
                        Global.Logger.Error("[UnifiedHID] error when attempting to open device {Name}", PrettyName);
                    }
                }
                catch (Exception exc)
                {
                    Global.Logger.Error(exc, "[UnifiedHID] error when attempting to open device {Name}:", PrettyName);
                }
            }

            return IsConnected;
        }

        public virtual bool Disconnect()
        {
            try
            {
                if (device != null)
                {
                    device.CloseDevice();

                    Global.Logger.Information("[UnifiedHID] disconnected from device {Name})", PrettyNameFull);
                }
            }
            catch (Exception exc)
            {
                Global.Logger.Error(exc, "[UnifiedHID] error when attempting to close device {Name}:", PrettyName);
            }

            return !IsConnected;
        }

        public virtual bool SetColor(DeviceKeys key, byte red, byte green, byte blue)
        {
            if (DeviceFuncMap.TryGetValue(key, out Func<byte, byte, byte, bool> func))
                return func.Invoke(red, green, blue);

            return false;
        }
    }
}

﻿using System.Collections.Generic;
using System.Text;
using Valve.VR;

namespace BatteryNotification
{
    public class CVRSystemHelper
    {
        private CVRSystem _cvrSystem;
        public CVRSystem CVRSystem
        {
            set
            {
                _cvrSystem = value;
            }
            get
            {
                if (_cvrSystem == null)
                {
                    EVRInitError evrInitError = new EVRInitError();
                    _cvrSystem = OpenVR.Init(ref evrInitError, EVRApplicationType.VRApplication_Background);
                }
                return _cvrSystem;
            }
        }
        public List<uint> GetViveTrackerIndexs()
        {
            return GetDeviceIndexListByRegisteredDeviceType("htc/vive_tracker");
        }
        public string GetRegisteredDeviceType(uint idx)
        {
            if (GetPropertyString(idx, ETrackedDeviceProperty.Prop_RegisteredDeviceType_String, out string result))
            {
                return result;
            }
            return string.Empty;
        }
        public List<uint> GetDeviceIndexListByRegisteredDeviceType(string name)
        {
            List<uint> devices = new List<uint>();

            int connectedDeviceNum = GetConnectedDevicesCount();
            uint connectedDeviceCount = 0;
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (IsDeviceConnected(i))
                {
                    string res = GetRegisteredDeviceType(i);
                    if (res != null)
                    {
                        if (res.Contains(name))
                        {
                            devices.Add(i);
                        }
                    }
                    connectedDeviceCount++;
                }
                if (connectedDeviceCount >= connectedDeviceNum)
                {
                    break;
                }
            }
            return devices;
        }
        public float GetControllerBatteryRemainingAmount(ETrackedControllerRole role)
        {
            uint index = CVRSystem.GetTrackedDeviceIndexForControllerRole(role);
            if (GetPropertyFloat(index, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, out float result))
            {
                return result * 100.0f;
            }
            return 0;
        }
        public uint GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole role)
        {
            return CVRSystem.GetTrackedDeviceIndexForControllerRole(role);
        }
        public float GetTrackerBatteryRemainingAmount(uint index)
        {
            if (GetPropertyFloat(index, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, out float result))
            {
                return result * 100.0f;
            }
            return 0;
        }
        public bool GetPropertyString(uint idx, ETrackedDeviceProperty prop, out string result)
        {
            result = null;
            ETrackedPropertyError error = new ETrackedPropertyError();
            uint size = CVRSystem.GetStringTrackedDeviceProperty(idx, prop, null, 0, ref error);
            if (error != ETrackedPropertyError.TrackedProp_BufferTooSmall)
            {
                return false;
            }
            StringBuilder s = new StringBuilder((int)size);
            s.Length = (int)size;
            CVRSystem.GetStringTrackedDeviceProperty(idx, prop, s, size, ref error);

            result = s.ToString();
            return (error == ETrackedPropertyError.TrackedProp_Success);
        }
        public bool GetPropertyFloat(uint idx, ETrackedDeviceProperty prop, out float result)
        {
            ETrackedPropertyError error = new ETrackedPropertyError();
            result = CVRSystem.GetFloatTrackedDeviceProperty(idx, prop, ref error);
            return (error == ETrackedPropertyError.TrackedProp_Success);
        }
        private int GetConnectedDevicesCount()
        {
            int connectedDevices = 0;
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (IsDeviceConnected(i))
                {
                    connectedDevices++;
                }
            }
            return connectedDevices;
        }
        public bool IsReady()
        {
            return CVRSystem != null;
        }
        private bool IsDeviceConnected(uint idx)
        {
            if (!IsReady()) { return false; }
            return CVRSystem.IsTrackedDeviceConnected(idx);
        }
    }
}

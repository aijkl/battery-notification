using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Valve.VR;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Aijkl.VRChat.BatterNotificaion.Desktop
{
    public class BatterySurveillancer
    {
        public EventHandler<VRDeviceEventArgs> BatteryRemainingChanged;        
        public IReadOnlyList<VRDevice> ReadOnlyVRDevice { private set; get; }
        private readonly CVRSystemHelper cvrSystemHelper;        
        private List<VRDevice> cachedVRDevices;
        public BatterySurveillancer()
        {
            cvrSystemHelper = new CVRSystemHelper();
            cachedVRDevices = new List<VRDevice>();
            ReadOnlyVRDevice = cachedVRDevices;            
        }                
        public void BeginLoop(CancellationToken cancellationToken, int intervalMiliSecond)
        {
            Stopwatch stopwatch = new Stopwatch();
            
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                stopwatch.Restart();

                try
                {
                    if (cvrSystemHelper.IsReady())
                    {                        
                        List<VRDevice> vrDevices = ReadDevices();                        
                        if(vrDevices.Count > 0)
                        {
                            BatteryRemainingChanged?.Invoke(this, new VRDeviceEventArgs(vrDevices.Where(x => cachedVRDevices.Where(y => y.Index == x.Index).FirstOrDefault() == null || x.BatteryRemaining != cachedVRDevices.Where(y => y.Index == x.Index).FirstOrDefault().Index).ToList()));
                            cachedVRDevices = vrDevices;
                            ReadOnlyVRDevice = vrDevices;
                        }                                                
                    }
                }
                catch
                {                    
                }

                stopwatch.Stop();
                Thread.Sleep(intervalMiliSecond - (int)stopwatch.ElapsedMilliseconds);
            }
        }               
        private List<VRDevice> ReadDevices()
        {
            List<VRDevice> devices = new List<VRDevice>();

            VRDevice leftHand = new VRDevice
            {
                BatteryRemaining = cvrSystemHelper.GetControllerBatteryRemainingAmount(ETrackedControllerRole.LeftHand),
                Index = cvrSystemHelper.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand),
                DeviceType = DeviceType.LeftHand
            };
            VRDevice rightHand = new VRDevice
            {
                BatteryRemaining = cvrSystemHelper.GetControllerBatteryRemainingAmount(ETrackedControllerRole.RightHand),
                Index = cvrSystemHelper.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand),
                DeviceType = DeviceType.RightHand
            };
            devices.Add(leftHand);
            devices.Add(rightHand);

            if (cvrSystemHelper.GetPropertyString(leftHand.Index, ETrackedDeviceProperty.Prop_RenderModelName_String, out string result))
            {
                Debug.WriteLine(result);
            }
            if (cvrSystemHelper.GetPropertyString(rightHand.Index, ETrackedDeviceProperty.Prop_RenderModelName_String, out string result2))
            {
                Debug.WriteLine(result2);
            }

            foreach (var index in cvrSystemHelper.GetViveTrackerIndexs())
            {
                devices.Add(new VRDevice
                {
                    BatteryRemaining = cvrSystemHelper.GetTrackerBatteryRemainingAmount(index),
                    Index = index,
                    DeviceType = DeviceType.ViveTracker
                });
            }
            
            return devices;
        }
    }    
}

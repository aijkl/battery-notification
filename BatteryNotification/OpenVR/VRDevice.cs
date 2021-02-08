using System;

namespace Aijkl.VRChat.BatteryNotification.Console
{
    public enum DeviceType
    {
        RightHand,
        LeftHand,
        ViveTracker
    }
    public class VRDevice
    {
        public string Name { set; get; }
        public float BatteryRemaining { set; get; }
        public uint Index { set; get; }
        public DeviceType DeviceType { set; get; }                
        public uint NotificationId { set; get; }
        public uint NotifiedRemaining { set; get; }        
    }
}

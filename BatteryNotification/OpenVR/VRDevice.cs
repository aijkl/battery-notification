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
        public float BatteryRemaining { set; get; }
        public uint Index { set; get; }
        public DeviceType DeviceType { set; get; }
    }
}

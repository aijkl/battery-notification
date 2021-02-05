namespace Aijkl.VRChat.BatterNotificaion.Desktop
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

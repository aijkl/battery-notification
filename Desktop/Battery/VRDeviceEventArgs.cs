using System;
using System.Collections.Generic;

namespace Aijkl.VRChat.BatterNotificaion.Desktop
{
    public class VRDeviceEventArgs : EventArgs
    {
        public VRDeviceEventArgs(List<VRDevice> vrDevices) : base()
        {
            VRDevices = vrDevices;
        }

        public List<VRDevice> VRDevices { set; get; }
    }
}

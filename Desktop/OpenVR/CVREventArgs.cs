using System;
using System.Collections.Generic;
using Valve.VR;

namespace Aijkl.VRChat.BatterNotificaion.Desktop
{
    public class CVREventArgs : EventArgs
    {
        public CVREventArgs(List<VREvent_t> vrEvents) : base()
        {
            VREvents = vrEvents;
        }

        public List<VREvent_t> VREvents { private set; get; }
    }
}

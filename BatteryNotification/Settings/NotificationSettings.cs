using Spectre.Console.Cli;

namespace Aijkl.VRChat.BatteryNotification.Console.Settings
{
    public class NotificationSettings : CommandSettings
    {        
        [CommandOption("--mode <MODE>")]
        public bool Mode { set; get; }
    }
}

using Aijkl.VRChat.BatteryNotification.Console.Commands;
using Spectre.Console.Cli;

namespace Aijkl.VRChat.BatteryNotification.Console
{
    class Program
    {        
        static int Main(string[] args)
        {
            CommandApp commandApp = new CommandApp();
            commandApp.Configure(configuration =>
            {
                configuration.AddCommand<NotificationCommand>("notification");
            });
            return commandApp.Run(args);
        }                                
    }
}

using Aijkl.VRChat.BatteryNotification.Console.Commands;
using Aijkl.VRChat.BatteryNotification.Console.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Aijkl.VRChat.BatteryNotification.Console
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        static int Main(string[] args)
        {
            using Mutex mutex = new Mutex(true, Assembly.GetExecutingAssembly().FullName, out bool createdNew);
            if (!createdNew)
            {
                AppSettings appSettings = AppSettings.Load(AppSettings.FILENAME);
                AnsiConsoleHelper.WrapMarkupLine(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.GeneralMutexError)), AnsiConsoleHelper.State.Info);
                return 1;
            }            

            CommandApp commandApp = new CommandApp();
            commandApp.Configure(configuration =>
            {
                configuration.AddCommand<NotificationCommand>("notification");
                configuration.AddCommand<RegisterCommand>("register");
                configuration.AddCommand<DeRegisterCommand>("deregister");
            });
            return commandApp.Run(args);                        
        }                                
    }
}

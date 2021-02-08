using Aijkl.VRChat.BatteryNotification.Console.Helpers;
using System;
using Spectre.Console;
using Spectre.Console.Cli;
using System.IO;
using Valve.VR;

namespace Aijkl.VRChat.BatteryNotification.Console.Commands
{
    class RegisterCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            AppSettings appSettings = AppSettings.Load(Path.GetFullPath(AppSettings.FILENAME));

            try
            {
                CVRSystemHelper cvrSystemhelper = new CVRSystemHelper(EVRApplicationType.VRApplication_Utility);
                EVRApplicationError vrApplicationError = cvrSystemhelper.CVRApplications.AddApplicationManifest(Path.GetFullPath(appSettings.ApplicationManifestPath), false);
                AnsiConsoleHelper.WrapMarkupLine($"{(appSettings.LanguageDataSet.GetValue(vrApplicationError == EVRApplicationError.None ? nameof(LanguageDataSet.StreamVRAddManifestSuccess) : nameof(LanguageDataSet.StreamVRAddManifestFailure)), vrApplicationError == EVRApplicationError.None ? AnsiConsoleHelper.State.Success : AnsiConsoleHelper.State.Failure)}", AnsiConsoleHelper.State.Success);
                if (vrApplicationError != (int)EVREventType.VREvent_None)
                {
                    AnsiConsoleHelper.WrapMarkupLine(vrApplicationError.ToString(), AnsiConsoleHelper.State.Failure);
                }
            }
            catch (Exception ex)
            {
                AnsiConsoleHelper.WrapMarkupLine(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.StreamVRAddManifestFailure)),AnsiConsoleHelper.State.Failure);
                AnsiConsole.WriteException(ex);
                return 1;
            }
            return 0;
        }
    }
}

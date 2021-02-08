using Aijkl.VRChat.BatteryNotification.Console.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using Valve.VR;

namespace Aijkl.VRChat.BatteryNotification.Console.Commands
{
    public class DeRegisterCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            AppSettings appSettings = AppSettings.Load(Path.GetFullPath(AppSettings.FILENAME));

            try
            {
                CVRSystemHelper cvrSystemhelper = new CVRSystemHelper(EVRApplicationType.VRApplication_Utility);
                EVRApplicationError vrApplicationError = cvrSystemhelper.CVRApplications.RemoveApplicationManifest(Path.GetFullPath(appSettings.ApplicationManifestPath));
                AnsiConsoleHelper.WrapMarkupLine(appSettings.LanguageDataSet.GetValue(vrApplicationError == EVRApplicationError.None ? nameof(LanguageDataSet.StreamVRRemoveManifestSuccess) : nameof(LanguageDataSet.StreamVRRemoveManifestFailure)), vrApplicationError == EVRApplicationError.None ? AnsiConsoleHelper.State.Success : AnsiConsoleHelper.State.Failure);
            }
            catch (Exception ex)
            {
                AnsiConsoleHelper.WrapMarkupLine(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.StreamVRRemoveManifestFailure)), AnsiConsoleHelper.State.Failure);
                AnsiConsole.WriteException(ex);
                return 1;
            }
            return 0;
        }
    }
}

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Aijkl.VRChat.BatteryNotification.Installer
{
    [RunInstaller(true)]
    public class MyInstallerClass : System.Configuration.Install.Installer
    {
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            string assemblypath = Context.Parameters["assemblypath"];
            Process.Start(new ProcessStartInfo()
            {                
                WorkingDirectory = Path.Combine(assemblypath.Replace($"{Path.DirectorySeparatorChar}{Path.GetFileName(assemblypath)}", string.Empty),"BatFiles"),
                FileName = "register.bat"
            });
        }

        public override void Uninstall(IDictionary stateSaver)
        {            
            string assemblypath = Context.Parameters["assemblypath"];            
            Process.Start(new ProcessStartInfo()
            {
                WorkingDirectory = Path.Combine(assemblypath.Replace($"{Path.DirectorySeparatorChar}{Path.GetFileName(assemblypath)}", string.Empty), "BatFiles"),
                FileName = "deregister.bat"
            });
            System.Threading.Thread.Sleep(2000);
            base.Uninstall(stateSaver);                        
        }
    }
}

    
using System;
using System.Windows.Forms;
using System.IO;

namespace Aijkl.VRChat.BatterNotificaion.Desktop
{
    static class Program
    {        
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                MainForm mainForm = new MainForm();
                Application.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageDataSet.ERROR, nameof(LanguageDataSet.ERROR));
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "error.log"), ex.ToString());
            }            
        }
    }
}

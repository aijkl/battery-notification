using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Aijkl.VRChat.BatterNotificaion.Desktop
{
    public partial class MainForm : MetroFramework.Forms.MetroForm
    {
        private readonly NotifyIcon notifyIcon;
        private readonly AppSettings appSettings;
        private readonly MainFormState mainFormState;
        private readonly BatterySurveillancer batterySurveillancer;
        public MainForm()
        {
            InitializeComponent();

            mainFormState = new MainFormState();

            try
            {
                appSettings = AppSettings.Load(Path.Combine(Directory.GetCurrentDirectory(), AppSettings.FILENAME));
                Application.ApplicationExit += Application_ApplicationExit;
            }
            catch(Exception ex)
            {
                throw new Exception(LanguageDataSet.CONFIGURE_FILE_NOT_FOUND, ex);                
            }

            try
            {                
                batterySurveillancer = new BatterySurveillancer();
                Task.Run(() =>
                {
                    batterySurveillancer.BeginLoop(new CancellationTokenSource().Token, appSettings.Interval);
                });                
            }
            catch(Exception ex)
            {
                throw new Exception(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.OpenVRInitError)), ex);
            }


            #region InitializeComponent

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Icon.FromHandle(Properties.Resources.Icon.GetHicon());
            notifyIcon.Visible = true;

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();

            ToolStripMenuItem configureMenuItem = new ToolStripMenuItem();
            configureMenuItem.Text = appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.GeneralConfigure));
            configureMenuItem.Click += ConfigureMenuItem_Click;
            contextMenuStrip.Items.Add(configureMenuItem);

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem();
            exitMenuItem.Text = appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.GeneralExit));
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenuStrip.Items.Add(exitMenuItem);

            
            intervalTextBox.Text = appSettings.Interval.ToString();
            
            notifyIcon.ContextMenuStrip = contextMenuStrip;

            #endregion            
        }

        private void SendTostNotification(string message, string imagePath, DateTimeOffset expirationTime)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
            XmlNodeList toastElements = toastXml.GetElementsByTagName("toast");
            XmlAttribute xmlAttribute = toastXml.CreateAttribute("duration");
            xmlAttribute.NodeValue = "long";
            toastElements[0].Attributes.SetNamedItem(xmlAttribute);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.BatteryAnnounce))));
            stringElements[1].AppendChild(toastXml.CreateTextNode(message));

            XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
            imageElements[0].Attributes.GetNamedItem("src").NodeValue = imagePath;

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = expirationTime;

            ToastNotificationManager.CreateToastNotifier(appSettings.ApplicationId).Show(toast);
        }
        private void ConfigureMenuItem_Click(object sender, EventArgs e)
        {
            Visible = true;
            mainFormState.IsActive = true;
        }
        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            mainFormState.IsActive = false;
            Application.Exit();
        }        
        private void MainForm_Closing(object sender, CancelEventArgs e)
        {
            if (mainFormState.IsActive)
            {
                e.Cancel = true;
                Visible = false;                
            }            
        }
        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            try
            {
                appSettings.SaveToFile();
            }
            catch
            {
                MessageBox.Show(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.ConfigError)));
            }

            if (batterySurveillancer.ReadOnlyVRDevice.Count != 0)
            {
                string message = string.Join("\n", batterySurveillancer.ReadOnlyVRDevice.Select(x => $"{x.DeviceType}:{x.BatteryRemaining}%"));
                SendTostNotification(message, Path.GetFullPath(appSettings.BatteryLogoPath), DateTimeOffset.Now.AddMilliseconds(appSettings.TostNotificationExpirationMiliSecond));
            }
        }
        private void IntervalTextBox_TextChanged(object sender, EventArgs e)
        {
            if(int.TryParse(intervalTextBox.Text,out int result))
            {
                appSettings.Interval = result;
            }
        }
    }
}


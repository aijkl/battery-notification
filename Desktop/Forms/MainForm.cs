using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
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

            Application.ApplicationExit += (object sender, EventArgs e) =>
            {
                SendTostNotification(DateTimeOffset.Now.AddMilliseconds(appSettings.TostNotificationExpirationMiliSecond));
            };

            #region Components
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Icon.FromHandle(Properties.Resources.Icon.GetHicon());
            notifyIcon.Visible = true;

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();

            ToolStripMenuItem configureMenuItem = new ToolStripMenuItem();
            configureMenuItem.Text = appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.GeneralConfigure));
            configureMenuItem.Click += (object sender, EventArgs e) =>
            {
                Visible = true;
                mainFormState.IsActive = true;
            };
            contextMenuStrip.Items.Add(configureMenuItem);

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem();
            exitMenuItem.Text = appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.GeneralExit));
            exitMenuItem.Click += (object sender, EventArgs e) =>
            {
                mainFormState.IsActive = false;
                Application.Exit();
            };
            contextMenuStrip.Items.Add(exitMenuItem);
            
            notifyIcon.ContextMenuStrip = contextMenuStrip;
            #endregion            
        }                
        private void SendTostNotification(DateTimeOffset expirationTime)
        {            
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var vrDevice in batterySurveillancer.ReadOnlyVRDevice)
            {
                stringBuilder.AppendLine($"{vrDevice.DeviceType}:{vrDevice.BatteryRemaining}%");
            }            

            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
            XmlNodeList toastElements = toastXml.GetElementsByTagName("toast");
            XmlAttribute xmlAttribute = toastXml.CreateAttribute("duration");
            xmlAttribute.NodeValue = "long";
            toastElements[0].Attributes.SetNamedItem(xmlAttribute);            

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.BatteryLow))));
            stringElements[1].AppendChild(toastXml.CreateTextNode(stringBuilder.ToString()));

            XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
            imageElements[0].Attributes.GetNamedItem("src").NodeValue = Path.GetFullPath(appSettings.BatteryLogoPath);

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = expirationTime;
            ToastNotificationManager.CreateToastNotifier(appSettings.ApplicationId).Show(toast);
        }
        private void MainForm_Closing(object sender, CancelEventArgs e)
        {
            if (mainFormState.IsActive)
            {
                e.Cancel = true;
                Visible = false;                
            }            
        }        
    }
}


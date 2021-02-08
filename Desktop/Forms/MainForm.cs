using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Valve.VR;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Aijkl.VRChat.BatterNotificaion.Desktop
{
    public partial class MainForm : MetroFramework.Forms.MetroForm
    {
        private readonly NotifyIcon notifyIcon;
        private readonly AppSettings appSettings;
        private readonly MainFormState mainFormState;        
        private readonly CVRSystemHelper cvrSystemHelper;
        public MainForm()
        {
            InitializeComponent();

            mainFormState = new MainFormState();

            try
            {
                appSettings = AppSettings.Load(Path.GetFullPath(AppSettings.FILENAME));                
            }
            catch(Exception ex)
            {
                throw new Exception(LanguageDataSet.CONFIGURE_FILE_NOT_FOUND, ex);                
            }

            try
            {
                cvrSystemHelper = new CVRSystemHelper();
                cvrSystemHelper.BeginEventLoop();
                cvrSystemHelper.CVREvent += CVRSystemHelper_CVREvent;                
            }
            catch(Exception ex)
            {
                throw new Exception(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.OpenVRInitError)), ex);
            }            

            #region InitializeComponent (User)

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
                                    
            notifyIcon.ContextMenuStrip = contextMenuStrip;

            #endregion            
        }

        private void CVRSystemHelper_CVREvent(object sender, CVREventArgs e)
        {
            foreach (var vrEvent in e.VREvents)
            {
                if(vrEvent.eventType == (int)EVREventType.VREvent_Quit)
                {
                    cvrSystemHelper.CVRSystem.AcknowledgeQuit_Exiting();
                    List<VRDevice> vrDevices = ReadDevices();
                    if (vrDevices.Count > 0)
                    {
                        string message = string.Join("\n", vrDevices.Select(x => $"{x.DeviceType}:{x.BatteryRemaining}%"));
                        SendTostNotification(message, Path.GetFullPath(appSettings.BatteryLogoPath), DateTimeOffset.Now.AddMilliseconds(appSettings.TostNotificationExpirationMiliSecond));

                        cvrSystemHelper.Dispose();
                        Application.Exit();
                    }
                }
            }
        }
        private List<VRDevice> ReadDevices()
        {
            List<VRDevice> devices = new List<VRDevice>();

            VRDevice leftHand = new VRDevice
            {
                BatteryRemaining = cvrSystemHelper.GetControllerBatteryRemainingAmount(ETrackedControllerRole.LeftHand),
                Index = cvrSystemHelper.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand),
                DeviceType = DeviceType.LeftHand
            };
            VRDevice rightHand = new VRDevice
            {
                BatteryRemaining = cvrSystemHelper.GetControllerBatteryRemainingAmount(ETrackedControllerRole.RightHand),
                Index = cvrSystemHelper.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand),
                DeviceType = DeviceType.RightHand
            };
            devices.Add(leftHand);
            devices.Add(rightHand);

            if (cvrSystemHelper.GetPropertyString(leftHand.Index, ETrackedDeviceProperty.Prop_RenderModelName_String, out string result))
            {
                Debug.WriteLine(result);
            }
            if (cvrSystemHelper.GetPropertyString(rightHand.Index, ETrackedDeviceProperty.Prop_RenderModelName_String, out string result2))
            {
                Debug.WriteLine(result2);
            }

            foreach (var index in cvrSystemHelper.GetViveTrackerIndexs())
            {
                devices.Add(new VRDevice
                {
                    BatteryRemaining = cvrSystemHelper.GetTrackerBatteryRemainingAmount(index),
                    Index = index,
                    DeviceType = DeviceType.ViveTracker
                });
            }

            return devices;
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
            intervalTextBox.Text = appSettings.Interval.ToString();
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
                appSettings.SaveToFile();
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


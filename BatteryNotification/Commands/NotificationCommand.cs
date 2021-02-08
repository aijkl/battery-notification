using Aijkl.VRChat.BatterNotificaion.Desktop;
using Aijkl.VRChat.BatteryNotification.Console.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;
using WinApi.User32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Aijkl.VRChat.BatteryNotification.Console.Commands
{
    public class NotificationCommand : Command<NotificationSettings>
    {        
        private readonly Dictionary<int, int> notifiedDeviceMap = new Dictionary<int, int>();
        private List<VRDevice> cachedVRDevices = new List<VRDevice>();
        private CVRSystemHelper cvrSystemHelper;
        private AppSettings appSettings;
        private ulong overlayWindowHandle = 0;
        public override int Execute(CommandContext context, NotificationSettings settings)
        {
            if (settings.HideWindow)
            {
                IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
                User32Methods.ShowWindow(handle, 0);
            }                                    

            try
            {
                appSettings = AppSettings.Load(Path.GetFullPath(AppSettings.FILENAME));
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(new Exception(LanguageDataSet.CONFIGURE_FILE_ERROR, ex));
                return 1;
            }            

            try
            {
                AnsiConsole.Status().Start(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.OpenVRInitializing)), action =>
                {
                    cvrSystemHelper = new CVRSystemHelper();

                    EVROverlayError vrOverlayError = OpenVR.Overlay.CreateOverlay(Guid.NewGuid().ToString(), appSettings.ApplicationId, ref overlayWindowHandle);
                    if (vrOverlayError != EVROverlayError.None)
                    {
                        throw new Exception($"{nameof(EVROverlayError)} {vrOverlayError}");
                    }
                });                
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine(new Exception(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.OpenVRInitError)), ex).ToString());
                AnsiConsole.WriteException(new Exception(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.OpenVRInitError)), ex));
                return 1;
            }

            cvrSystemHelper.CVREvent += CVRSystemHelper_CVREvent;

            ReadDevices();
            ShowBatteryInfoConsole();

            cvrSystemHelper.BeginEventLoop();

            System.Console.ReadLine();
            return 0;
        }
        private void CVRSystemHelper_CVREvent(object sender, CVREventArgs e)
        {
            foreach (var vrEvent in e.VREvents)
            {
                AnsiConsole.WriteLine(((EVREventType)vrEvent.eventType).ToString());
                switch ((EVREventType)vrEvent.eventType)
                {
                    case EVREventType.VREvent_TrackedDeviceRoleChanged:
                        ReadDevices();
                        ShowBatteryInfoConsole();
                        break;
                    case EVREventType.VREvent_PropertyChanged:
                        ReadDevices();
                        ShowBatteryInfoConsole();                        
                        foreach (var vrDevice in cachedVRDevices)
                        {
                            ShowOverlayBatteryNotification(vrDevice);
                        }                        
                        break;
                    case EVREventType.VREvent_Quit:
                        if (cachedVRDevices.Count > 0)
                        {
                            ShowToastNotification(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.BatteryAnnounce)), ConvertToString(), appSettings.ApplicationId, Path.GetFullPath(appSettings.BatteryLogoPath), DateTimeOffset.Now.AddMilliseconds(appSettings.TostNotificationExpirationMiliSecond));
                            System.Threading.Thread.Sleep(1000);                            
                        }
                        break;
                    default:
                        return;                        
                }
            }
        }
        private void ReadDevices()
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
            
            foreach (var index in cvrSystemHelper.GetViveTrackerIndexs())
            {
                devices.Add(new VRDevice
                {
                    BatteryRemaining = cvrSystemHelper.GetTrackerBatteryRemainingAmount(index),
                    Index = index,
                    DeviceType = DeviceType.ViveTracker,
                    Name = cvrSystemHelper.GetRegisteredDeviceType(index)
                });
            }

            devices.ToList().ForEach(x =>
            {
                VRDevice vrDevice = cachedVRDevices.Where(y => y.Index == x.Index).FirstOrDefault();
                if(vrDevice != null)
                {
                    x.NotificationId = vrDevice.NotificationId;                    
                    x.NotifiedRemaining = (uint)vrDevice.BatteryRemaining;                    
                }
            });

            cachedVRDevices = devices;
        }                
        private void ShowBatteryInfoConsole()
        {
            AnsiConsole.Console.Clear(true);

            Table table = new Table();
            table.AddColumn("Device");
            table.AddColumn("Battery");
            foreach (var vrDevice in cachedVRDevices)
            {
                table.AddRow(vrDevice.DeviceType.ToString(), $"{vrDevice.BatteryRemaining}%");
            }
            AnsiConsole.Render(table);
        }
        private string ConvertToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{DeviceType.LeftHand} {cachedVRDevices.Where(x => x.DeviceType == DeviceType.LeftHand).Select(x => x.BatteryRemaining).First()}%");
            stringBuilder.Append("  ");
            stringBuilder.AppendLine($"{DeviceType.RightHand} {cachedVRDevices.Where(x => x.DeviceType == DeviceType.RightHand).Select(x => x.BatteryRemaining).First()}%");

            cachedVRDevices.Where(x => x.DeviceType == DeviceType.ViveTracker).ToList().ForEach(x =>
            {
                stringBuilder.AppendLine($"{x.DeviceType} {x.BatteryRemaining}%");
            });
            return stringBuilder.ToString();
        }        
        private bool ShowOverlayBatteryNotification(VRDevice vrDevice)
        {
            if (NeedNotification(vrDevice))
            {
                uint id = vrDevice.Index + (uint)vrDevice.BatteryRemaining;
                EVRNotificationError evrNotificationError = ShowOverlayNotification($"{vrDevice.Name} {vrDevice.BatteryRemaining}%", id);
                if (evrNotificationError == EVRNotificationError.OK)
                {
                    vrDevice.NotificationId = id;
                    vrDevice.NotifiedRemaining = (uint)vrDevice.BatteryRemaining;
                    return true;
                }
            }
            return false;
        }
        private bool NeedNotification(VRDevice vrDevice)
        {
            if(vrDevice.BatteryRemaining <= appSettings.BatteryLowThreshold && vrDevice.NotifiedRemaining - vrDevice.BatteryRemaining >= appSettings.NotificationBatteryInterval)
            {
                return true;
            }
            return false;
        }
        private EVRNotificationError ShowOverlayNotification(string message, uint id)
        {            
            NotificationBitmap_t notificationBitmap_T = new NotificationBitmap_t();
            return cvrSystemHelper.CVRNotifications.CreateNotification(overlayWindowHandle, 0, EVRNotificationType.Transient, message, EVRNotificationStyle.Application, ref notificationBitmap_T, ref id);
        }
        private void ShowToastNotification(string title, string message, string applicationId, string imagePath, DateTimeOffset expirationTime)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
            XmlNodeList toastElements = toastXml.GetElementsByTagName("toast");
            XmlAttribute xmlAttribute = toastXml.CreateAttribute("duration");
            xmlAttribute.NodeValue = "long";
            toastElements[0].Attributes.SetNamedItem(xmlAttribute);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(title));
            stringElements[1].AppendChild(toastXml.CreateTextNode(message));

            XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
            imageElements[0].Attributes.GetNamedItem("src").NodeValue = imagePath;

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = expirationTime;

            ToastNotificationManager.CreateToastNotifier(applicationId).Show(toast);
        }
    }
}

using Aijkl.VRChat.BatterNotificaion.Desktop;
using Aijkl.VRChat.BatteryNotification.Console.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Valve.VR;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Aijkl.VRChat.BatteryNotification.Console.Commands
{
    public class NotificationCommand : Command<NotificationSettings>
    {
        private CVRSystemHelper cvrSystemHelper;                        
        public override int Execute(CommandContext context, NotificationSettings settings)
        {
            AppSettings appSettings;
            List<VRDevice> cachedVRDevices = new List<VRDevice>();            
            ulong overlayWindowHandle = 0;

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
                cvrSystemHelper = new CVRSystemHelper();

                EVROverlayError vrOverlayError = OpenVR.Overlay.CreateOverlay(Guid.NewGuid().ToString(), appSettings.ApplicationId, ref overlayWindowHandle);
                if (vrOverlayError != EVROverlayError.None)
                {
                    throw new Exception($"{nameof(EVROverlayError)} {vrOverlayError}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine(new Exception(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.OpenVRInitError)), ex).ToString());
                AnsiConsole.WriteException(new Exception(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.OpenVRInitError)), ex));
                return 1;
            }
            
            cvrSystemHelper.CVREvent += (object sender, CVREventArgs vrEventArgs) =>
            {
                foreach (var vrEvent in vrEventArgs.VREvents)
                {
                    switch ((EVREventType)vrEvent.eventType)
                    {
                        //case EVREventType.VREvent_PropertyChanged:
                        //    cachedVRDevices = ReadDevices();
                        //    uint id = (uint)Math.Abs(new Random().Next());
                        //    NotificationBitmap_t notificationBitmap_T = new NotificationBitmap_t();
                        //    cvrSystemHelper.CVRNotifications.CreateNotification(overlayWindowHandle, 0, EVRNotificationType.Transient, ConvertToString(cachedVRDevices), EVRNotificationStyle.Application, ref notificationBitmap_T, ref id);
                        //    break;
                        case EVREventType.VREvent_Quit:                            
                            SendToastNotification(appSettings.LanguageDataSet.GetValue(nameof(LanguageDataSet.BatteryAnnounce)), ConvertToString(cachedVRDevices), appSettings.ApplicationId, Path.GetFullPath(appSettings.BatteryLogoPath), new DateTimeOffset().AddMilliseconds(appSettings.TostNotificationExpirationMiliSecond));
                            break;                        
                        default:
                            return;
                    }
                }
            };


            Task.Run(() =>
            {
                cvrSystemHelper.BeginEventLoop();
            }).Wait();

            return 0;
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
        private string ConvertToString(List<VRDevice> vrDevices)
        {
            return string.Join("\n", vrDevices.Select(x => $"{x.DeviceType} {x.BatteryRemaining}%"));
        }        
        private void SendToastNotification(string title, string message, string applicationId, string imagePath, DateTimeOffset expirationTime)
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

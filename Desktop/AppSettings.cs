using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Aijkl.VRChat.BatterNotificaion.Desktop
{
    public class AppSettings
    {
        [JsonIgnore]
        public const string FILENAME = "appsettings.json";

        [JsonIgnore]
        public string FilePath { private set; get; }

        [JsonProperty("batteryLogPath")]
        public string BatteryLogoPath { set; get; }

        [JsonProperty("applicationId")]
        public string ApplicationId { set; get; }

        [JsonProperty("interval")]
        public int Interval { set; get; }

        [JsonProperty("tostNotificationExpirationMiliSecond")]
        public int TostNotificationExpirationMiliSecond { set; get; }

        [JsonProperty("languageDataSet")]
        public LanguageDataSet LanguageDataSet { set; get; }

        public static AppSettings Load(string filePath)
        {
            AppSettings appSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(filePath));
            appSettings.FilePath = filePath;
            return appSettings;
        }
        public void SaveToFile()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this));
        }
    }
    public class LanguageDataSet
    {
        public const string CONFIGURE_FILE_NOT_FOUND = "Configuration file not found";
        public const string ERROR = "An error has occurred";
        public string GetValue(string memberName)
        {
            Dictionary<string, string> keyValuePairs = (Dictionary<string, string>)GetType().GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(this);
            if (keyValuePairs.TryGetValue(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, out string value))
            {
                return value;
            }
            else
            {
                value = keyValuePairs.ToList().FirstOrDefault().Value;
                value = string.IsNullOrEmpty(value) ? string.Empty : value;
                return value;
            }
        }

        [JsonProperty("General.Configure")]
        public Dictionary<string,string> GeneralConfigure { set; get; }

        [JsonProperty("General.Exit")]
        public Dictionary<string, string> GeneralExit { set; get; }

        [JsonProperty("OpenVR.InitError")]
        public Dictionary<string, string> OpenVRInitError { set; get; }

        [JsonProperty("Battery.Low")]
        public Dictionary<string, string> BatteryLow { set; get; }
    }
}

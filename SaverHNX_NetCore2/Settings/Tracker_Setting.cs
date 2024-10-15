using SaverHNX_NetCore2.Extensions;

namespace SaverHNX_NetCore2.Settings
{
    public class Tracker_Setting
    {
        public string API_URL { get; set; } = string.Empty;
        public string APPNAME_GRAFANA {  get; set; } = string.Empty ;
        public static Tracker_Setting MapValue(IConfiguration config)
        {
            var setting = config.Get<Tracker_Setting>();

            return setting;
        }
        public static void Validate(Tracker_Setting _trackerSetting)
        {
            if (string.IsNullOrWhiteSpace(_trackerSetting.API_URL))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Tracking.API_URL chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(_trackerSetting.APPNAME_GRAFANA))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Tracking.APPNAME_GRAFANA chưa đc khởi tạo !!!!");
        }
    }
}

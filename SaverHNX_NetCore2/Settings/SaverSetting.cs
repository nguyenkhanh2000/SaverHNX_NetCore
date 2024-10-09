using SaverHNX_NetCore2.Extensions;

namespace SaverHNX_NetCore2.Settings
{
    public class SaverSetting
    {
        public string FILE_INI_TEMPLATE { get; set; } = string.Empty;
        public string URL_MONITOR_LISTENER { get; set; } = string.Empty;
        public int TIMER_PROCESS_DATA_REDIS {  get; set; }   
        public int TIMER_PROCESS_DATA_DATABASE { get; set; }
        public static SaverSetting MapValue(IConfiguration config)
        {
            var setting = config.Get<SaverSetting>();  

            return setting;
        }
        public static void Validate(SaverSetting _saverSetting)
        {
            if (string.IsNullOrWhiteSpace(_saverSetting.FILE_INI_TEMPLATE))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Saver.FILE_INI_TEMPLATE chưa đc khởi tạo !!!!");
            if(string.IsNullOrWhiteSpace(_saverSetting.URL_MONITOR_LISTENER))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Saver.URL_MONITOR_LISTENER chưa đc khởi tạo !!!!");
        }
    }
}

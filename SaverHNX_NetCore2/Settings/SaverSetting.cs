using SaverHNX_NetCore2.Extensions;

namespace SaverHNX_NetCore2.Settings
{
    public class SaverSetting
    {
        public string FILE_INI_TEMPLATE { get; set; } = string.Empty;
        public string URL_MONITOR_LISTENER { get; set; } = string.Empty;
        public  string CONNECTION_STRING_68 { get; set; } = string.Empty;
        public string CONNECTION_STRING_DBORACLE {  get; set; } = string.Empty; 
        public string SPSPIRE_S5G_HNXSAVER_IG_DI_U {  get; set; } = string.Empty;
        public int TIMER_PROCESS_DATA_REDIS {  get; set; }   
        public int TIMER_PROCESS_DATA_DATABASE { get; set; }
        public int SQL_COMMAND_TIMEOUT { get; set; }
        public string TIME_STOP_TRUNCATE_DB { get; set; } = string.Empty;
        public string prc_S5G_HNX_SAVER_IG__CLEAR { get; set; } = "";
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

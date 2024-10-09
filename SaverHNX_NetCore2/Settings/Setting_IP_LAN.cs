using SaverHNX_NetCore2.Extensions;

namespace SaverHNX_NetCore2.Settings
{
    public class Setting_IP_LAN
    {
        public string PREFIX_IP_LAN_FOX { get; set; } = string.Empty;
        public string PREFIX_IP_LAN_HSX { get; set; } = string.Empty;
        public string PREFIX_IP_LAN_HNX { get; set; } = string.Empty;
        public string PREFIX_IP_LAN_FPTS { get; set; } = string.Empty;
        public string PREFIX_IP_LAN_FPTS_4 { get; set; } = string.Empty;
        public string PREFIX_IP_LAN_FPTS_BLAZE { get; set; } = string.Empty;
        public string PREFIX_IP_LAN_VM { get; set; } = string.Empty;
        public static Setting_IP_LAN MapValue(IConfiguration config)
        {
            var setting = config.Get<Setting_IP_LAN>();
            Validate(setting);
            return setting;
        }
        private static void Validate(Setting_IP_LAN setting)
        {
            if (string.IsNullOrWhiteSpace(setting.PREFIX_IP_LAN_FOX))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_IP_LAN.PREFIX_IP_LAN_FOX chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(setting.PREFIX_IP_LAN_HSX))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_IP_LAN.PREFIX_IP_LAN_HSX chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(setting.PREFIX_IP_LAN_HNX))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_IP_LAN.PREFIX_IP_LAN_HNX chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(setting.PREFIX_IP_LAN_FPTS))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_IP_LAN.PREFIX_IP_LAN_FPTS chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(setting.PREFIX_IP_LAN_FPTS_4))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_IP_LAN.PREFIX_IP_LAN_FPTS_4 chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(setting.PREFIX_IP_LAN_FPTS_BLAZE))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_IP_LAN.PREFIX_IP_LAN_FPTS_BLAZE chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(setting.PREFIX_IP_LAN_VM))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_IP_LAN.PREFIX_IP_LAN_VM chưa đc khởi tạo !!!!");
        }
    }
}

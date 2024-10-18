using SaverHNX_NetCore2.Extensions;

namespace SaverHNX_NetCore2.Settings
{
    public class RedisSetting
    {
        public string Endpoints { get; set; } = string.Empty;
        public string Endpoints_FOX {  get; set; } = string.Empty;
        public int Redis_DB { get; set; }
        public string TEMPLATE_REDIS_KEY_LE_VOL { get; set; } = string.Empty;
        public string TEMPLATE_REDIS_KEY_LE_VAL { get; set; } = string.Empty;
        public string TEMPLATE_REDIS_KEY_KL_LS { get; set; } = string.Empty;
        public string KEY_DELETE_REDIS_TKTT { get; set; } = string.Empty;
        public string KEY_DELETE_REDIS_LS { get; set; } = string.Empty;
        public string KEY_S5G_IG_LOGON { get; set; } = string.Empty;    

        public static RedisSetting MapValue(IConfiguration _config)
        {
            var RedisSetting = _config.Get<RedisSetting>(); 

            return RedisSetting;
        }
        private static void Validate(RedisSetting redisSetting) 
        {
            if (string.IsNullOrWhiteSpace(redisSetting.Endpoints))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Redis.Endpoints chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(redisSetting.TEMPLATE_REDIS_KEY_LE_VOL))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Redis.TEMPLATE_REDIS_KEY_LE_VOL chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(redisSetting.TEMPLATE_REDIS_KEY_LE_VAL))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Redis.TEMPLATE_REDIS_KEY_LE_VAL chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(redisSetting.TEMPLATE_REDIS_KEY_KL_LS))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Redis.TEMPLATE_REDIS_KEY_KL_LS chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(redisSetting.KEY_DELETE_REDIS_TKTT))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Redis.KEY_DELETE_REDIS_TKTT chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(redisSetting.KEY_DELETE_REDIS_LS))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Redis.KEY_DELETE_REDIS_LS chưa đc khởi tạo !!!!");
        }

    }
}

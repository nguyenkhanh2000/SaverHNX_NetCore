using SaverHNX_NetCore2.Extensions;

namespace SaverHNX_NetCore2.Settings
{
    public class BaseSetting
    {
        public static string BASE_APP_NAME { get; set; } = string.Empty;
        public static string BASE_LOG_ERROR { get; set; } = string.Empty ;
        public static string BASE_LOG_SQL { get; set; } = string.Empty;
        public static string BASE_LOG_MULTI_THREAD { get; set; } = string.Empty;
        public static string BASE_LOG_PATH_ERROR { get; set; } = string.Empty;
        public static string BASE_LOG_PATH_SQL { get; set; } = string.Empty;
        public static string BASE_LOG_PATH_TEXT { get; set; } = string.Empty;
        public static string BASE_LOG_PATH_EX { get; set; } = string.Empty;
        public static string BASE_TEMPLATE_LOG_EX_FILENAME { get; set; } = string.Empty;
        public static BaseSetting MapValue(IConfiguration config)
        {
            var Setting = config.Get<BaseSetting>();
            Validate(Setting);
            return Setting;
        }
        private static void Validate(BaseSetting _baseSetting)
        {
            if (string.IsNullOrEmpty(BASE_APP_NAME))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BASE_APP_NAME chưa đc khởi tạo !!!!");

            if (string.IsNullOrEmpty(BASE_LOG_ERROR))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BASE_LOG_ERROR chưa đc khởi tạo !!!!");            

            if (string.IsNullOrEmpty(BASE_LOG_SQL))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BASE_LOG_SQL chưa đc khởi tạo !!!!");

            if (string.IsNullOrEmpty(BASE_LOG_MULTI_THREAD))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BASE_LOG_MULTI_THREAD chưa đc khởi tạo !!!!");

            if (string.IsNullOrEmpty(BASE_LOG_PATH_ERROR))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BASE_LOG_PATH_ERROR chưa đc khởi tạo !!!!");

            if (string.IsNullOrEmpty(BASE_LOG_PATH_SQL))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_ASE_LOG_PATH_SQL chưa đc khởi tạo !!!!");

            if (string.IsNullOrEmpty(BASE_LOG_PATH_TEXT))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BASE_LOG_PATH_TEXT chưa đc khởi tạo !!!!");

            if (string.IsNullOrEmpty(BASE_LOG_PATH_EX))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BASE_LOG_PATH_EX chưa đc khởi tạo !!!!");

            if (string.IsNullOrEmpty(BASE_TEMPLATE_LOG_EX_FILENAME))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BASE_TEMPLATE_LOG_EX_FILENAME chưa đc khởi tạo !!!!");            
        }
    }
}

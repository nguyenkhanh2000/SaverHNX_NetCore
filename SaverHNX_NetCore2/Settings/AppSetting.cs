namespace SaverHNX_NetCore2.Settings
{
    public class AppSetting
    {
        public BaseSetting BaseSetting { get; set; }
        public BrokerSetting BrokerSetting { get; set; }
        public RedisSetting RedisSetting { get; set; }
        public Setting_IP_LAN Setting_IP_LAN { get; set;}
        public SaverSetting SaverSetting { get; set; }  
        public Tracker_Setting Tracker_Setting { get; set; }
        public static AppSetting MapValue(IConfiguration _config)
        {
            var _baseSetting = BaseSetting.MapValue(_config.GetSection(nameof(BaseSetting)));
            var _brokerSetting = BrokerSetting.MapValue(_config.GetSection(nameof(BrokerSetting)));
            var _redisSetting = RedisSetting.MapValue(_config.GetSection(nameof(RedisSetting)));
            var _ipSetting = Setting_IP_LAN.MapValue(_config.GetSection(nameof(Setting_IP_LAN)));
            var _saverSetting = SaverSetting.MapValue(_config.GetSection(nameof(SaverSetting)));
            var _trackerSetting = Tracker_Setting.MapValue(_config.GetSection(nameof(Tracker_Setting)));
            return new AppSetting
            {
                BaseSetting = _baseSetting,
                BrokerSetting = _brokerSetting,
                RedisSetting = _redisSetting,
                Setting_IP_LAN = _ipSetting,
                SaverSetting = _saverSetting,
                Tracker_Setting = _trackerSetting,
            };
        }
    }
}

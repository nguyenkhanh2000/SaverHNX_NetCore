using Microsoft.AspNetCore.Mvc;
using SaverHNX_NetCore2.Extensions;

namespace SaverHNX_NetCore2.Settings
{
    public class BrokerSetting
    {
        public string Host { get; set; } =  string.Empty;
        public int Port { get; set; } 
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
        public string ExchangeName { get; set; } = string.Empty;
        public string RoutingKey { get; set; } = string.Empty;
        public string Durable { get; set; } = string.Empty;
        public static BrokerSetting MapValue(IConfiguration config)
        {
            var _brokerSetting =  config.Get<BrokerSetting>();
            Validate(_brokerSetting);
            return _brokerSetting;
        }
        private static void Validate(BrokerSetting brokerSetting)
        {
            if (string.IsNullOrWhiteSpace(brokerSetting.Host))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_BrokerHost chưa đc khởi tạo !!!!");
            if (brokerSetting.Port <= 0 )
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Broker_Port chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(brokerSetting.Username))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Broker_Username chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(brokerSetting.Password))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Broker_Password chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(brokerSetting.QueueName))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Broker_QueueName chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(brokerSetting.ExchangeName))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Broker_ExchangeNamet chưa đc khởi tạo !!!!");
            if (string.IsNullOrWhiteSpace(brokerSetting.RoutingKey))
                CLog.LogError(CBase.GetDeepCaller(), "KhanhNV_Setting_Broker_RoutingKey chưa đc khởi tạo !!!!");
        }
    }
}

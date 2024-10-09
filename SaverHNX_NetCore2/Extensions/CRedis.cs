using Newtonsoft.Json;
using SaverHNX_NetCore2.Models;
using SaverHNX_NetCore2.Settings;
using StackExchange.Redis;

namespace SaverHNX_NetCore2.Extensions
{
    public class CRedis
    {
        private readonly Lazy<ConnectionMultiplexer> m_lazyConnection;
        private IDatabase m_database;

        private string m_strServiceName;
        private List<string> m_lstEndpoints;
        private ConnectionMultiplexer m_ConnectionMultiplexer;
        private IDatabase m_Database;
        public IDatabase RC
        {
            get { return this.m_database; }
        }
        public CRedis(string strServiceName, List<string> lstEndpoints)
        {
            this.m_strServiceName = strServiceName;
            this.m_lstEndpoints = lstEndpoints;
            ConnectRedisSentinel(this.m_strServiceName, this.m_lstEndpoints);
        }
        public CRedis(RedisSetting _setting)
        {
            m_lazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(_setting.Endpoints));
            this.m_database = m_lazyConnection.Value.GetDatabase(_setting.Redis_DB);
        }

        public IDatabase Database
        {
            get { return this.m_Database; }
        }

        public void ConnectRedisSentinel(string strServiceName, List<string> lstEndpoints)
        {
            try
            {
                var options = new ConfigurationOptions
                {
                    ServiceName = strServiceName,
                    CommandMap = CommandMap.Sentinel,
                    AbortOnConnectFail = false,
                };

                foreach (var endpoint in lstEndpoints)
                {
                    options.EndPoints.Add(endpoint);
                }

                m_ConnectionMultiplexer = ConnectionMultiplexer.Connect(options);
                m_Database = m_ConnectionMultiplexer.GetDatabase();
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
        public void DeleteKeysByPattern(string strPattern)
        {
            try
            {
                //Lấy tất cả các endpoints
                var endpoints = m_lazyConnection.Value.GetEndPoints();

                foreach (var endpoint in endpoints)
                {
                    // Lấy server tương ứng với endpoint
                    var server = m_lazyConnection.Value.GetServer(endpoint);

                    // Lặp qua tất cả các key phù hợp với pattern trên server

                    foreach (var key in server.Keys(database: m_database.Database, pattern: strPattern))
                    {
                        m_database.KeyDelete(key);
                    }
                }
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
        public bool SetCacheBI(string strKey, string strValue, int intDuration)
        {
            bool redis = false;
            bool redisFox= false;
            try
            {
                
                RC.StringSet(strKey, strValue, TimeSpan.FromMinutes(intDuration));
                // Backup dữ liệu theo định dạng khóa khác
                string backupKey = "BACKUP:" + DateTime.Today.ToString("yyyy:MM:dd:") + strKey;

                RC.StringSet(backupKey, strValue, TimeSpan.FromMinutes(intDuration));

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
    }
}

using Newtonsoft.Json;
using SaverHNX_NetCore2.Models;
using SaverHNX_NetCore2.Settings;
using StackExchange.Redis;
using System.Text;

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

        private const string TEMPLATE_REDIS_VALUE = "{\"Time\":\"(Now)\",\"Data\":[(RedisData)]}";
        private const string FORMAT_DATETIME_6 = "yyyy-MM-dd HH:mm:ss.fff";//8:39 AM Friday, April 01, 2016
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
        public bool SetCache(string strKey, string strValue, int intDuration)
        {
            try
            {
                string strValue_Set = this.AddHeaderFooter(strValue);
                RC.StringSet(strKey, strValue_Set, TimeSpan.FromMinutes(intDuration));

                string backupKey = "BACKUP:" + DateTime.Today.ToString("yyyy:MM:dd:") + strKey;
                RC.StringSet(backupKey, strValue_Set, TimeSpan.FromMinutes(intDuration));

                return true;
            }
            catch (Exception ex) 
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
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
        private string AddHeaderFooter(string strRedisValue)
        {
            StringBuilder sb = new StringBuilder(TEMPLATE_REDIS_VALUE);
            try
            {
                sb = sb.Replace("(Now)", DateTime.Now.ToString(FORMAT_DATETIME_6));  //FORMAT_TIME
                sb = sb.Replace("(RedisData)", strRedisValue);
                string result = JsonConvert.SerializeObject(sb.ToString());
                return result;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "[]";
            }
        }
    }
}

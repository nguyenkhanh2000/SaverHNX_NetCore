using Newtonsoft.Json;
using SaverHNX_NetCore2.Models;
using SaverHNX_NetCore2.Settings;
using StackExchange.Redis;
using System.Text;

namespace SaverHNX_NetCore2.Extensions
{
    public class CRedis
    {
        private RedisSetting _redisSetting;
        private readonly Lazy<ConnectionMultiplexer> m_lazyConnection;
        private readonly Lazy<ConnectionMultiplexer> m_lazyConnectionFOX;
        private IDatabase m_database;
        private IDatabase m_databaseFox;

        private string m_strServiceName;
        private List<string> m_lstEndpoints;
        private ConnectionMultiplexer m_ConnectionMultiplexer;

        private const string TEMPLATE_REDIS_VALUE = "{\"Time\":\"(Now)\",\"Data\":[(RedisData)]}";
        private const string FORMAT_DATETIME_6 = "yyyy-MM-dd HH:mm:ss.fff";//8:39 AM Friday, April 01, 2016
        public IDatabase RC
        {
            get { return this.m_database; }
        }
        public IDatabase RC_Fox
        {
            get { return this.m_databaseFox; }
        }
        public CRedis(RedisSetting _setting)
        {
            try
            {
                this._redisSetting = _setting;
                //LLQ
                m_lazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(_setting.Endpoints));
                this.m_database = m_lazyConnection.Value.GetDatabase(_setting.Redis_DB);
                //FOX
                m_lazyConnectionFOX = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(_setting.Endpoints_FOX));
                this.m_databaseFox = m_lazyConnectionFOX.Value.GetDatabase(7);
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
        public async Task SortedSetAddAsync(string key, string value, double score)
        {
            try
            {
                var taskLLQ = Task.Run(() =>
                {
                    try
                    {
                        if (RC != null)
                        {
                            // LLQ cache set
                            RC.SortedSetAdd(key, value, score);
                        }
                    }
                    catch (Exception ex)
                    {
                        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                    }
                });

                //FOX
                var taskFOX = Task.Run(() =>
                {
                    try
                    {
                        if (RC_Fox != null)
                        {
                            // FOX cache set
                            RC_Fox.SortedSetAdd(key, value, score);
                        }
                    }
                    catch (Exception ex)
                    {
                        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                    }
                });
                await Task.WhenAll(taskLLQ, taskFOX);
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
                string backupKey = "BACKUP:" + DateTime.Today.ToString("yyyy:MM:dd:") + strKey;
                //LLQ
                var taskLLQ = Task.Run(() =>
                {
                    try
                    {
                        if(RC != null)
                        {
                            // LLQ cache set
                            RC.StringSet(strKey, strValue_Set, TimeSpan.FromMinutes(intDuration));
                            RC.StringSet(backupKey, strValue_Set, TimeSpan.FromMinutes(intDuration));
                        } 
                    }
                    catch (Exception ex)
                    {
                        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                    }
                });

                //FOX
                var taskFOX = Task.Run(() =>
                {
                    try
                    {
                        if (RC_Fox != null)
                        {
                            // FOX cache set
                            RC_Fox.StringSet(strKey, strValue_Set, TimeSpan.FromMinutes(intDuration));
                            RC_Fox.StringSet(backupKey, strValue_Set, TimeSpan.FromMinutes(intDuration));
                        }  
                    }
                    catch (Exception ex)
                    {
                        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                    }
                });
                Task.WhenAll(taskLLQ, taskFOX).Wait();

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
            try
            {
                // Backup dữ liệu theo định dạng khóa khác
                string backupKey = "BACKUP:" + DateTime.Today.ToString("yyyy:MM:dd:") + strKey;
                //LLQ
                var taskLLQ = Task.Run(() =>
                {
                    try
                    {
                        if (RC != null)
                        {
                            RC.StringSet(strKey, strValue, TimeSpan.FromMinutes(intDuration));

                            RC.StringSet(backupKey, strValue, TimeSpan.FromMinutes(intDuration));
                        }
                    }
                    catch (Exception ex)
                    {
                        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                    }
                });

                var taskFOX = Task.Run(() =>
                {
                    try
                    {
                        if (RC_Fox != null)
                        {
                            RC_Fox.StringSet(strKey, strValue, TimeSpan.FromMinutes(intDuration));

                            RC_Fox.StringSet(backupKey, strValue, TimeSpan.FromMinutes(intDuration));
                        }
                    }
                    catch (Exception ex)
                    {
                        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                    }
                });
                Task.WhenAll(taskLLQ, taskFOX).Wait();

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

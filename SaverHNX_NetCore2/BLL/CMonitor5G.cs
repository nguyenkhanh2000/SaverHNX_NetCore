using SaverHNX_NetCore2.Extensions;
using SaverHNX_NetCore2.Settings;
using System.Net;
using System.Text;

namespace SaverHNX_NetCore2.BLL
{
    public class CMonitor5G
    {
        // -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        //public const string CHANNEL_MONITOR_5G = "S5G_MONITOR_5G"; 
        private const string FORMAT_DATETIME = "yyyy-MM-dd HH:mm:ss.fff";

        //const string PREFIX_IP_LAN_FOX = "172.16.0.";
        //const string PREFIX_IP_LAN_HSX = "172.24.58.200";
        //const string PREFIX_IP_LAN_HNX = "172.24.58.201";
        //const string PREFIX_IP_LAN_FPTS = "10.26.2.";
        //const string PREFIX_IP_LAN_FPTS_4 = "10.26.4."; // 2018-08-13 16:49:57 Hungpv
        //const string PREFIX_IP_LAN_FPTS_BLAZE = "10.26.5."; // 2018-07-06 08:07:57 ngocta2
        //const string PREFIX_IP_LAN_VM = "10.26.249.";// 2020-07-17 17:06:13 ngocta2
        /*
         * Type=1 => append message vao Info div
         * Type=2 => update status table tbMonitor
         * Type=3 => ping ip
         * Type=4 => insert new row vao table tblSingalR ( insert vao tren cung, day row cu xuong duoi ) [http://www.w3schools.com/jsref/tryit.asp?filename=tryjsref_table_insert_deleterow]
         */
        private const string TEMPLATE_JSON_TYPE_SIGNALR = "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"4\",\"Data\":\"(ServerDateTime)|(ServerIp)|(ClientIp)|(UserAgent)|(Transport)|(ConnectionId)\"}]";
        private const string TEMPLATE_JSON_TYPE_MESSAGE = "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"1\",\"Data\":\"(AppDateTime) | (AppIp) | (AppName) | (AppMsg)\"}]";
        private const string TEMPLATE_JSON_TYPE_STATUS_FEEDER = "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"2\",\"Data\":[{\"RowID\":\"(RowID)\",\"Info\":[[2,\"(StartedTime)\"],[6,\"(ActiveTime)\"],[7,\"(RowCount)\"],[8,\"(DurationFeeder)\"]]}]}]";
        private const string TEMPLATE_JSON_TYPE_STATUS_IIS = "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"2\",\"Data\":[{\"RowID\":\"(RowID)\",\"Info\":[[10,\"(DurationIIS)\"]]}]}]";
        private const string TEMPLATE_JSON_TYPE_STATUS_CHECK_SUB = "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"5\",\"Data\":\"(AppDateTime) | (AppIp) | (AppName) | (AppMsg)\"}]";
        private const string TEMPLATE_JSON_TYPE_STATUS_READER = "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"7\",\"Data\":[{\"RowID\":\"(RowID)\",\"Info\":[[1,\"(UpdateCount)\"],[2,\"(RowCount)\"],[3,\"(DurationReader)\"]]}]}]";

        static object m_objLocker = new object();
        // lock multi-thread cung truy xuat 1 var

        private Dictionary<string, string> m_dicStartedTime = new Dictionary<string, string>();

        //[{"Time":"2015-12-23 08:24:38.333","Type":"2","Data":[{"RowID":"HOF5.0Q","Info":[[6,"2015-12-23 08:24:38.333"],[7,"55,741"],[8,"155"],[10,"199"]]},{"RowID":"HOF5","Info":[[6,"2015-12-23 08:24:38.333"],[7,"55,741"],[8,"113"],[10,"233"]]}]}]
        //[{"Time":"2015-12-23 08:24:38.333","Type":"2","Data":[{"RowID":"HOF5.0PT","Info":[[2,"2015-12-23 08:24:38.111"],[6,"2015-12-23 08:24:38.333"],[7,"55,741"],[8,"155"],[10,"199"]]}]}]
        //[{"Time":"2015-12-23 08:24:38.333","Type":"1","Data":"2015-12-29 10:07:24 | 172.16.0.51 | StockQuote_HOSE | reload Last Index DONE …"}]
        //private string[] ARRAY_MONITOR_APP = { "HOS5.0", "HOF5.0Q", "HOF5.0I", "HOF5.0PT", "IGS5.0", "IGF5.0", "IGF4.5", "IIS_Root", "IIS_VD_HSX", "IIS_VD_HNX", "IIS_VD_CHART" };

        // lien quan den Monitor\js\Monitor\config.js

        private string[] ARRAY_MONITOR_TYPE = { "1", "2" };

        // ARRAY_MONITOR_APP dung index cua MONITOR_APP
        /*private*/
        public string[] ARRAY_MONITOR_APP = {
                                                 "HSXS5.0",             //0
                                                 "HSXF5.0Q",            //1
                                                 "HSXF5.0I",            //2
                                                 "HSXF5.0PT",           //3
                                                 "HNXS5.0",             //4
                                                 "HNXF5.0",             //5
                                                 "HNXF4.5",             //6
                                                 "IIS_Root",            //7
                                                 "IIS_VD_HSX",          //8
                                                 "IIS_VD_HNX",          //9
                                                 "IIS_VD_CHART",        //10
                                                 "WEB_MONITOR",         //11
                                                 "HSXF5.0",             //12
                                                 "HNXF5.0Pro",          //13
                                                 "HSXF5.0Pro",          //14
                                                 "IIS_VD_Pro_HNX",      //15
                                                 "IIS_VD_Pro_HSX",      //16
                                                 "HNXS5.0DB",           //17
                                                 "QRHO5.0",	            //18
                                                 "QRHA5.0",	            //19
                                                 "QRUP5.0",	            //20
                                                 "HNX_LOGON",	        //21
												 "Fu2_CO_GetPrice",	    //22
                                                 "HSXF5.5Q",	        //23
                                                 "HSXF5.5I",	        //24
                                                 "HSXF5.5",             //25
                                                
                                             };

        public enum MONITOR_APP
        {
            HSX_Saver5G = 0,
            HSX_Feeder5G_Q = 1,
            HSX_Feeder5G_I = 2,
            HSX_Feeder5G_PT = 3,
            HNX_Saver5G = 4,
            HNX_Feeder5G = 5,
            HNX_Feeder45G = 6,
            IIS_Root = 7,
            IIS_VD_HSX = 8,
            IIS_VD_HNX = 9,
            IIS_VD_CHART = 10,
            Web_Monitor = 11,
            HSX_Feeder5G = 12,
            HNXPro_Feeder5G = 13,
            HSXPro_Feeder5G = 14,
            IIS_VD_HNX_Pro = 15,
            IIS_VD_HSX_Pro = 16,
            HNX_Saver5G_DB = 17,
            QuoteReader_HO5G = 18,
            QuoteReader_HA5G = 19,
            QuoteReader_UP5G = 20,
            HNX_Logon_Saver5G = 21,
            Fu2_CO_GetPrice = 22,
            HSX_FeederEx_Q = 23,
            HSX_FeederEx_I = 24,
            HSX_Feeder55G = 25,


        }
        public enum MONITOR_TYPE
        {
            Message = 0,
            Status = 1
        }
        private string m_strStartedTime = ""; // time active dau tien
        private string m_strActiveTime = "";  // time active gan day nhat (pub msg)

        private CRedis m_RC;//           = new CRedisClient(CConfig.REDIS_HOST, CConfig.REDIS_PORT);    // ("10.26.2.250", 6379);
        private static AppSetting m_Setting;


        private string m_strChannelMonitor = "";

        public CRedis Monitor5G_CRedisClientInstance
        {
            set { this.m_RC = value; }
            get { return this.m_RC; }
        }


        // constructor
        public CMonitor5G()
        {

        }
        public CMonitor5G(AppSetting appSetting)
        {
            m_RC = new CRedis(appSetting.RedisSetting);
            m_Setting = appSetting;
        }

        public CMonitor5G(CRedis RC)
        {
            this.m_RC = RC;
        }

        // bat buoc phai gan gia tri m_strChannelMonitor khi su dung 
        public string ChannelMonitor
        {
            get { return this.m_strChannelMonitor; }
            set { this.m_strChannelMonitor = value; }
        }

        /// <summary>
        /// send text message vao web Monitor
        /// thich hop de send thong tin code list
        /// [{"Time":"2015-12-23 08:24:38.333","Type":"1","Data":"2015-12-29 10:07:24 | 172.16.0.51 | StockQuote_HOSE | reload Last Index DONE …"}]
        /// 
        /// private const string TEMPLATE_JSON_TYPE_MESSAGE="[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"1\",\"Data\":\"(AppDateTime) | (AppIp) | (AppName) | (AppMsg)\"}]";
        /// </summary>
        /// <param name="strMessage">hello World</param>
        /// <returns></returns>
        public bool Monitor5G_SendMessage(string strDateTime, string strIP, MONITOR_APP MA, string strMessage)
        {
            try
            {
                // chua co ten kenh
                if (this.m_strChannelMonitor == "") return false;

                string strJsonB = TEMPLATE_JSON_TYPE_MESSAGE;// [{"Time":"2015-12-23 08:24:38.333","Type":"1","Data":"2015-12-29 10:07:24 | 172.16.0.51 | StockQuote_HOSE | reload Last Index DONE …"}]
                string strAppName = ARRAY_MONITOR_APP[(int)MA];

                // tao json
                strJsonB = strJsonB.Replace("(DateTimeMonitor)", DateTime.Now.ToString(FORMAT_DATETIME));
                strJsonB = strJsonB.Replace("(AppDateTime)", strDateTime);
                strJsonB = strJsonB.Replace("(AppIp)", GetLANIP(strIP));
                strJsonB = strJsonB.Replace("(AppName)", strAppName);
                strJsonB = strJsonB.Replace("(AppMsg)", strMessage);
                strJsonB = strJsonB.Replace("\\", "\\\\");

                lock (m_objLocker) // lock multi-thread => Message requested was not found in the queue specified
                {
                    // pub vao channel monitor
                    if (this.m_RC.RC != null)
                    {
                        // pub vao channel monitor
                        this.m_RC.RC.Publish(this.m_strChannelMonitor, strJsonB);
                    }
                    //if (this.m_RC.RCFox != null)
                    //{
                    //    this.m_RC.RCFox.PublishMessage(this.m_strChannelMonitor, strJsonB);
                    //}


                }
                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }

        /// <summary>
        /// 2016-01-04 16:01:36 ngocta2
        /// private const string TEMPLATE_JSON_TYPE_STATUS_FEEDER = "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"2\",\"Data\":[{\"RowID\":\"(RowID)\",\"Info\":[[2,\"(StartedTime)\"],[6,\"(ActiveTime)\"],[7,\"(RowCount)\"],[8,\"(DurationFeeder)\"]]}]}]";
        /// </summary>
        /// <param name="strDateTime"></param>
        /// <param name="strIP"></param>
        /// <param name="MA"></param>
        /// <param name="strActiveTime"></param>
        /// <param name="intRowCount"></param>
        /// <param name="lngDuration"></param>
        /// <returns></returns>
        public bool Monitor5G_SendStatusFeeder(string strActiveTime, string strIP, MONITOR_APP MA, int intRowCount, long lngDuration)
        {
            try
            {
                string strJsonB = TEMPLATE_JSON_TYPE_STATUS_FEEDER;// "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"2\",\"Data\":[{\"RowID\":\"(RowID)\",\"Info\":[[2,\"(StartedTime)\"],[6,\"(ActiveTime)\"],[7,\"(RowCount)\"],[8,\"(DurationFeeder)\"]]}]}]";
                string strAppName = ARRAY_MONITOR_APP[(int)MA];
                string strRowID = strAppName;
                string strKey = strRowID;
                string strOut = "";

                // xu ly dic
                if (!this.m_dicStartedTime.TryGetValue(strKey, out strOut))
                    this.m_dicStartedTime.Add(strKey, strActiveTime);// add new key

                // tao json
                strJsonB = strJsonB.Replace("(DateTimeMonitor)", DateTime.Now.ToString(FORMAT_DATETIME));
                strJsonB = strJsonB.Replace("(RowID)", strRowID);
                strJsonB = strJsonB.Replace("(StartedTime)", this.m_dicStartedTime[strKey]);
                strJsonB = strJsonB.Replace("(ActiveTime)", strActiveTime);
                strJsonB = strJsonB.Replace("(RowCount)", intRowCount.ToString());
                strJsonB = strJsonB.Replace("(DurationFeeder)", lngDuration.ToString());

                lock (m_objLocker) // lock multi-thread => Message requested was not found in the queue specified
                {
                    // pub vao channel monitor
                    if (this.m_RC.RC != null)
                    {
                        this.m_RC.RC.Publish(this.m_strChannelMonitor, strJsonB);
                    }
                    //if (this.m_RC.RCFox != null)
                    //{
                    //    this.m_RC.RCFox.PublishMessage(this.m_strChannelMonitor, strJsonB);
                    //}

                }

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }

        /// <summary>
        /// 2017-12-04 16:26:57 ngocta2
        /// push data xuong js client, update GUI
        /// </summary>
        /// <param name="strActiveTime"></param>
        /// <param name="strIP"></param>
        /// <param name="MA"></param>
        /// <param name="strTrId"></param>
        /// <param name="intUpdateCount"></param>
        /// <param name="intRowCount"></param>
        /// <param name="lngDuration"></param>
        /// <returns></returns>
        public bool Monitor5G_SendStatusReader(string strActiveTime, string strIP, MONITOR_APP MA, string strTrId, int intUpdateCount, int intRowCount, long lngDuration)
        {
            try
            {
                string strJsonB = TEMPLATE_JSON_TYPE_STATUS_READER;// "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"7\",\"Data\":[{\"RowID\":\"(RowID)\",\"Info\":[[1,\"(UpdateCount)\"],[2,\"(RowCount)\"],[3,\"(DurationReader)\"]]}]}]";
                string strAppName = ARRAY_MONITOR_APP[(int)MA];
                string strRowID = strAppName;
                string strKey = strRowID;
                string strOut = "";

                // xu ly dic
                if (!this.m_dicStartedTime.TryGetValue(strKey, out strOut))
                    this.m_dicStartedTime.Add(strKey, strActiveTime);// add new key

                // tao json
                strJsonB = strJsonB.Replace("(DateTimeMonitor)", DateTime.Now.ToString(FORMAT_DATETIME));
                strJsonB = strJsonB.Replace("(RowID)", strTrId);
                strJsonB = strJsonB.Replace("(UpdateCount)", intUpdateCount.ToString());
                strJsonB = strJsonB.Replace("(RowCount)", intRowCount.ToString());
                strJsonB = strJsonB.Replace("(DurationReader)", lngDuration.ToString());

                lock (m_objLocker) // lock multi-thread => Message requested was not found in the queue specified
                {
                    // pub vao channel monitor
                    if (this.m_RC.RC != null)
                    {
                        this.m_RC.RC.Publish(this.m_strChannelMonitor, strJsonB);
                    }
                    //if (this.m_RC.RCFox != null)
                    //{
                    //    this.m_RC.RCFox.PublishMessage(this.m_strChannelMonitor, strJsonB);
                    //}

                }

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }

        /// <summary>
        /// 2016-01-04 16:03:57 ngocta2
        /// tinh trang xu ly data tai IIS, chi co moi duration tai IIS
        /// private const string TEMPLATE_JSON_TYPE_STATUS_IIS = "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"2\",\"Data\":[{\"RowID\":\"(RowID)\",\"Info\":[[10,\"(DurationIIS)\"]]}]}]";
        /// </summary>
        /// <param name="MA"></param>
        /// <param name="lngDuration"></param>
        /// <returns></returns>
        public bool Monitor5G_SendStatusIIS(MONITOR_APP MA, long lngDuration)
        {
            try
            {
                string strJsonB = TEMPLATE_JSON_TYPE_STATUS_IIS;// "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"2\",\"Data\":[{\"RowID\":\"(RowID)\",\"Info\":[[10,\"(DurationIIS)\"]]}]}]";
                string strAppName = ARRAY_MONITOR_APP[(int)MA];
                string strRowID = strAppName;

                // tao json
                strJsonB = strJsonB.Replace("(DateTimeMonitor)", DateTime.Now.ToString(FORMAT_DATETIME));
                strJsonB = strJsonB.Replace("(RowID)", strRowID);
                strJsonB = strJsonB.Replace("(DurationIIS)", lngDuration.ToString());

                lock (m_objLocker) // lock multi-thread => Message requested was not found in the queue specified
                {
                    // pub vao channel monitor
                    if (this.m_RC.RC != null)
                    {
                        this.m_RC.RC.Publish(this.m_strChannelMonitor, strJsonB);
                    }
                    //if (this.m_RC.RCFox != null)
                    //{
                    //    this.m_RC.RCFox.PublishMessage(this.m_strChannelMonitor, strJsonB);
                    //}
                }
                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }

        /// <summary>
        /// 2016-04-21 10:46:19 ngocta2
        /// gui thong tin tinh trang ket noi SignalR (WebServer => Client) ve web Monitor
        /// </summary>
        /// <param name="strServerTime">2016-04-21 10:39:16.117</param>
        /// <param name="strServerIP">::1</param>
        /// <param name="strClientIP">::1</param>
        /// <param name="strUserAgent">Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.112 Safari/537.36</param>
        /// <param name="strTransport">serverSentEvents</param>
        /// <returns></returns>
        public bool Monitor5G_SendStatusSignalR(string strServerTime, string strServerIP, string strClientIP, string strUserAgent, string strTransport, string strConnectionId)
        {
            try
            {
                StringBuilder sbJsonB = new StringBuilder(TEMPLATE_JSON_TYPE_SIGNALR); //= "[{\"Time\":\"(DateTimeMonitor)\",\"Type\":\"3\",\"Data\":\"(ServerDateTime)|(ServerIp)|(ClientIp)|(UserAgent)|(Transport)\"}]";

                // tao json                
                sbJsonB.Replace("(DateTimeMonitor)", DateTime.Now.ToString(FORMAT_DATETIME));
                sbJsonB.Replace("(ServerDateTime)", strServerTime);
                sbJsonB.Replace("(ServerIp)", strServerIP);
                sbJsonB.Replace("(ClientIp)", strClientIP);
                sbJsonB.Replace("(UserAgent)", strUserAgent);
                sbJsonB.Replace("(Transport)", strTransport);
                sbJsonB.Replace("(ConnectionId)", strConnectionId);

                lock (m_objLocker) // lock multi-thread => Message requested was not found in the queue specified
                {
                    // pub vao channel monitor
                    if (this.m_RC.RC != null)
                    {
                        this.m_RC.RC.Publish(this.m_strChannelMonitor, sbJsonB.ToString());
                    }
                    //if (this.m_RC.RCFox != null)
                    //{
                    //    this.m_RC.RCFox.PublishMessage(this.m_strChannelMonitor, sbJsonB.ToString());
                    //}
                }

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }

        /// <summary>
        /// 2016-06-22 16:37:35 ngocta2
        /// kiem trang tinh trang subcribe channel
        /// </summary>
        /// <param name="strDateTime"></param>
        /// <param name="strIP"></param>
        /// <param name="MA"></param>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        public bool Monitor5G_SendStatusCheckSub(string strDateTime, string strIP, MONITOR_APP MA, string strMessage)
        {
            try
            {
                string strJsonB = TEMPLATE_JSON_TYPE_STATUS_CHECK_SUB;// [{"Time":"(DateTimeMonitor)","Type":"5","Data":"(AppDateTime) | (AppIp) | (AppName) | (AppMsg)"}]
                string strAppName = ARRAY_MONITOR_APP[(int)MA];

                // tao json
                strJsonB = strJsonB.Replace("(DateTimeMonitor)", DateTime.Now.ToString(FORMAT_DATETIME));
                strJsonB = strJsonB.Replace("(AppDateTime)", strDateTime);
                strJsonB = strJsonB.Replace("(AppIp)", GetLANIP(strIP));
                strJsonB = strJsonB.Replace("(AppName)", strAppName);
                strJsonB = strJsonB.Replace("(AppMsg)", strMessage);
                strJsonB = strJsonB.Replace("\\", "\\\\");

                lock (m_objLocker) // lock multi-thread => Message requested was not found in the queue specified
                {
                    // pub vao channel monitor
                    if (this.m_RC.RC != null)
                    {
                        this.m_RC.RC.Publish(this.m_strChannelMonitor, strJsonB);
                    }
                    //if (this.m_RC.RCFox != null)
                    //{
                    //    this.m_RC.RCFox.PublishMessage(this.m_strChannelMonitor, strJsonB);
                    //}
                }
                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }

        /// <summary>
        /// lay datetime hien tai theo format
        /// </summary>
        /// <returns></returns>
        public static string GetLocalDateTime()
        {
            try
            {
                string strDateTime = DateTime.Now.ToString(FORMAT_DATETIME);

                return strDateTime;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }

        /// <summary>
        /// To get the local IP address 
        ///  lay ip hien tai
        ///  "192.168.2.18,172.16.0.18" => chi lay 172.16.0.18
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIP()
        {
            try
            {
                string sHostName = Dns.GetHostName();
                System.Net.IPHostEntry ipE = Dns.GetHostByName(sHostName);
                IPAddress[] IpA = ipE.AddressList;
                foreach (IPAddress ip in IpA)
                {
                    string strLocalIP = ip.ToString();
                    if (strLocalIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_FOX) != -1       //= "172.16.0.";
                    || strLocalIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_HSX) != -1       //= "10.26.248.";
                    || strLocalIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_VM) != -1       //= "10.26.249.";
                    || strLocalIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_HNX) != -1       //= "10.26.100.";
                    || strLocalIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_FPTS) != -1       //= "10.26.2.";
                    || strLocalIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_FPTS_BLAZE) != -1   //= "10.26.5.";
                    || strLocalIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_FPTS_4) != -1   //= "10.26.4."; 2018-08-15 09:01:56 hungpv
                    )
                        return strLocalIP;
                }
                return "";
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }

        /// <summary>
        /// 9:45 AM Tuesday, March 29, 2016
        /// cac server co 2 IP thi chi lay IP LAN thuoc noi bo FPTS
        /// "10.26.2.212"                   => "10.26.2.212"
        /// "172.16.0.51,192.168.2.51"      => "172.16.0.51"
        /// "10.26.248.66,172.17.58.4"      => "10.26.248.66"
        /// "10.26.100.18,xxx.xxx.xxx.xxx"  => "10.26.100.18"
        /// </summary>
        /// <param name="strIP"></param>
        /// <returns></returns>
        public static string GetLANIP(string strIP)
        {
            try
            {

                string[] arr = strIP.Split(',');
                if (arr.Length > 1)
                {
                    foreach (string strLanIP in arr)
                    {
                        if (strLanIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_FOX) != -1 || strLanIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_HSX) != -1 || strLanIP.IndexOf(m_Setting.Setting_IP_LAN.PREFIX_IP_LAN_HNX) != -1)
                        {
                            strIP = strLanIP;
                            break;
                        }
                    }
                }
                return strIP;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }
        // -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    }
}

using Newtonsoft.Json;
using SaverHNX_NetCore2.DAL;
using SaverHNX_NetCore2.Extensions;
using SaverHNX_NetCore2.Settings;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;

namespace SaverHNX_NetCore2.BLL
{
    public class CSaverHNXLib
    {
        public const int __CODE_EXIT_APP_FROM_CONSOLE_PRESS_Y = 1;
        public const int __CODE_EXIT_APP_FROM_AUTO_EXIT = 2;
        public const int __CODE_EXIT_APP_FROM_FAILED_INIT_QUOTE = 3;

        //monitor5g
        private CMonitor5G m_M5G;

        private Lazy<ConnectionMultiplexer> lazyConnection;
        private readonly AppSetting _appSetting;
        private readonly CBroker _cBroker;
        private readonly CRedis _cRedis;
        private CDatabase _cDatabase;
        private COracle _cOracle;
        private CTracker _cTracker;

        private ConcurrentQueue<string> m_queueRedis = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> m_queueSQL = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> m_queueOracle = new ConcurrentQueue<string>();

        private System.Timers.Timer m_tmrGroupREDIS = new System.Timers.Timer();
        private System.Timers.Timer m_tmrGroupSQL = new System.Timers.Timer();
        private System.Timers.Timer m_tmrGroupORACLE = new System.Timers.Timer();
        //Timer Tracker - Đẩy count msg vào InfluxDb
        private System.Timers.Timer m_tmrInfluxDB = new System.Timers.Timer();
        private int _msgCount_RD = 0; // Track message count
        private int _msgCount_SQL = 0;
        private int _msgCount_ORCL = 0;

        private bool m_blnTruncateDb = false;

        private readonly SemaphoreSlim semaphoreREDIS = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim semaphoreSQL = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim semaphoreORACLE = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim semaphoreBroker = new SemaphoreSlim(1, 1);

        public CHandCode _cHandCode;

        public CSaverHNXLib(AppSetting _setting, CBroker _broker, CRedis _redis)
        {
            _appSetting = _setting;
            _cBroker = _broker; 
            _cRedis = _redis;            
            _cDatabase = new CDatabase(_setting.SaverSetting);
            _cOracle = new COracle(_setting.SaverSetting);
            _cTracker = new CTracker(_setting.Tracker_Setting);
            m_M5G = new CMonitor5G(_setting);
            this._cHandCode = new CHandCode(_setting, _cRedis, _cDatabase, _cTracker);

            this.Init();

            this.InitPubSubData();

            TruncateDb();

            //Nếu time < 9h cho xóa key
            DateTime currentTime = DateTime.Now;
            if (currentTime.Hour < 20)
            {
                //xóa key LE/LS
                this.ClearLE2LSKeys();
            }

            //setup broker get msg từ đây và xử lý
            if (this._cBroker != null)
            {
                this._cBroker.OnMessage = (receviedMsg) => { this.ReceiveMessageFromMessageQueue(receviedMsg); };
                this._cBroker.SetupOnReceivedEventHandler(); // SetupOnReceivedEventHandler chi goi 1 lan, da dung o day thi ko setup o tren
            }

            this.StartTimers();
        }
        /// <summary>
        /// start timer
        /// </summary>
        /// <returns></returns>
        private bool StartTimers()
        {
            try
            {
                this.m_tmrGroupREDIS.Enabled = true;
                this.m_tmrGroupSQL.Enabled = true;
                this.m_tmrGroupORACLE.Enabled = true;

                m_tmrInfluxDB.Interval = 5000; // 1 second interval
                m_tmrInfluxDB.Elapsed += OnInfluxDBTimerElapsed;
                //m_tmrInfluxDB.Start();
                Console.WriteLine("Timers started");
                //send Monitor
                StringBuilder sbMsg = new StringBuilder("StartTimers: ");
                sbMsg.Append($"QueueName:{this._appSetting.BrokerSetting.QueueName} - ExchangeQueue:{this._appSetting.BrokerSetting.ExchangeName} - RoutingKey:{this._appSetting.BrokerSetting.RoutingKey}");
                sbMsg.Append("; this.m_tmrGroupREDIS.Interval=" + this.m_tmrGroupREDIS.Interval.ToString());
                sbMsg.Append("; this.m_tmrGroupDataDB.Interval=" + this.m_tmrGroupSQL.Interval.ToString());
                this.m_M5G.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G, sbMsg.ToString());
                return true;
            }
            catch (Exception ex) 
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        /// <summary>
        /// stop timer
        /// </summary>
        /// <returns></returns>
        public bool StopTimers()
        {
            try
            {
                this.m_tmrGroupREDIS.Enabled = false;
                this.m_tmrGroupSQL.Enabled = false;
                this.m_tmrGroupORACLE.Enabled = false;
                // 2016-01-04 16:27:16 ngocta2      send CMonitor5G
                StringBuilder sbMsg = new StringBuilder("StopTimers");
                this.m_M5G.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G, sbMsg.ToString());

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        /// <summary>
        /// init Timer
        /// </summary>
        private void Init() 
        {
            try
            {
                //debug
                CLog.LogEx("flow.js", "InitConfig()");
                CConfig.ReadIniFile(_appSetting.SaverSetting);

                this.m_tmrGroupREDIS.Interval = _appSetting.SaverSetting.TIMER_PROCESS_DATA_REDIS;
                this.m_tmrGroupREDIS.Elapsed += TimerProc_GroupREDIS_Wrapper;

                this.m_tmrGroupSQL.Interval = _appSetting.SaverSetting.TIMER_PROCESS_DATA_DATABASE;
                this.m_tmrGroupSQL.Elapsed += TimerProc_GroupSQL_Wrapper;

                this.m_tmrGroupORACLE.Interval = _appSetting.SaverSetting.TIMER_PROCESS_DATA_DATABASE;
                this.m_tmrGroupORACLE.Elapsed += TimerProc_GroupORACLE_Wrapper;

                //monitor
                m_M5G.ChannelMonitor = CConfig.CHANNEL_MONITOR;
                this.m_M5G.Monitor5G_SendStatusFeeder(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G, 0, 0);

            }
            catch (Exception ex) 
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
        private bool InitPubSubData()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    try
                    {
                        var redisDb = _cRedis.RC;
                        var subscriber = _cRedis.RC.Multiplexer.GetSubscriber();

                        subscriber.Subscribe(CConfig.CHANNEL_S5G_COMMAND_HNX_SAVER, (channel, msg) =>
                        {
                            // Send monitor5G
                            this.m_M5G.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HSX_Saver5G, "InitPubSubData.OnSubscribe=>RedisChannel=" + channel);
                            try
                            {
                                switch (msg)
                                {
                                    case CConfig.COMMAND_ACTION_DEBUG:
                                        string strPath = "";
                                        this.m_M5G.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G, "InitPubSubData.DONE => COMMAND_ACTION_DEBUG => BEGIN...");
                                        strPath = CDebug.AllFields2LogFile(CDebug.GIN(() => this.m_M5G), this.m_M5G);
                                        this.m_M5G.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G, "InitPubSubData.DONE => COMMAND_ACTION_DEBUG => " + strPath);
                                        this.m_M5G.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G, "InitPubSubData.DONE => COMMAND_ACTION_DEBUG => DONE!!!");
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                            }
                        });

                        // OnSubscribe event handler (inside subscription logic)
                        subscriber.Unsubscribe(CConfig.CHANNEL_S5G_COMMAND_HNX_SAVER, (channel, count) =>
                        {
                            try
                            {
                                // OnUnsubscribe logic
                                this.m_M5G.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HSX_Saver5G, $"InitPubSubData.OnUnsubscribe=>RedisChannel={channel}");
                            }
                            catch (Exception ex)
                            {
                                // Error handling for OnUnsubscribe logic
                                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        /// <summary>
        /// xoa tat ca du lieu khi bat dau Start Timer lan dau tien
        /// </summary>
        /// <returns></returns>
        private bool TruncateDb()
        {
            CLog.LogEx("flow.js", "TruncateDb()");
            try
            {
                //-------------------------------------
                // 2021-11-15 11:13:22 ngocta2 khong cho truncate db khi qua gio trong config
                CLog.LogEx("flow.js", $"CConfig.TIME_STOP_TRUNCATE_DB={_appSetting.SaverSetting.TIME_STOP_TRUNCATE_DB}");
                string[] arr = _appSetting.SaverSetting.TIME_STOP_TRUNCATE_DB.Split(':');
                int hour = Convert.ToInt32(arr[0]), minute = Convert.ToInt32(arr[1]);
                int h = DateTime.Now.Hour, m = DateTime.Now.Minute;
                // qua moc time nay roi thi exit func >> ko cho truncate
                if (h > hour || (h == hour && m >= minute))
                    return true;
                //-------------------------------------

                if (!this.m_blnTruncateDb)
                {
                    // truncate tables
                    _cDatabase.ExecuteScriptQuoteSaverHNX(_appSetting.SaverSetting.prc_S5G_HNX_SAVER_IG__CLEAR);

                    // send monitor5G
                    System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
                    builder.ConnectionString = _appSetting.SaverSetting.CONNECTION_STRING_68;
                    string strIP = builder.DataSource;
                    string strDB = builder.InitialCatalog;
                    StringBuilder sbMsg = new StringBuilder("TruncateDb: ").Append(strIP).Append(".").Append(strDB);
                    //this.m_crbMQ.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CReaderBaseMQ.MONITOR_APP.HNX_Saver5G, sbMsg.ToString());

                    // update flag
                    this.m_blnTruncateDb = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError("OnInfluxDBTimerElapsed", CBase.GetDetailError(ex));
                return false;
            }
        }
        private async void OnInfluxDBTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                // Send the message count to InfluxDB asynchronously
                 await this._cTracker.MsgRD2InfluxDBAsync(_msgCount_RD);
            }
            catch (Exception ex)
            {
                CLog.LogError("OnInfluxDBTimerElapsed", CBase.GetDetailError(ex));
            }

            // Reset the message count for the next interval
            _msgCount_RD = 0;
        }
        private void TimerProc_GroupREDIS_Wrapper(object sender, ElapsedEventArgs e)
        {
            try
            {             
                _ = TimerProc_GroupREDIS();
            }
            catch (Exception ex)
            {
                CLog.LogError("TimerProc_GroupREDIS_Wrapper", CBase.GetDetailError(ex));
            }           
        }
        private void TimerProc_GroupSQL_Wrapper(object sender, ElapsedEventArgs e)
        {
            try
            {
                //TimerProc_GroupSQL().GetAwaiter().GetResult();
                _ = TimerProc_GroupSQL();
            }
            catch (Exception ex)
            {
                CLog.LogError("TimerProc_GroupSQL_Wrapper", CBase.GetDetailError(ex));
            }
        }
        private void TimerProc_GroupORACLE_Wrapper(object sender, ElapsedEventArgs e)
        {
            try
            {
                //TimerProc_GroupORACLE().GetAwaiter().GetResult();
                _ = TimerProc_GroupORACLE();
            }
            catch (Exception ex)
            {
                CLog.LogError("TimerProc_GroupORACLE_Wrapper", CBase.GetDetailError(ex));
            }
        }
        public async Task ReceiveMessageFromMessageQueue(string messageBlock)
        {
            await semaphoreBroker.WaitAsync();
            try
            {
                ProcessAndEnqueueMessage(messageBlock);
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));  
            }
            finally
            {
                semaphoreBroker.Release();
            }
        }
        /// <summary>
        /// Hàm xử lý single message FIX
        /// </summary>
        /// <param name="strMessage"></param>
        private void ProcessAndEnqueueMessage(string strMessage)
        {
            try
            {
                var SW1 = Stopwatch.StartNew(); 
                var SW2 = Stopwatch.StartNew();
                if(string.IsNullOrEmpty(strMessage)) return;

                //Log nhận msg
                CLog.LogEx("ReceviedMsg.txt", $"ProcessDataSingle:1->Received message: {strMessage}");
                
                string strCSV = "";
                string strORCL = "";
                string strLogonInfor = "";
                // Hàm xử lý msg FIX Msg -> msg CSV
                strCSV = this._cHandCode.Message2CSV(strMessage, ref strORCL, ref strLogonInfor);

                //Nếu có thông tin LogonInfor, chỉ đẩy vào queue Redis và không đẩy vào các queue khác
                if (!string.IsNullOrEmpty(strLogonInfor))
                {
                    EnqueueMsg(this.m_queueRedis, strLogonInfor);
                    return; // Không xử lý các queue khác nữa
                }

                // Đẩy CSV vào Redis và SQL 
                if (!string.IsNullOrEmpty(strCSV))
                {
                    EnqueueMsg(this.m_queueRedis, strCSV);
                    EnqueueMsg(this.m_queueSQL, strCSV);
                    //Log Enqueue
                    CLog.LogEx("Enqueue_RD_SQL.txt", $"MsgEnqueue: {strCSV} - Time 1 msg:{SW1.ElapsedMilliseconds}");
                }

                if (!string.IsNullOrEmpty(strORCL))
                {
                    EnqueueMsg(this.m_queueOracle, strORCL);
                    //Log Enqueue
                    CLog.LogEx("Enqueue_Oracle.txt", $"MsgEnqueue: {strORCL} - Time 1 msg:{SW2.ElapsedMilliseconds}");
                }
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
        private bool EnqueueMsg(ConcurrentQueue<string> queue, string message)
        {
            try
            {
                if (queue != null && !string.IsNullOrEmpty(message))
                {
                    queue.Enqueue(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        private void ClearLE2LSKeys()
        {
            try
            {
                if (_cRedis.RC != null)
                {
                    _cRedis.DeleteKeysByPattern(_appSetting.RedisSetting.KEY_DELETE_REDIS_TKTT);
                    _cRedis.DeleteKeysByPattern(_appSetting.RedisSetting.KEY_DELETE_REDIS_LS);
                }
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }

        private async Task TimerProc_GroupREDIS()
        {
            await semaphoreREDIS.WaitAsync();
            try
            {
                string strCSV = "";
                var stopWatch = Stopwatch.StartNew(); //Stopwatch validate Timer
                int intTotalRow = 0;
                var SW_RD = Stopwatch.StartNew(); //Stopwatch đo tg xử lý 1 Timer
                while (stopWatch.ElapsedMilliseconds < 500 && this.m_queueRedis.TryDequeue(out strCSV))
                {
                    var CW = Stopwatch.StartNew();
                    string strLogonInfor = "";
                    if (!string.IsNullOrEmpty(strCSV))
                    {
                        strLogonInfor = await this._cHandCode.ProcessDataRedis(strCSV);
                        if (strLogonInfor != "")
                        {
                            //Pub Monitor
                            this.m_M5G.Monitor5G_SendMessage(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Logon_Saver5G, strLogonInfor);
                        }
                        intTotalRow++;
                        _msgCount_RD++;
                        CLog.LogEx("Dequeue_msg_REDIS.txt", $"Dequeue_REDIS: {strCSV} - Time: {CW.ElapsedMilliseconds} - Count: {_msgCount_RD}"); 
                    }                    
                }
                //send monitor
                this.m_M5G.Monitor5G_SendStatusFeeder(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G, intTotalRow, SW_RD.ElapsedMilliseconds);
            }
            catch (Exception ex) 
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
            finally
            {
                CLog.LogEx("Dequeue_msg_REDIS.txt", $"RELEASE: ------------------------------------------- Count: {_msgCount_RD}");
                semaphoreREDIS.Release();
            }
        }
        /// <summary>
        ///Tạo StringBuilder cho strTransaction:
            /*
                BEGIN TRANSACTION
                EXEC prc_S5G_HNX_SAVER_IG_SI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='258',@MsgType='SI',@SenderCompID='HNX',@SendingTime='20241007-09:00:02',@IDSymbol='12892',@Symbol='KST',@SecurityType='ST',@IssueDate='00010101-12:01:00',@CeilingPrice='13000',@FloorPrice='10800',@SecurityTradingStatus='0',@BasicPrice='11900',@BoardCode='LIS_BRD_01',@RemainForeignQtty='2913089',@Parvalue='10000',@PriorClosePrice='11900',@Tradingdate='20241007',@Time='09:00:00',@TradingUnit='100',@TotalListingQtty='5992020.000000',@DateNo='3441',@ReferenceStatus='0',@TradingSessionID='LIS_CON_NML',@TradSesStatus='1',@ListingStatus='0'
                EXEC prc_S5G_HNX_SAVER_IG_SI_UPDATE @BeginString = 'HNX.TDS.1',@BodyLength = '263',@MsgType = 'SI',@SenderCompID = 'HNX',@SendingTime = '20241007-09:00:02',@IDSymbol = '5564',@Symbol = 'MKV',@SecurityType = 'ST',@IssueDate = '00010101-12:01:00',@CeilingPrice = '10200',@FloorPrice = '8400',@SecurityTradingStatus = '0',@BasicPrice = '9300',@BoardCode = 'LIS_BRD_01',@RemainForeignQtty = '2293424',@Parvalue = '10000',@PriorOpenPrice = '8900',@PriorClosePrice = '9300',@Tradingdate = '20241007',@Time = '09:00:00',@TradingUnit = '100',@TotalListingQtty = '5000038.000000',@DateNo = '3968',@ReferenceStatus = '0',@TradingSessionID = 'LIS_CON_NML',@TradSesStatus = '1',@ListingStatus = '0'
                COMMIT TRANSACTION  
            */
        /// </summary>
        /// <returns></returns>
        private async Task TimerProc_GroupSQL()
        {
            await semaphoreSQL.WaitAsync();
            try
            {
                StringBuilder sbAllSQL = new StringBuilder(CConfig.SQL_BEGIN_TRANSACTION + Environment.NewLine);
                int intTotalRow = 0;
                string strCSV = "";
                bool hasData = false; //Flag theo dõi có dữ liệu hay ko?
                var SW_DB = Stopwatch.StartNew();
                while (this.m_queueSQL.TryDequeue(out strCSV))
                {
                    if (!string.IsNullOrEmpty(strCSV))
                    {
                        intTotalRow++;
                        CLog.LogEx("Dequeue_msg_SQL.txt", strCSV);
                        sbAllSQL.Append(CConfig.SQL_EXEC).Append(strCSV).Append(Environment.NewLine);
                        hasData = true;
                    }
                }
                if(hasData)
                {
                    sbAllSQL.Append(CConfig.SQL_COMMIT_TRANSACTION); //Đóng transaction bằng COMMIT
                    string strTransaction = sbAllSQL.ToString();
                    this._cHandCode.ProcessDataSQL(strTransaction);
                }
                //send Monitor
                this.m_M5G.Monitor5G_SendStatusFeeder(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G_DB, intTotalRow, SW_DB.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
            finally
            {
                semaphoreSQL.Release();
            }
        }
        private async Task TimerProc_GroupORACLE()
        {
            await semaphoreORACLE.WaitAsync();
            try
            {
                int intTotalRow = 0;
                string strCSV = "";
                var CW_ORCL = Stopwatch.StartNew();
                while(this.m_queueOracle.TryDequeue(out strCSV))
                {
                    if (!string.IsNullOrEmpty(strCSV))
                    {
                        CLog.LogEx("Dequeue_msg_ORACLE.txt", strCSV);
                        _cOracle.InsertDataHNXDI(strCSV);
                    }
                    intTotalRow++;
                }         
                //send Monitor
                this.m_M5G.Monitor5G_SendStatusFeeder(CMonitor5G.GetLocalDateTime(), CMonitor5G.GetLocalIP(), CMonitor5G.MONITOR_APP.HNX_Saver5G_DB, intTotalRow, CW_ORCL.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
            finally 
            { 
                semaphoreORACLE.Release(); 
            }
        }
    }
}

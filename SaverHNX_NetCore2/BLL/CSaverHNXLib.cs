using Newtonsoft.Json;
using SaverHNX_NetCore2.DAL;
using SaverHNX_NetCore2.Extensions;
using SaverHNX_NetCore2.Settings;
using StackExchange.Redis;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

namespace SaverHNX_NetCore2.BLL
{
    public class CSaverHNXLib
    {
        public const int __CODE_EXIT_APP_FROM_CONSOLE_PRESS_Y = 1;
        public const int __CODE_EXIT_APP_FROM_AUTO_EXIT = 2;
        public const int __CODE_EXIT_APP_FROM_FAILED_INIT_QUOTE = 3;

        private readonly AppSetting _appSetting;
        private readonly CBroker _cBroker;
        private readonly CRedis _cRedis;
        private CDatabase _cDatabase;

        private Queue<string> m_queueRedis = new Queue<string>();
        private Queue<string> m_queueSQL = new Queue<string>();
        private Queue<string> m_queueOracle = new Queue<string>();

        private System.Timers.Timer m_tmrGroupREDIS = new System.Timers.Timer();
        private System.Timers.Timer m_tmrGroupSQL = new System.Timers.Timer();
        private System.Timers.Timer m_tmrGroupORACLE = new System.Timers.Timer();

        private readonly SemaphoreSlim semaphoreREDIS = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim semaphoreSQL = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim semaphoreORACLE = new SemaphoreSlim(1, 1);

        public CHandCode _cHandCode;

        public CSaverHNXLib(AppSetting _setting, CBroker _broker, CRedis _redis)
        {
            _appSetting = _setting;
            _cBroker = _broker; 
            _cRedis = _redis;            
            _cDatabase = new CDatabase(_setting.SaverSetting);
            this._cHandCode = new CHandCode(_setting, _cRedis, _cDatabase);

            this.Init();

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
        private bool StartTimers()
        {
            try
            {
                this.m_tmrGroupREDIS.Enabled = true;
                this.m_tmrGroupSQL.Enabled = true;
                this.m_tmrGroupORACLE.Enabled = true;
                Console.WriteLine("Timers started");
                return true;
            }
            catch (Exception ex) 
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
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

            }
            catch (Exception ex) 
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
        private void TimerProc_GroupREDIS_Wrapper(object sender, ElapsedEventArgs e)
        {
            TimerProc_GroupREDIS().GetAwaiter().GetResult();
        }
        private void TimerProc_GroupSQL_Wrapper(object sender, ElapsedEventArgs e)
        {
            TimerProc_GroupSQL().GetAwaiter().GetResult();
        }
        private void TimerProc_GroupORACLE_Wrapper(object sender, ElapsedEventArgs e)
        {
            TimerProc_GroupORACLE().GetAwaiter().GetResult();
        }
        public async Task ReceiveMessageFromMessageQueue(string messageBlock)
        {
            //await semaphoreBroker.WaitAsync();
            try
            {                  
                ProcessDataSingle(messageBlock);
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));  
            }
            //finally
            //{
            //    semaphoreBroker.Release();
            //}
        }
        private void ProcessDataSingle(string strMessage)
        {
            try
            {
                if(string.IsNullOrEmpty(strMessage)) return;
                //Log nhận msg
                CLog.LogEx("ReceviedMsg.txt", $"ProcessDataSingle:1->Received message: {strMessage}");
                
                string strCSV = "";
                string strSQL = "";
                string strORCL = "";
                // Hàm xử lý msg FIX Msg -> msg CSV
                strCSV = this._cHandCode.Message2CSV(strMessage, ref strORCL);

                if (!string.IsNullOrEmpty(strMessage))
                {
                    //ghi log strCSV
                    CLog.LogEx("strCSV_message.txt", JsonConvert.SerializeObject(strCSV));
                    //Đẩy cho queue Redis
                    this.m_queueRedis.Enqueue(strCSV);                   

                    this.m_queueSQL.Enqueue(strCSV);
                    //this.m_queueOracle.Enqueue(strCSV);
                }
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
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
                int intTotalRow = 0;
                while (this.m_queueRedis.Count > 0){

                    string strLogonInfor = "";
                    //Get msg dau tien trong queue
                    string strCSV = this.m_queueRedis.Dequeue();
                    CLog.LogEx("Test_msg_Redis.txt", strCSV);
                    //strCSV có thể lấy ra giá trị null
                    if(!string.IsNullOrEmpty(strCSV))
                    {
                        CLog.LogEx("Dequeue_MSG.txt", strCSV);
                        this._cHandCode.ProcessDataRedis(strCSV, ref strLogonInfor);
                    }                   
                    if(strLogonInfor != "")
                    {
                        //insertRedis + Pub Monitor
                    }
                    
                }
            }
            catch (Exception ex) 
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
            finally
            {
                semaphoreREDIS.Release();
            }
        }
        private async Task TimerProc_GroupSQL()
        {
            await semaphoreSQL.WaitAsync();
            try
            {
                StringBuilder sbAllSQL = new StringBuilder("");
                int intTotalRow = 0;
                int batchSize = 1000; // Limit 1000 msg
                int batchCounter = 0; // Theo dõi số lượng msg đc xử lý

                while (this.m_queueSQL.Count > 0)
                {
                    // Dequeue the message
                    string strCSV = this.m_queueSQL.Dequeue();

                    // Check if message is valid
                    if (!string.IsNullOrEmpty(strCSV))
                    {
                        // Add message to the StringBuilder for processing
                        sbAllSQL.Append(Environment.NewLine + CConfig.SQL_EXEC + strCSV);
                        intTotalRow++;
                        batchCounter++;

                        // Khi đạt limit batchSize -> Xử lý 
                        if (batchCounter >= batchSize)
                        {
                            // Add transaction control SQL
                            sbAllSQL.Insert(0, CConfig.SQL_BEGIN_TRANSACTION + Environment.NewLine);
                            sbAllSQL.Append(Environment.NewLine + CConfig.SQL_COMMIT_TRANSACTION);

                            // Convert StringBuilder to string for SQL execution
                            string strTransaction = sbAllSQL.ToString();
                            this._cHandCode.ProcessDataSQL(strTransaction);

                            // Reset StringBuilder and counters for the next batch
                            sbAllSQL.Clear();
                            batchCounter = 0;
                        }
                    }
                }

                // Xử lý dữ liệu còn lại nếu có
                if (batchCounter > 0)
                {
                    sbAllSQL.Insert(0, CConfig.SQL_BEGIN_TRANSACTION + Environment.NewLine);
                    sbAllSQL.Append(Environment.NewLine + CConfig.SQL_COMMIT_TRANSACTION);

                    string strTransaction = sbAllSQL.ToString();
                    this._cHandCode.ProcessDataSQL(strTransaction);
                }
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

        //private async Task TimerProc_GroupSQL()
        //{
        //    await semaphoreSQL.WaitAsync();
        //    try
        //    {
        //        StringBuilder sbAllSQL = new StringBuilder("");
        //        int intTotalRow = 0;

        //        while (this.m_queueSQL.Count > 0)
        //        {
        //            intTotalRow++;                    

        //            string strCSV = this.m_queueSQL.Dequeue();
        //            //Xử lý data chính => Insert DB
        //            if (!string.IsNullOrEmpty(strCSV))
        //            {
        //                sbAllSQL.Append(Environment.NewLine + CConfig.SQL_EXEC + strCSV);
        //                //this._cHandCode.ProcessDataSQL(strCSV);
        //            }    
        //        }
        //        if(intTotalRow > 1000)
        //        {
        //            sbAllSQL.Insert(0, CConfig.SQL_BEGIN_TRANSACTION + Environment.NewLine);
        //            sbAllSQL.Append(Environment.NewLine + CConfig.SQL_COMMIT_TRANSACTION);
        //            string strTransaction = sbAllSQL.ToString();
        //            this._cHandCode.ProcessDataSQL(strTransaction);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
        //    }
        //    finally 
        //    { 
        //        semaphoreSQL.Release(); 
        //    }
        //}
        private async Task TimerProc_GroupORACLE()
        {
            await semaphoreORACLE.WaitAsync();
            try
            {

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

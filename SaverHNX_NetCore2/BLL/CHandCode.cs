﻿using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SaverHNX_NetCore2.DAL;
using SaverHNX_NetCore2.Extensions;
using SaverHNX_NetCore2.Models;
using SaverHNX_NetCore2.Settings;
using StackExchange.Redis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SaverHNX_NetCore2.BLL
{
    public class CHandCode
    {
        private Dictionary<int, string> m_dicI = new Dictionary<int, string>();    // number => string
        private Dictionary<int, string> m_dicS = new Dictionary<int, string>();
        private Dictionary<int, string> m_dicSI = new Dictionary<int, string>();
        private Dictionary<int, string> m_dicTP = new Dictionary<int, string>();
        public Dictionary<int, string> m_dicBI = new Dictionary<int, string>();
        private Dictionary<int, string> m_dicEP = new Dictionary<int, string>();
        private Dictionary<int, string> m_dicPO = new Dictionary<int, string>();
        private Dictionary<int, string> m_dicIV = new Dictionary<int, string>();
        private Dictionary<int, string> m_dicTE = new Dictionary<int, string>();
        private Dictionary<int, string> m_dicDI = new Dictionary<int, string>();
        private Dictionary<int, string> m_dicDIO = new Dictionary<int, string>();//2018-07-20 15:38:58 hungpv
        private Dictionary<int, string> m_dicLogon = new Dictionary<int, string>();//2018-11-01 09:18:23 hungpv

        private Dictionary<string, IG_BI_FULL> m_dicBI2 = new Dictionary<string, IG_BI_FULL>();

        // msg type
        public const string MSG_TYPE_INDEX = "I";      // HNX30/HNX30TRI/HNXCon/HNXFin/HNXIndex/HNXLCap/HNXMan/HNXMSCap/HNXUpcomIndex
        public const string MSG_TYPE_BASKET_INDEX = "S";      // HNX30/HNXCon/HNXFin/HNXIndex/HNXLCap/HNXMan/HNXMSCap/HNXUpcomIndex
        public const string MSG_TYPE_STOCK_INFO = "SI";     // HA+UP    => ListCodeABC
        public const string MSG_TYPE_TOP_N_PRICE = "TP";     // HA+UP    => ListCodeABC
        public const string MSG_TYPE_BOARD_INFO = "BI";     // HA+UP
        public const string MSG_TYPE_AUTION_MATCH = "EP";     // HA       => gia du kien phien ATC
        public const string MSG_TYPE_TOP_PRICE_ODDLOT = "PO";     // HA+UP    => ListCodeABC
        public const string MSG_TYPE_ETF_NET_VALUE = "IV";     // E1SSHN30
        public const string MSG_TYPE_ETF_TRACKING_ERROR = "TE";     // no data
        public const string MSG_TYPE_DERIVATIVES_INFO = "DI";     // thị trường Phái sinh. DerivativesQuotes
        public const string MSG_TYPE_DERIVATIVES_INFO_ORACLE = "DIO";     //2018-07-20 15:38:58 hungpv
        public const string MSG_TYPE_DERIVATIVES_INFO_A_LOGIN = "A";     //2018-11-01 09:15:46 hungpv
        public const string MSG_TYPE_DERIVATIVES_INFO_TP = "DI_TP";  // thị trường Phái sinh. DerivativesQuotes, TP nhung chua data cua thi truong phai sinh: unit va index deu khac voi TP cua thi truong co so

        public const string MSG_TYPE_STOCK_INFO_PT = "SI_PT";  // HA+UP, msg nay ko co that, chi dung de xu ly du lieu PT, dang lay ra tu msg SI 2015-12-16 10:32:39 ngocta2 (SI_PT) >> UPCOM cung co giao dich PT
        // data LE (danh sach lenh khop)
        const string LE_MT_LONG = "Time";
        const string LE_MT_SHORT = "MT";
        const string LE_MP_LONG = "MatchPrice";
        const string LE_MP_SHORT = "MP";
        const string LE_MQ_LONG = "MatchQtty";
        const string LE_MQ_SHORT = "MQ";
        const string LE_TQ_LONG = "NM_TotalTradedQtty";
        const string LE_TQ_SHORT = "TQ";
        const string LE_TV_LONG = "NM_TotalTradedValue";
        const string LE_TV_SHORT = "TV";
        // luu du lieu NM_TotalTradedQtty cua lan read truoc do
        private Dictionary<string, string> m_dicLE_NM_TotalTradedQtty = new Dictionary<string, string>();
        private Dictionary<string, string> m_dicLS_NM_TotalTradedQtty = new Dictionary<string, string>();
        //const REDIS
        private const string REGEX_TYPE_BOARD_INFO = @"\bMsgType='BI'\b";
        private const string REGEX_CSV_CHECK_IS_SI_TP = "(MsgType='TP|SI')";
        private const string REGEX_TYPE_DERIVATIVES_INFO_A_LOGIN = "(MsgType='A')";
        private const string REGEX_LOGONINFO = @"Logon_Infor='([^']*)'";
        private const string REGEX_RD_GET_SYMBOL = @"\bSymbol='([^']*)'";

        private const string REGEX_RD_GET_PROP_VAL_4_LE = @"(?<Data>\bTime\b|\bMatchPrice\b|\bNM_TotalTradedValue\b|\bNM_TotalTradedQtty\b)='(?<Val>.*?)'";
        private const string REGEX_RD_GET_PROP_VAL_2_LS = @"(?<DataLS>\bTime\b|\bMatchPrice\b|\bMatchQtty\b|\bNM_TotalTradedQtty\b)='(?<Val>.*?)'";

        private const string REGEX_CHECK_DI_VAL = "(MsgType='DI')";
        //PO
        private const string REGEX_RD_CHECK_IS_PO = "@MsgType='PO'";
        private const string REGEX_SQL_GET_PROP_VAL_4_PO = "@MsgType='PO'.*?@SendingTime='(?<SendingTime>.*?)',@Symbol='(?<Symbol>.*?)'.*?@BidPrice_1='(?<BidPrice_1>.*?)',@BidQtty_1='(?<BidQtty_1>.*?)',@BidPrice_2='(?<BidPrice_2>.*?)',@BidQtty_2='(?<BidQtty_2>.*?)',@BidPrice_3='(?<BidPrice_3>.*?)',@BidQtty_3='(?<BidQtty_3>.*?)',@OfferPrice_1='(?<OfferPrice_1>.*?)',@OfferQtty_1='(?<OfferQtty_1>.*?)',@OfferPrice_2='(?<OfferPrice_2>.*?)',@OfferQtty_2='(?<OfferQtty_2>.*?)',@OfferPrice_3='(?<OfferPrice_3>.*?)',@OfferQtty_3='(?<OfferQtty_3>.*?)'";
        const string TEMPLATE_JSONC_PO = "{\"T\":\"(T)\",\"S\":\"(S)\",\"BP1\":(BP1),\"BQ1\":(BQ1),\"BP2\":(BP2),\"BQ2\":(BQ2),\"BP3\":(BP3),\"BQ3\":(BQ3),\"SP1\":(SP1),\"SQ1\":(SQ1),\"SP2\":(SP2),\"SQ2\":(SQ2),\"SP3\":(SP3),\"SQ3\":(SQ3)}";    //
        private const string TEMPLATE_REDIS_KEY_PO = "PO:S5G_(Symbol)"; //   PO:S5G_(Symbol)
        //realtime => REDIS
        private const string TEMPLATE_REDIS_KEY_REALTIME = "REALTIME:S5G_(Symbol)"; //   REALTIME:S5G_(Symbol)
        private const string REDIS_KEY_HNX_BI = "S5G_HNX_BI";
        private const string REGEX_CSV_GET_TYPE = "@MsgType='(?<Type>.*?)'";
        private const string REGEX_CSV_GET_SENDING_TIME = "SendingTime='(?<SendingTime>.*?)'";
        private const string REGEX_CSV_GET_SYMBOL = @"\bSymbol\b='(?<Symbol>.*?)'";//"Symbol='(?<Symbol>.*?)'";
        private const string REGEX_CSV_GET_PROP_VAL = @"(\w+)='([^']*)'";//"@(?<SQLParam>.*?)='(?<Val>.*?)'";
        private const string REGEX_CSV_GET_PROP_VAL2 = @"(?<CSVParam>[A-Za-z0-9_]+)='(?<Val>.*?)'";//"@(?<CSVParam>.*?)='(?<Val>.*?)'";
        private const string TEMPLATE_REDIS_KEY_REALTIME_DATE = "yyyy:MM:dd"; //(Date)=(yyyy):(MM):(dd)

        private const string REGEX_SQL_REPLACE_REMOVE_TRASH = @"\b(AdjustQtty|AdjustRate|BeginString|BestBidPrice|BestBidQtty|BestOfferPrice|BestOfferQtty|BodyLength|CouponRate|DateNo|DividentRate|IssueDate|Issuer|MatchValue|MaturityDate|MsgType|Parvalue|PriorClosePrice|PriorOpenPrice|PriorPrice|SellCount|SecurityDesc|SenderCompID|SendingTime|TotalSellTradingQtty|TotalSellTradingValue|TradingUnit|BoardCode|MsgType|Symbol)\b='.*?'";

        private const string TEMPLATE_JSONC = "{\"STime\":\"(STime)\",\"SI\":{(SI)},\"TP\":{(TP)}}"; // {"SI":{}, "TP":{"ST":"20151119-09:00:21","NTP":"2","BBP1":"19900","BBQ1":"3000","BBP2":"18500","BBQ2":"1000"}}
        private const string TYPE_SI = "SI";
        private const string TYPE_TP = "TP";

        // Mapping dictionary for key-value assignments
        private static readonly Dictionary<string, Action<IG_BI_FULL, string>> propertyMap_BI = new Dictionary<string, Action<IG_BI_FULL, string>>
        {
            { "BeginString", (obj, value) => obj.BeginString = value },
            { "BodyLength", (obj, value) => obj.BodyLength = value },
            { "MsgType", (obj, value) => obj.MsgType = value },
            { "SenderCompID", (obj, value) => obj.SenderCompID = value },
            { "SendingTime", (obj, value) => obj.SendingTime = value },
            { "Name", (obj, value) => obj.Name = value },
            { "Shortname", (obj, value) => obj.Shortname = value },
            { "numSymbolAdvances", (obj, value) => obj.numSymbolAdvances = value },
            { "numSymbolDeclines", (obj, value) => obj.numSymbolDeclines = value },
            { "8numSymbolNochange", (obj, value) => obj.numSymbolNochange = value },
            { "totalNormalTradedQttyOd", (obj, value) => obj.totalNormalTradedQttyOd = value },
            { "totalNormalTradedValueOd", (obj, value) => obj.totalNormalTradedValueOd = value },
            { "totalNormalTradedQttyRd", (obj, value) => obj.totalNormalTradedQttyRd = value },
            { "totalNormalTradedValueRd", (obj, value) => obj.totalNormalTradedValueRd = value },
            { "totalPTTradedQtty", (obj, value) => obj.totalPTTradedQtty = value },
            { "totalPTTradedValue", (obj, value) => obj.totalPTTradedValue = value },
            { "TotalTrade", (obj, value) => obj.TotalTrade = value },
            { "TotalStock", (obj, value) => obj.TotalStock = value },
            { "DateNo", (obj, value) => obj.DateNo = value },
            { "BoardCode", (obj, value) => obj.BoardCode = value },
            { "BoardStatus", (obj, value) => obj.BoardStatus = value },
            { "Tradingdate", (obj, value) => obj.Tradingdate = value },
            { "Time", (obj, value) => obj.Time = value },
            { "TradingSessionID", (obj, value) => obj.TradingSessionID = value },
            { "TradSesStatus", (obj, value) => obj.TradSesStatus = value },
            { "f341", (obj, value) => obj.f341 = value }
        };
        //const SQL
        // ten SP se tao theo quy tac
        //prc_S5G_HNX_SAVER_IG_SI_UPDATE
        //prc_S5G_HNX_SAVER_IG_TP_UPDATE
        public const string TEMPLATE_SP_S5G_HNX_SAVER = "prc_S5G_HNX_SAVER_IG_(Type)_UPDATE"; //prc_S5G_HNX_SAVER_IG_SI_UPDATE

        private CRedis m_RC;
        private AppSetting _appsetting;
        private CDatabase _cDatabase;
        private CTracker _cTracker;
        public CHandCode(AppSetting _appsetting, CRedis _cRedis, CDatabase cdatabase, CTracker _tracker)
        {
            this.m_RC = _cRedis;
            this._appsetting = _appsetting;
            this._cDatabase = cdatabase;
            this._cTracker = _tracker;

            this.m_dicI = this.InitDic(MSG_TYPE_INDEX);                  // MSG_TYPE_MESSAGE_INDEX = "I";
            this.m_dicS = this.InitDic(MSG_TYPE_BASKET_INDEX);           // MSG_TYPE_BASKET_INDEX = "S";
            this.m_dicSI = this.InitDic(MSG_TYPE_STOCK_INFO);             // MSG_TYPE_STOCK_INFO = "SI";
            this.m_dicTP = this.InitDic(MSG_TYPE_TOP_N_PRICE);            // MSG_TYPE_TOP_N_PRICE = "TP";
            this.m_dicBI = this.InitDic(MSG_TYPE_BOARD_INFO);             // MSG_TYPE_BOARD_INFO = "BI";
            this.m_dicEP = this.InitDic(MSG_TYPE_AUTION_MATCH);           // MSG_TYPE_AUTION_MATCH = "EP";
            this.m_dicPO = this.InitDic(MSG_TYPE_TOP_PRICE_ODDLOT);       // MSG_TYPE_TOP_PRICE_ODDLOT = "PO";
            this.m_dicIV = this.InitDic(MSG_TYPE_ETF_NET_VALUE);          // MSG_TYPE_ETF_NET_VALUE = "IV";
            this.m_dicTE = this.InitDic(MSG_TYPE_ETF_TRACKING_ERROR);     // MSG_TYPE_ETF_TRACKING_ERROR = "TE";
            this.m_dicDI = this.InitDic(MSG_TYPE_DERIVATIVES_INFO);       // MSG_TYPE_DERIVATIVES_INFO = "DI";
            this.m_dicDIO = this.InitDic(MSG_TYPE_DERIVATIVES_INFO_ORACLE);       // MSG_TYPE_DERIVATIVES_INFO_ORACLE = "DIO";
            this.m_dicLogon = this.InitDic(MSG_TYPE_DERIVATIVES_INFO_A_LOGIN);       // MSG_TYPE_DERIVATIVES_INFO_ORACLE = "A";
            
            
            
        }
        public enum REPLACE_TAGS                                                // xac dinh muc dich replace tag, khi pub can tiet kiem bandwith, de ten fieldName ngan nhat co the
        {
            FOR_PUB,                                                            // de pub vao channel : 425 => f425
            FOR_DB                                                              // de exec sp insert db : 425 => BoardCode
        }
        public async Task<string> ProcessDataRedis(string strCSV)
        {
            string strLogonInfor = "";
            try
            {
                //Lấy ra type của strCSV => switch case xử lý theo type
                string strCSVType = Regex.Match(strCSV, REGEX_CSV_GET_TYPE).Groups[1].Value;
                if (strCSVType == "")
                    CLog.LogEx("Error_Type.txt", strCSV);
                switch (strCSVType)
                {
                    case "A":
                        Redis_msg_A(strCSV, ref strLogonInfor);                        
                        break;
                    case "SI":
                        await Task.WhenAll(InsertRealtime2Redis(strCSV), InsertLE2Redis(strCSV), InsertLS2Redis(strCSV));
                        break;
                    case "TP":
                        //Hàm xử lý REDIS key REALTIME:S5G_A32
                        await InsertRealtime2Redis(strCSV);
                        break;
                    case "DI":
                        await Task.WhenAll(InsertLE2Redis(strCSV), InsertLS2Redis(strCSV));
                        break ;
                    case "BI":
                        //Hàm xử lý REDIS key S5G_HNX_BI
                        InsertBI2Redis(strCSV);
                        break;
                    case "PO":
                        InsertPO2Redis(strCSV);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
            return strLogonInfor;
        }
        /// <summary>
        /// strCSV = "BeginString='HNX.TDS.1',BodyLength='528',MsgType='TP',SenderCompID='HNX',SendingTime='20241007-09:59:59',Symbol='VN30F2410',BoardCode='DER_BRD_01',NoTopPrice='10',NumTopPrice='1',BestBidPrice='1344.50',BestBidQtty='26',BestOfferPrice='1344.60',BestOfferQtty='121',NumTopPrice='2',BestBidPrice='1344.40',BestBidQtty='16',BestOfferPrice='1344.80',BestOfferQtty='283',NumTopPrice='3',BestBidPrice='1344.30',BestBidQtty='61',BestOfferPrice='1344.90',BestOfferQtty='26',NumTopPrice='4',BestBidPrice='1344.20',BestBidQtty='61',BestOfferPrice='1345',BestOfferQtty='297',NumTopPrice='5',BestBidPrice='1344.10',BestBidQtty='180',BestOfferPrice='1345.20',BestOfferQtty='57',NumTopPrice='6',BestBidPrice='1344',BestBidQtty='178',BestOfferPrice='1345.30',BestOfferQtty='68',NumTopPrice='7',BestBidPrice='1343.90',BestBidQtty='8',BestOfferPrice='1345.40',BestOfferQtty='38',NumTopPrice='8',BestBidPrice='1343.80',BestBidQtty='82',BestOfferPrice='1345.50',BestOfferQtty='59',NumTopPrice='9',BestBidPrice='1343.70',BestBidQtty='16',BestOff..."
        /// strSQL = "prc_S5G_HNX_SAVER_IG_TP_UPDATE @BeginString='HNX.TDS.1',@BodyLength='528',@MsgType='TP',@SenderCompID='HNX',@SendingTime='20241007-09:59:59',@Symbol='VN30F2410',@BoardCode='DER_BRD_01',@NoTopPrice='10',@NumTopPrice='1',@BestBidPrice='1344.50',@BestBidQtty='26',@BestOfferPrice='1344.60',@BestOfferQtty='121',@NumTopPrice='2',@BestBidPrice='1344.40',@BestBidQtty='16',@BestOfferPrice='1344.80',@BestOfferQtty='283',@NumTopPrice='3',@BestBidPrice='1344.30',@BestBidQtty='61',@BestOfferPrice='1344.90',@BestOfferQtty='26',@NumTopPrice='4',@BestBidPrice='1344.20',@BestBidQtty='61',@BestOfferPrice='1345',@BestOfferQtty='297',@NumTopPrice='5',@BestBidPrice='1344.10',@BestBidQtty='180',@BestOfferPrice='1345.20',@BestOfferQtty='57',@NumTopPrice='6',@BestBidPrice='1344',@BestBidQtty='178',@BestOfferPrice='1345.30',@BestOfferQtty='68',@NumTopPrice='7',@BestBidPrice='1343.90',@BestBidQtty='8',@BestOfferPrice='1345.40',@BestOfferQtty='38',@NumTopPrice='8',@BestBidPrice='1343.80',@BestBidQtty='82',@BestOfferPrice='1345.50',@Bes..."
        /// </summary>
        /// <param name="strCSV"></param>
        public void ProcessDataSQL(string strCSV)
        {
            try
            {
                //string strSPname = "";
                //string strSQL = "";
                //StringBuilder sb = new StringBuilder(strCSV, strCSV.Length * 2);
                ////Thêm dấu phẩy vào đầu
                ////sb.Insert(0, ",");
                ////Thêm @ vào trước key
                //sb.Replace(",", ",@");
                //// Xóa dấu phẩy ở đầu
                ////sb.Remove(0,1);

                ////Lấy ra type của strCSV => Đặt vào template 
                //string strCSVType = Regex.Match(strCSV, REGEX_CSV_GET_TYPE).Groups[1].Value;

                //strSPname = TEMPLATE_SP_S5G_HNX_SAVER.Replace("(Type)", strCSVType);

                //sb.Insert(0, strSPname + " @");

                //strSQL = sb.ToString();

                //if (strSQL == "") return;
                _cDatabase.ExecuteScriptQuoteSaverHNX(strCSV);

                //send Monitor5G
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
        /// <summary>
        /// Xử lý type = 'A' => REDIS
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="strLogonInfor"></param>
        /// <returns></returns>
        private bool Redis_msg_A(string strA, ref string strLogonInfor)
        {
            try
            {               
                if(strA == "") return false;
                Match match = Regex.Match(strA, REGEX_LOGONINFO);
                if (match.Success)
                {
                    strLogonInfor = match.Groups[1].Value;
                }
                double Z_SCORE = Convert.ToDouble(DateTime.Now.ToString(CConfig.FORMAT_TIME_5));
                // insert ZSet vao redis
                if (this.m_RC.RC != null)
                {
                    this.m_RC.RC.SortedSetAdd(_appsetting.RedisSetting.KEY_S5G_IG_LOGON, strLogonInfor, Z_SCORE);
                }
                if (this.m_RC.RC_Fox != null)
                {
                    this.m_RC.RC_Fox.SortedSetAdd(_appsetting.RedisSetting.KEY_S5G_IG_LOGON, strLogonInfor, Z_SCORE);
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
        /// Hàm xử lý msg SI/TP => insert key REALTIME:S5G_A32
        /// </summary>
        /// <param name="strSI_TP"></param>
        /// <returns></returns>
        private async Task InsertRealtime2Redis(string strSI_TP) 
        {
            try
            {
                StringBuilder sbJsonC = new StringBuilder(TEMPLATE_JSONC);
                StringBuilder sbSI = new StringBuilder("");
                StringBuilder sbTP = new StringBuilder("");
                string strReplacedCSV = "";
                string strJsonC = "";

                // lay type tu strCSV (SI/TP)
                string strType = Regex.Match(strSI_TP, REGEX_CSV_GET_TYPE).Groups[1].Value;

                // lay symbol tu strSQL (SI/TP)
                string strSymbol = Regex.Match(strSI_TP, REGEX_CSV_GET_SYMBOL).Groups[1].Value;
                // lay SendingTime tu strSQL (SI/TP)
                string strSendingTime = Regex.Match(strSI_TP, REGEX_CSV_GET_SENDING_TIME).Groups[1].Value;

                // replace de xoa cac thong tin ko can thiet
                strReplacedCSV = Regex.Replace(strSI_TP, REGEX_SQL_REPLACE_REMOVE_TRASH, "");

                // lay ra tat ca Prop/Val strReplacedCSV
                Regex RegexObj = new Regex(REGEX_CSV_GET_PROP_VAL2);
                Match MatchResults = RegexObj.Match(strReplacedCSV);
                while (MatchResults.Success)
                {
                    StringBuilder sbProp = new StringBuilder(MatchResults.Groups["CSVParam"].Value);
                    StringBuilder sbVal = new StringBuilder(MatchResults.Groups["Val"].Value);

                    //replace CSVParam dài -> sbProp ngắn
                    sbProp = this.SQLParam2ShortProp(sbProp);
                    sbVal = this.LongVal2ShortVal(sbVal);

                    // noi string json
                    if (strType == TYPE_SI) sbSI.Append(",\"").Append(sbProp).Append("\":\"").Append(sbVal).Append("\""); // ,"BBQ1":"3000"
                    if (strType == TYPE_TP) sbTP.Append(",\"").Append(sbProp).Append("\":\"").Append(sbVal).Append("\""); // ,"BBQ1":"3000"

                    MatchResults = MatchResults.NextMatch();
                }
                // xoa ky , tu dau tien
                if (sbSI.Length > 0) sbSI.Remove(0, 1);
                if (sbTP.Length > 0) sbTP.Remove(0, 1);
                sbJsonC
                    .Replace("(STime)", DateTime.Now.ToString(CConfig.FORMAT_DATETIME_6)) //strSendingTime ko co phan nghin second nen phai dung server time cua FPTS
                    .Replace("(SI)", sbSI.ToString())
                    .Replace("(TP)", sbTP.ToString());

                strJsonC = sbJsonC.ToString();

                //Insert SI/TP -> key Realtime
                string Z_KEY = TEMPLATE_REDIS_KEY_REALTIME
                .Replace("(Date)", DateTime.Now.ToString(TEMPLATE_REDIS_KEY_REALTIME_DATE))
                .Replace("(Symbol)", strSymbol);

                double Z_SCORE = Convert.ToDouble(DateTime.Now.ToString(CConfig.FORMAT_TIME_5));
                string Z_VALUE = strJsonC;

                await this.m_RC.SortedSetAddAsync(Z_KEY, Z_VALUE, Z_SCORE);
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
        /// <summary>
        /// Hàm xử lý msg BI => insert key S5G_HNX_BI
        /// </summary>
        /// <param name="strBI"></param>
        /// <returns></returns>
        private bool InsertBI2Redis(string strBI)
        {
            try
            {
                if(strBI == "") return false ;

                //CLog.LogEx("Msg_BI.txt", strCSV);
                string strBoardCode = Regex.Match(strBI, @"BoardCode='([^']*)'").Groups[1].Value;
                string strKey = REDIS_KEY_HNX_BI;
                List<IG_BI_FULL> _listBI = new List<IG_BI_FULL>();
                IG_BI_FULL ig_bi_full = new IG_BI_FULL();
                // Regex để tìm các cặp key-value theo định dạng key='value'
                string pattern = REGEX_CSV_GET_PROP_VAL;
                var matches = Regex.Matches(strBI, pattern);
                foreach (Match match in matches)
                {
                    var key = match.Groups[1].Value;
                    var value = match.Groups[2].Value;
                    if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(key))
                    {
                        if(propertyMap_BI.TryGetValue(key, out var assignValue))
                        {
                            assignValue(ig_bi_full, value); 
                        }
                    }
                }

                Dic_AddOrUpdate(ref m_dicBI2, strBoardCode, ig_bi_full);

                foreach (KeyValuePair<string, IG_BI_FULL> pair in m_dicBI2)
                {
                    IG_BI_FULL ig_full = pair.Value;
                    _listBI.Add(ig_full);
                }
                EDataSingle eDataSingle = new EDataSingle(_listBI);

                //Tạo JsonSerializerSettings để bỏ qua giá trị null
                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                };
                string jsonData = JsonConvert.SerializeObject(eDataSingle, jsonSettings);

                //insert BI => Redis
                this.m_RC.SetCacheBI(strKey, jsonData, CConfig.intPeriod);
                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false ;
            }
        }
        /// <summary>
        /// Hàm xử lý msg SI => insert key LS
        /// </summary>
        /// <param name="strLS"></param>
        /// <returns></returns>
        private async Task InsertLS2Redis(string strLS)
        {
            try
            {
                if (strLS == "") return;

                //Kiểm tra nếu strLS có DI
                bool checkDI = Regex.IsMatch(strLS, REGEX_CHECK_DI_VAL);

                int intMatchCount = 0;
                string strSymbol = "", strJsonC = "";

                // lay symbol tu strResult (SI/DI)
                strSymbol = Regex.Match(strLS, REGEX_RD_GET_SYMBOL).Groups[1].Value;

                LS_Model ls = new LS_Model();
                //lay ra tat ca Prop/Val trong strResult
                Regex RegexObj = new Regex(REGEX_RD_GET_PROP_VAL_2_LS);
                Match MatchResults = RegexObj.Match(strLS);

                while (MatchResults.Success)
                {
                    //Group "Data": MatchQtty
                    //Group "Val": 8000
                    string strProp = MatchResults.Groups["DataLS"].Value.ToString();
                    string strVal = MatchResults.Groups["Val"].Value.ToString();

                    // {"MT":"11:17:15","MQ":20,"MP":48.9}
                    switch (strProp)
                    {
                        case LE_MT_LONG:    // "Time":
                            ls.MT = strVal;
                            break;
                        case LE_MP_LONG:    //"MatchPrice":
                            if (!CBase.IsNumeric(strVal)) return;// ko phai so thi return luon
                            ls.MP = Convert.ToDouble(strVal);
                            //ls.MP = Convert.ToInt32(strVal);
                            break;

                        case LE_MQ_LONG:    //"MatchQtty":
                            if (!CBase.IsNumeric(strVal)) return;// ko phai so thi return luon
                            ls.MQ = long.Parse(strVal.Replace(".000000", ""));
                            break;
                        case LE_TQ_LONG: //NM_TotalTradedQtty
                            if (!CBase.IsNumeric(strVal)) return ;// ko phai so thi return luon
                            string strOldNMQtty_LS = Dic_GetValue(this.m_dicLS_NM_TotalTradedQtty, strSymbol, "0");
                            string strNewNMQtty = strVal;

                            // thong tin moi van giong thong tin cu thi exit 
                            if (strOldNMQtty_LS == strNewNMQtty)
                                return ;
                            else
                                Dic_AddOrUpdate(ref this.m_dicLS_NM_TotalTradedQtty, strSymbol, strNewNMQtty);
                            break;

                    }
                    intMatchCount++;// ko co intMatchCount thi ko the return string jsonC
                    MatchResults = MatchResults.NextMatch();
                }
                // ko co match thi return ""
                if (intMatchCount <= 1)
                    return ;

                // Lấy thời gian hiện tại (local time)
                DateTime localDateTime = DateTime.Now;
                // Chuyển đổi local time sang Unix timestamp
                long unixTimestamp = ((DateTimeOffset)localDateTime).ToUnixTimeSeconds();

                //Xử lý CN -  Lấy Guid (random)
                Guid guid = Guid.NewGuid();
                string guidString = guid.ToString("N"); // Lấy chuỗi không dấu gạch ngang
                string first10Digits = guidString.Substring(0, 10);
                // Chuyển đổi 10 ký tự này thành số nguyên long
                long lsCN = long.Parse(first10Digits.Substring(0, 10), System.Globalization.NumberStyles.HexNumber);
                // Đảm bảo ls.CN có 10 chữ số bằng cách chia cho 10 nếu cần
                while (lsCN >= 10000000000)
                {
                    lsCN /= 10;
                }
                ls.CN = lsCN;
                strJsonC = JsonConvert.SerializeObject(checkDI ? ls : new
                {
                    ls.CN,
                    ls.MT,
                    MP = (int)ls.MP,
                    ls.MQ,
                    ls.SIDE
                });

                string Z_KEY = _appsetting.RedisSetting.TEMPLATE_REDIS_KEY_KL_LS.Replace("(Symbol)", strSymbol);
                double Z_SCORE = unixTimestamp;
                string Z_VALUE = strJsonC;

                await this.m_RC.SortedSetAddAsync(Z_KEY, Z_VALUE, Z_SCORE);
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return ;
            }
        }
        /// <summary>
        /// Hàm xử lý msg SI => insert key TKTT
        /// </summary>
        /// <param name="strLE"></param>
        /// <returns></returns>
        private async Task InsertLE2Redis(string strLE)
        {
            try
            {
                if(strLE == "") return;

                // Kiểm tra nếu strLE có DI
                bool checkDI = Regex.IsMatch(strLE, REGEX_CHECK_DI_VAL);

                int intMatchCount = 0;
                string strSymbol = "", strJsonC = "", strJson_Base = "";
                // lay symbol tu strResult (SI/DI)
                strSymbol = Regex.Match(strLE, REGEX_RD_GET_SYMBOL).Groups[1].Value;
                LE_Model le = new LE_Model();
                //lay ra tat ca Prop/Val trong strResult
                Regex RegexObj = new Regex(REGEX_RD_GET_PROP_VAL_4_LE);
                Match MatchResults = RegexObj.Match(strLE);
                while (MatchResults.Success)
                {
                    //Group "SQLParam": MatchQtty
                    //Group "Val": 8000
                    string strProp = MatchResults.Groups["Data"].Value.ToString();
                    string strVal = MatchResults.Groups["Val"].Value.ToString();
                    // {"MT":"11:17:15","MQ":20,"MP":48.9,"TQ":10340}
                    switch (strProp)
                    {
                        case LE_MT_LONG:    // "Time":
                            le.MT = strVal;
                            break;
                        case LE_MP_LONG:    //"MatchPrice":
                            if (!CBase.IsNumeric(strVal)) return ;// ko phai so thi return luon

                            le.MP = Convert.ToDouble(strVal);
                            //le.MP = (int)(Convert.ToDecimal(strVal)); 
                            break;
                        case LE_TV_LONG:
                            if (!CBase.IsNumeric(strVal)) return ;// ko phai so thi return luon

                            le.TV = long.Parse(strVal.Replace(".000000", "")) / 1000000;
                            break;
                        case LE_TQ_LONG:    //"NM_TotalTradedQtty":
                            if (!CBase.IsNumeric(strVal)) return ;// ko phai so thi return luon

                            strVal = strVal.Replace(".000000", ""); // bo so sau dau phay

                            string strOldNMQtty = Dic_GetValue(this.m_dicLE_NM_TotalTradedQtty, strSymbol, "0");
                            string strNewNMQtty = strVal;

                            // thong tin moi van giong thong tin cu thi exit 
                            if (strOldNMQtty == strNewNMQtty)
                                return;
                            else
                                Dic_AddOrUpdate(ref this.m_dicLE_NM_TotalTradedQtty, strSymbol, strNewNMQtty);

                            le.TQ = long.Parse(strNewNMQtty);
                            break;
                    }
                    intMatchCount++;// ko co intMatchCount thi ko the return string jsonC
                    MatchResults = MatchResults.NextMatch();
                }
                // ko co match thi return ""
                // match time luon ok vay la phai >1
                if (intMatchCount <= 1)
                    return ;

                /* <MatchPrice của CK trả về kiểu int>
                 * MatchPrice của Phái Sinh 
                  ---TKTT chỉ lấy Data của VOL(khối lượng) => trả về double
                  ---key VAL => giữ nguyên về kiểu int
                */
                strJson_Base = JsonConvert.SerializeObject(new
                {
                    le.MT,
                    MP = (int)le.MP,
                    le.TQ,
                    le.TV
                });
                //strJsonC cho TKTT_VOL => MatchPrice <double>
                strJsonC = checkDI ? JsonConvert.SerializeObject(le) : strJson_Base;
                
                //Insert key LE vào Redis
                string Z_KEY_VAL = _appsetting.RedisSetting.TEMPLATE_REDIS_KEY_LE_VAL.Replace("(Symbol)", strSymbol);
                string Z_KEY_VOL = _appsetting.RedisSetting.TEMPLATE_REDIS_KEY_LE_VOL.Replace("(Symbol)", strSymbol);

                // Chuyển đổi local time sang Unix timestamp
                double Z_SCORE = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

                string Z_VALUE = strJsonC;

                var taskVol = this.m_RC.SortedSetAddAsync(Z_KEY_VOL, Z_VALUE, Z_SCORE);
                var taskVal = this.m_RC.SortedSetAddAsync(Z_KEY_VAL, strJson_Base, Z_SCORE);

                // Đợi cả hai tác vụ hoàn thành
                await Task.WhenAll(taskVol, taskVal);

                ////TKTT_VOL
                //await this.m_RC.SortedSetAddAsync(Z_KEY_VOL, Z_VALUE, Z_SCORE);
                ////TKTT_VAL
                //await this.m_RC.SortedSetAddAsync(Z_KEY_VAL, strJson_Base, Z_SCORE);

                return;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return;
            }
        }
        /// <summary>
        /// Hàm xử lý msg PO => insert key PO
        /// </summary>
        /// <param name="strPO"></param>
        /// <returns></returns>
        private bool InsertPO2Redis(string strPO)
        {
            try
            {
                if(strPO == "") return false;
                const string DEBUG_CODE = "VND";
                //declare
                StringBuilder sbJsonC = new StringBuilder(TEMPLATE_JSONC_PO);
                string strJsonC = "";
                string strSymbol = "";
                int intMatchCount = 0;

                if (strSymbol == DEBUG_CODE) CLog.LogEx("PO.txt", "strPO=" + strPO);

                // lay symbol tu strSQL (SI/TP)
                strSymbol = Regex.Match(strPO, REGEX_RD_GET_SYMBOL).Groups[1].Value;
                // lay ra tat ca Prop/Val trong strPO
                Regex RegexObj = new Regex(REGEX_SQL_GET_PROP_VAL_4_PO);
                Match MatchResults = RegexObj.Match(strPO);
                if (MatchResults.Success) 
                {
                    string T = MatchResults.Groups["SendingTime"].Value.ToString();
                    string S = MatchResults.Groups["Symbol"].Value.ToString();
                    string BP1 = MatchResults.Groups["BidPrice_1"].Value.ToString();
                    string BQ1 = MatchResults.Groups["BidQtty_1"].Value.ToString();
                    string BP2 = MatchResults.Groups["BidPrice_2"].Value.ToString();
                    string BQ2 = MatchResults.Groups["BidQtty_2"].Value.ToString();
                    string BP3 = MatchResults.Groups["BidPrice_3"].Value.ToString();
                    string BQ3 = MatchResults.Groups["BidQtty_3"].Value.ToString();
                    string SP1 = MatchResults.Groups["OfferPrice_1"].Value.ToString();
                    string SQ1 = MatchResults.Groups["OfferQtty_1"].Value.ToString();
                    string SP2 = MatchResults.Groups["OfferPrice_2"].Value.ToString();
                    string SQ2 = MatchResults.Groups["OfferQtty_2"].Value.ToString();
                    string SP3 = MatchResults.Groups["OfferPrice_3"].Value.ToString();
                    string SQ3 = MatchResults.Groups["OfferQtty_3"].Value.ToString();

                    if (CBase.IsNumeric(BP1)) BP1 = (Convert.ToDecimal(BP1) / 1000).ToString();
                    if (CBase.IsNumeric(BP2)) BP2 = (Convert.ToDecimal(BP2) / 1000).ToString();
                    if (CBase.IsNumeric(BP3)) BP3 = (Convert.ToDecimal(BP3) / 1000).ToString();
                    if (CBase.IsNumeric(SP1)) SP1 = (Convert.ToDecimal(SP1) / 1000).ToString();
                    if (CBase.IsNumeric(SP2)) SP2 = (Convert.ToDecimal(SP2) / 1000).ToString();
                    if (CBase.IsNumeric(SP3)) SP3 = (Convert.ToDecimal(SP3) / 1000).ToString();

                    sbJsonC
                        .Replace("(T)", T)
                        .Replace("(S)", S)
                        .Replace("(BP1)", BP1)
                        .Replace("(BQ1)", BQ1)
                        .Replace("(BP2)", BP2)
                        .Replace("(BQ2)", BQ2)
                        .Replace("(BP3)", BP3)
                        .Replace("(BQ3)", BQ3)
                        .Replace("(SP1)", SP1)
                        .Replace("(SQ1)", SQ1)
                        .Replace("(SP2)", SP2)
                        .Replace("(SQ2)", SQ2)
                        .Replace("(SP3)", SP3)
                        .Replace("(SQ3)", SQ3);
                    intMatchCount++;// ko co intMatchCount thi ko the return string jsonC
                }

                if (strSymbol == DEBUG_CODE) CLog.LogEx("PO.txt", "sbJsonC=" + sbJsonC.ToString());
                if (strSymbol == DEBUG_CODE) CLog.LogEx("PO.txt", "intMatchCount=" + intMatchCount.ToString());

                strJsonC = sbJsonC.ToString();

                // tao key/value
                string strKey = TEMPLATE_REDIS_KEY_PO.Replace("(Symbol)", strSymbol);
                //insert Redis
                if(m_RC.RC != null)
                {
                    m_RC.SetCache(strKey, strJsonC, CConfig.intPeriod);
                }

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        ///<sumary>
        ///Convert msgCSV thành msg TopNPrice
        ///strCSV = 8='HNX.TDS.1',BodyLength='528',MsgType='TP',SenderCompID='HNX',SendingTime='20241007-09:59:59',Symbol='VN30F2410',BoardCode='DER_BRD_01',NoTopPrice='10',NumTopPrice='1',BestBidPrice='1344.50',BestBidQtty='26',BestOfferPrice='1344.60',BestOfferQtty='121',NumTopPrice='2',BestBidPrice='1344.40',BestBidQtty='16',BestOfferPrice='1344.80',BestOfferQtty='283',NumTopPrice='3',BestBidPrice='1344.30',BestBidQtty='61',BestOfferPrice='1344.90',BestOfferQtty='26',NumTopPrice='4',BestBidPrice='1344.20',BestBidQtty='61',BestOfferPrice='1345',BestOfferQtty='297',NumTopPrice='5',BestBidPrice='1344.10',BestBidQtty='180',BestOfferPrice='1345.20',BestOfferQtty='57',NumTopPrice='6',BestBidPrice='1344',BestBidQtty='178',BestOfferPrice='1345.30',BestOfferQtty='68',NumTopPrice='7',BestBidPrice='1343.90',BestBidQtty='8',BestOfferPrice='1345.40',BestOfferQtty='38',NumTopPrice='8',BestBidPrice='1343.80',BestBidQtty='82',BestOfferPrice='1345.50',BestOfferQtty='59',NumTopPrice='9',BestBidPrice='1343.70',BestBidQtty='16',BestOfferPrice='1345.60',BestOfferQtty='49',NumTopPrice='10',BestBidPrice='1343.60',BestBidQtty='97',BestOfferPrice='1345.70',BestOfferQtty='42'
        ///Result:        
        private string CSV2TopNPrice(string strCSV)
        {
            try
            {
                string strTP = "";
                //Cắt th NumTopPrice xuống =>  
                //NumTopPrice='1',BestBidPrice='31200',BestBidQtty='4300',BestOfferPrice='31300',BestOfferQtty='36600',
                //NumTopPrice = '2',BestBidPrice = '31100',BestBidQtty = '87900',BestOfferPrice = '31400',BestOfferQtty = '46000',
                strTP = strCSV.Replace("@NumTopPrice", "\r\n" + "@NumTopPrice");

                Regex RegexObj = new Regex("(?<FullRow>@NumTopPrice.*)");
                Match MatchResults = default(Match);
                Match MatchResults2 = default(Match);
                string strFullRow = "";
                string strFullRowNew = "";
                string BESTBIDPRICE = "@BestBidPrice";
                string BESTBIDQTTY = "@BestBidQtty";
                string BESTOFFERPRICE = "@BestOfferPrice";
                string BESTOFFERQTTY = "@BestOfferQtty";

                MatchResults = RegexObj.Match(strTP);

                while (MatchResults.Success) 
                {
                    strFullRow = (MatchResults.Groups["FullRow"].Value);
                    MatchResults2 = Regex.Match(strFullRow, "(?<FullNumTop>@NumTopPrice='(?<NumTop>\\d*?)',)");
                    string strNumTop = MatchResults2.Groups["NumTop"].Value;
                    string strFullNumTop = MatchResults2.Groups["FullNumTop"].Value;
                    strFullRowNew = strFullRow.Replace(BESTBIDPRICE, BESTBIDPRICE + strNumTop);
                    strFullRowNew = strFullRowNew.Replace(BESTBIDQTTY, BESTBIDQTTY + strNumTop);
                    strFullRowNew = strFullRowNew.Replace(BESTOFFERPRICE, BESTOFFERPRICE + strNumTop);
                    strFullRowNew = strFullRowNew.Replace(BESTOFFERQTTY, BESTOFFERQTTY + strNumTop);
                    strTP = strTP.Replace(strFullRow, strFullRowNew);
                    MatchResults = MatchResults.NextMatch();
                }
                //result =>
                //NumTopPrice='1',BestBidPrice1='31200',BestBidQtty1='4300',BestOfferPrice1='31300',BestOfferQtty1='36600',
                //NumTopPrice = '2',BestBidPrice2 = '31100',BestBidQtty2 = '87900',BestOfferPrice2 = '31400',BestOfferQtty2 = '46000',

                //Bỏ xuống dòng
                strTP = strTP.Replace("\r\n", "");

                strTP = Regex.Replace(strTP, "@NumTopPrice.*?,", ""); // xoa param NumTopPrice vi ko can thiet

                strTP = RemoveHeader(strTP, "@");

                return strTP;
            }
            catch(Exception ex) 
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }
        /// <summary>
        /// Xử lý msg FIX: 8=HNX.TDS.19=52835=TP49=HNX52=20241007-09:59:5955=VN30F2410425=DER_BRD_01555=10556=1132=1344.501321=26133=1344.601331=121556=2132=1344.401321=16133=1344.801331=283556=3132=1344.301321=61133=1344.901331=26556=4132=1344.201321=61133=13451331=297556=5132=1344.101321=180133=1345.201331=57556=6132=13441321=178133=1345.301331=68556=7132=1343.901321=8133=1345.401331=38556=8132=1343.801321=82133=1345.501331=59556=9132=1343.701321=16133=1345.601331=49556=10132=1343.601321=97133=1345.701331=42
        /// => "BeginString='HNX.TDS.1',BodyLength='528',MsgType='TP',SenderCompID='HNX',SendingTime='20241007-09:59:59',Symbol='VN30F2410',BoardCode='DER_BRD_01',NoTopPrice='10',NumTopPrice='1',BestBidPrice='1344.50',BestBidQtty='26',BestOfferPrice='1344.60',BestOfferQtty='121',NumTopPrice='2',BestBidPrice='1344.40',BestBidQtty='16',BestOfferPrice='1344.80',BestOfferQtty='283',NumTopPrice='3',BestBidPrice='1344.30',BestBidQtty='61',BestOfferPrice='1344.90',BestOfferQtty='26',NumTopPrice='4',BestBidPrice='1344.20',BestBidQtty='61',BestOfferPrice='1345',BestOfferQtty='297',NumTopPrice='5',BestBidPrice='1344.10',BestBidQtty='180',BestOfferPrice='1345.20',BestOfferQtty='57',NumTopPrice='6',BestBidPrice='1344',BestBidQtty='178',BestOfferPrice='1345.30',BestOfferQtty='68',NumTopPrice='7',BestBidPrice='1343.90',BestBidQtty='8',BestOfferPrice='1345.40',BestOfferQtty='38',NumTopPrice='8',BestBidPrice='1343.80',BestBidQtty='82',BestOfferPrice='1345.50',BestOfferQtty='59',NumTopPrice='9',BestBidPrice='1343.70',BestBidQtty='16',BestOfferPrice='1345.60',BestOfferQtty='49',NumTopPrice='10',BestBidPrice='1343.60',BestBidQtty='97',BestOfferPrice='1345.70',BestOfferQtty='42'"
        /// </summary>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        public string Message2CSV(string strMessage, ref string strORCL, ref string strLogon)
        {
            try
            {
                string strType = "";
                string strResult = "";                
                string strCSV = "";
                string strSPname = "";
                string[] arrPart = null;

                // replace so thanh chu
                strResult = this.ReplaceTags(strMessage, ref strType, ref strORCL, ref arrPart ,REPLACE_TAGS.FOR_DB);

                if (string.IsNullOrEmpty(strResult))
                    return "";
                
                // tim ra ten SP theo quy tac
                strSPname = TEMPLATE_SP_S5G_HNX_SAVER.Replace("(Type)", strType);

                // obj SB de xu ly toc do nhanh
                StringBuilder sb = new StringBuilder(strResult, strResult.Length * 2);
                StringBuilder sbOracle = new StringBuilder(strORCL, strORCL.Length * 2);
                // binMsg= 8=HNX.TDS.19=015135=BI49=HNX52=20150922-10:45:22421=LIS_BRD_ETF422=LIS_BRD_ETF17=179425=LIS_BRD_ETF426=A388=20150922399=10:45:22336=LIS_CON_NML340=1341=LIS
                // => 8=HNX.TDS.1","9=0151","35=BI","49=HNX","52=20150922-10:45:22","421=LIS_BRD_ETF","422=LIS_BRD_ETF","17=179","425=LIS_BRD_ETF","426=A","388=20150922","399=10:45:22","336=LIS_CON_NML","340=1","341=LIS","
                sb.Replace(((char)1).ToString(), "',@");
                
                if (strType == MSG_TYPE_DERIVATIVES_INFO)
                {
                    sbOracle.Replace(((char)1).ToString(), "|");
                    strORCL = sbOracle.ToString();

                }
                // => 8":"HNX.TDS.1","9":"0151","35":"BI","49":"HNX","52":"20150922-10:45:22","421":"LIS_BRD_ETF","422":"LIS_BRD_ETF","17":"179","425":"LIS_BRD_ETF","426":"A","388":"20150922","399":"10:45:22","336":"LIS_CON_NML","340":"1","341":"LIS","
                sb.Replace("=", "='");

                // => ......,"341":"LIS","  => ,"341":"LIS"
                sb.Length -= 3;

                // => {"BeginString":"HNX.TDS.1","BodyLength":"0
                sb.Insert(0, strSPname + " @");

                // => .....,"TradingSessionID":"LIS_CON_NML","TradSesStatus":"1"}
                sb.Append("'");

                strCSV = sb.ToString();

                //Nếu @MsgType='A' => strLogon
                if (strType == MSG_TYPE_DERIVATIVES_INFO_A_LOGIN)
                {
                    strLogon = strCSV;
                    return "";
                }

                //Nếu TopNPrice thì xử lý riêng
                if (strType == MSG_TYPE_TOP_N_PRICE)
                {
                    return this.CSV2TopNPrice(strCSV);    
                }

                return strCSV;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }
        /// <summary>
        /// replace cac tag thanh text
        /// 8 => BeginString
        /// 9 => BodyLength
        /// 35 => MsgType
        /// ........
        /// </summary>
        /// <param name="strInput"></param>
        /// <returns></returns>
        public string ReplaceTags(string strMessage, ref string strType, ref string strORCL, ref string[] arrPart ,REPLACE_TAGS RT)
        {
            try
            {
                string strResult = "";
                Dictionary<int, string> dic = null;                     // dic tuy thuoc vao tung type                
                Dictionary<int, string> dicOracle = null;
                arrPart = strMessage.Split((char)1);           // cac thong tin dc cach nhau bang ky tu co ma ascii=1 (ko nhin thay dc trong 1 so editor nhu NotePad, phai dung NotePad++ moi thay duoc)
                StringBuilder sb = new StringBuilder(strMessage, strMessage.Length * 2); // khai bao string builder va phan chia mem
                StringBuilder sbOracle = new StringBuilder(strMessage, strMessage.Length * 2);
                string strSeparator = ((char)1).ToString();
                // xac dinh type
                strType = arrPart[2].Substring(3);       // "35=BI"=> "BI"

                
                // khong the dung chung 1 dic duoc vi number co the giong nhau
                // VD:
                // type SI co 15=StockNo
                // type BI co 15=MarketIndex
                switch (strType)
                {
                    case MSG_TYPE_INDEX:      // "I";
                        dic = this.m_dicI;
                        break;
                    case MSG_TYPE_BASKET_INDEX:       // "S";
                        dic = this.m_dicS;
                        break;
                    case MSG_TYPE_STOCK_INFO:         // "SI";
                        dic = this.m_dicSI;
                        break;
                    case MSG_TYPE_TOP_N_PRICE:        // "TP";
                        dic = this.m_dicTP;
                        break;
                    case MSG_TYPE_BOARD_INFO:         // "BI";
                        dic = this.m_dicBI;
                        break;
                    case MSG_TYPE_AUTION_MATCH:       // "EP";
                        dic = this.m_dicEP;
                        break;
                    case MSG_TYPE_TOP_PRICE_ODDLOT:   // "PO";
                        dic = this.m_dicPO;
                        break;
                    case MSG_TYPE_ETF_NET_VALUE:      // "IV";
                        dic = this.m_dicIV;
                        break;
                    case MSG_TYPE_ETF_TRACKING_ERROR: // "TE";
                        dic = this.m_dicTE;
                        break;
                    case MSG_TYPE_DERIVATIVES_INFO:   // "DI";
                        dic = this.m_dicDI;
                        dicOracle = this.m_dicDIO;
                        break;
                    case MSG_TYPE_DERIVATIVES_INFO_A_LOGIN:
                        dic = this.m_dicLogon;
                        break;


                    //<?xml version="1.0"?>..<string>8=HNX.TDS.1|9=56|35=A|49=HNX|52=00010101-00:00:00|58=Accept login|108=30|</string>
                    //16:49:41.421|QuoteBaseLib.BLL.CReaderBaseMQ_Message2JsonA_QuoteBaseLib.BLL.CReaderBaseMQ_ReplaceTags_|FOR_PUB
                    //Message		= Invalid character after parsing property name. Expected ':' but got: H. Path 'BlockBody[0].Body', line 1, position 62.
                    //QuoteBaseLib.BLL.CProcessorIG.EntryPoint(String strMessageBlock)|strMessageBlock={"BlockHeader":"DATA","BlockBody":[{"Header":{},"Body":{"}},{"Header":{"f8":"HNX.TDS.1","f9":"134","f35":"PO","f49":"HNX","f52":"201603
                    default:
                        return strResult;
                }
                // them strSeparator vao dau tien
                sb.Insert(0, strSeparator);
                // replace moi ky tu so thanh keyword
                foreach (int k in dic.Keys)
                {
                    if (RT == REPLACE_TAGS.FOR_DB)
                        sb.Replace(strSeparator + k.ToString() + "=", strSeparator + dic[k] + "=");
                    if (RT == REPLACE_TAGS.FOR_PUB)
                        sb.Replace(strSeparator + k.ToString() + "=", strSeparator + "f" + k.ToString() + "=");
                }
                if (dicOracle != null)
                {
                    sbOracle.Length -= 1;
                    sbOracle.Insert(0, strSeparator);
                    foreach (var k in dicOracle.Keys)
                    {
                        sbOracle.Replace(strSeparator + k.ToString() + "=", strSeparator + dicOracle[k] + "=");
                        if (sbOracle.ToString().IndexOf(dicOracle[k]) <= -1)
                        {
                            sbOracle.Append(strSeparator + dicOracle[k] + "=");
                        }
                    }
                    // xoa strSeparator cho dau tien
                    sbOracle.Remove(0, 1);

                    sbOracle.Replace("oracle", "");

                    strORCL = sbOracle.ToString();
                }
                // xoa strSeparator cho dau tien
                sb.Remove(0, 1);
                
                // output ref string
                strResult = sb.ToString();
                //Xử lý cho SQL
                // strSQL = BeginString=HNX.TDS.1@BodyLength=694@MsgType=SI@SenderCompID=HNX@SendingTime=20241007-10:04:33@IDSymbol=2724@Symbol=PVS@SecurityType=ST@IssueDate=00010101-12:01:00@CeilingPrice=45600@FloorPrice=37400@SecurityTradingStatus=0@BasicPrice=41500@BestBidPrice=41800@BestBidQtty=56900@BestOfferQtty=95600@BestOfferPrice=41900@TotalBidQtty=2270500.000000@TotalOfferQtty=4515500.000000@MatchQtty=100@MatchPrice=41800@TotalVolumeTraded=1292004.000000@TotalValueTraded=54269221100.000000@BidCount=902@NM_TotalTradedValue=54248060000.000000@BoardCode=LIS_BRD_01@TotalBuyTradingValue=54269221100.000000@TotalBuyTradingQtty=1292004.000000@TotalSellTradingValue=54269221100.000000@TotalSellTradingQtty=1292004.000000@BuyForeignQtty=5200.000000@BuyForeignValue=218040000.000000@RemainForeignQtty=127702116@BuyCount=869@SellCount=869@Parvalue=10000@OpenPrice=37400@PriorOpenPrice=40800@PriorClosePrice=41500@Tradingdate=20241007@Time=10:04:33@TradingUnit=100@TotalListingQtty=477966290.000000@DateNo=4260@MatchValue=4180000.000000@HighestPice=42200@LowestPrice=37400@NM_TotalTradedQtty=1291500.000000@ReferenceStatus=0@TradingSessionID=LIS_CON_NML@TradSesStatus=1@OfferCount=1776@ListingStatus=0@TotalBidQtty_OD=825.000000@TotalOfferQtty_OD=1297.000000@
                //strSQL = Convert2SQL(strResult, strType);

                return strResult;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";                
            }
        }
        private string Convert2SQL(string strCSV, string strType)
        {
            try
            {
                string strSQl = "";
                StringBuilder sbSQL = new StringBuilder(strCSV,strCSV.Length * 2);
                sbSQL.Replace(((char)1).ToString(), ((char)1).ToString() + "@");
                string strSPname = TEMPLATE_SP_S5G_HNX_SAVER.Replace("(Type)", strType);
                sbSQL.Insert(0, strSPname + " @");
                strSQl = sbSQL.ToString();

                return strSQl;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }
        /// <summary>
        /// khoi tao cac gia tri cho dic
        /// chi goi 1 lan 
        /// </summary>
        /// <param name="strType"></param>
        public Dictionary<int, string> InitDic(string strType)
        {
            try
            {
                Dictionary<int, string> dic = new Dictionary<int, string>();                     // dic tuy thuoc vao tung type

                switch (strType)
                {
                    case MSG_TYPE_INDEX:      // "I";
                        //'1.'Private Const MSG_TYPE_MESSAGE_INDEX$ = "35=I"                  
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(1, "IDIndex");
                        dic.Add(2, "IndexCode");
                        dic.Add(3, "Value");
                        dic.Add(4, "CalTime");
                        dic.Add(5, "Change");
                        dic.Add(6, "RatioChange");
                        dic.Add(7, "TotalQtty");
                        dic.Add(14, "TotalValue");
                        dic.Add(19, "TradingDate");
                        dic.Add(21, "CurrentStatus");
                        dic.Add(22, "TotalStock");
                        dic.Add(23, "PriorIndexVal");
                        dic.Add(24, "HighestIndex");
                        dic.Add(25, "LowestIndex");
                        dic.Add(26, "CloseIndex");
                        dic.Add(27, "TypeIndex");
                        dic.Add(18, "IndexName");
                        break;
                    case MSG_TYPE_BASKET_INDEX:       // "S";
                        //'2.'Private Const MSG_TYPE_BASKET_INDEX$ = "35=S"                   
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(1, "IDIndex");
                        dic.Add(2, "IndexCode");
                        dic.Add(15, "IDSymbol");
                        dic.Add(55, "Symbol");
                        dic.Add(11, "TotalQtty");
                        dic.Add(12, "Weighted");
                        dic.Add(28, "AddDate");
                        break;
                    case MSG_TYPE_STOCK_INFO:         // "SI";
                        //'3.'Private Const MSG_TYPE_STOCK_INFO$ = "35=SI"                    
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(55, "Symbol");
                        dic.Add(15, "IDSymbol");
                        dic.Add(425, "BoardCode");
                        dic.Add(336, "TradingSessionID");
                        dic.Add(340, "TradSesStatus");
                        dic.Add(326, "SecurityTradingStatus");
                        dic.Add(327, "ListingStatus");
                        dic.Add(167, "SecurityType");
                        dic.Add(225, "IssueDate");
                        dic.Add(106, "Issuer");
                        dic.Add(107, "SecurityDesc");
                        dic.Add(132, "BestBidPrice");
                        dic.Add(1321, "BestBidQtty");
                        dic.Add(133, "BestOfferPrice");
                        dic.Add(1331, "BestOfferQtty");
                        dic.Add(134, "TotalBidQtty");
                        dic.Add(135, "TotalOfferQtty");
                        dic.Add(260, "BasicPrice");
                        dic.Add(333, "FloorPrice");
                        dic.Add(332, "CeilingPrice");
                        dic.Add(334, "Parvalue");
                        dic.Add(31, "MatchPrice");
                        dic.Add(32, "MatchQtty");
                        dic.Add(137, "OpenPrice");
                        dic.Add(138, "PriorOpenPrice");
                        dic.Add(139, "ClosePrice");
                        dic.Add(140, "PriorClosePrice");
                        dic.Add(387, "TotalVolumeTraded");
                        dic.Add(3871, "TotalValueTraded");
                        dic.Add(631, "MidPx");
                        dic.Add(388, "Tradingdate");
                        dic.Add(399, "Time");
                        dic.Add(400, "TradingUnit");
                        dic.Add(109, "TotalListingQtty");
                        dic.Add(17, "DateNo");
                        dic.Add(230, "AdjustQtty");
                        dic.Add(232, "ReferenceStatus");
                        dic.Add(233, "AdjustRate");
                        dic.Add(244, "DividentRate");
                        dic.Add(255, "CurrentPrice");
                        dic.Add(2551, "CurrentQtty");
                        dic.Add(266, "HighestPice");
                        dic.Add(2661, "LowestPrice");
                        dic.Add(277, "PriorPrice");
                        dic.Add(310, "MatchValue");
                        dic.Add(320, "OfferCount");
                        dic.Add(321, "BidCount");
                        dic.Add(391, "NM_TotalTradedQtty");
                        dic.Add(392, "NM_TotalTradedValue");
                        dic.Add(393, "PT_MatchQtty");
                        dic.Add(3931, "PT_MatchPrice");
                        dic.Add(394, "PT_TotalTradedQtty");
                        dic.Add(3941, "PT_TotalTradedValue");
                        dic.Add(395, "TotalBuyTradingQtty");
                        dic.Add(3951, "BuyCount");
                        dic.Add(3952, "TotalBuyTradingValue");
                        dic.Add(396, "TotalSellTradingQtty");
                        dic.Add(3961, "SellCount");
                        dic.Add(3962, "TotalSellTradingValue");
                        dic.Add(397, "BuyForeignQtty");
                        dic.Add(3971, "BuyForeignValue");
                        dic.Add(398, "SellForeignQtty");
                        dic.Add(3981, "SellForeignValue");
                        dic.Add(3301, "RemainForeignQtty");
                        dic.Add(541, "MaturityDate");
                        dic.Add(223, "CouponRate");
                        dic.Add(1341, "TotalBidQtty_OD");
                        dic.Add(1351, "TotalOfferQtty_OD");
                        dic.Add(13, "fx13");        // MatchChange
                        dic.Add(311, "fx311");        // gia khop gan nhat
                                                      //dic.Add("fx13",  "13");		// MC  = ????? [tu tinh]
#if USE_IG_SI_PT_FL_CE
                        dic.Add(3331, "FloorPricePT");      // Giá sàn cho giao dịch thỏa thuận ngoài biên độ (nghiệp vụ)
                        dic.Add(3321, "CeilingPricePT");   // Giá trần cho giao dịch thỏa thuận ngoài biên độ (nghiệp vụ)
#endif
                        break;
                    case MSG_TYPE_TOP_N_PRICE:        // "TP";
                        //'4.'Private Const MSG_TYPE_TOP_N_PRICE$ = "35=TP"                   
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(55, "Symbol");
                        dic.Add(425, "BoardCode");
                        dic.Add(555, "NoTopPrice");
                        dic.Add(556, "NumTopPrice");
                        dic.Add(132, "BestBidPrice");
                        dic.Add(1321, "BestBidQtty");
                        dic.Add(133, "BestOfferPrice");
                        dic.Add(1331, "BestOfferQtty");
                        break;
                    case MSG_TYPE_BOARD_INFO:         // "BI";
                        //'5.'Private Const MSG_TYPE_BOARD_INFO$ = "35=BI"            
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------

                        dic.Add(425, "BoardCode");
                        dic.Add(426, "BoardStatus");
                        dic.Add(336, "TradingSessionID");
                        dic.Add(340, "TradSesStatus");
                        dic.Add(421, "Name");
                        dic.Add(422, "Shortname");
                        dic.Add(388, "Tradingdate");
                        dic.Add(399, "Time");
                        dic.Add(270, "TotalTrade");
                        dic.Add(250, "TotalStock");
                        dic.Add(251, "numSymbolAdvances");
                        dic.Add(252, "numSymbolNochange");
                        dic.Add(253, "numSymbolDeclines");
                        dic.Add(17, "DateNo");
                        dic.Add(220, "totalNormalTradedQttyRd");
                        dic.Add(221, "totalNormalTradedValueRd");
                        dic.Add(210, "totalNormalTradedQttyOd");
                        dic.Add(211, "totalNormalTradedValueOd");
                        dic.Add(240, "totalPTTradedQtty");
                        dic.Add(241, "totalPTTradedValue");
                        dic.Add(341, "f341");
                        break;
                    case MSG_TYPE_AUTION_MATCH:       // "EP";
                        //'6.'Private Const MSG_TYPE_AUTION_MATCH$ = "35=EP"          
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(33, "ActionType");
                        dic.Add(55, "Symbol");
                        dic.Add(31, "Price");
                        dic.Add(32, "Qtty");
                        dic.Add(336, "TradingSessionID");
                        break;
                    case MSG_TYPE_TOP_PRICE_ODDLOT:   // "PO";
                        //'7.'Private Const MSG_TYPE_TOP_PRICE_ODDLOT$ = "35=PO"
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //''------------------------------
                        dic.Add(55, "Symbol");
                        dic.Add(425, "BoardCode");
                        dic.Add(132, "BidPrice_1");
                        dic.Add(1321, "BidQtty_1");
                        dic.Add(133, "BidPrice_2");
                        dic.Add(1331, "BidQtty_2");
                        dic.Add(134, "BidPrice_3");
                        dic.Add(1341, "BidQtty_3");
                        dic.Add(135, "OfferPrice_1");
                        dic.Add(1351, "OfferQtty_1");
                        dic.Add(136, "OfferPrice_2");
                        dic.Add(1361, "OfferQtty_2");
                        dic.Add(137, "OfferPrice_3");
                        dic.Add(1371, "OfferQtty_3");
                        break;
                    case MSG_TYPE_ETF_NET_VALUE:      // "IV";
                        //'8.'Private Const MSG_TYPE_ETF_NET_VALUE$ = "35=IV"         '8.
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(56, "CODE");
                        dic.Add(57, "TIME");
                        dic.Add(58, "INAV");
                        break;
                    case MSG_TYPE_ETF_TRACKING_ERROR: // "TE";
                        //'9.'Private Const MSG_TYPE_ETF_TRACKING_ERROR$ = "35=TE"    '9.
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(56, "CODE");
                        dic.Add(59, "WEEK");
                        dic.Add(60, "DATE");
                        dic.Add(61, "TE_VALUE");
                        break;
                    case MSG_TYPE_DERIVATIVES_INFO:         // "DI";
                        //MSG_TYPE_DERIVATIVES_INFO = "35=DI"
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(55, "Symbol");                    //2018-05-30 09:31:55 ngocta2
                        dic.Add(15, "SymbolID");
                        dic.Add(800, "Underlying");
                        dic.Add(425, "BoardCode");
                        dic.Add(336, "TradingSessionID");
                        dic.Add(340, "TradeSesStatus");
                        dic.Add(326, "SecurityTradingStatus");
                        dic.Add(327, "ListingStatus");
                        dic.Add(167, "SecurityType");
                        dic.Add(801, "OpenInterest");
                        dic.Add(8011, "OpenInterestChange");
                        dic.Add(802, "FirstTradingDate");
                        dic.Add(803, "LastTradingDate");
                        dic.Add(132, "BestBidPrice");
                        dic.Add(1321, "BestBidQtty");
                        dic.Add(133, "BestOfferPrice");
                        dic.Add(1331, "BestOfferQtty");
                        dic.Add(134, "TotalBidQtty");
                        dic.Add(135, "TotalOfferQtty");
                        dic.Add(260, "BasicPrice");
                        dic.Add(333, "FloorPrice");
                        dic.Add(332, "CeilingPrice");
                        dic.Add(31, "MatchPrice");
                        dic.Add(32, "MatchQtty");
                        dic.Add(137, "OpenPrice");
                        dic.Add(138, "PriorOpenPrice");
                        dic.Add(804, "OpenQtty");
                        dic.Add(139, "ClosePrice");
                        dic.Add(140, "PriorClosePrice");
                        dic.Add(805, "CloseQtty");
                        dic.Add(387, "TotalVolumeTraded");
                        dic.Add(3871, "TotalValueTraded");
                        dic.Add(388, "Tradingdate");
                        dic.Add(399, "Time");
                        dic.Add(400, "TradingUnit");
                        dic.Add(17, "DateNo");
                        dic.Add(255, "CurrentPrice");
                        dic.Add(2551, "CurrentQtty");
                        dic.Add(266, "HighestPrice");
                        dic.Add(2661, "LowestPrice");
                        dic.Add(310, "MatchValue");
                        dic.Add(320, "OfferCount");
                        dic.Add(321, "BidCount");
                        dic.Add(391, "NM_TotalTradedQtty");
                        dic.Add(392, "NM_TotalTradedValue");
                        dic.Add(393, "PT_MatchQtty");
                        dic.Add(3931, "PT_MatchPrice");
                        dic.Add(394, "PT_TotalTradedQtty");
                        dic.Add(3941, "PT_TotalTradedValue");
                        dic.Add(814, "NM_BuyForeignQtty");
                        dic.Add(815, "PT_BuyForeignQtty");
                        dic.Add(397, "BuyForeignQtty");
                        dic.Add(8141, "NM_BuyForeignValue");
                        dic.Add(8151, "PT_BuyForeignValue");
                        dic.Add(3971, "BuyForeignValue");
                        dic.Add(816, "NM_SellForeignQtty");
                        dic.Add(817, "PT_SellForeignQtty");
                        dic.Add(398, "SellForeignQtty");
                        dic.Add(8161, "NM_SellForeignValue");
                        dic.Add(8171, "PT_SellForeignValue");
                        dic.Add(3981, "SellForeignValue");
                        break;

                    case MSG_TYPE_DERIVATIVES_INFO_ORACLE:         // "DI";2018-07-20 12:26:56 hungpv

                        dic.Add(8, "-1oracle=BeginString");
                        dic.Add(9, "-1oracle=BodyLength");
                        dic.Add(35, "-1oracle=MsgType");
                        dic.Add(49, "-1oracle=SenderCompID");
                        dic.Add(52, "-1oracle=SendingTime");
                        //'------------------------------
                        dic.Add(55, "0oracle=p_Symbol");
                        dic.Add(15, "1oracle=p_SymbolID");
                        dic.Add(800, "2oracle=p_Underlying");
                        dic.Add(425, "3oracle=p_BoardCode");
                        dic.Add(336, "4oracle=p_TradingSessionID");
                        dic.Add(340, "5oracle=p_TradeSesStatus");
                        dic.Add(326, "6oracle=p_SecurityTradingStatus");
                        dic.Add(327, "7oracle=p_ListingStatus");
                        dic.Add(167, "8oracle=p_SecurityType");
                        dic.Add(801, "9oracle=p_OpenInterest");
                        dic.Add(8011, "10oracle=p_OpenInterestChange");
                        dic.Add(802, "11oracle=p_FirstTradingDate");
                        dic.Add(803, "12oracle=p_LastTradingDate");
                        dic.Add(132, "13oracle=p_BestBidPrice");
                        dic.Add(1321, "14oracle=p_BestBidQtty");
                        dic.Add(133, "15oracle=p_BestOfferPrice");
                        dic.Add(1331, "16oracle=p_BestOfferQtty");
                        dic.Add(134, "17oracle=p_TotalBidQtty");
                        dic.Add(135, "18oracle=p_TotalOfferQtty");
                        dic.Add(260, "19oracle=p_BasicPrice");
                        dic.Add(333, "20oracle=p_FloorPrice");
                        dic.Add(332, "21oracle=p_CeilingPrice");
                        dic.Add(31, "22oracle=p_MatchPrice");
                        dic.Add(32, "23oracle=p_MatchQtty");
                        dic.Add(137, "24oracle=p_OpenPrice");
                        dic.Add(138, "25oracle=p_PriorOpenPrice");
                        dic.Add(804, "26oracle=p_OpenQtty");
                        dic.Add(139, "27oracle=p_ClosePrice");
                        dic.Add(140, "28oracle=p_PriorClosePrice");
                        dic.Add(805, "29oracle=p_CloseQtty");
                        dic.Add(387, "30oracle=p_TotalVolumeTraded");
                        dic.Add(3871, "31oracle=p_TotalValueTraded");
                        dic.Add(388, "32oracle=p_Tradingdate");
                        dic.Add(399, "33oracle=p_Time");
                        dic.Add(400, "34oracle=p_TradingUnit");
                        dic.Add(17, "35oracle=p_DateNo");
                        dic.Add(255, "36oracle=p_CurrentPrice");
                        dic.Add(2551, "37oracle=p_CurrentQtty");
                        dic.Add(266, "38=oraclep_HighestPrice");
                        dic.Add(2661, "39oracle=p_LowestPrice");
                        dic.Add(310, "40oracle=p_MatchValue");
                        dic.Add(320, "41oracle=p_OfferCount");
                        dic.Add(321, "42oracle=p_BidCount");
                        dic.Add(391, "43oracle=p_NM_TotalTradedQtty");
                        dic.Add(392, "44oracle=p_NM_TotalTradedValue");
                        dic.Add(393, "45oracle=p_PT_MatchQtty");
                        dic.Add(3931, "46oracle=p_PT_MatchPrice");
                        dic.Add(394, "47oracle=p_PT_TotalTradedQtty");
                        dic.Add(3941, "48oracle=p_PT_TotalTradedValue");
                        dic.Add(814, "49oracle=p_NM_BuyForeignQtty");
                        dic.Add(815, "50oracle=p_PT_BuyForeignQtty");
                        dic.Add(397, "51oracle=p_BuyForeignQtty");
                        dic.Add(8141, "52oracle=p_NM_BuyForeignValue");
                        dic.Add(8151, "53oracle=p_PT_BuyForeignValue");
                        dic.Add(3971, "54oracle=p_BuyForeignValue");
                        dic.Add(816, "55oracle=p_NM_SellForeignQtty");
                        dic.Add(817, "56oracle=p_PT_SellForeignQtty");
                        dic.Add(398, "57oracle=p_SellForeignQtty");
                        dic.Add(8161, "58oracle=p_NM_SellForeignValue");
                        dic.Add(8171, "59oracle=p_PT_SellForeignValue");
                        dic.Add(3981, "60oracle=p_SellForeignValue");
                        break;
                    case MSG_TYPE_DERIVATIVES_INFO_A_LOGIN:      // "I";
                        //'1.'Private Const MSG_TYPE_MESSAGE_INDEX$ = "35=I"                  
                        dic.Add(8, "BeginString");
                        dic.Add(9, "BodyLength");
                        dic.Add(35, "MsgType");
                        dic.Add(49, "SenderCompID");
                        dic.Add(52, "SendingTime");
                        //'------------------------------
                        dic.Add(58, "Logon_Infor");
                        break;


                }

                return dic;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return null;
            }

        }
        /// <summary>
        ///  update dictionary: 
        ///  + them moi key neu chua co
        ///  + cap nhat key neu da co
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="strKey"></param>
        /// <param name="strValue"></param>
        /// <returns></returns>
        protected bool Dic_AddOrUpdate(ref Dictionary<string, IG_BI_FULL> dic, string strKey, IG_BI_FULL strValue)
        {
            try
            {
                IG_BI_FULL bi_out = new IG_BI_FULL();

                // xu ly dic
                if (dic.TryGetValue(strKey, out bi_out))
                    dic[strKey] = strValue; // update existing key
                else
                    dic.Add(strKey, strValue);// add new key

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }

        protected bool Dic_AddOrUpdate(ref Dictionary<string, string> dic, string strKey, string strValue)
        {
            try
            {
                string strOut = "";

                // xu ly dic
                if (dic.TryGetValue(strKey, out strOut))
                    dic[strKey] = strValue; // update existing key
                else
                    dic.Add(strKey, strValue);// add new key

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        private StringBuilder SQLParam2ShortProp(StringBuilder sbCSVParam)
        {
            try
            {
                return sbCSVParam
                    //SendingTime
                    .Replace("SendingTime", "STime")

                    //SI
                    .Replace("MidPx", "AP")
                    .Replace("BoardCode", "BoC")
                    .Replace("BidCount", "BiC")
                    .Replace("BuyCount", "BuC")
                    .Replace("BuyForeignQtty", "BFQ")
                    .Replace("BuyForeignValue", "BFV")
                    .Replace("BasicPrice", "BP")
                    .Replace("CeilingPrice", "CeP")
                    .Replace("ClosePrice", "ClP")
                    .Replace("CurrentPrice", "Cp")
                    .Replace("CurrentQtty", "CQ")
                    .Replace("FloorPrice", "FP")
                    .Replace("HighestPice", "HP")
                    .Replace("IDSymbol", "ID")
                    .Replace("ListingStatus", "LS")
                    .Replace("LowestPrice", "LP")
                    .Replace("MatchPrice", "MP")
                    .Replace("MatchQtty", "MQ")
                    .Replace("NM_TotalTradedQtty", "NMTTQ")
                    .Replace("NM_TotalTradedValue", "NMTTV")
                    .Replace("OfferCount", "OC")
                    .Replace("OpenPrice", "OP")
                    .Replace("PT_MatchPrice", "PTMP")
                    .Replace("PT_MatchQtty", "PTMQ")
                    .Replace("PT_TotalTradedQtty", "PTTTQ")
                    .Replace("PT_TotalTradedValue", "PTTTV")
                    .Replace("RemainForeignQtty", "RFQ")
                    .Replace("ReferenceStatus", "RS")
                    .Replace("SellForeignQtty", "SFQ")
                    .Replace("SellForeignValue", "SFV")
                    .Replace("SecurityType", "ST")
                    .Replace("SecurityTradingStatus", "STS")
                    .Replace("Symbol", "Sym")
                    .Replace("TotalBidQtty", "TBQ")
                    .Replace("TotalBidQtty_OD", "TBQOD")
                    .Replace("TotalBuyTradingQtty", "TBTQ")
                    .Replace("TotalBuyTradingValue", "TBTV")
                    .Replace("Tradingdate", "TD")
                    .Replace("Time", "Time")
                    .Replace("TotalListingQtty", "TLQ")
                    .Replace("TotalOfferQtty", "TOQ")
                    .Replace("TotalOfferQtty_OD", "TOQOD")
                    .Replace("TotalValueTraded", "TVaT")
                    .Replace("TotalVolumeTraded", "TVoT")
                    .Replace("TradingSessionID", "TSID")
                    .Replace("TradSesStatus", "TSS")

                    //TP
                    .Replace("NoTopPrice", "NTP")
                    .Replace("BestBidPrice", "BBP")
                    .Replace("BestBidQtty", "BBQ")
                    .Replace("BestOfferPrice", "BOP")
                    .Replace("BestOfferQtty", "BOQ");
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return sbCSVParam;
            }
        }
        /// <summary>
        /// can bo het ".000000"
        //"TVoT": "62600.000000",
        //"TVaT": "413160000.000000",
        //"BiC": "58",
        //"NMTTV": "413160000.000000",
        //"TBTV": "413160000.000000",
        //"TBTQ": "62600.000000",
        /// </summary>
        /// <param name="sbLongVal"></param>
        /// <returns></returns>
        private StringBuilder LongVal2ShortVal(StringBuilder sbLongVal)
        {
            try
            {
                return sbLongVal
                    //SendingTime
                    .Replace(".000000", "");
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return sbLongVal;
            }
        }

        /// <summary>
        /// xoa header cho string
        /// </summary>
        /// <param name="strInput">JSON/SQL</param>
        /// <param name="strPrefix">@/"</param>
        /// <returns></returns>
        private string RemoveHeader(string strInput, string strPrefix)
        {
            try
            {
                string strOutput = strInput;

                // xoa bot cac param ko can thiet
                // @BeginString='HNX.TDS.1',@BodyLength='345',@MsgType='TP',@SenderCompID='HNX',@SendingTime='20150922-10:45:21',
                // @NumTopPrice='1',
                // @NumTopPrice='2',
                // ......
                strOutput = Regex.Replace(strOutput, strPrefix + "BeginString.*?,", "");
                strOutput = Regex.Replace(strOutput, strPrefix + "BodyLength.*?,", "");
                //strOutput = Regex.Replace(strOutput, strPrefix + "MsgType.*?,", "");
                strOutput = Regex.Replace(strOutput, strPrefix + "SenderCompID.*?,", "");
                //strOutput = Regex.Replace(strOutput, strPrefix + "SendingTime.*?,", "");

                strOutput = Regex.Replace(strOutput, strPrefix + "f8\".*?,", "");
                strOutput = Regex.Replace(strOutput, strPrefix + "f9\".*?,", "");
                //strOutput = Regex.Replace(strOutput, strPrefix + "f35\".*?,", "");
                strOutput = Regex.Replace(strOutput, strPrefix + "f49\".*?,", "");
                strOutput = Regex.Replace(strOutput, strPrefix + "f52\".*?,", "");  // 2017-02-09 14:16:14 ngocta2 => Can not add Newtonsoft.Json.Linq.JValue to Newtonsoft.Json.Linq.JObject => double "f52" prop => can phai xoa "f52" trong Body

                return strOutput;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }
        // <summary>
        ///  lay gia tri tu dic theo key, neu ko co value tu key do thi return default value
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="strKey"></param>
        /// <param name="strDefaultValue"></param>
        /// <returns></returns>
        protected string Dic_GetValue(Dictionary<string, string> dic, string strKey, string strDefaultValue)
        {
            try
            {
                // ko co data nao trong dic , return luon default value
                if (dic.Count == 0)
                    return strDefaultValue;

                string strOut = "";

                // test co value trong dic ko
                if (dic.TryGetValue(strKey, out strOut))
                    return dic[strKey]; // get value from existing key
                else
                    return strDefaultValue;// return strDefaultValue
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }
    }        
}

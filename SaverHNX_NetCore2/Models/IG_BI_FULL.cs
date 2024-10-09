using Newtonsoft.Json;

namespace SaverHNX_NetCore2.Models
{
    public class IG_BI_FULL
    {
        public string BeginString { get; set; }

        public string BodyLength { get; set; } 

        public string MsgType { get; set; }

        public string SenderCompID { get; set; } 

        public string SendingTime { get; set; } 

        public string Name { get; set; } 

        public string Shortname { get; set; }

        public string numSymbolAdvances { get; set; } 

        public string numSymbolDeclines { get; set; }

        public string numSymbolNochange { get; set; } 

        public string totalNormalTradedQttyOd { get; set; } 

        public string totalNormalTradedValueOd { get; set; } 

        public string totalNormalTradedQttyRd { get; set; } 

        public string totalNormalTradedValueRd { get; set; }   

        public string totalPTTradedQtty { get; set; } 

        public string totalPTTradedValue { get; set; } 

        public string TotalTrade { get; set; } 

        public string DateNo { get; set; } 

        public string BoardCode { get; set; }

        public string BoardStatus { get; set; }

        public string Tradingdate { get; set; } 

        public string Time { get; set; } 

        public string TotalStock { get; set; } 

        public string TradingSessionID { get; set; } 

        public string TradSesStatus { get; set; } 

        public string f341 { get; set; } 
    }
}

using SaverHNX_NetCore2.BLL;
using SaverHNX_NetCore2.Extensions;
using SaverHNX_NetCore2.Settings;

namespace SaverHNX_NetCore2.DAL
{
    public class CDatabase
    {
        private string CONNECTION_STRING_68 = "";
        private int SQL_COMMAND_TIMEOUT = 0;
        public CDatabase(SaverSetting _savarSetting) 
        {
            CONNECTION_STRING_68 = _savarSetting.CONNECTION_STRING_68;
            SQL_COMMAND_TIMEOUT = _savarSetting.SQL_COMMAND_TIMEOUT;
        }
        public bool ExecuteScriptQuoteSaverHNX(string strSQL)
        {
            try
            {
                // ko co SQL script thi quit
                if (string.IsNullOrEmpty(strSQL))
                    return false;

                // -----------------------------------------------------
                // code dung tam thoi chua chay, ko cho exec sp prc_S5G_HNX_SAVER_IG_DI_UPDATE vi dang sai phan sp
                // doc nham spec, viet sp co 10 param, nhung thuc te co 50 param >>> chet 
                // Message		= Procedure or function prc_S5G_HNX_SAVER_IG_DI_UPDATE has too many arguments specified.
                //bool foundMatch = Regex.IsMatch(strSQL, "prc_S5G_HNX_SAVER_IG_DI_UPDATE", RegexOptions.Multiline);
                //if (foundMatch)
                //    return false; // ko exec sp >> tranh server bi do 
                // KHONG DUOC, phai replace xoa dong DI va van tiep tuc exec script block nay
                //------------------------
                /*
                BEGIN TRANSACTION
                EXEC prc_S5G_HNX_SAVER_IG_SI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='621',@MsgType='SI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@IDSymbol='3284',@Symbol='VCS',@SecurityType='ST',@IssueDate='00010101-12:01:00',@CeilingPrice='241000',@FloorPrice='197200',@SecurityTradingStatus='0',@BasicPrice='219100',@BestBidPrice='215600',@BestBidQtty='500',@BestOfferQtty='100',@BestOfferPrice='217800',@TotalBidQtty='10400.000000',@TotalOfferQtty='20000.000000',@MatchQtty='100',@MatchPrice='218100',@TotalVolumeTraded='100.000000',@TotalValueTraded='21810000.000000',@BidCount='47',@NM_TotalTradedValue='21810000.000000',@BoardCode='LIS_BRD_01',@TotalBuyTradingValue='21810000.000000',@TotalBuyTradingQtty='100.000000',@TotalSellTradingValue='21810000.000000',@TotalSellTradingQtty='100.000000',@RemainForeignQtty='37169201',@BuyCount='1',@SellCount='1',@Parvalue='10000',@OpenPrice='218100',@PriorOpenPrice='225000',@PriorClosePrice='219100',@Tradingdate='20180222',@Time='09:18:10',@TradingUnit='100',@TotalListingQtty='80000000.000000',@DateNo='2538',@MatchValue='21810000.000000',@HighestPice='218100',@LowestPrice='218100',@NM_TotalTradedQtty='100.000000',@ReferenceStatus='0',@TradingSessionID='LIS_CON_NML',@TradSesStatus='1',@OfferCount='79',@ListingStatus='0',@TotalBidQtty_OD='198.000000',@TotalOfferQtty_OD='50.000000'
                EXEC prc_S5G_HNX_SAVER_IG_BI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='311',@MsgType='BI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@Name='LIS_BRD_01',@Shortname='LIS_BRD_01',@numSymbolAdvances='27',@numSymbolDeclines='40',@numSymbolNochange='38',@totalNormalTradedQttyOd='514.000000',@totalNormalTradedValueOd='13259000.000000',@totalNormalTradedQttyRd='4998800.000000',@totalNormalTradedValueRd='79873250000.000000',@totalPTTradedQtty='188210.000000',@totalPTTradedValue='7547221000.000000',@TotalTrade='1712.000000',@DateNo='3059',@BoardCode='LIS_BRD_01',@BoardStatus='A',@Tradingdate='20180222',@Time='09:18:10',@TotalStock='105',@TradingSessionID='LIS_CON_NML',@TradSesStatus='1',@f341='LIS'                
                EXEC prc_S5G_HNX_SAVER_IG_DI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='488',@MsgType='DI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@55='VN30F1803',@15='1085',@TradingSessionID='OPEN',@TradSesStatus='A',@SecurityTradingStatus='A',@ListingStatus='A',@SecurityType='FU',@BoardCode='DER_BRD_01',@800='VN30',@801='7457',@802='10/08/2017',@803='15/03/2018',@132='1072.30',@1321='7',@133='1072.40',@1331='3',@134='2570',@135='2464',@260='1082',@333='1006.30',@332='1157.70',@31='1072.10',@32='5',@137='1071.50',@138='1074.20',@140='1082',@804='539',@387='2084',@3871='223480070000',@388='20180222',@399='09:17:45',@400='1',@17='134',@266='1074.10',@2661='1071',@310='536050000',@320='705',@321='717',@391='2084',@392='223480070000',@816='10',@398='10',@8161='1073000000',@3981='1073000000'
                EXEC prc_S5G_HNX_SAVER_IG_BI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='267',@MsgType='BI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@Name='UPC_BRD_01',@Shortname='UPC_BRD_01',@numSymbolAdvances='28',@numSymbolDeclines='25',@numSymbolNochange='12',@totalNormalTradedQttyOd='200.000000',@totalNormalTradedValueOd='4316900.000000',@totalNormalTradedQttyRd='723300.000000',@totalNormalTradedValueRd='18218330000.000000',@TotalTrade='742.000000',@DateNo='2166',@BoardCode='UPC_BRD_01',@BoardStatus='A',@Tradingdate='20180222',@Time='09:18:10',@TotalStock='71',@TradingSessionID='UPC_CON_NML',@TradSesStatus='1',@f341='UPC'
                EXEC prc_S5G_HNX_SAVER_IG_SI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='614',@MsgType='SI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@IDSymbol='75588',@Symbol='NAS',@SecurityType='ST',@IssueDate='00010101-12:01:00',@CeilingPrice='36900',@FloorPrice='27300',@SecurityTradingStatus='0',@BasicPrice='32100',@BestBidPrice='32000',@BestBidQtty='2900',@BestOfferQtty='11400',@BestOfferPrice='32500',@TotalBidQtty='3600.000000',@TotalOfferQtty='13500.000000',@MatchQtty='2000',@MatchPrice='32000',@TotalVolumeTraded='2100.000000',@TotalValueTraded='67200000.000000',@BidCount='3',@NM_TotalTradedValue='67200000.000000',@BoardCode='UPC_BRD_01',@TotalBuyTradingValue='67200000.000000',@TotalBuyTradingQtty='2100.000000',@TotalSellTradingValue='67200000.000000',@TotalSellTradingQtty='2100.000000',@RemainForeignQtty='4000862',@BuyCount='2',@SellCount='2',@Parvalue='10000',@OpenPrice='32000',@PriorOpenPrice='32000',@PriorClosePrice='32200',@Tradingdate='20180222',@Time='09:18:10',@TradingUnit='100',@TotalListingQtty='8315764.000000',@DateNo='269',@MatchValue='64000000.000000',@HighestPice='32000',@LowestPrice='32000',@NM_TotalTradedQtty='2100.000000',@ReferenceStatus='0',@TradingSessionID='UPC_CON_NML',@TradSesStatus='1',@OfferCount='5',@ListingStatus='0',@TotalBidQtty_OD='396.000000',@TotalOfferQtty_OD='108.000000'                
                COMMIT TRANSACTION
                //------------------------
                BEGIN TRANSACTION
                EXEC prc_S5G_HNX_SAVER_IG_SI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='621',@MsgType='SI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@IDSymbol='3284',@Symbol='VCS',@SecurityType='ST',@IssueDate='00010101-12:01:00',@CeilingPrice='241000',@FloorPrice='197200',@SecurityTradingStatus='0',@BasicPrice='219100',@BestBidPrice='215600',@BestBidQtty='500',@BestOfferQtty='100',@BestOfferPrice='217800',@TotalBidQtty='10400.000000',@TotalOfferQtty='20000.000000',@MatchQtty='100',@MatchPrice='218100',@TotalVolumeTraded='100.000000',@TotalValueTraded='21810000.000000',@BidCount='47',@NM_TotalTradedValue='21810000.000000',@BoardCode='LIS_BRD_01',@TotalBuyTradingValue='21810000.000000',@TotalBuyTradingQtty='100.000000',@TotalSellTradingValue='21810000.000000',@TotalSellTradingQtty='100.000000',@RemainForeignQtty='37169201',@BuyCount='1',@SellCount='1',@Parvalue='10000',@OpenPrice='218100',@PriorOpenPrice='225000',@PriorClosePrice='219100',@Tradingdate='20180222',@Time='09:18:10',@TradingUnit='100',@TotalListingQtty='80000000.000000',@DateNo='2538',@MatchValue='21810000.000000',@HighestPice='218100',@LowestPrice='218100',@NM_TotalTradedQtty='100.000000',@ReferenceStatus='0',@TradingSessionID='LIS_CON_NML',@TradSesStatus='1',@OfferCount='79',@ListingStatus='0',@TotalBidQtty_OD='198.000000',@TotalOfferQtty_OD='50.000000'
                EXEC prc_S5G_HNX_SAVER_IG_BI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='311',@MsgType='BI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@Name='LIS_BRD_01',@Shortname='LIS_BRD_01',@numSymbolAdvances='27',@numSymbolDeclines='40',@numSymbolNochange='38',@totalNormalTradedQttyOd='514.000000',@totalNormalTradedValueOd='13259000.000000',@totalNormalTradedQttyRd='4998800.000000',@totalNormalTradedValueRd='79873250000.000000',@totalPTTradedQtty='188210.000000',@totalPTTradedValue='7547221000.000000',@TotalTrade='1712.000000',@DateNo='3059',@BoardCode='LIS_BRD_01',@BoardStatus='A',@Tradingdate='20180222',@Time='09:18:10',@TotalStock='105',@TradingSessionID='LIS_CON_NML',@TradSesStatus='1',@f341='LIS'                

                EXEC prc_S5G_HNX_SAVER_IG_BI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='267',@MsgType='BI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@Name='UPC_BRD_01',@Shortname='UPC_BRD_01',@numSymbolAdvances='28',@numSymbolDeclines='25',@numSymbolNochange='12',@totalNormalTradedQttyOd='200.000000',@totalNormalTradedValueOd='4316900.000000',@totalNormalTradedQttyRd='723300.000000',@totalNormalTradedValueRd='18218330000.000000',@TotalTrade='742.000000',@DateNo='2166',@BoardCode='UPC_BRD_01',@BoardStatus='A',@Tradingdate='20180222',@Time='09:18:10',@TotalStock='71',@TradingSessionID='UPC_CON_NML',@TradSesStatus='1',@f341='UPC'
                EXEC prc_S5G_HNX_SAVER_IG_SI_UPDATE @BeginString='HNX.TDS.1',@BodyLength='614',@MsgType='SI',@SenderCompID='HNX',@SendingTime='20180222-09:18:10',@IDSymbol='75588',@Symbol='NAS',@SecurityType='ST',@IssueDate='00010101-12:01:00',@CeilingPrice='36900',@FloorPrice='27300',@SecurityTradingStatus='0',@BasicPrice='32100',@BestBidPrice='32000',@BestBidQtty='2900',@BestOfferQtty='11400',@BestOfferPrice='32500',@TotalBidQtty='3600.000000',@TotalOfferQtty='13500.000000',@MatchQtty='2000',@MatchPrice='32000',@TotalVolumeTraded='2100.000000',@TotalValueTraded='67200000.000000',@BidCount='3',@NM_TotalTradedValue='67200000.000000',@BoardCode='UPC_BRD_01',@TotalBuyTradingValue='67200000.000000',@TotalBuyTradingQtty='2100.000000',@TotalSellTradingValue='67200000.000000',@TotalSellTradingQtty='2100.000000',@RemainForeignQtty='4000862',@BuyCount='2',@SellCount='2',@Parvalue='10000',@OpenPrice='32000',@PriorOpenPrice='32000',@PriorClosePrice='32200',@Tradingdate='20180222',@Time='09:18:10',@TradingUnit='100',@TotalListingQtty='8315764.000000',@DateNo='269',@MatchValue='64000000.000000',@HighestPice='32000',@LowestPrice='32000',@NM_TotalTradedQtty='2100.000000',@ReferenceStatus='0',@TradingSessionID='UPC_CON_NML',@TradSesStatus='1',@OfferCount='5',@ListingStatus='0',@TotalBidQtty_OD='396.000000',@TotalOfferQtty_OD='108.000000'                
                COMMIT TRANSACTION
                //------------------------
                */
                // 2018-05-30 15:24:01 ngocta2 bo comment de exec insert DI vao db
                // strSQL = Regex.Replace(strSQL, "EXEC prc_S5G_HNX_SAVER_IG_DI_UPDATE.*", "", RegexOptions.Multiline);
                // -----------------------------------------------------

                CSQL objSQL = new CSQL(CONNECTION_STRING_68);
                objSQL.Command.CommandTimeout = Convert.ToInt32(SQL_COMMAND_TIMEOUT);

                //  2018 - 05 - 31 15:13:18 hungpv
                //  bo ghi log, tranh ghi log SQL 2 lan, vi trong ExecuteScript lai ghi log SQL 1 lan nua
                //CLog.LogSQL("IG", strSQL);

                objSQL.ExecuteScript(strSQL);
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

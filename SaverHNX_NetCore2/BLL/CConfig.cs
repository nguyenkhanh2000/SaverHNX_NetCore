using SaverHNX_NetCore2.Extensions;
using SaverHNX_NetCore2.Settings;

namespace SaverHNX_NetCore2.BLL
{
    public class CConfig
    {
        private const string __DATETIME_FORMAT_1 = "yyyy-MM-dd HH:mm:ss.fff";
        public const string FORMAT_TIME_5 = "yyyyMMddHHmmssfff";
        public const string FORMAT_DATETIME_6 = "yyyy-MM-dd HH:mm:ss.fff";
        static public string DateTimeNow => DateTime.Now.ToString(__DATETIME_FORMAT_1);

        public const int intPeriod = 43830; //đủ time cho key sống 1 tháng

        //const CLog.cs
        public static string NO_LOG_SET = "0";
        public static string SINGLE_THREAD = "0";

        public static string TEMPLATE_LOG_DATA = "(Time)|(Title)|(Detail)";
        public static string LOG_EXT = ".txt";

        public const string PATTERN_FORMAT_NUMBER = "N00";           //10,352,772
        public const string PATTERN_FORMAT_NUMBER3 = "F02";           //772.00
        public const string PATTERN_FORMAT_NUMBER4 = "F03";           //772.000
        public const string PATTERN_FORMAT_NUMBER5 = "{0:C}";           // $1,234.00
        public const string PATTERN_FORMAT_NUMBER8 = "{0:N}";           // 100.00
        public const string CultureInfo_US = "en-US";
        public const string CultureInfo_VN = "vi-VN";
        public const string FORMAT_DATETIME_1 = "dd/MM/yyyy HH:mm";// (24/10/2012 15:36)
        public const string FORMAT_DATETIME_2 = "dd/MM/yyyy";// (24/10/2012)
        public const string FORMAT_TIME_1 = "HH:mm:ss";// (11:30:00)
        public const string FORMAT_TIME_2 = "dd/MM/yyyy HH:mm:ss";// (24/10/2012 11:30:00)

        public const string CHAR_CRLF = "\r\n";
        public const string CHAR_TAB = "\t";

        //Phan nay cua INI
        public const string INI_SECTION_REDIS = "REDIS";
        //SQL
        public const string SQL_EXEC = "EXEC ";
        public const string SQL_BEGIN_TRANSACTION = "BEGIN TRANSACTION";
        public const string SQL_COMMIT_TRANSACTION = "COMMIT TRANSACTION";

        // monitor
        public static string CHANNEL_MONITOR = "";
        public static string CHANNEL_COMMAND_HNX_VD = "";
        public static string CHANNEL_COMMAND_HNX_SAVER = "";  //ChannelCOMMANDHNXSAVER=S5G_COMMAND_HNX_SAVER
        public const string INI_KEY_CHANNELMONITOR = "ChannelMONITOR";  //ChannelMONITOR=S5G_MONITOR
        public const string INI_KEY_CHANNELCOMMAND_HNX_VD = "ChannelCOMMANDHNX";  //ChannelCOMMANDHNX=S5G_COMMAND_HNX        
        public const string INI_KEY_CHANNELCOMMAND_HNX_SAVER = "ChannelCOMMANDHNXSAVER";   //ChannelCOMMANDHNXSAVER=S5G_COMMAND_HNX_SAVER
        public const string COMMAND_ACTION_RESET = "RESET"; // reset tat ca var ve null, tuong duong restart IIS = tay
        public const string COMMAND_ACTION_DEBUG = "DEBUG"; // write all var name / var value vao file text log
        public const string COMMAND_ACTION_LOAD_REDIS_ENABLE = "LOAD_REDIS_ENABLE"; // cho load data tu redis
        public const string COMMAND_ACTION_LOAD_REDIS_DISABLE = "LOAD_REDIS_DISABLE"; // khong cho load data tu redis
        public const string COMMAND_ACTION_RELOAD_LAST_INDEX = "RELOAD_LAST_INDEX"; // load lai index tu REDIS : VNI, VNX
        //
        public static string TEMPLATE_KEY_CHART;
        public static string REDIS_LAST_INDEX_HA;
        public static string REDIS_KEY_FULL_ROW_HA;
        public static string REDIS_CHANNEL_IG_PRO;
        public static string REDIS_KEY_IG_HNX_LIST;
        //public static string REDIS_KEY_S5G_IG_LOGON = "";
        public static string REDIS_KEY_IG_HNX_LIST_DS;
        //
        public const string INI_SECTION_CHART_DATA = "CHART_DATA";
        public const string INI_KEY_TEMPLATE_KEY_CHART = "TemplateKeyChart";
        public const string INI_SECTION_SAVER = "SAVER";
        public const string INI_KEY_TEMPLATE_KEY_LAST_INDEX_HA = "KeyLastIndexHA";
        public const string INI_SECTION_GROUP_PT = "GROUP_PT";
        public const string INI_KEY_FULL_ROW_HA = "RedisKeyFullRowHA";
        public const string INI_SECTION_IG_REDIS = "IG_REDIS";
        public const string INI_KEY_LIST_DS = "KeyHNXListDS";
        public const string INI_KEY_LIST = "KeyHNXList";
        public const string INI_KEY_S5G_IG_LOGON = "KeyHNXLogon";//S5G_IG_LOGON
        // command
        public static string CHANNEL_S5G_COMMAND_HNX_SAVER = "S5G_COMMAND_HNX_SAVER";//ChannelCOMMANDHSXSAVER=S5G_COMMAND_HSX_SAVER
        public CConfig(SaverSetting _Setting) 
        {
            ReadIniFile(_Setting);
        }
        public static void ReadIniFile(SaverSetting _saverSetting)
        {
            try
            {
                CIniFile CIF = new CIniFile(_saverSetting.FILE_INI_TEMPLATE);

                CHANNEL_MONITOR = CIF.IniReadValue(CConfig.INI_SECTION_REDIS, CConfig.INI_KEY_CHANNELMONITOR);//ChannelMONITOR=S5G_MONITOR
                CHANNEL_COMMAND_HNX_VD = CIF.IniReadValue(CConfig.INI_SECTION_REDIS, CConfig.INI_KEY_CHANNELCOMMAND_HNX_VD); // ChannelCOMMANDHNX=S5G_COMMAND_HNX
                CHANNEL_COMMAND_HNX_SAVER = CIF.IniReadValue(CConfig.INI_SECTION_REDIS, CConfig.INI_KEY_CHANNELCOMMAND_HNX_SAVER); //ChannelCOMMANDHNXSAVER=S5G_COMMAND_HNX_SAVER

                CConfig.TEMPLATE_KEY_CHART = CIF.IniReadValue(CConfig.INI_SECTION_CHART_DATA, CConfig.INI_KEY_TEMPLATE_KEY_CHART);
                CConfig.REDIS_LAST_INDEX_HA = CIF.IniReadValue(CConfig.INI_SECTION_SAVER, CConfig.INI_KEY_TEMPLATE_KEY_LAST_INDEX_HA);
                CConfig.REDIS_KEY_FULL_ROW_HA = CIF.IniReadValue(CConfig.INI_SECTION_GROUP_PT, CConfig.INI_KEY_FULL_ROW_HA);

                //  2018-06-13 15:20:03 ngocta2
                CConfig.REDIS_KEY_IG_HNX_LIST_DS = CIF.IniReadValue(CConfig.INI_SECTION_IG_REDIS, CConfig.INI_KEY_LIST_DS);

                //  2018-10-22 15:54:52 hungpv
                CConfig.REDIS_KEY_IG_HNX_LIST = CIF.IniReadValue(CConfig.INI_SECTION_IG_REDIS, CConfig.INI_KEY_LIST);
                //CConfig.REDIS_KEY_S5G_IG_LOGON = CIF.IniReadValue(CConfig.INI_SECTION_IG_REDIS, CConfig.INI_KEY_S5G_IG_LOGON);
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
    }
}

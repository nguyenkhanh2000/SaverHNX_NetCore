using SaverHNX_NetCore2.BLL;
using SaverHNX_NetCore2.Settings;

namespace SaverHNX_NetCore2.Extensions
{
    public class CLog
    {
        //lock thread
        static private readonly object m_objLocker = new object();
        /// <summary>
        /// log thong tin ERROR vao C:\
        ///     <!-- C:\Log\MyApp\ERROR\2014\05\17\1gzjxwqq.p0b  |  C:\Log\MyApp\Error\2014-05-17.txt -->
        ///     <add key="BASE_LOG_PATH_ERROR"    value="C:\\Log\\MyTestApp\\ERROR\\(yyyy)\\(MM)\\(dd)\\" />
        /// </summary>
        /// <param name="strFunctionName"></param>
        /// <param name="strErrorMesssage"></param>
        public static void LogError(string strFunctionName, string strErrorMesssage)
        {
            System.Diagnostics.Debug.Print("===========================" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "========================\r\nLogError=>" + strErrorMesssage);
            if (BaseSetting.BASE_LOG_ERROR == CConfig.NO_LOG_SET) return;
            //------------------------------------------------------------------------------------
            //Log(Base_Config.BASE_LOG_PATH_ERROR, strFunctionName, strErrorMesssage);
            LogModule2FileName(BaseSetting.BASE_LOG_PATH_ERROR, strFunctionName, strErrorMesssage);
        }
        /// <summary>
        /// log thong tin SQL vao C:\
        /// </summary>
        /// <param name="strVarName">v1</param>
        /// <param name="strSQLscript">select * from table</param>
        public static void LogSQL(string strVarName, string strSQLscript)
        {
            if (BaseSetting.BASE_LOG_SQL == CConfig.NO_LOG_SET) return;
            //------------------------------------------------------------------------------------
            Log(BaseSetting.BASE_LOG_PATH_SQL, strVarName, strSQLscript);
        }
        /// <summary>
        /// log thong tin khac vao C:\
        /// </summary>
        /// <param name="strTitle"></param>
        /// <param name="strDetail"></param>
        public static void LogText(string strTitle, string strDetail)
        {
            Log(BaseSetting.BASE_LOG_PATH_TEXT, strTitle, strDetail);
        }

        /// <summary>
        /// 2016-07-19 08:50:25 ngocta2
        /// chi log hanh dong AddItemToSortedSet
        /// Z_KEY, Z_VALUE, Z_SCORE
        ///Log(Base_Config.BASE_LOG_PATH_TEXT, strTitle, strDetail);
        ///<!-- C:\Log\MyApp\TEXT\2014\05\17\1gzjxwqq.p0b   |  C:\Log\MyApp\TEXT\2014-05-17.txt -->
        ///<add key="BASE_LOG_PATH_TEXT" value="C:\Log\(AppName)\TEXT\(yyyy)\(MM)\(dd)\" />
        ///<!-- C:\Log\MyApp\TYPE\2014\05\17\1_SECURITY_14_22_59__1gzjxwqq.txt -->
        ///<add key="BASE_LOG_PATH_EX" value="C:\Log\(AppName)\LogEx\(yyyy)\(MM)\(dd)\(FileName)" />
        ///15:21:34.843^20160719090544764^{"Time":"09:05:44","Data":{"TimeJS":"1468893900000","Index":"86.99","Vol":"111500"}}
        /// </summary>
        /// <param name="strFileNameEx"></param>
        /// <param name="strBody"></param>
        public static void LogRedis(string strZKey, double dblZScore, string strZValue)
        {
            try
            {
                string strPath = BaseSetting.BASE_LOG_PATH_TEXT
                    .Replace("(AppName)", BaseSetting.BASE_APP_NAME)
                    .Replace("(yyyy)", DateTime.Now.Year.ToString())
                    .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                    .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2));

                // check folder
                CheckDirectory(strPath);

                // noi them filename, chu y error
                //LE:S5G_ACM => error
                strPath += strZKey.Replace(":", "___") + ".js";

                // tao body
                string strBody = Convert.ToInt64(dblZScore).ToString() + "^" + strZValue;

                // write
                StreamWriter fs = new StreamWriter(strPath, true); // append
                //15:21:34.843^20160719090544764^{"Time":"09:05:44","Data":{"TimeJS":"1468893900000","Index":"86.99","Vol":"111500"}}
                fs.WriteLine(DateTime.Now.ToString("HH:mm:ss.") + DateTime.Now.Millisecond.ToString("000") + "^" + strBody);
                fs.Close();

            }
            catch (Exception)
            {
                // do nothing
            }
            finally
            {

            }
        }
        /// <summary>
        /// ghi log file
        /// </summary>
        /// <param name="strPathTemplate"></param>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        private static void Log(string strPathTemplate, string str1, string str2)
        {
            try
            {
                string strPath = strPathTemplate.Replace("(AppName)", BaseSetting.BASE_APP_NAME);
                string strLine = "";

                if (BaseSetting.BASE_LOG_MULTI_THREAD != CConfig.SINGLE_THREAD)
                {
                    // multi - ko duoc write chung 1 file - error "file locked by other process"
                    strPath = strPath
                    .Replace("(yyyy)", DateTime.Now.Year.ToString())
                    .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                    .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2));
                    strPath += Path.GetRandomFileName();
                }
                else
                {
                    // single
                    strPath = strPath.Replace("(yyyy)\\(MM)\\(dd)\\", "(yyyy)-(MM)-(dd)");
                    strPath = strPath
                    .Replace("(yyyy)", DateTime.Now.Year.ToString())
                    .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                    .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2));
                    strPath += CConfig.LOG_EXT;
                }


                // data
                strLine = CConfig.TEMPLATE_LOG_DATA
                    .Replace("(Time)", DateTime.Now.ToString("HH:mm:ss.") + DateTime.Now.Millisecond.ToString("000"))
                    .Replace("(Title)", str1)
                    .Replace("(Detail)", str2);

                // check folder
                CheckDirectory(strPath);

                // write
                StreamWriter fs = new StreamWriter(strPath, true); // append
                fs.WriteLine(strLine);
                fs.Close();

            }
            catch (Exception)
            {
                // do nothing
            }
            finally
            {

            }
        }
        /// <summary>
        /// log cao cap, chi tiet hon: thuong dung de log cac du lieu big, phai chia nho file nhu log SQL (single file = 800MB)
        /// 1 file chi ghi 1 lan, ko ghi nhieu lan (nhieu line) trong 1 file
        /// <add key="BASE_LOG_PATH_EX"       value="C:\Log\(AppName)\(Type)\(yyyy)\(MM)\(dd)\(FileName).txt" />
        /// <add key="BASE_TEMPLATE_LOG_EX_FILENAME"      value="(thread)__(type)__(hh)_(mm)_(ss)__(random).txt" />
        /// </summary>
        /// <param name="strFolder">SQL/ERROR/TEXT</param>
        /// <param name="strFileNameEx">1_security_14_33_59_afj43laf</param>
        /// <param name="strBody">SQL script</param>
        public static string LogEx(string strFileNameEx, string strBody)
        {
            try
            {
                lock (m_objLocker)
                {
                    // C:\Log\5G_QuoteFeeder_HOSE\SQL\2014\12\04\1__security__14_34_59__afj43laf.txt
                    string strPath = BaseSetting.BASE_LOG_PATH_EX
                        .Replace("(AppName)", BaseSetting.BASE_APP_NAME)
                        .Replace("(yyyy)", DateTime.Now.Year.ToString())
                        .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                        .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2))
                        .Replace("(FileName)", strFileNameEx)
                        ;

                    // check folder
                    CheckDirectory(strPath);

                    // neu noi dung ko co gi thi chi lay path (ko write)
                    if (strBody == "")
                        return strPath;

                    // write
                    StreamWriter fs = new StreamWriter(strPath, true); // append
                    fs.WriteLine(DateTime.Now.ToString("HH:mm:ss.") + DateTime.Now.Millisecond.ToString("000") + " => " + strBody);
                    fs.Close();

                    // 
                    return strPath;
                }
            }
            catch (Exception)
            {
                // do nothing
                return "";
            }
            finally
            {

            }
        }
        /// <summary>
        /// 2017-11-02 15:54:57 ngocta2
        /// ghi data vao file de co luc load lai, ko ghi cac moc time (de debug) vao trong noi dung file
        /// su dung chung folder LogEx
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="strData"></param>
        /// <returns></returns>
        public static string LogDataSave(string strFileName, string strData)
        {
            try
            {
                lock (m_objLocker)
                {
                    // D:\LOG\StockHNX2\LogEx\2017\11\02\BASKET_HNX.js
                    string strPath = BaseSetting.BASE_LOG_PATH_EX
                        .Replace("(AppName)", BaseSetting.BASE_APP_NAME)
                        .Replace("(yyyy)", DateTime.Now.Year.ToString())
                        .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                        .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2))
                        .Replace("(FileName)", strFileName)
                        ;

                    // check folder
                    CheckDirectory(strPath);

                    // neu noi dung ko co gi thi chi lay path (ko write)
                    if (strData == "")
                        return strPath;

                    // neu file ton tai thi xoa truoc khi write
                    if (File.Exists(strPath))
                        File.Delete(strPath);

                    // write
                    StreamWriter fs = new StreamWriter(strPath, true); // append
                    fs.WriteLine(strData);
                    fs.Close();

                    // return path
                    return strPath;
                }
            }
            catch (Exception)
            {
                // do nothing
                return "";
            }
            finally
            {

            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strFileName"></param>
        /// <returns></returns>
        public static string LogDataLoad(string strFileName)
        {
            try
            {
                lock (m_objLocker)
                {
                    // D:\LOG\StockHNX2\LogEx\2017\11\02\BASKET_HNX.js
                    string strPath = BaseSetting.BASE_LOG_PATH_EX
                        .Replace("(AppName)", BaseSetting.BASE_APP_NAME)
                        .Replace("(yyyy)", DateTime.Now.Year.ToString())
                        .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                        .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2))
                        .Replace("(FileName)", strFileName)
                        ;

                    // check folder
                    CheckDirectory(strPath);

                    // read
                    string strData = "";
                    using (StreamReader sr = new StreamReader(strPath))
                    {
                        strData = sr.ReadToEnd();
                    }

                    return strData;
                }
            }
            catch (Exception)
            {
                // do nothing
                return "";
            }
            finally
            {

            }
        }
        /// <summary>
        /// tuong tu LogEx
        /// </summary>
        /// <param name="strFileNameEx"></param>
        /// <param name="strBody"></param>
        /// <param name="arrHeader"></param>
        public static void LogCSV(string strFileNameEx, string strBody, string[] arrHeader)
        {
            try
            {
                // C:\Log\5G_QuoteFeeder_HOSE\SQL\2014\12\04\1__security__14_34_59__afj43laf.txt
                string strPath = BaseSetting.BASE_LOG_PATH_EX
                    .Replace("(AppName)", BaseSetting.BASE_APP_NAME)
                    .Replace("(yyyy)", DateTime.Now.Year.ToString())
                    .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                    .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2))
                    .Replace("(FileName)", strFileNameEx)
                    ;

                // check folder
                CheckDirectory(strPath);

                // tao row header neu file chua ton tai
                string strHeader = "";
                if (!File.Exists(strPath))
                {
                    strHeader = "Time";
                    foreach (string str in arrHeader)
                        strHeader += "," + str;
                }

                // write
                StreamWriter fs = new StreamWriter(strPath, true); // append
                if (strHeader != "")
                    fs.WriteLine(strHeader);
                fs.WriteLine(DateTime.Now.ToString("HH:mm:ss.") + DateTime.Now.Millisecond.ToString("000") + "," + strBody);
                fs.Close();

            }
            catch (Exception)
            {
                // do nothing
            }
            finally
            {

            }
        }
        public static void CheckDirectory(string strFilePath)
        {
            try
            {
                FileInfo f = new FileInfo(strFilePath);
                if (!f.Directory.Exists)
                {
                    f.Directory.Create();
                }
            }
            catch (Exception)
            {
                // do nothing
            }
        }
        /// <summary>
        /// 2015-08-27 09:12:23 ngocta2
        /// tuong tu log binh thuong nhung ghi luon ten Module vao FileName (de random filename kho nhan ra file can tim)
        /// chi su dung cho log error
        /// C:\Log\StockQuote\ERROR\2015\08\27\bwd0pgfa.jma (09:03:23.996|QuoteBaseLib.BLL.CComparer`1->CompareDataD=>QuoteBaseLib.BLL.CReaderBase`1->FindOldRecord)
        /// C:\Log\StockQuote\ERROR\2015\08\27\090323996__QuoteBaseLib.BLL.CComparer_CompareDataD_QuoteBaseLib.BLL.CReaderBase_FindOldRecord__bwd0pgfa.jma
        /// </summary>
        /// <param name="strPathTemplate"></param>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        private static void LogModule2FileName(string strPathTemplate, string str1, string str2)
        {
            try
            {
                string strPath = strPathTemplate.Replace("(AppName)", BaseSetting.BASE_APP_NAME);
                string strLine = "";

                if (BaseSetting.BASE_LOG_MULTI_THREAD != CConfig.SINGLE_THREAD)
                {
                    // multi - ko duoc write chung 1 file - error "file locked by other process"
                    strPath = strPath
                    .Replace("(yyyy)", DateTime.Now.Year.ToString())
                    .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                    .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2));
                    //strPath += Path.GetRandomFileName();
                }
                else
                {
                    // single
                    //strPath = strPath.Replace("(yyyy)\\(MM)\\(dd)\\", "(yyyy)-(MM)-(dd)");  // 2016-06-23 10:41:53 ngocta2 bo dong nay
                    strPath = strPath
                    .Replace("(yyyy)", DateTime.Now.Year.ToString())
                    .Replace("(MM)", CBase.Right("00" + DateTime.Now.Month.ToString(), 2))
                    .Replace("(dd)", CBase.Right("00" + DateTime.Now.Day.ToString(), 2));
                    //strPath += Base_Config.LOG_EXT;
                }

                // xu ly them path
                str1 = str1.Replace("`1", "");
                str1 = str1.Replace("=>", "_");
                str1 = str1.Replace("->", "_");
                strPath += "\\";
                strPath += DateTime.Now.ToString("HHmmss");
                strPath += "__";
                strPath += str1;
                strPath += "__";
                strPath += Path.GetRandomFileName();
                strPath = FixPath(strPath);

                // data
                strLine = CConfig.TEMPLATE_LOG_DATA
                    .Replace("(Time)", DateTime.Now.ToString("HH:mm:ss.") + DateTime.Now.Millisecond.ToString("000"))
                    .Replace("(Title)", str1)
                    .Replace("(Detail)", str2);

                // check folder
                CheckDirectory(strPath);

                // write
                StreamWriter fs = new StreamWriter(strPath, true); // append
                fs.WriteLine(strLine);
                fs.Close();

            }
            catch (Exception)
            {
                // do nothing
            }
            finally
            {

            }
        }
        /// <summary>
        /// bo ky tu ko hop le
        /// C:\Log\Monitor\ERROR\2016-04-08\103843__System.Threading.QueueUserWorkItemCallback_WaitCallback_Context_Monitor.Hubs.CMonitor5G_<InitPubSubData>b__0___xgbmys1i.5tx
        /// Illegal characters in path.
        /// </summary>
        /// <param name="strPath"></param>
        /// <returns></returns>
        public static string FixPath(string strPath)
        {
            try
            {
                strPath = strPath.Replace("<", "(");
                strPath = strPath.Replace(">", ")");
                strPath = strPath.Replace("`", "_");

                return strPath;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return strPath;
            }
        }
    }
}

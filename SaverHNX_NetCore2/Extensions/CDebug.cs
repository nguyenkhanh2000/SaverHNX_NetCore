using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;

namespace SaverHNX_NetCore2.Extensions
{
    public class CDebug
    {
        private const string FILE_EXT = ".js";
        private const string FORMAT_TIME = "HH_mm_ss";
        private const string SEPARATOR = "=======================================================================================================================";
        private const string TEMPLATE_DIC_ELE = "{'(key)':'(val)'}";
        private const string REGEX_REMOVE_TRASH = "<(.*?)>k__BackingField";

        public static void PrintText(string strText)
        {
            //Debug.Print(strText);
        }

        public static void PrintText2(string strText)
        {
            Debug.Print(strText);
        }

        public static void LogFile(string strFileNameEx, string strBody)
        {
            CLog.LogEx(strFileNameEx, strBody);
        }

        /// <summary>
        /// 11:01 AM Wednesday, March 16, 2016
        /// log all var to file text de debug nguyen nhan error
        /// http://stackoverflow.com/questions/6536163/how-to-list-all-variables-of-class
        /// </summary>
        /// <param name="objAny"></param>
        public static string AllFields2LogFile(string strVarName, object objAny)
        {
            try
            {
                if (objAny == null) return "null";

                BindingFlags bindingFlags = BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance |
                            BindingFlags.Static;

                StringBuilder sbBody = new StringBuilder("");
                foreach (FieldInfo field in objAny.GetType().GetFields(bindingFlags))
                {
                    string strFieldName = field.Name;
                    string strFieldValue = GetValueOfField(objAny, field, strFieldName);

                    sbBody.Append("" + strFieldName + " = " + strFieldValue + ""
                        + Environment.NewLine
                        + SEPARATOR
                        + Environment.NewLine);
                }
                string strFileName = strVarName + "___" + DateTime.Now.ToString(FORMAT_TIME) + FILE_EXT;  // "dd/MM/yyyy HH:mm";// (24/10/2012 15:36)
                CLog.LogEx(strFileName, sbBody.ToString());

                return strFileName;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }

        /// <summary>
        /// su dung de log big string vao file log
        /// </summary>
        /// <param name="strVarName"></param>
        /// <param name="strLongString"></param>
        /// <returns></returns>
        public static string LongString2LogFile(string strVarName, string strLongString)
        {
            try
            {
                string strFileName = strVarName + "___" + DateTime.Now.ToString(FORMAT_TIME) + FILE_EXT;  // "dd/MM/yyyy HH:mm";// (24/10/2012 15:36)
                CLog.LogEx(strFileName, strLongString);

                return strFileName;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }

        /// <summary>
        /// lay gia tri cua field, gom cac kieu: string, bool, int, long, array, dic
        //+		obj	{QuoteBaseLib.BLL.CReaderBaseMQ}	object {QuoteBaseLib.BLL.CReaderBaseMQ}
        //+		field	{System.Collections.Generic.Dictionary`2[System.Int32,System.String] m_dicI}	object {System.Reflection.RtFieldInfo}
        //+		((FieldInfo)field).GetValue(obj)	Count = 22	object {System.Collections.Generic.Dictionary<int,string>}
        //+		(Dictionary<int,string>)((FieldInfo)field).GetValue(obj)	Count = 22	System.Collections.Generic.Dictionary<int,string>
        //        ((Dictionary<int,string>)((FieldInfo)field).GetValue(obj))[8]	"BeginString"	string
        //        ((FieldInfo)field).GetValue(obj).GetType().IsGenericType	true	bool
        //        ((FieldInfo)field).GetValue(obj).GetType().GetGenericTypeDefinition()== typeof(Dictionary<,>)	true	bool
        //+		((FieldInfo)field).GetValue(obj).GetType().GetGenericArguments()[0];	{Name = "Int32" FullName = "System.Int32"}	System.Type {System.RuntimeType}
        //+		((FieldInfo)field).GetValue(obj).GetType().GetGenericArguments()[1];	{Name = "String" FullName = "System.String"}	System.Type {System.RuntimeType}
        //        ((FieldInfo)field).GetValue(obj).GetType().GetGenericArguments()[0].Name	"Int32"	string
        //        ((FieldInfo)field).GetValue(obj).GetType().GetGenericArguments()[1].Name	"String"	string
        //		valueType.Name	"Int64"	string
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="field"></param>
        /// <param name="strFieldName"></param>
        /// <returns></returns>
        private static string GetValueOfField(object objClass, FieldInfo FI, string strFieldName)
        {
            try
            {
                object objField = FI.GetValue(objClass);
                if (objField == null)
                    return "null";
                else
                {
                    //JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string jsonString = "";
                    //==========================================
                    var iDict = objField as IDictionary;
                    if (iDict != null)
                    {
                        // My object is an IDictionary
                        StringBuilder sb = new StringBuilder("");
                        sb.Append("[");
                        foreach (System.Collections.DictionaryEntry o in iDict)
                        {
                            sb.Append(TEMPLATE_DIC_ELE
                                .Replace("(key)", RemoveTrash(o.Key.ToString()))
                                .Replace("(val)", RemoveTrash(o.Value.ToString()))
                                ).Append(",");
                        }
                        sb.Length--;
                        sb.Append("]");
                        jsonString = sb.ToString();
                    }
                    else
                    {
                        // My object is not an IDictionary
                        //jsonString = serializer.Serialize((object)objField);
                        jsonString = JsonSerializer.Serialize((object)objField);

                    }
                    //==========================================

                    return jsonString;
                }
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }


        /// <summary>
        /// 2:10 PM Wednesday, March 23, 2016
        /// get instance name
        ///string strMyName = "aaaaaaaaa";
        ///string result = Check(() => strMyName);  // result = "strMyName"
        /// http://stackoverflow.com/questions/16363753/get-instance-name-c-sharp
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static string GIN<T>(Expression<Func<T>> expr)
        {
            var body = ((MemberExpression)expr.Body);
            return body.Member.Name;
        }


        /*
        http://stackoverflow.com/questions/3685732/c-sharp-serialize-dictionaryulong-ulong-to-json
         * I am trying to serialize a Dictionary to JSON, and get the following exception:
         * Type 'System.Collections.Generic.Dictionary2
         * [
         * [System.UInt64, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],
         * [System.Nullable1[[System.UInt64, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]
         * ]
         * , mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]' 
         * is not supported for serialization/deserialization of a dictionary, keys must be strings or objects.
         * ==================================
         * var dict = mapping.ToDictionary(item => item.Key.ToString(), item => item.Value.ToString());
         * that will convert any Dictionary<K,V> to Dictionary<string,string> and serialization then works.
         * https://dukelupus.wordpress.com/2011/05/04/asp-net-mvc-json-and-a-generic-dictionary/
        */
        //public static Dictionary<string, object> ToJsonDictionary<TKey, TValue>(this Dictionary<TKey, TValue> input)
        //{
        //    var output = new Dictionary<string, object>(input.Count);
        //    foreach (KeyValuePair<TKey, TValue> pair in input)
        //        output.Add(pair.Key.ToString(), pair.Value);
        //    return output;
        //}  


        // <f55>k__BackingField => f55
        private static string RemoveTrash(string strInput)
        {
            try
            {
                Match MatchResults = null;
                MatchResults = Regex.Match(strInput, REGEX_REMOVE_TRASH);
                if (MatchResults.Success)
                    return MatchResults.Groups[1].Value;
                else
                    return strInput;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return "";
            }
        }


        /// <summary>
        // 2016-11-23 14:39:46 ngocta2
        // kiem tra gia tri null , not null, type ...... giup debug tim nguyen nhan error
        //---------------
        //string ss1 = null;
        //string ss2 = "";
        //string ss3 = "abc";
        //BasicObject BO1 = new BasicObject();
        //BasicObject BO2 = new BasicObject();
        //BO1 = null;
        //int i = 1;
        //int j = -2;
        //double x = 3.5;
        //---------------
        //string strDetailError = "";
        //---------------
        //strDetailError += BaseLib.CDebug.CheckObject(CDebug.GIN(() => ss1), ss1);
        //strDetailError += BaseLib.CDebug.CheckObject(CDebug.GIN(() => ss2), ss2);
        //strDetailError += BaseLib.CDebug.CheckObject(CDebug.GIN(() => ss3), ss3);
        //strDetailError += BaseLib.CDebug.CheckObject(CDebug.GIN(() => BO1), BO1);
        //strDetailError += BaseLib.CDebug.CheckObject(CDebug.GIN(() => BO2), BO2);
        //strDetailError += BaseLib.CDebug.CheckObject(CDebug.GIN(() => i), i);
        //strDetailError += BaseLib.CDebug.CheckObject(CDebug.GIN(() => j), j);
        //strDetailError += BaseLib.CDebug.CheckObject(CDebug.GIN(() => x), x);
        //---------------
        //[ss1] obj is null
        //[ss2] obj is not null => Type=System.String => Value=Empty('')
        //[ss3] obj is not null => Type=System.String => Value='abc'
        //[BO1] obj is null
        //[BO2] obj is not null => Type=ZWinFormTest.Program+BasicObject => Value='ZWinFormTest.Program+BasicObject'
        //[i] obj is not null => Type=System.Int32 => Value='1'
        //[j] obj is not null => Type=System.Int32 => Value='-2'
        //[x] obj is not null => Type=System.Double => Value='3.5'
        //---------------
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string CheckObject(string strVarName, object obj)
        {
            if (obj == null)
            {
                return "\r\n [" + strVarName + "] obj is null";
            }
            else
            {
                string str = "\r\n [" + strVarName + "] obj is not null => Type=" + obj.GetType().ToString();

                if (obj is string)
                {
                    string s = obj.ToString();
                    if (s.Length == 0)
                        str += " => Value=Empty('')";
                    else
                        str += " => Value='" + s + "'";
                }
                else
                {
                    str += " => Value='" + obj.ToString() + "'";
                }

                return str;
            }

        }
    }
}

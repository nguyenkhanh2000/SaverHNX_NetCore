using Oracle.ManagedDataAccess.Client;
using SaverHNX_NetCore2.BLL;
using SaverHNX_NetCore2.Extensions;
using SaverHNX_NetCore2.Settings;
using System.Data;


namespace SaverHNX_NetCore2.DAL
{
    public class COracle
    {
        public const string CHAR_CRLF = "\r\n";
        public const string CHAR_TAB = "\t";
        private string CONNECTION_STRING_ORACLE_PRICE = "";
        private SaverSetting _saverSetting;
        public COracle(SaverSetting _setting) 
        {
            try
            {
                this._saverSetting = _setting;
                this.CONNECTION_STRING_ORACLE_PRICE = _setting.CONNECTION_STRING_DBORACLE;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));            
            }
        }
        /// <summary>
        /// InsertDataHNXDI(string oraclequery)
        /// </summary>
        /// <param name="oraclequery"></param>
        /// <returns></returns>
        //public bool InsertDataHNXDI(string oraclequery)
        //{
        //    try
        //    {
        //        int[] arrayNvarChar = new int[] { 0, 2, 3, 4, 5, 6, 7, 8, 11, 12, 32, 33 };

        //        if (oraclequery == "")
        //            return true;
        //        var listData = oraclequery.Split('|');
        //        OracleParameter[] arrParams = new OracleParameter[listData.Length - 5];
        //        // input params
        //        foreach (var queryitem in listData)
        //        {
        //            var values = queryitem.Split('=');
        //            // check bo may thang hearder
        //            if (values[0].IndexOf("-1", StringComparison.Ordinal) > -1)
        //                continue;
        //            //2018-07-23 10:19:01 hungpv
        //            var rowParam = int.Parse(values[0]);
        //            //Check pram la so hoac char thi offer TypeDb
        //            if (arrayNvarChar.Contains(rowParam))
        //            {
        //                arrParams[rowParam] = new OracleParameter(values[1], OracleDbType.Varchar2);
        //                arrParams[rowParam].Direction = ParameterDirection.Input;
        //                arrParams[rowParam].Value = values[2] == "" ? (object)DBNull.Value : values[2];
        //            }
        //            else
        //            {
        //                arrParams[rowParam] = new OracleParameter(values[1], OracleDbType.Decimal);
        //                arrParams[rowParam].Direction = ParameterDirection.Input;
        //                arrParams[rowParam].Value = values[2] == "" ? (object)DBNull.Value : decimal.Parse(values[2]);
        //            }
        //        }

        //        // log sql
        //        CLog.LogSQL(CBase.GetDeepCaller(), _saverSetting.SPSPIRE_S5G_HNXSAVER_IG_DI_U + GetParamORC(arrParams));

        //        // exec                
        //        OracleHelper.ExecuteNonQuery(this.CONNECTION_STRING_ORACLE_PRICE, CommandType.StoredProcedure, _saverSetting.SPSPIRE_S5G_HNXSAVER_IG_DI_U, arrParams);

        //        // log output
        //        CLog.LogSQL(CBase.GetDeepCaller(), CHAR_CRLF);

        //        // return
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        // log error
        //        CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
        //        return false;
        //    }
        //}
        public bool InsertDataHNXDI(string oraclequery)
        {
            try
            {
                int[] arrayNvarChar = new int[] { 0, 2, 3, 4, 5, 6, 7, 8, 11, 12, 32, 33 };
                if (string.IsNullOrEmpty(oraclequery))
                    return true;

                var listData = oraclequery.Split('|');
                OracleParameter[] arrParams = new OracleParameter[listData.Length - 5];

                // Process input params
                foreach (var queryitem in listData)
                {
                    var values = queryitem.Split('=');

                    // Skip items with "-1" in the first part
                    if (values[0].IndexOf("-1", StringComparison.Ordinal) > -1)
                        continue;

                    var rowParam = int.Parse(values[0]);

                    // Check if param is varchar or decimal and assign proper type
                    if (arrayNvarChar.Contains(rowParam))
                    {
                        arrParams[rowParam] = new OracleParameter(values[1], OracleDbType.Varchar2)
                        {
                            Direction = ParameterDirection.Input,
                            Value = string.IsNullOrEmpty(values[2]) ? (object)DBNull.Value : values[2]
                        };
                    }
                    else
                    {
                        arrParams[rowParam] = new OracleParameter(values[1], OracleDbType.Decimal)
                        {
                            Direction = ParameterDirection.Input,
                            Value = string.IsNullOrEmpty(values[2]) ? (object)DBNull.Value : decimal.Parse(values[2])
                        };
                    }
                }

                // Execute stored procedure using OracleHelper
                // exec                
                
                OracleHelper.ExecuteNonQuery(
                    CONNECTION_STRING_ORACLE_PRICE,
                    CommandType.StoredProcedure,
                    _saverSetting.SPSPIRE_S5G_HNXSAVER_IG_DI_U,
                    arrParams);

                return true;
            }
            catch (Exception ex)
            {
                // Log error
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        //d:\project\ezauth\authlib\authlib\cdatabaseorc.cs
        public static string GetParamORC(OracleParameter[] arrParams)
        {
            string s = CHAR_CRLF;
            for (int i = 0; i < arrParams.Length; i++)
            {
                s += CHAR_TAB + arrParams[i].ParameterName + "='" + arrParams[i].Value + "'," + CHAR_CRLF;
            }

            return s;
        }
    }
}

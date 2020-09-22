using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DBSupport
{
    public class PublicFunction
    {
        #region 验证输入是否数字(包括小数)
        /// <summary>
        /// 验证输入是否数字(包括小数)
        /// </summary>
        /// <param name="str">验证源</param>
        /// <returns>true是，false否</returns>
        public static bool IsNumeric(string str)
        {
            Regex reg1 = new Regex(@"^[-]?(\d+\.?\d*|\.\d+)$");
            return reg1.IsMatch(str);
        }
        #endregion

        #region 验证输入是否是正整数
        /// <summary>
        /// 验证输入是否是正整数
        /// </summary>
        /// <param name="str">验证源</param>
        /// <returns>true是，false否</returns>
        public static bool IsInteger(string str)
        {
            Regex reg = new Regex(@"^[1-9]\d*$");
            return reg.IsMatch(str);
        }
        #endregion

        #region 验证输入是否合法日期
        /// <summary>
        /// 验证输入是否合法日期
        /// </summary>
        /// <param name="str">验证源</param>
        /// <returns>true是，false否</returns>
        public static bool IsDate(string str)
        {
            string regex = @"^((\d{2}(([02468][048])|([13579][26]))[\-\/\s]?((((0?[13578]"
                + @")|(1[02]))[\-\/\s]?((0?[1-9])|([1-2][0-9])|(3[01])))|(((0?[4"
                + @"69])|(11))[\-\/\s]?((0?[1-9])|([1-2][0-9])|(30)))|(0?2[\-\/\"
                + @"s]?((0?[1-9])|([1-2][0-9])))))|(\d{2}(([02468][1235679])|([1"
                + @"3579][01345789]))[\-\/\s]?((((0?[13578])|(1[02]))[\-\/\s]?(("
                + @"0?[1-9])|([1-2][0-9])|(3[01])))|(((0?[469])|(11))[\-\/\s]?(("
                + @"0?[1-9])|([1-2][0-9])|(30)))|(0?2[\-\/\s]?((0?[1-9])|(1[0-9]"
                + @")|(2[0-8]))))))$";
            Regex reg = new Regex(regex);
            return reg.IsMatch(str);
        }
        #endregion

        #region 验证输入身份证是否合法
        /// <summary>
        /// 验证输入身份证号是否合法
        /// </summary>
        /// <param name="str">验证源</param>
        /// <returns>true是，false否</returns>
        public static bool IsPaperNum(string str)
        {
            Regex reg = new Regex(@"\d{18}|\d{15}");
            return reg.IsMatch(str);
        }
        #endregion

        #region 验证输入的电话号码(国内)是否合法
        /// <summary>
        /// 验证输入的电话号码(国内)是否合法
        /// </summary>
        /// <param name="str">验证源</param>
        /// <returns>true是，false否</returns>
        public static bool IsTelNum(string str)
        {
            Regex reg = new Regex(@"^(\(\d{3,4}\)|\d{3,4}-)?\d{7,8}$");
            return reg.IsMatch(str);
        }
        #endregion

        #region 验证输入的手机号码(中国)是否合法
        /// <summary>
        /// 验证输入的手机号码(中国)是否合法
        /// </summary>
        /// <param name="str">验证源</param>
        /// <returns>true是，false否</returns>
        public static bool IsMobilNum(string str)
        {
            Regex reg = new Regex(@"\d{11}");
            return reg.IsMatch(str);
        }
        #endregion

        #region 获取数据库连接字符串

        public static string GetConnectString()
        {
            string ret = "";
            try
            {
                ret = System.Configuration.ConfigurationSettings.AppSettings["ConnectString"];
            }
            catch
            {
                ret = "";
            }
            return ret;
        }

        #endregion

        #region 获取数据库类型

        public static DBSupport.DataAccessFactory.DBType GetDatabaseType()
        {
            DBSupport.DataAccessFactory.DBType ret;
            string dttype = "";
            try
            {
                dttype = System.Configuration.ConfigurationSettings.AppSettings["DatabaseType"];
            }
            catch
            {
                dttype = "";
            }

            switch (dttype.Trim().ToLower())
            { 
                case "mssql":
                    ret = DBSupport.DataAccessFactory.DBType.MsSql;
                    break;

                case "sqlserver":
                    ret = DBSupport.DataAccessFactory.DBType.SqlServer;
                    break;

                case "odbc":
                    ret = DBSupport.DataAccessFactory.DBType.Odbc;
                    break;

                case "oracle":
                    ret = DBSupport.DataAccessFactory.DBType.Oracle;
                    break;

                default:
                    ret = DBSupport.DataAccessFactory.DBType.MsSql;
                    break;
            }

            return ret;
        }

        #endregion

        #region 获取数据库访问对象

        /// <summary>
        /// 获取数据库访问对象
        /// </summary>
        /// <returns>数据库访问对象</returns>
        public static DBSupport.IDataAccess GetDataAccess()
        {
            DBSupport.IDataAccess da = null;
            try
            {
                da = DBSupport.DataAccessFactory.CreateDataAccess(
                    GetDatabaseType(), GetConnectString());
            }
            catch(Exception ex)
            { 
              
            }
            return da;
        }

        #endregion


    }
}

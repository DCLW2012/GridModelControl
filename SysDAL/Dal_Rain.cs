using SysModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SysDAL
{
    /// <summary>
    /// 雨量查询DAL
    /// </summary>
    public class Dal_Rain
    {
        public static DataTable GetDataBySql(string dbname, string sql)
        {
            DataTable dt = new DataTable();


            try
            {
                ClientConn.m_dataBaseConnects[dbname].Open();
                DataSet dset = ClientConn.m_dataBaseConnects[dbname].ExecuteDataSet(sql);
                if (dset.Tables.Count > 0)
                {
                    dt = dset.Tables[0];
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                ClientConn.m_dataBaseConnects[dbname].Close();
            }
            return dt;
        }

        /// <summary>
        /// SqlServerBulk导入数据
        /// </summary>
        public static void BulkToDB(string dbname, DataTable dt, string TableName)
        {
            if (CheckExistsTable(dbname, TableName))
            {
                ClientConn.m_dataBaseConnects[dbname].BulkToDB(dt, TableName);
            }
            
        }

        #region 判断数据库表是否存在，通过指定专用的连接字符串，执行一个不需要返回值的SqlCommand命令。
        /// <summary>
        /// 判断数据库表是否存在，返回页头，通过指定专用的连接字符串，执行一个不需要返回值的SqlCommand命令。
        /// </summary>
        /// <param name="tablename">bhtsoft表</param>
        /// <returns></returns>
        public static bool CheckExistsTable(string dbname, string TableName)
        {
            String tableNameStr = "select count(1) from sysobjects where name = '" + TableName + "'";
            DataTable dt = GetDataBySql(dbname, tableNameStr);
            if (dt.Rows.Count > 0)
            {
                if(dt.Rows[0][0].Equals(0))
                {
                    return false;
                }
                else
                {
                    return true;
                }
                
            }
            else
            {
                return false;
            }
            
        }
        #endregion
    }
}

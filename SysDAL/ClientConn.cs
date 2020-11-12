using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using DBSupport;
using System.Text.RegularExpressions;
using System.IO;
using System.Configuration;
using System.Data;

namespace SysDAL
{
    public static class ClientConn
    {
        public static string DataBaseName;
        /// <summary>
        /// 数据连接接口
        //！1\创建个map容器，key:value（数据库名称：链接）
        public static Dictionary<string, IDataAccess> m_dataBaseConnects = new Dictionary<string, IDataAccess>();

        //! 数据库链接对应的要处理的表名列表
        //！3\创建个map容器，key:value（数据库名称：数据库表名map容器,包含2的部分）
        // 容器中包含子容器，子容器存储类型对应的表名（创建个map容器，key:value（表类型：数据库表名）
        public static Dictionary<string, Dictionary<string, string>> m_dbTableTypes = new Dictionary<string, Dictionary<string, string> >();

        public static Dictionary<string, Dictionary<string, DataTable>> m_dbTableConfig = new Dictionary<string, Dictionary<string, DataTable>>();

        //每个计算机ip对应的computernode值
        public static Dictionary<string, string> m_computerValues = new Dictionary<string, string>();
        
        //! 根据config中的数据库链接，取出name，建立链接，存储到map中
        public static void PraseDataBaseConfig(bool isAllConnect)
        {
            int num = System.Configuration.ConfigurationManager.ConnectionStrings.Count;
            for(int i = 1; i != num; ++i)
            {
                //!! 取出name
                String dbName = System.Configuration.ConfigurationManager.ConnectionStrings[i].Name;

                //！2.建立链接(根据数据库类型，建立链接，目前默认为sqlserver)
                string oledbclient = System.Configuration.ConfigurationManager.ConnectionStrings[dbName].ToString();
                IDataAccess DataAccess = null;

                if (isAllConnect || dbName.Equals("china"))
                {
                    DataAccess = DataAccessFactory.CreateDataAccess(DataAccessFactory.DBType.SqlServer, oledbclient);
                }
                

                //3.存储到map容器中
                m_dataBaseConnects.Add(dbName, DataAccess);
            }
        }

        //! 解析了数据库的链接后，根据数据库的code名，找该库对应的要处理的数据类型列表
        public static void PraseTableTypeConfig()
        {
            int num = m_dataBaseConnects.Count;
            foreach (var curDB in m_dataBaseConnects) 
            {
                //！！取出当前key值
                string keyString = curDB.Key;

                //! 数据库表类型对应的表名
                //！2\创建个map容器，key:value（表类型：数据库表名）
                Dictionary<string, string> tableTypes = new Dictionary<string, string>();

                // 获取同名对应的section配置文件中的值.
                System.Collections.Specialized.NameValueCollection appSettingSection =
                  (System.Collections.Specialized.NameValueCollection)ConfigurationManager.GetSection(keyString);
                  if(appSettingSection == null)
                  {
                      continue;
                  }

                //！遍历table，存储起来
                int tableNum = appSettingSection.Count;
                for(int i = 0; i != tableNum; ++i)
                {
                    //!当前表名类型
                    string tableTypeName = appSettingSection.AllKeys[i];
                    string value = appSettingSection.Get(tableTypeName);
                    tableTypes.Add(tableTypeName, value);
                }

                //! 存储
                m_dbTableTypes.Add(keyString, tableTypes);
            }
            
        }


        public static void PraseGridUnitConfig()
        {
            int num = m_dataBaseConnects.Count;
            foreach (var curDB in m_dataBaseConnects)
            {
                //！！取出当前key值
                string keyString = curDB.Key;

                //! 数据库表类型对应的表名
                //！2\创建个map容器，key:value（表类型：数据库表名）
                Dictionary<string, DataTable> tableTypes = new Dictionary<string, DataTable>();

                //！遍历table，存储起来
                string[] tbnames = { "GRID_HSFX_UNIT", "HSFX_ComputeUnit" };
                int tableNum = tbnames.Length;
                for (int i = 0; i != tableNum; ++i)
                {
                    //!当前表名类型
                    string tableTypeName = tbnames[i];

                    string sql = String.Format("SELECT *  from {0}", tbnames[i]);
                    if (tableTypeName == "GRID_HSFX_UNIT")
                    {
                        sql = String.Format("SELECT *  from {0} order by ID", tbnames[i]);
                    }
                    else if (tableTypeName == "HSFX_ComputeUnit")
                    {
                        sql = String.Format("select * from {0} where ComputeUnit > 100 ORDER BY ComputeUnit", tbnames[i]);
                    }
                    DataTable value = Dal_Rain.GetDataBySql(keyString, sql);
                    tableTypes.Add(tableTypeName, value);
                }

                //! 存储
                m_dbTableConfig.Add(keyString, tableTypes);
            }

        }

        //解析存储全国区域网格表
        public static void PraseGridUnitConfigAllChina(string computernode)
        {
            //！！取出当前key值
            string keyString = "china";

            //! 数据库表类型对应的表名
            //！2\创建个map容器，key:value（表类型：数据库表名）
            Dictionary<string, DataTable> tableTypes = new Dictionary<string, DataTable>();

            //！遍历table，存储起来
            string[] tbnames = { "GRID_HSFX_UNIT", "HSFX_ComputeUnit" };
            int tableNum = tbnames.Length;
            for (int i = 0; i != tableNum; ++i)
            {
                //!当前表名类型
                string tableTypeName = tbnames[i];

                string sql = String.Format("SELECT *  from {0}", tbnames[i]);
                if (tableTypeName == "GRID_HSFX_UNIT")
                {
                    sql = String.Format(@"SELECT
                                                T1.*
                                        FROM
                                                {0} T1
                                        INNER JOIN(
                                                SELECT
                                                        a.ComputeUnit,
                                                        b.bswatacd
                                                FROM
                                                        HSFX_ComputeUnit a
                                                LEFT JOIN HSFX_Computer b ON a.ComputeNode = b.ComputeNode
                                                WHERE
                                                        a.ComputeNode = '{1}'
                                        ) T2 ON T1.GroupID = T2.ComputeUnit
                                        AND T1.bswatacd = T2.bswatacd
                                        ORDER BY province asc", tbnames[i], computernode);
                }
                else if (tableTypeName == "HSFX_ComputeUnit")
                {
                    sql = String.Format(@"SELECT
                                                *
                                        FROM
                                                {0}
                                        WHERE
                                                ComputeNode = '{1}'
                                        AND ComputeUnit > 100
                                        ORDER BY
                                                ComputeUnit ", tbnames[i], computernode);
                }
                DataTable value = Dal_Rain.GetDataBySql(keyString, sql);
                tableTypes.Add(tableTypeName, value);
            }

            //! 存储
            m_dbTableConfig.Add(keyString, tableTypes);

        }


        public static void PraseComputerTableConfig()
        {
            //！！取出当前key值
            string keyString = "china";

            string tableTypeName = "HSFX_COMPUTER_Group";
            string sql = String.Format("SELECT *  from {0}", tableTypeName);

            DataTable value = Dal_Rain.GetDataBySql(keyString, sql);
            //! 遍历DataTable存储
            for (int i = 0; i < value.Rows.Count; ++i)
            {
                string ip = value.Rows[i]["ComputerIP"].ToString();
                string node = value.Rows[i]["ComputeNode"].ToString();
                m_computerValues.Add(ip, node);
            }
            
        }
    }
}

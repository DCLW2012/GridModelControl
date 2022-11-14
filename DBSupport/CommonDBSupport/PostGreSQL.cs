using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using System.Collections;
using System.Data;
using static DBSupport.DataAccessFactory;
using System.Globalization;

namespace DBSupport
{
    internal class PostGreSQL : IDataAccess
    {
        #region 字段、属性

        //连接对象
        private NpgsqlConnection cnn;

        //命令对象
        private NpgsqlCommand cmd;

        //数据库连接字符串
        private string connectString = "";
        private DBType type = DBType.SqlServer;
        //错误信息
        private string errorMessage = "";

        /// <summary>
        /// 设置或获取连接字符串
        /// </summary>
        public string ConnectString
        {
            get
            {
                return connectString;
            }
            set
            {
                connectString = value;
            }
        }
        public DBType SqlDBType
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }
        /// <summary>
        /// 获取错误信息
        /// </summary>
        public string LastError
        {
            get
            {
                return errorMessage;
            }
            set
            {
                errorMessage = null;
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 无参构造
        /// </summary>
        public PostGreSQL()
        {
            cnn = new NpgsqlConnection();
            cmd = new NpgsqlCommand();
            cmd.Connection = cnn;
        }

        /// <summary>
        /// 带参构造
        /// </summary>
        /// <param name="connectString">数据库连接字符传</param>
        public PostGreSQL(string connectString)
        {
            try
            {
                this.connectString = connectString;
                cnn = new NpgsqlConnection(connectString);
                cmd = new NpgsqlCommand();
                cmd.Connection = cnn;
                cnn.Open();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        #endregion

        #region 事务处理

        /// <summary>
        /// 开始事务,成功返回true,失败返回false
        /// </summary>
        /// <returns>true成功，false失败</returns>
        public bool BeginTransaction()
        {
            bool ret = false;
            try
            {
                Open();
                cmd.Transaction = cnn.BeginTransaction();
                ret = true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            return ret;
        }

        /// <summary>
        /// 提交事务，成功返回true，失败返回false
        /// </summary>
        /// <returns>成功返回true,失败返回false</returns>
        public bool CommitTransacton()
        {
            bool ret = false;
            try
            {
                cmd.Transaction.Commit();
                Close();
                ret = true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            return ret;
        }

        /// <summary>
        /// 回滚事务，成功返回true,失败返回false
        /// </summary>
        /// <returns>成功返回true,失败返回false</returns>
        public bool RollbackTransaction()
        {
            bool ret = false;
            try
            {
                cmd.Transaction.Rollback();
                Close();
                ret = true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            return ret;
        }

        #endregion

        #region 打开、关闭连接
        /// <summary>
        /// 打开连接
        /// </summary>
        public void Open()
        {
            errorMessage = "";
            if (cnn.State != ConnectionState.Open)
            {
                cnn.ConnectionString = connectString;
                cnn.Open();
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            if (cnn.State == ConnectionState.Open)
            {
                cnn.Close();
            }
        }

        #endregion

        #region 创建参数列表

        /// <summary>
        /// 创建参数列表
        /// </summary>
        /// <param name="paramCount">参数个数</param>
        /// <returns>参数列表</returns>
        public IDataParameter[] CreateParamters(int paramCount)
        {
            NpgsqlParameter[] Params = new NpgsqlParameter[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                Params[i] = new NpgsqlParameter();
            }
            return Params;
        }

        #endregion

        #region 创建NpgsqlCommand对象并配置其参数列表(私有方法)

        /// <summary>
        /// 创建NpgsqlCommand对象并配置其参数列表
        /// </summary>
        /// <param name="IsProceduceType">命令对象类型标志</param>
        /// <param name="commandText">命令文本</param>
        /// <param name="Params">参数列表</param>
        /// <returns>配置后的NpgsqlCommand对象</returns>
        private void SetParamters(bool IsProceduceType, string commandText, object[] Params)
        {
            cmd.Parameters.Clear();
            cmd.CommandText = commandText;

            if (IsProceduceType)
            {
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                cmd.CommandType = CommandType.Text;
            }

            if (Params != null)
            {
                NpgsqlParameter[] p = (NpgsqlParameter[])Params;

                if (p.Length > 0)
                {
                    for (int i = 0; i < Params.Length; i++)
                    {
                        cmd.Parameters.Add(p[i]);
                    }
                }
            }
        }

        #endregion

        #region 执行非查询方法
        /// <summary>
        /// 执行非查询SQL语句,成功返回影响的行数，失败返回-1
        /// </summary>
        /// <param name="commandText">非查询无参Sql</param>
        /// <returns>成功返回影响的行数，失败返回-1</returns>
        public int ExecuteNonQuery(string commandText)
        {
            int ret = -1;

            try
            {
                Open();
                cmd.CommandText = commandText;

                ret = cmd.ExecuteNonQuery();
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
            }
            finally
            {
                Close();
            }
            return ret;
        }

        /// <summary>
        /// 执行带参非查询SQL语句，成功返回影响的行数，失败返回-1
        /// </summary>
        /// <param name="commandText">非查询带参Sql语句</param>
        /// <param name="Params">参数列表</param>
        /// <returns>成功返回影响的行数，失败返回-1<returns>
        public int ExecuteNonQuery(string commandText, object[] Params)
        {
            int ret = -1;

            try
            {
                Open();
                SetParamters(false, commandText, Params);

                ret = cmd.ExecuteNonQuery();
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
            }
            finally
            {
                Close();
            }

            return ret;
        }

        /// <summary>
        /// 执行无参非查询存储过程，成功返回影响的行数，失败返回-1
        /// </summary>
        /// <param name="strProcduceName">无参非查询存储过程名</param>
        /// <returns>成功返回影响的行数，失败返回-1</returns>
        public int ExecuteNonQueryForProcduce(string strProcduceName)
        {
            int ret = -1;

            try
            {
                Open();
                SetParamters(true, strProcduceName, null);

                ret = cmd.ExecuteNonQuery();
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
            }
            finally
            {
                Close();
            }

            return ret;
        }

        /// <summary>
        /// 执行有参非查询存储过程，成功返回影响的行数，失败返回-1
        /// </summary>
        /// <param name="strProcduceName">有参非查询存储过程名</param>
        /// <param name="Params">参数列表</param>
        /// <returns>成功返回影响的行数，失败返回-1</returns>
        public int ExecuteNonQueryForProcduce(string strProcduceName, object[] Params)
        {
            int ret = -1;

            try
            {
                Open();
                SetParamters(true, strProcduceName, Params);

                ret = cmd.ExecuteNonQuery();
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
            }
            //finally
            //{
            //    Close();
            //}
            return ret;
        }

        #endregion

        #region 执行查询方法返回DataSet对象

        /// <summary>
        /// 执行无参SQL返回DataSet对象，失败返回null
        /// </summary>
        /// <param name="commandText">无参查询SQL语句</param>
        /// <returns>DataSet对象，失败返回null</returns>
        public System.Data.DataSet ExecuteDataSet(string commandText)
        {
            DataSet ret = null;

            try
            {
                cmd.CommandText = commandText;
                NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd);

                ret = new DataSet();
                sda.Fill(ret);
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                ret = null;
            }
            //finally
            //{
            //    Close();
            //}
            return ret;
        }

        /// <summary>
        /// 执行带参查询SQL语句返回DataSet对象，失败返回null
        /// </summary>
        /// <param name="commandText">有参查询SQL语句</param>
        /// <param name="Params">参数列表</param>
        /// <returns>DataSet对象，失败返回null</returns>
        public DataSet ExecuteDataSet(string commandText, object[] Params)
        {
            DataSet ret = null;

            try
            {
                SetParamters(false, commandText, Params);

                NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd);
                ret = new DataSet();
                sda.Fill(ret);
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                ret = null;
            }
            //finally
            //{
            //    Close();
            //}
            return ret;
        }

        /// <summary>
        /// 执行无参查询存储过程返回DataSet对象，失败返回null
        /// </summary>
        /// <param name="strProcduceName">有参查询存储过程名</param>
        /// <returns>DataSet对象，失败返回null</returns>
        public DataSet ExecuteDataSetForProcduce(string strProcduceName)
        {
            DataSet ret = null;

            try
            {
                SetParamters(true, strProcduceName, null);

                NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd);
                ret = new DataSet();
                sda.Fill(ret);
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                ret = null;
            }
            //finally
            //{
            //    Close();
            //}
            return ret;
        }

        /// <summary>
        /// 执行有参查询存储过程返回DataSet对象，失败返回null
        /// </summary>
        /// <param name="strProcduceName">有参查询存储过程名</param>
        /// <param name="Params">参数列表</param>
        /// <returns>DataSet对象，失败返回null</returns>
        public DataSet ExecuteDataSetForProcduce(string strProcduceName, object[] Params)
        {
            DataSet ret = null;

            try
            {
                SetParamters(true, strProcduceName, Params);

                NpgsqlDataAdapter sda = new NpgsqlDataAdapter(cmd);
                ret = new DataSet();
                sda.Fill(ret);
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                ret = null;
            }
            //finally
            //{
            //    Close();
            //}

            return ret;
        }

        #endregion

        #region 执行查询方法返回NpgsqlDataReader对象

        /// <summary>
        /// 执行无参查询SQL返回NpgsqlDataReader对象，失败返回null
        /// </summary>
        /// <param name="commandText">无参查询SQL语句</param>
        /// <returns>NpgsqlDataReader对象，失败返回null</returns>
        public IDataReader ExecuteDataReader(string commandText)
        {
            IDataReader ret = null;

            try
            {
                Open();
                cmd.CommandText = commandText;

                ret = cmd.ExecuteReader();
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                //Close();
            }

            return ret;
        }

        /// <summary>
        /// 执行带参查询SQL语句返回NpgsqlDataReader对象，失败返回null
        /// </summary>
        /// <param name="commandText">SQL语句</param>
        /// <param name="Params">参数列表</param>
        /// <returns>NpgsqlDataReader对象，失败返回null</returns>
        public IDataReader ExecuteDataReader(string commandText, object[] Params)
        {
            IDataReader ret = null;

            try
            {
                Open();
                SetParamters(false, commandText, Params);

                ret = cmd.ExecuteReader();
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                //Close();
            }

            return ret;
        }

        /// <summary>
        /// 执行无参查询存储过程返回NpgsqlDataReader对象，失败返回null
        /// </summary>
        /// <param name="strProcduceName">存储过程名</param>
        /// <returns>NpgsqlDataReader对象，失败返回null</returns>
        public IDataReader ExecuteDataReaderForProcduce(string strProcduceName)
        {
            IDataReader ret = null;

            try
            {
                Open();
                SetParamters(true, strProcduceName, null);

                ret = cmd.ExecuteReader();
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                //Close();
            }

            return ret;
        }

        /// <summary>
        /// 执行有参查询存储过程返回NpgsqlDataReader对象，失败返回null
        /// </summary>
        /// <param name="strProcduceName">存储过程名</param>
        /// <param name="Params">参数列表</param>
        /// <returns>NpgsqlDataReader对象，失败返回null</returns>
        public IDataReader ExecuteDataReaderForProcduce(string strProcduceName, object[] Params)
        {
            IDataReader ret = null;

            try
            {
                Open();
                SetParamters(true, strProcduceName, Params);

                ret = cmd.ExecuteReader();
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                //Close();
            }

            return ret;
        }

        #endregion

        #region 执行查询方法返回单一值

        /// <summary>
        /// 执行无参查询SQL返回单一值,失败返回null
        /// </summary>
        /// <param name="commandText">无参查询SQL语句</param>
        /// <returns>单一值,失败返回null</returns>
        public object ExecuteScalar(string commandText)
        {
            object ret = null;

            try
            {
                Open();
                cmd.CommandText = commandText;

                object obj = cmd.ExecuteScalar();
                ret = obj == null ? "" : obj;
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
            }
            //finally
            //{
            //    Close();
            //}

            return ret;
        }

        /// <summary>
        /// 执行带参查询SQL语句返回单一值，失败返回null
        /// </summary>
        /// <param name="commandText">有参查询SQL语句</param>
        /// <param name="Params">参数列表</param>
        /// <returns>返回单一值，失败返回null</returns>
        public object ExecuteScalar(string commandText, object[] Params)
        {
            object ret = null;

            try
            {
                Open();
                SetParamters(false, commandText, Params);

                object obj = cmd.ExecuteScalar();
                ret = obj == null ? "" : obj;
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
            }
            //finally
            //{
            //    Close();
            //}

            return ret;
        }

        /// <summary>
        /// 执行无参查询存储过程返回单一值，失败返回null
        /// </summary>
        /// <param name="strProcduceName">有参查询存储过程名</param>
        /// <returns>单一值，失败返回null</returns>
        public object ExecuteScalarForProcduce(string strProcduceName)
        {
            object ret = null;

            try
            {
                Open();
                SetParamters(true, strProcduceName, null);

                object obj = cmd.ExecuteScalar();
                ret = obj == null ? "" : obj;
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
            }
            //finally
            //{
            //    Close();
            //}

            return ret;
        }

        /// <summary>
        /// 执行有参查询存储过程返回单一值，失败返回null
        /// </summary>
        /// <param name="strProcduceName">有参查询存储过程名</param>
        /// <param name="Params">参数列表</param>
        /// <returns>返回单一值，失败返回null</returns>
        public object ExecuteScalarForProcduce(string strProcduceName, object[] Params)
        {
            object ret = null;

            try
            {
                Open();
                SetParamters(true, strProcduceName, Params);

                object obj = cmd.ExecuteScalar();
                ret = obj == null ? "" : obj;
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
            }
            //finally
            //{
            //    Close();
            //}

            return ret;
        }

        #endregion

        #region 执行无参查询SQL返回IList对象

        /// <summary>
        /// 执行无参查询SQL返回IList对象,失败返回null
        /// </summary>
        /// <param name="commandText">无参查询SQL语句</param>
        /// <returns>IList对象,,失败返回null</returns>
        public IList List(string commandText)
        {
            IList ret = new ArrayList();

            try
            {
                Open();
                cmd.CommandText = commandText;
                NpgsqlDataReader sqr = cmd.ExecuteReader();
                while (sqr.Read())
                {
                    IList temp = new ArrayList();
                    for (int i = 0; i < sqr.FieldCount; i++)
                    {
                        temp.Add(sqr.GetValue(i));
                    }
                    ret.Add(temp);
                }
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                ret = null;
            }
            //finally 
            //{
            //    Close();
            //}
            return ret;
        }

        /// <summary>
        /// 执行带参查询SQL语句返回IList对象,失败返回null
        /// </summary>
        /// <param name="commandText">有参查询SQL语句</param>
        /// <param name="Params">参数列表</param>
        /// <returns>IList对象,,失败返回null</returns>
        public IList List(string commandText, object[] Params)
        {
            IList ret = new ArrayList();

            try
            {
                Open();
                SetParamters(false, commandText, Params);

                NpgsqlDataReader sqr = cmd.ExecuteReader();
                while (sqr.Read())
                {
                    IList temp = new ArrayList();
                    for (int i = 0; i < sqr.FieldCount; i++)
                    {
                        temp.Add(sqr.GetValue(i));
                    }
                    ret.Add(temp);
                }
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                ret = null;
            }
            //finally
            //{
            //    Close();
            //}
            return ret;
        }

        /// <summary>
        /// 执行无参查询存储过程返回IList对象,,失败返回null
        /// </summary>
        /// <param name="strProcduceName">存储过程名</param>
        /// <returns>IList对象,,失败返回null</returns>
        public IList ListForProcduce(string strProcduceName)
        {
            IList ret = new ArrayList();

            try
            {
                Open();
                SetParamters(true, strProcduceName, null);

                NpgsqlDataReader sqr = cmd.ExecuteReader();
                while (sqr.Read())
                {
                    IList temp = new ArrayList();
                    for (int i = 0; i < sqr.FieldCount; i++)
                    {
                        temp.Add(sqr.GetValue(i));
                    }
                    ret.Add(temp);
                }
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                ret = null;
            }
            //finally
            //{
            //    Close();
            //}
            return ret;
        }

        /// <summary>
        /// 执行有参查询存储过程返回IList对象,,失败返回null
        /// </summary>
        /// <param name="strProcduceName">存储过程名</param>
        /// <param name="Params">参数列表</param>
        /// <returns>IList对象,,失败返回null</returns>
        public IList ListForProcduce(string strProcduceName, object[] Params)
        {
            IList ret = new ArrayList();

            try
            {
                Open();
                SetParamters(true, strProcduceName, Params);

                NpgsqlDataReader sqr = cmd.ExecuteReader();
                while (sqr.Read())
                {
                    IList temp = new ArrayList();
                    for (int i = 0; i < sqr.FieldCount; i++)
                    {
                        temp.Add(sqr.GetValue(i));
                    }
                    ret.Add(temp);
                }
            }
            catch (NpgsqlException e)
            {
                errorMessage = e.Message;
                ret = null;
            }
            //finally
            //{
            //    Close();
            //}
            return ret;
        }

        #endregion

        #region SqlServerBulk导入
        /// <summary>
        /// 执行sqlserverBulk导入
        /// </summary>
        /// <param name="dt">导入的数据列表</param>
        /// <param name="tbname">导入的数据库名称</param>
        public void BulkToDB1(DataTable dt, string tbname)
        {
            //NpgsqlConnection bulkCopy = new NpgsqlConnection(cnn);
            //bulkCopy.DestinationTableName = tbname;
            //bulkCopy.BatchSize = dt.Rows.Count;
            //Console.WriteLine("bulkcopy记录数：" + bulkCopy.BatchSize + tbname);
            //bulkCopy.BulkCopyTimeout = 3000;//设置导入时长3分钟
            //try
            //{
            //    Open();
            //    if (dt != null && dt.Rows.Count != 0)
            //        bulkCopy.WriteToServer(dt);
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
            //finally
            //{
            //    Close();
            //    if (bulkCopy != null)
            //        bulkCopy.Close();
            //}



        }

        /// <summary>
        /// 通过PostgreSQL连接把dataTable中的数据整块填充到数据库对应的数据表中
        /// 注意，该函数不负责NpgsqlConnection的创建、打开以及关闭
        /// </summary>
        /// <param name="conn">PostgreSQL连接</param>
        /// <param name="dataTable">数据表</param>
        public void BulkToDB(DataTable dataTable, string tbname)
        {
            var commandFormat = string.Format(CultureInfo.InvariantCulture, "COPY {0} FROM STDIN BINARY", tbname.ToLower());
            try
            {
                //** 测试示例
                //dataTable.Clear();
                //DataRow dr_rivl = dataTable.NewRow();
                //dr_rivl[1] = "1";
                //dr_rivl[2] = "555";
                //dr_rivl[3] = DateTime.Now;
                //dr_rivl[4] = "5555";
                //dr_rivl[5] = 0.2;
                //dr_rivl[6] = 0;
                //dr_rivl[7] = DateTime.Now;

                //dr_rivl[8] = DateTime.Now;    //! 峰值流量时间
                //dr_rivl[9] = 5.6;
                //dr_rivl[10] = "2021-08-08 00:00";
                //dr_rivl[12] = 33;
                //dataTable.Rows.Add(dr_rivl);
                //** 测试示例
                Open();
                //if (dataTable != null && dataTable.Rows.Count != 0)
                //{
                //    using (var writer = cnn.BeginBinaryImport(commandFormat))
                //    {
                //        foreach (DataRow item in dataTable.Rows) {
                //            writer.WriteRow(item.ItemArray);
                //        }
                //        writer.Complete();
                //    }

                //}
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Close();
                if (cnn != null)
                    cnn.Close();
            }
            
        }
        #endregion
    }
}

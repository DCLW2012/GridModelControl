
#region 描述
/*---------------------------------------------------------------------------
 * 名称:数据库通用访问接口IDataAccess
 * 功能:对常用数据访问类进行封装，屏蔽创建数据库连接对象、数据查询对
 *      象等操作，提供对不同数据访问驱动执行添删改查等数据库的操作，
 *      方便开发过程中对数据库进行操作
 * 编写人员:zydong
 * 编写时间:2006-5
 * 修改时间:2007-5
 * -------------------------------------------------------------------------*/
#endregion

using System;
using System.Data;
using System.Collections;

namespace DBSupport
{
	/// <summary>
	/// IDataAceess 通用数据库访问接口
	/// </summary>
	public interface IDataAccess
	{
		#region 属性

		/// <summary>
		/// 设置或获取连接字符串
		/// </summary>
		string ConnectString{get;set;}

		/// <summary>
		/// 获取错误信息
		/// </summary>
        string LastError { get; set; }

		#endregion

		#region 打开、关闭连接
		/// <summary>
		/// 打开连接
		/// </summary>
		void Open();

		/// <summary>
		/// 关闭连接
		/// </summary>
		void Close();

		#endregion

        #region 事务处理

        /// <summary>
        /// 开始事务,失败返回false;
        /// </summary>
        bool BeginTransaction();

        /// <summary>
        /// 提交事务,失败返回false;
        /// </summary>
        bool CommitTransacton();

        /// <summary>
        /// 回滚事务,失败返回false;
        /// </summary>
        bool RollbackTransaction();

        #endregion 

        #region 获取参数列表

        /// <summary>
		/// 创建参数列表
		/// </summary>
		/// <param name="paramsCount">参数个数</param>
		/// <returns>参数列表</returns>
		IDataParameter[] CreateParamters(int paramCount);
		#endregion

		#region 执行非查询方法
		/// <summary>
        /// 执行非查询SQL语句,失败返回-1;
		/// </summary>
		/// <param name="commandText">非查询无参Sql</param>
        /// <returns>所影响的行数,失败返回-1</returns>
		int ExecuteNonQuery(string commandText);

		/// <summary>
        /// 执行带参非查询SQL语句,失败返回-1
		/// </summary>
		/// <param name="commandText">非查询带参Sql语句</param>
		/// <param name="Params">参数列表</param>
        /// <returns>所影响的行数,失败返回-1</returns>
		int ExecuteNonQuery(string commandText, object[] Params);

		/// <summary>
        /// 执行无参非查询存储过程,失败返回-1
		/// </summary>
		/// <param name="strProcduceName">无参非查询存储过程名</param>
        /// <returns>所影响的行数,失败返回-1</returns>
		int ExecuteNonQueryForProcduce(string strProcduceName);
		
		/// <summary>
        /// 执行有参非查询存储过程,失败返回-1
		/// </summary>
		/// <param name="strProcduceName">有参非查询存储过程名</param>
		/// <param name="Params">参数列表</param>
        /// <returns>所影响的行数,失败返回-1</returns>
		int ExecuteNonQueryForProcduce(string strProcduceName, object[] Params);
       
		#endregion

		#region 执行查询方法返回DataSet对象

		/// <summary>
        /// 执行无参SQL返回DataSet对象,失败返回null
		/// </summary>
		/// <param name="commandText">无参查询SQL语句</param>
        /// <returns>DataSet对象,失败返回null</returns>
		DataSet ExecuteDataSet(string commandText);
		
		/// <summary>
        /// 执行带参非查询SQL语句,失败返回null
		/// </summary>
		/// <param name="commandText">有参查询SQL语句</param>
		/// <param name="Params">参数列表</param>
        /// <returns>DataSet对象,失败返回null</returns>
		DataSet ExecuteDataSet(string commandText, object[] Params);

		/// <summary>
        /// 执行无参查询存储过程,失败返回null
		/// </summary>
		/// <param name="strProcduceName">有参查询存储过程名</param>
        /// <returns>DataSet对象,失败返回null</returns>
		DataSet ExecuteDataSetForProcduce(string strProcduceName);

		/// <summary>
        /// 执行有参查询存储过程,失败返回null
		/// </summary>
		/// <param name="strProcduceName">有参查询存储过程名</param>
		/// <param name="Params">参数列表</param>
        /// <returns>DataSet对象,失败返回null</returns>
		DataSet ExecuteDataSetForProcduce(string strProcduceName, object[] Params);
		#endregion	

		#region 执行查询方法返回SqlDataReader对象

		/// <summary>
        /// 执行无参查询SQL返回SqlDataReader对象,失败返回null
		/// </summary>
		/// <param name="commandText">无参查询SQL语句</param>
        /// <returns>SqlDataReader对象,失败返回null</returns>
		IDataReader ExecuteDataReader(string commandText);
	
		/// <summary>
        /// 执行带参查询SQL语句返回SqlDataReader对象,失败返回null
		/// </summary>
		/// <param name="commandText">有参查询SQL语句</param>
		/// <param name="Params">参数列表</param>
        /// <returns>SqlDataReader对象,失败返回null</returns>
		IDataReader ExecuteDataReader(string commandText, object[] Params);
		
		/// <summary>
        /// 执行无参查询存储过程返回SqlDataReader对象,失败返回null
		/// </summary>
		/// <param name="strProcduceName">有参查询存储过程名</param>
        /// <returns>SqlDataReader对象,失败返回null</returns>
		IDataReader ExecuteDataReaderForProcduce(string strProcduceName);
		

		/// <summary>
        /// 执行有参查询存储过程返回SqlDataReader对象,失败返回null
		/// </summary>
		/// <param name="strProcduceName">有参查询存储过程名</param>
		/// <param name="Params">参数列表</param>
        /// <returns>SqlDataReader对象,失败返回null</returns>
		IDataReader ExecuteDataReaderForProcduce(string strProcduceName, object[] Params);

		#endregion	

		#region 执行查询方法返回单一值

		/// <summary>
        /// 执行无参查询SQL返回单一值,失败返回null
		/// </summary>
		/// <param name="commandText">无参查询SQL语句</param>
        /// <returns>单一值,失败返回null</returns>
		object ExecuteScalar(string commandText);

		/// <summary>
        /// 执行带参查询SQL语句返回单一值,失败返回null
		/// </summary>
		/// <param name="commandText">有参查询SQL语句</param>
		/// <param name="Params">参数列表</param>
        /// <returns>单一值,失败返回null</returns>
		object ExecuteScalar(string commandText, object[] Params);

		/// <summary>
        /// 执行无参查询存储过程返回单一值,失败返回null
		/// </summary>
		/// <param name="strProcduceName">有参查询存储过程名</param>
        /// <returns>单一值,失败返回null</returns>
		object ExecuteScalarForProcduce(string strProcduceName);

		/// <summary>
        /// 执行有参查询存储过程返回单一值,失败返回null
		/// </summary>
		/// <param name="strProcduceName">有参查询存储过程名</param>
		/// <param name="Params">参数列表</param>
        /// <returns>单一值,失败返回null</returns>
		object ExecuteScalarForProcduce(string strProcduceName, object[] Params);

		#endregion	

        #region 执行返回IList

        /// <summary>
        /// 执行无参查询SQL返回IList对象,失败返回null
        /// </summary>
        /// <param name="commandText">无参查询SQL语句</param>
        /// <returns>IList对象,失败返回null</returns>
        IList List(string commandText);

        /// <summary>
        /// 执行带参查询SQL语句返回IList对象,失败返回null
        /// </summary>
        /// <param name="commandText">有参查询SQL语句</param>
        /// <param name="Params">参数列表</param>
        /// <returns>IList对象,失败返回null</returns>
        IList List(string commandText, object[] Params);

        /// <summary>
        /// 执行无参查询存储过程返回IList对象,失败返回null
        /// </summary>
        /// <param name="strProcduceName">有参查询存储过程名</param>
        /// <returns>IList对象,失败返回null</returns>
        IList ListForProcduce(string strProcduceName);


        /// <summary>
        /// 执行有参查询存储过程返回IList对象,失败返回null
        /// </summary>
        /// <param name="strProcduceName">有参查询存储过程名</param>
        /// <param name="Params">参数列表</param>
        /// <returns>IList对象,失败返回null</returns>
        IList ListForProcduce(string strProcduceName, object[] Params);


        #endregion

        #region 执行SQLSERVERBULK导入
        /// <summary>
        /// 执行sqlserverBulk导入
        /// </summary>
        /// <param name="dt">导入的数据列表</param>
        /// <param name="tbname">导入的数据库名称</param>
        void BulkToDB(DataTable dt,string tbname);
        #endregion

    }
}

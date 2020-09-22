


using System;

namespace DBSupport
{
    /// <summary>
    /// DataAccessFactory 通用数据库访问对象工厂
    /// </summary>
    public class DataAccessFactory
    {
        public DataAccessFactory()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }

        /// <summary>
        /// 数据库类型枚举
        /// </summary>
        public enum DBType { MsSql = 0, Oracle, Odbc, SqlServer, Access, SqLite }

        /// <summary>
        /// 创建数据库访问对象
        /// </summary>
        /// <param name="dbt">数据库类型枚举</param>
        /// <returns>通用数据库访问接口</returns>
        public static IDataAccess CreateDataAccess(DBType dbt)
        {
            switch (dbt)
            {
                case DBType.MsSql:
                    return new MsSql();

                case DBType.Oracle:
                    return new Oracle();

                case DBType.Odbc:
                    return new Odbc();

                case DBType.SqlServer:
                    return new SqlServer();

                case DBType.Access:
                    return new Access();

                case DBType.SqLite:
                    return new SqLite();

                default: return null;
            }
        }

        /// <summary>
        /// 创建数据库访问对象
        /// </summary>
        /// <param name="dbt">数据库类型枚举</param>
        /// <param name="connectString">连接字符串</param>
        /// <returns>通用数据库访问接口</returns>
        public static IDataAccess CreateDataAccess(DBType dbt, string connectString)
        {
            switch (dbt)
            {
                case DBType.MsSql:
                    return new MsSql(connectString);

                case DBType.Oracle:
                    return new Oracle(connectString);

                case DBType.Odbc:
                    return new Odbc(connectString);

                case DBType.SqlServer:
                    return new SqlServer(connectString);

                case DBType.Access:
                    return new Access(connectString);

                case DBType.SqLite:
                    return new SqLite(connectString);

                default: return null;
            }
        }
    }
}

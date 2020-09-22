using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using SysDAL;
using Common;

namespace Common
{
    

    //bulk数据库中写入数据，即使字段名称不一样，也会写入到数据库中，甚至int char数据类型不一致也能写入
    public class DataHandler
    {
        //! 生成实时的st_pptn_r数据
        public virtual DataTable GenPPTNRainTable(string keyString, string tableName)
        {
            DataTable rainList = new DataTable();


            return rainList;
        }
        
    }
}

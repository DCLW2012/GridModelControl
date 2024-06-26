using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysModel
{
    public class ConnectInfo
    {
        //成员变量name connectionString rainTileFolder
        private string name;
        private string connectionString;
        private string rainTileFolder;

        //构造函数
        public ConnectInfo(string name, string connectionString, string rainTileFolder)
        {
            this.name = name;
            this.connectionString = connectionString;
            this.rainTileFolder = rainTileFolder;
        }

        //属性
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        public string RainTileFolder
        {
            get { return rainTileFolder; }
            set { rainTileFolder = value; }
        }

    }
}

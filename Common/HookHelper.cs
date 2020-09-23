using SysModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

namespace Common
{
    public class HookHelper
    {
        //！ 日志文件内容
        public static string Log { get; set; }

        //! 雨水情模板数据文件目录
        public static string rainSRCDirectory { get; set; }

        //! tile文件在本地的放置目录
        public static string rainTileDirectory { get; set; }

        //python降雨切片脚本
        public static string raindataForPython { get; set; }

        //bat文件名称
        public static string rubbatForDOS { get; set; }


        //bool控制变量
        public static bool updateraintile { get; set; }
        public static bool isgenraintile { get; set; }
        public static bool updatebyfile { get; set; }
        public static bool isstartbat { get; set; }
        public static bool isCalcPerRegion { get; set; }
        public static bool isshowcmd { get; set; }
        
    }
}

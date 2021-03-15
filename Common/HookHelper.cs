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
using System.Management;
using System.Diagnostics;

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
        public static string method { get; set; }
        public static string curProvince { get; set; }
        
        public static string computerNode { get; set; }
        //bool控制变量
        public static bool updateraintile { get; set; }
        public static bool isgenraintile { get; set; }
        public static bool updatebyfile { get; set; }
        public static bool isstartbat { get; set; }
        public static bool isCloseCMD { get; set; }
        public static bool isshowchildprocess { get; set; }
        public static bool isUpdateParams { get; set; }
        public static bool isUpdateRivlParams { get; set; }
        public static int processnum { get; set; }

        public static bool isGridout { get; set; }

        public static string serachIP { get; set; }
        public static string gridsize { get; set; }
        public static string useCSVLOG { get; set; }
        public static int waitcount { get; set; }
        

        /**
 * 传入参数：父进程id
 * 功能：根据父进程id，杀死与之相关的进程树
 */
        public static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                Console.WriteLine(string.Format("kill process by id {0}!", pid));
                proc.Kill();
            }
            catch (ArgumentException)
            {
                /* process already exited */
            }
        }
    }
}

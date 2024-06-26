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
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

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
        public static string raintype { get; set; }
        public static string tilemehtod { get; set; }

        public static bool isLocatTest { get; set; }

        public static ConfigurationBuilder AppSettings { get; set; }
        public static List<ConnectInfo> ConnectInfoList { get; set; }
        

        //是否使用数据库表中的场次目录信息进行计算，必须分辨率是固定的或者表里指定
        public static bool isUseDatTable { get; set; }
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
        public static string localIP { get; set; }
        
        public static string gridsize { get; set; }
        public static string useCSVLOG { get; set; }
        public static int waitcount { get; set; }
        public static string processModel { get; set; }

        public static void GetAppconfig()
        {
            //初始化参数信息
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            AppSettings = (ConfigurationBuilder)builder;

            //获取配置文件中的connectionStrings 段配置信息

            IConfigurationSection connectionStringSec = AppSettings.Build().GetSection("connectionStrings");
            //遍历connectionStringSec中的子json对象
            ConnectInfoList = new List<ConnectInfo>();
            foreach (var item in connectionStringSec.GetChildren())
            {
                //获取子json对象的key和value
                string key = item.Key;

                //获取item中的 三个section的值 connectionString name rainTileFolder
                string connectionString = item.GetSection("connectionString").Value;
                string name = item.GetSection("name").Value;
                string rainTileFolder = item.GetSection("rainTileFolder").Value;

                //将获取的值封装到ConnectInfo对象中
                ConnectInfo connectInfo = new ConnectInfo(name, connectionString, rainTileFolder);

                //将ConnectInfo对象添加到集合中
                ConnectInfoList.Add(connectInfo);
            }

            int aa = 9;
        }

        /**
         * 传入参数：父进程id
         * 功能：根据父进程id，杀死与之相关的进程树
         */
        public static void KillProcessAndChildren(int pid)
        {
            //判断是windows操作系统
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                catch (Exception ex)
                {
                    /* process already exited */
                    //Console.WriteLine(string.Format("process already exited") );
                    HookHelper.Log += string.Format("process already exited，进程已自动关闭") + ex + "," + DateTime.Now + ";\r\n";
                }
            }

            //如果是 linux系统
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {

            }



        }
    }
}

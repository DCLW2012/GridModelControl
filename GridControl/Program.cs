using SysDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Data;
using Common;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Net;

namespace GridControl
{
    class Program
    {
        static void Main(string[] args)
        {
            //!!删除指定目录下的所有的txt文件日志文件
            WriteLog.DeleteLog();

            //! 1.根据命令行中的值，初始化hook
            //! 1、解析参数,更新初始值
            argsPrase(args);

            //! 2、初始化数据库链接信息
            ClientConn.PraseDataBaseConfig(!HookHelper.method.Equals("wata"));  //解析配置文件，并建立数据库连接。
            ClientConn.PraseTableTypeConfig(); //解析数据库对应要解析的表类型链接
            ClientConn.PraseComputerTableConfig();

            //！3、 初始化与数据库相关的hookhelper变量
            InitHookHelper(args);


            //CSVLog
            string serverIP = Program.GetLocalIP(HookHelper.serachIP);
            string hostName = Dns.GetHostName();
            string path = ConfigurationManager.AppSettings["CSVLogPath"].ToString()
                + "\\" + hostName.ToString() + "_" + serverIP.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
            if (HookHelper.useCSVLOG.Equals("true"))
            {
                CSVData.CSVInit(path);
            }
                

            //! 为了后续计算速度快，提前从数据库中读取unit单元信息和模型路径信息，
            if (HookHelper.method.Equals("wata"))
            {
                ClientConn.PraseGridUnitConfigAllChina(HookHelper.computerNode);
            }
            else
            {
                ClientConn.PraseGridUnitConfig();
            }
            

            // 每个省对应一个数据库连接，每个连接里包含了降雨切片目录
            Dictionary<string, Dictionary<string, string>> dbValues = ClientConn.m_dbTableTypes;
            Dictionary<string, Dictionary<string, DataTable>> dbTableConfigs = ClientConn.m_dbTableConfig;
            
            //testEXEListening();

            try
            {
                if (HookHelper.method == "province")
                {
                    CalcOneByOne.runBySingleCC();
                    //执行插入日志
                    WriteLog.WriteLogMethod(HookHelper.Log, "runByCCFolder");
                }

                if (HookHelper.method == "wata")
                {
                    WriteUnitInfo.GetAllHsfxUnitTableByWATA();


                    CalcOneByOneWata.runBySingleCC();
                    //执行插入日志
                    WriteLog.WriteLogMethod(HookHelper.Log, "runByCCFolder");
                }

                //! 阻塞程序不关闭
                Console.WriteLine(string.Format("当前主机节点{0}网格计算调度完成  ", HookHelper.computerNode) + DateTime.Now);

                //执行插入日志
                WriteLog.WriteLogMethod(HookHelper.Log);

                if (!HookHelper.isCloseCMD)
                {
                    Console.Read();
                }
                
            }
            catch (Exception ex){
                Console.WriteLine(ex);
                //打印日志
                string log = HookHelper.Log + ex;
                WriteLog.WriteLogMethod(log);
                Environment.Exit(0);
                //throw;
            }
            //CSVLog
            if (HookHelper.useCSVLOG.Equals("true"))
            {
                CSVFile.SaveCSV(CSVData.GetDataTable(), path);
            }
            
        }


        public static string GetLocalIP(string ipPrefix)
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string ip = IpEntry.AddressList[i].ToString();
                        if (ip.StartsWith(ipPrefix))
                            return ip;
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        static void InitHookHelper(string[] args)
        {
            Dictionary<string, string> computerValues = ClientConn.m_computerValues;

            //! 台风数据文件目录
            HookHelper.rainSRCDirectory = ConfigurationManager.AppSettings["rainSRC"].ToString();

            HookHelper.raindataForPython = ConfigurationManager.AppSettings["raindataForPython"].ToString();

            HookHelper.gridsize = ConfigurationManager.AppSettings["gridsize"].ToString();
            HookHelper.useCSVLOG = ConfigurationManager.AppSettings["useCSVLOG"].ToString();

            //等待次数
            //HookHelper.waitcount = ConfigurationManager.AppSettings["waitcount"].ToString();
            if (ConfigurationManager.AppSettings["waitcount"] != null)
            {
                HookHelper.waitcount = int.Parse(ConfigurationManager.AppSettings["waitcount"].ToString());
            }else
            {
                HookHelper.waitcount = 60;
            }

            HookHelper.rubbatForDOS = ConfigurationManager.AppSettings["rubbatForDOS"].ToString();
            HookHelper.computerNode = ConfigurationManager.AppSettings["computerNode"].ToString();
            //!根据数据库中配置的当前ip对应的node值，更新该选项
            HookHelper.serachIP = ConfigurationManager.AppSettings["searchIP"].ToString();
            string localIP = GetLocalIP(HookHelper.serachIP);
            if (!string.IsNullOrWhiteSpace(localIP) && computerValues.ContainsKey(localIP))
            {
                string curNode = computerValues[localIP];
                if (!string.IsNullOrWhiteSpace(curNode))
                {
                    HookHelper.computerNode = curNode;
                    Console.WriteLine(string.Format("当前主机节点{0}的computernode值为{1}  ", localIP, curNode) + DateTime.Now);
                }
                else
                {
                    Console.WriteLine(string.Format("当前主机节点{0}不存在有效的computernode值{1}  ", localIP, curNode) + DateTime.Now);
                }

            }else
            {
                Console.WriteLine(string.Format("当前主机节点{0}没有在数据库中配置node编号，将使用默认值  ", localIP) + DateTime.Now);
            }

            
        }

        static void argsPrase(string[] args)
        {
            //！程序功能控制参数
            HookHelper.isgenraintile = true;
            if (args.Contains("-isgenraintile"))
            {
                int index = args.ToList().IndexOf("-isgenraintile");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isgenraintile = bool.Parse(args[index + 1]);
                }

            }

            //--wata
            HookHelper.method = "province";
            if (args.Contains("-method"))
            {
                int index = args.ToList().IndexOf("-method");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.method = args[index + 1];
                }

            }

            //--指定某个省
            HookHelper.curProvince = "";
            if (args.Contains("-province"))
            {
                int index = args.ToList().IndexOf("-province");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.curProvince = args[index + 1];
                }

            }

            HookHelper.isstartbat = false;
            if (args.Contains("-isstartbat"))
            {
                int index = args.ToList().IndexOf("-isstartbat");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isstartbat = bool.Parse(args[index + 1]);
                }

            }

            HookHelper.updatebyfile = true;
            if (args.Contains("-updatebyfile"))
            {
                int index = args.ToList().IndexOf("-updatebyfile");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.updatebyfile = bool.Parse(args[index + 1]);
                }

            }

            HookHelper.isCloseCMD = false;
            if (args.Contains("-isCloseCMD"))
            {
                int index = args.ToList().IndexOf("-isCloseCMD");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isCloseCMD = bool.Parse(args[index + 1]);
                }

            }

            HookHelper.isshowchildprocess = false;
            if (args.Contains("-isshowchildprocess"))
            {
                int index = args.ToList().IndexOf("-isshowchildprocess");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isshowchildprocess = bool.Parse(args[index + 1]);
                }

            }

            HookHelper.isUpdateParams = false;
            if (args.Contains("-isUpdateParams"))
            {
                int index = args.ToList().IndexOf("-isUpdateParams");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isUpdateParams = bool.Parse(args[index + 1]);
                }

            }

            HookHelper.isUpdateRivlParams = false;
            if (args.Contains("-isUpdateRivlParams"))
            {
                int index = args.ToList().IndexOf("-isUpdateRivlParams");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isUpdateRivlParams = bool.Parse(args[index + 1]);
                }

            }
            

            HookHelper.processnum = 32;
            if (args.Contains("-processnum"))
            {
                int index = args.ToList().IndexOf("-processnum");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.processnum = int.Parse(args[index + 1]);
                }

            }


            HookHelper.isGridout = false;
            if (args.Contains("-isGridout"))
            {
                int index = args.ToList().IndexOf("-isGridout");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    bool pa = bool.Parse(args[index + 1]);
                    HookHelper.isGridout = pa;
                }

            }


            HookHelper.updateraintile = true;
            if (args.Contains("-updateraintile"))
            {
                int index = args.ToList().IndexOf("-updateraintile");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.updateraintile = bool.Parse(args[index + 1]);
                }

            }

        }

        static void testEXEListening()
        {
            Process[] ps = Process.GetProcesses();
            

            Process[] localByName = Process.GetProcessesByName("cmd");
            Process curProcss = null;
            try
            {
                curProcss = Process.GetProcessById(123);
            }
            catch (Exception ex)
            {
                curProcss = null;
            }
            Process curProcss1 = Process.GetProcessById(5432);
            int b = 6;
        }
    }
}

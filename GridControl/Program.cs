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
            if (HookHelper.isUseDatTable)
            {
                ClientConn.PraseThirdWebConfig();
            }

            //CSVLog
            string serverIP = Program.GetLocalIP(HookHelper.serachIP);
            string hostName = Dns.GetHostName();
            string path = ConfigurationManager.AppSettings["CSVLogPath"].ToString()
                + "\\" + hostName.ToString() + "_" + serverIP.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
            if (HookHelper.useCSVLOG.Equals("true"))
            {
                CSVData.CSVInit(path);
            }
            
            
            try
            {
                string[] nodes = new string[] { HookHelper.computerNode };
                if (HookHelper.computerNode.ToUpper().Equals("ALLNODE"))
                {
                    nodes = new string[] { "ComputeNode1", "ComputeNode2", "ComputeNode3", "ComputeNode4", "ComputeNode5", "ComputeNode6", "ComputeNode7", "ComputeNode8", "ComputeNode9", "ComputeNode10", "ComputeNode11" };
                }

                //! 设置计时器，所有节点时间累加值
                Stopwatch totalNodeFolderDat = new Stopwatch();
                totalNodeFolderDat.Start();

                if (HookHelper.isUseDatTable)
                {
                    //获取数据库表中的场次名称，遍历没一个场次的所有节点
                    //使用sql获取grid_taifeng_filestatus_baseinfo表中 iscalcfinish字段为0的记录 根据id排序。 算完后更新状态值
                    string taifenginfSql = @"SELECT
                                        *
                                    FROM

                                        grid_taifeng_filestatus_baseinfo
                                    WHERE

                                        iscalcfinish = 1
                                    ORDER BY
                                    ID ASC";
                    DataTable taifenginfoForcalc = Dal_ThirdWeb.GetDataBySql(taifenginfSql);

                    if (taifenginfoForcalc.Rows.Count == 0)
                    {
                        Console.WriteLine(string.Format("数据库表{0}中不存在有效的dat降雨场次  ", "grid_taifeng_filestatus_baseinfo") + DateTime.Now);
                        return;
                    }
                    int datnum = taifenginfoForcalc.Rows.Count;
                    for (int d = 0; d < datnum; ++d)
                    {
                        string fullpath = taifenginfoForcalc.Rows[d]["filepath"].ToString();
                        string datname = taifenginfoForcalc.Rows[d]["filename"].ToString();
                        int datID = int.Parse(taifenginfoForcalc.Rows[d]["ID"].ToString());
                        for (int i = 0; i < nodes.Length; ++i)
                        {
                            HookHelper.computerNode = nodes[i];
                            //! 为了后续计算速度快，提前从数据库中读取unit单元信息和模型路径信息，
                            if (HookHelper.method.Equals("wata"))
                            {
                                ClientConn.PraseGridUnitConfigAllChina(HookHelper.computerNode, HookHelper.curProvince);
                            }
                            else
                            {
                                ClientConn.PraseGridUnitConfig();
                            }


                            // 每个省对应一个数据库连接，每个连接里包含了降雨切片目录
                            Dictionary<string, Dictionary<string, string>> dbValues = ClientConn.m_dbTableTypes;
                            Dictionary<string, Dictionary<string, DataTable>> dbTableConfigs = ClientConn.m_dbTableConfig;

                            if (HookHelper.method == "wata")
                            {
                                CalcOneByOneWata.runBySingleCCUseDatTable(fullpath, d);
                                //执行插入日志
                                WriteLog.WriteLogMethod(HookHelper.Log, "runByCCFolder");
                            }

                            //! 阻塞程序不关闭
                            Console.WriteLine(string.Format("当前主机节点{0}网格计算调度完成  ", HookHelper.computerNode) + DateTime.Now);

                            //执行插入日志
                            WriteLog.WriteLogMethod(HookHelper.Log);
                            Console.WriteLine(string.Format("####################################################################") + DateTime.Now);
                            Console.WriteLine(string.Format("                                                                    ") + DateTime.Now);
                            Console.WriteLine(string.Format("                                                                    ") + DateTime.Now);
                            Console.WriteLine(string.Format("####################################################################") + DateTime.Now);
                        }

                        //更新场次名称
                        //更新本场次结算状态 为2
                        bool iscurdatErrorExist = ClientConn.IsValidDat(datname);
                        int isErrorunits = 0;
                        if (iscurdatErrorExist)
                        {
                            isErrorunits = 1;
                        }
                        String sqldatstatusBaseInfo = String.Format("UPDATE grid_taifeng_filestatus_baseinfo set iscalcfinish = 2,iserror = {1} where ID = {0}", datID, isErrorunits);
                        Dal_ThirdWeb.ExecuteSqlInserting(sqldatstatusBaseInfo);
                    }

                    Console.WriteLine(string.Format("{0}场台风场次逐场次流域计算完成  ", datnum) + DateTime.Now);
                    HookHelper.Log += string.Format("{0}场台风场次逐场次流域计算完成  ", datnum) + DateTime.Now + ";\r\n";
                    Console.WriteLine(string.Format("*********(-_-) **************{0}场台风场次逐场次流域计算完成*******..~^_^~..***********", datnum) + DateTime.Now);
                    Console.WriteLine(string.Format("*********(-_-) ***************{0}场台风场次逐场次流域计算完成*******..~^_^~..**********", datnum) + DateTime.Now);
                    Console.WriteLine(string.Format("*********(-_-) ***************{0}场台风场次逐场次流域计算完成*******..~^_^~..**********", datnum) + DateTime.Now);
                    Console.WriteLine(string.Format("********(-_-) **************{0}场台风场次逐场次流域计算完成*******..~^_^~..**********", datnum) + DateTime.Now);
                    Console.WriteLine(string.Format("********(-_-) ***************{0}场台风场次逐场次流域计算完成*******..~^_^~..*********", datnum) + DateTime.Now);
                    Console.WriteLine(string.Format("********(-_-) ****************{0}场台风场次逐场次流域计算完成*******..~^_^~..**********", datnum) + DateTime.Now);
                }
                else
                {
                    for (int i = 0; i < nodes.Length; ++i)
                    {
                        HookHelper.computerNode = nodes[i];
                        //! 为了后续计算速度快，提前从数据库中读取unit单元信息和模型路径信息，
                        if (HookHelper.method.Equals("wata"))
                        {
                            ClientConn.PraseGridUnitConfigAllChina(HookHelper.computerNode, HookHelper.curProvince);
                        }
                        else
                        {
                            ClientConn.PraseGridUnitConfig();
                        }


                        // 每个省对应一个数据库连接，每个连接里包含了降雨切片目录
                        Dictionary<string, Dictionary<string, string>> dbValues = ClientConn.m_dbTableTypes;
                        Dictionary<string, Dictionary<string, DataTable>> dbTableConfigs = ClientConn.m_dbTableConfig;

                        if (HookHelper.method == "province")
                        {

                            //默认是使用原来的流程解析dat文件
                            if (HookHelper.raintype.ToUpper().Equals("DAT"))
                            {
                                CalcOneByOne.runBySingleCC();
                            }
                            else if (HookHelper.raintype.ToUpper().Equals("NC"))   //nc支持目录下时间子目录支持，和目录下单个nc文件包含多个数据支持
                            {
                                CalcOneByOne.runBySingleCCFromNC();
                            }


                            //执行插入日志
                            WriteLog.WriteLogMethod(HookHelper.Log, "runByCCFolder");
                            //按省份计算，一次节点算完就结束。
                            break;
                        }

                        if (HookHelper.method == "wata")
                        {
                            //WriteUnitInfo.GetAllHsfxUnitTableByWATA();

                            //默认是使用原来的流程解析dat文件
                            if (HookHelper.raintype.ToUpper().Equals("DAT"))
                            {

                                CalcOneByOneWata.runBySingleCC();
                            }
                            else if (HookHelper.raintype.ToUpper().Equals("NC"))   //nc支持目录下时间子目录支持，和目录下单个nc文件包含多个数据支持
                            {
                                CalcOneByOneWata.runBySingleCCFromNC();
                            }
                            //执行插入日志
                            WriteLog.WriteLogMethod(HookHelper.Log, "runByCCFolder");
                        }

                        //! 阻塞程序不关闭
                        Console.WriteLine(string.Format("当前主机节点{0}网格计算调度完成  ", HookHelper.computerNode) + DateTime.Now);

                        //执行插入日志
                        WriteLog.WriteLogMethod(HookHelper.Log);
                        Console.WriteLine(string.Format("####################################################################") + DateTime.Now);
                        Console.WriteLine(string.Format("                                                                    ") + DateTime.Now);
                        Console.WriteLine(string.Format("                                                                    ") + DateTime.Now);
                        Console.WriteLine(string.Format("####################################################################") + DateTime.Now);
                    }
                }

                
                totalNodeFolderDat.Stop();
                TimeSpan totalNodeFolderDatTime = totalNodeFolderDat.Elapsed;
                Console.WriteLine(string.Format("{0}个节点调度总耗时：{1}秒", nodes.Length, totalNodeFolderDatTime.TotalMilliseconds / 1000));
                HookHelper.Log += string.Format("{0}个节点调度总耗时：{1}秒", nodes.Length, totalNodeFolderDatTime.TotalMilliseconds / 1000) + DateTime.Now + ";\r\n";

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
            HookHelper.waitcount = int.Parse( ConfigurationManager.AppSettings["waitcount"].ToString());
            if (ConfigurationManager.AppSettings["waitcount"] != null)
            {
                HookHelper.waitcount = int.Parse(ConfigurationManager.AppSettings["waitcount"].ToString());
            }else
            {
                HookHelper.waitcount = 60;
            }

            if (ConfigurationManager.AppSettings["thirdwebfront"] != null)
            {

            }

            HookHelper.rubbatForDOS = ConfigurationManager.AppSettings["rubbatForDOS"].ToString();
            HookHelper.computerNode = ConfigurationManager.AppSettings["computerNode"].ToString();
            //!根据数据库中配置的当前ip对应的node值，更新该选项
            HookHelper.serachIP = ConfigurationManager.AppSettings["searchIP"].ToString();
            string localIP = GetLocalIP(HookHelper.serachIP);

            //本地模式下，忽略主机ip查询
            if (HookHelper.isLocatTest) {
                Console.WriteLine(string.Format("测试模式，将使用默认值节点{0}  ", HookHelper.computerNode) + DateTime.Now);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(localIP) && computerValues.ContainsKey(localIP) && HookHelper.computerNode.ToUpper() != "ALLNODE")
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

                }
                else
                {
                    Console.WriteLine(string.Format("当前主机节点{0}没有在数据库中配置node编号，将使用默认值节点{1}  ", localIP, HookHelper.computerNode) + DateTime.Now);
                }
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

            //--rainType 降雨类型 | dat中再模式还是 nc文件模式
            HookHelper.raintype = "dat";
            if (args.Contains("-raintype"))
            {
                int index = args.ToList().IndexOf("-raintype");
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.raintype = args[index + 1];
                }
            }

            //降雨dat切片前数组存储方式
            HookHelper.tilemehtod = "all";
            if (args.Contains("-tilemehtod"))
            {
                int index = args.ToList().IndexOf("-tilemehtod");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.tilemehtod = args[index + 1];
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

            //是否本地测试模式，固定使用配置文件中的节点值
            HookHelper.isLocatTest = false;
            if (args.Contains("-isLocatTest"))
            {
                int index = args.ToList().IndexOf("-isLocatTest");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isLocatTest = bool.Parse(args[index + 1]);
                }

            }

            //是否使用数据库表中的场次目录信息进行计算，必须分辨率是固定的或者表里指定
            HookHelper.isUseDatTable = false;
            if (args.Contains("-isUseDatTable"))
            {
                int index = args.ToList().IndexOf("-isUseDatTable");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isUseDatTable = bool.Parse(args[index + 1]);
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

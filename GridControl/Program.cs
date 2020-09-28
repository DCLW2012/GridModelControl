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
namespace GridControl
{
    class Program
    {
        static void Main(string[] args)
        {

            //!!删除指定目录下的所有的txt文件日志文件
            WriteLog.DeleteLog();
            

            //！1、 为默认的全局控制参数设定初始值
            InitHookHelper(args);

            //! 2、初始化数据库链接信息
            ClientConn.PraseDataBaseConfig();  //解析配置文件，并建立数据库连接。
            ClientConn.PraseTableTypeConfig(); //解析数据库对应要解析的表类型链接

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
                if (!HookHelper.isCalcPerRegion)
                {
                    //! 遍历指定目录下的降雨数据
                    if (HookHelper.isgenraintile)
                    {
                        FileInfo[] fInfo = GenRainTile.GetRaindatList();
                        int datnum = fInfo.Length;
                        for (int d = 0; d < datnum; ++d)
                        {
                            //! 当前dat文件全路径
                            string curDatFullname = fInfo[d].FullName;

                            int num = dbValues.Count;
                            //！ 遍历每个省的网格模型
                            foreach (var curDB in dbValues)
                            {
                                //! key名称
                                string keyString = curDB.Key;

                                //！ 每个省的切片目录,对每个省执行两步操作
                                //! 1、根据指定的src降雨目录，执行python切片，输出路径为tile指定的目录
                                //！2、根据数据库中配置的模型存放目录，写出批量启动bat命令，一次性启动当前省份的n个并行程序
                                foreach (var curTBType in curDB.Value)
                                {
                                    //! 只有配置了raintile目录才执行计算
                                    string keyTableType = curTBType.Key;
                                    string folderPath = curTBType.Value;
                                    HookHelper.rainTileDirectory = "";
                                    if (keyTableType.ToUpper() == "RAINTILEFOLDER")
                                    {
                                        HookHelper.rainTileDirectory = folderPath;

                                        //!1、执行切片，调用python执行
                                        if (HookHelper.isgenraintile)
                                        {
                                            bool isGenTilesucess = GenRainTile.CreateTile(curDatFullname, folderPath);

                                            if (!isGenTilesucess)
                                            {
                                                Console.WriteLine(string.Format("{0}区域降雨切片执行失败  ", keyString) + DateTime.Now);
                                                continue;
                                            }
                                            else
                                            {
                                                Console.WriteLine(string.Format("{0}区域降雨切片执行成功  ", keyString) + DateTime.Now);
                                            }
                                        }

                                        //! 启动bat前是否执行exec.bat文件的更新--仅更新降雨目录
                                        if (d == 0)
                                        {
                                            //！遍历当前省份下 computer中的所有app路径，在路径下找到exec.bat文件，替换其中的out值
                                            if (dbTableConfigs[keyString].Count > 0)
                                            {
                                                int appnum = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows.Count;
                                                for (int a = 0; a < appnum; ++a)
                                                {
                                                    //!当前路径
                                                    string apppath = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                                                    string ComputeUnit = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
                                                    //execbat路径
                                                    string execpath = apppath + "exec.bat";
                                                    if (apppath.EndsWith("/"))
                                                    {
                                                        execpath = apppath + "exec.bat";
                                                    }
                                                    else
                                                    {
                                                        execpath = apppath + "\\" + "exec.bat";
                                                    }


                                                    string outpath = HookHelper.rainTileDirectory;

                                                    // 更新execpath的值
                                                    bool isUpExec = false;
                                                    //！覆盖更新通过模板文件
                                                    if (HookHelper.updatebyfile)
                                                    {
                                                        isUpExec = GenRainTile.UpdateExecBatFileByTemplate(execpath, ComputeUnit);
                                                    }

                                                    if (HookHelper.updateraintile)
                                                    {
                                                        isUpExec = GenRainTile.UpdateExecBatFile(execpath, outpath);
                                                    }

                                                    if (isUpExec)
                                                    {
                                                        Console.WriteLine(string.Format("{0}区域{1}文件exec.bat更新成功  ", keyString, apppath) + DateTime.Now);
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine(string.Format("{0}区域更新exec.bat失败  ", keyString) + DateTime.Now);
                                            }
                                        }


                                        //12、写出bat，执行,传入bat批量启动全路径
                                        if (dbTableConfigs[keyString].Count > 0 && HookHelper.isstartbat)
                                        {
                                            if (dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows.Count > 0)
                                            {
                                                string apppath = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[0]["AppPath"].ToString();
                                                DirectoryInfo info = new DirectoryInfo(apppath);
                                                String batRootPath = info.Parent.FullName;
                                                bool isStartsucess = GenRainTile.StartCurDBBatGroup(batRootPath, false);
                                                if (isStartsucess)
                                                {
                                                    Console.WriteLine(string.Format("{0}区域所有单元批量执行成功  ", keyString) + DateTime.Now);
                                                }
                                                else
                                                {
                                                    Console.WriteLine(string.Format("{0}区域所有单元批量{1}执行失败  ", keyString, batRootPath) + DateTime.Now);
                                                }

                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine(string.Format("{0}区域所有单元批量执行失败  ", keyString) + DateTime.Now);
                                        }

                                    }



                                }
                                Console.WriteLine(string.Format("{0}台风场{1}省份区域计算完成  ", curDatFullname, keyString) + DateTime.Now);
                                Console.WriteLine(string.Format("-------------------{0}场次{1}区域END---------------------", curDatFullname, keyString) + DateTime.Now);
                            }

                            Console.WriteLine(string.Format("{0}台风场所有省份计算完成  ", curDatFullname) + DateTime.Now);
                            Console.WriteLine(string.Format("*****************************{0}场次END**************************", curDatFullname) + DateTime.Now);
                        }
                    }
                    else
                    {
                        //! 当前dat文件全路径
                        string curDatFullname = "仅更新参数文件，不切片";

                        int num = dbValues.Count;
                        //！ 遍历每个省的网格模型
                        foreach (var curDB in dbValues)
                        {
                            //! key名称
                            string keyString = curDB.Key;

                            //！ 每个省的切片目录,对每个省执行两步操作
                            //! 1、根据指定的src降雨目录，执行python切片，输出路径为tile指定的目录
                            //！2、根据数据库中配置的模型存放目录，写出批量启动bat命令，一次性启动当前省份的n个并行程序
                            foreach (var curTBType in curDB.Value)
                            {
                                //! 只有配置了raintile目录才执行计算
                                string keyTableType = curTBType.Key;
                                string folderPath = curTBType.Value;
                                HookHelper.rainTileDirectory = "";
                                if (keyTableType.ToUpper() == "RAINTILEFOLDER")
                                {
                                    HookHelper.rainTileDirectory = folderPath;

                                    //!1、执行切片，调用python执行
                                    if (HookHelper.isgenraintile)
                                    {
                                        bool isGenTilesucess = GenRainTile.CreateTile(curDatFullname, folderPath);

                                        if (!isGenTilesucess)
                                        {
                                            Console.WriteLine(string.Format("{0}区域降雨切片执行失败  ", keyString) + DateTime.Now);
                                            continue;
                                        }
                                        else
                                        {
                                            Console.WriteLine(string.Format("{0}区域降雨切片执行成功  ", keyString) + DateTime.Now);
                                        }
                                    }

                                    //! 启动bat前是否执行exec.bat文件的更新--仅更新降雨目录

                                    {
                                        //！遍历当前省份下 computer中的所有app路径，在路径下找到exec.bat文件，替换其中的out值
                                        if (dbTableConfigs[keyString].Count > 0)
                                        {
                                            int appnum = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows.Count;
                                            for (int a = 0; a < appnum; ++a)
                                            {
                                                //!当前路径
                                                string apppath = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                                                string ComputeUnit = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
                                                //execbat路径
                                                string execpath = apppath + "exec.bat";
                                                if (apppath.EndsWith("/"))
                                                {
                                                    execpath = apppath + "exec.bat";
                                                }
                                                else
                                                {
                                                    execpath = apppath + "\\" + "exec.bat";
                                                }


                                                string outpath = HookHelper.rainTileDirectory;

                                                // 更新execpath的值
                                                bool isUpExec = false;
                                                //！覆盖更新通过模板文件
                                                if (HookHelper.updatebyfile)
                                                {
                                                    isUpExec = GenRainTile.UpdateExecBatFileByTemplate(execpath, ComputeUnit);
                                                }

                                                if (HookHelper.updateraintile)
                                                {
                                                    isUpExec = GenRainTile.UpdateExecBatFile(execpath, outpath);
                                                }

                                                if (isUpExec)
                                                {
                                                    Console.WriteLine(string.Format("{0}区域{1}文件exec.bat更新成功  ", keyString, apppath) + DateTime.Now);
                                                }
                                            }

                                        }
                                    }


                                    //12、写出bat，执行,传入bat批量启动全路径
                                    if (dbTableConfigs[keyString].Count > 0 && HookHelper.isstartbat)
                                    {
                                        if (dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows.Count > 0)
                                        {
                                            string apppath = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[0]["AppPath"].ToString();
                                            DirectoryInfo info = new DirectoryInfo(apppath);
                                            String batRootPath = info.Parent.FullName;

                                            bool isStartsucess = GenRainTile.StartCurDBBatGroup(batRootPath, false);
                                            if (isStartsucess)
                                            {
                                                Console.WriteLine(string.Format("{0}区域所有单元批量执行成功  ", keyString) + DateTime.Now);
                                            }
                                            else
                                            {
                                                Console.WriteLine(string.Format("{0}区域所有单元批量{1}执行失败  ", keyString, batRootPath) + DateTime.Now);
                                            }

                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(string.Format("{0}区域所有单元批量执行失败  ", keyString) + DateTime.Now);
                                    }

                                }



                            }
                            Console.WriteLine(string.Format("{0}台风场{1}省份区域计算完成  ", curDatFullname, keyString) + DateTime.Now);
                            Console.WriteLine(string.Format("-------------------{0}场次{1}区域END---------------------", curDatFullname, keyString) + DateTime.Now);
                        }

                        Console.WriteLine(string.Format("{0}台风场所有省份计算完成  ", curDatFullname) + DateTime.Now);
                        Console.WriteLine(string.Format("*****************************{0}场次END**************************", curDatFullname) + DateTime.Now);
                    }
                }else
                {
                    if(HookHelper.method == "province")
                    {
                        if (HookHelper.isSingleCC)
                        {
                            CalcOneByOne.runBySingleCC();
                            //执行插入日志
                            WriteLog.WriteLogMethod(HookHelper.Log, "runByCCFolder");
                        }
                        else
                        {
                            CalcOneByOne.run();
                            //执行插入日志
                            WriteLog.WriteLogMethod(HookHelper.Log, "runByCCTable");
                        }
                    }

                    if(HookHelper.method == "wata")
                    {
                        WriteUnitInfo.GetAllHsfxUnitTableByWATA();


                        if (HookHelper.isSingleCC)
                        {
                            CalcOneByOneWata.runBySingleCC();
                            //执行插入日志
                            WriteLog.WriteLogMethod(HookHelper.Log, "runByCCFolder");
                        }
                        else
                        {
                            CalcOneByOneWata.run();
                            //执行插入日志
                            WriteLog.WriteLogMethod(HookHelper.Log, "runByCCTable");
                        }
                    }



                    //! 阻塞程序不关闭
                    Console.WriteLine(string.Format("当前主机节点{0}网格计算调度完成  ", HookHelper.computerNode) + DateTime.Now);
                    Console.Read();
                }

                //执行插入日志
                WriteLog.WriteLogMethod(HookHelper.Log);
            }
            catch (Exception ex){
                Console.WriteLine(ex);
                //打印日志
                string log = HookHelper.Log + ex;
                WriteLog.WriteLogMethod(log);
                Environment.Exit(0);
                //throw;
            }
        }

        static void InitHookHelper(string[] args)
        {
            //! 台风数据文件目录
            HookHelper.rainSRCDirectory = ConfigurationManager.AppSettings["rainSRC"].ToString();

            HookHelper.raindataForPython = ConfigurationManager.AppSettings["raindataForPython"].ToString();

            HookHelper.rubbatForDOS = ConfigurationManager.AppSettings["rubbatForDOS"].ToString();
            HookHelper.computerNode = ConfigurationManager.AppSettings["computerNode"].ToString();
            

            //! 2.根据命令行中的值，初始化hook
            //! 2、解析参数,更新初始值
            argsPrase(args);
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

            HookHelper.isCalcPerRegion = false;
            if (args.Contains("-isCalcPerRegion"))
            {
                int index = args.ToList().IndexOf("-isCalcPerRegion");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isCalcPerRegion = bool.Parse(args[index + 1]);
                }

            }

            HookHelper.isSingleCC = false;
            if (args.Contains("-isSingleCC"))
            {
                int index = args.ToList().IndexOf("-isSingleCC");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isSingleCC = bool.Parse(args[index + 1]);
                }

            }
            

            HookHelper.isshowcmd = true;
            if (args.Contains("-isshowcmd"))
            {
                int index = args.ToList().IndexOf("-isshowcmd");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.isshowcmd = bool.Parse(args[index + 1]);
                }

            }

            HookHelper.processnum = 64;
            if (args.Contains("-processnum"))
            {
                int index = args.ToList().IndexOf("-processnum");

                //！ 参数标识符 后放的有值，才更新初始控制参数
                if (index + 1 <= args.Length - 1)
                {
                    HookHelper.processnum = int.Parse(args[index + 1]);
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

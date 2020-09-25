using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SysDAL;
using System.Data;
using Common;
using System.Diagnostics;
using System.IO;

namespace GridControl
{
    public class CalcOneByOne
    {
        // 每个省对应一个数据库连接，每个连接里包含了降雨切片目录
        public static Dictionary<string, Dictionary<string, string>> dbValues = ClientConn.m_dbTableTypes;
        public static Dictionary<string, Dictionary<string, DataTable>> dbTableConfigs = ClientConn.m_dbTableConfig;
        //！ pid列表，通过定时查询用来判断是否清空
        public static Dictionary<int, int> pids = new Dictionary<int, int>();
        public static bool run()
        {
            int num = dbValues.Count;

            

            //！ 遍历每个省的网格模型
            foreach (var curDB in dbValues)
            {
                //! key名称，省份的名称
                string keyString = curDB.Key;

                //！判断记录省份apppath的 pid列表是否为null，则说明省份执行结束了，否则进入等待
                //1\查询pids是否为空
                while(pids.Count > 0)
                {
                    //! 执行等待，然后查询更新pids列表.等待5分钟
                    Console.WriteLine(string.Format("等待前一个省份区域进程计算完成并关闭，pid进程查询更新等待中...") + DateTime.Now);
                    System.Threading.Thread.Sleep(1000 * 60 * 1);

                    //！ 遍历pids，查询windows process中是否存在这个pid，不存在，则移除
                    int pidnum = pids.Count;
                    foreach (var item in pids.ToList())
                    {
                        int curPID = item.Value;
                        Process curProcss = null;
                        try
                        {
                            curProcss = Process.GetProcessById(curPID);
                        }
                        catch (Exception ex)
                        {
                            curProcss = null;
                        }
                        bool isInProcess = curProcss == null ? false : true; 
                        if (!isInProcess)
                        {
                            pids.Remove(item.Key);
                        }
                    }

                    if (pids.Count == 0)
                    {
                        break;
                    }
                }

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
                        //12、写出bat，执行,传入bat批量启动全路径
                        if (dbTableConfigs[keyString].Count > 0)
                        {
                            //！每个省下的所有app
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


                                //! 启动该exec.bat
                                bool isOneStart = StartOneByOneExec(execpath, ComputeUnit);
                                if (isOneStart)
                                {
                                    Console.WriteLine(string.Format("{0}区域{1}执行成功  ", keyString, execpath) + DateTime.Now);
                                }else
                                {
                                    Console.WriteLine(string.Format("{0}区域{1}执行失败  ", keyString, execpath) + DateTime.Now);
                                }
                                
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("{0}区域所有单元执行失败  ", keyString) + DateTime.Now);
                        }

                    }
                }

            }


            return true;
        }

        public static bool StartOneByOneExec(string appPath, string computerunit)
        {
            if (!File.Exists(appPath))
            {
                Console.WriteLine(string.Format("{0}启动文件不存在  ", appPath) + DateTime.Now);
                return false;
            }

            try
            {
                #region window专用
                Process myProcess = new Process();
                string fileName = appPath;
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(fileName);
                if (!HookHelper.isshowcmd)
                {
                    myProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏黑屏，不让执行exe的黑屏弹出
                }
                
                myProcess.StartInfo = myProcessStartInfo;
                myProcess.StartInfo.Arguments = appPath;
                bool isStart = myProcess.Start();

                if (isStart)
                {
                    pids.Add(int.Parse(computerunit), myProcess.Id);
                }else
                {
                    Console.WriteLine(string.Format("{0}启动文件通过process执行start异常  ", appPath) + DateTime.Now);
                    return false;
                }
                

                //myProcess.WaitForExit();
                #endregion
            }
            catch (Exception exp)
            {
                //MessageBox.Show(exp.Message, exp.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(string.Format("{0}启动文件通过process执行start异常  ", appPath) + DateTime.Now);
                return false;
            }

            return true;
        }

        public static bool StartOneByOneExecsingle(string appPath, string computerunit)
        {
            if (!File.Exists(appPath))
            {
                Console.WriteLine(string.Format("{0}启动文件不存在  ", appPath) + DateTime.Now);
                return false;
            }

            try
            {
                #region window专用
                Process myProcess = new Process();
                string fileName = appPath;
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(fileName);
                if (!HookHelper.isshowcmd)
                {
                    myProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏黑屏，不让执行exe的黑屏弹出
                }

                myProcess.StartInfo = myProcessStartInfo;
                myProcess.StartInfo.Arguments = appPath;
                bool isStart = myProcess.Start();

                if (isStart)
                {
                    pids.Add(int.Parse(computerunit), myProcess.Id);
                }
                else
                {
                    Console.WriteLine(string.Format("{0}启动文件通过process执行start异常  ", appPath) + DateTime.Now);
                    return false;
                }


                //myProcess.WaitForExit();
                #endregion
            }
            catch (Exception exp)
            {
                //MessageBox.Show(exp.Message, exp.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(string.Format("{0}启动文件通过process执行start异常  ", appPath) + DateTime.Now);
                return false;
            }

            return true;
        }
        
        public static bool runBySingleCC()
        {
            //! 遍历指定目录下的降雨数据
            if (!Directory.Exists(HookHelper.rainSRCDirectory))
            {
                Console.WriteLine(string.Format("{0}台风场dat降雨目录不存在  ", HookHelper.rainSRCDirectory) + DateTime.Now);
                return false;
            }

            FileInfo[] fInfo = GenRainTile.GetRaindatList();
            int datnum = fInfo.Length;
            for (int d = 0; d < datnum; ++d)
            {
                //! 当前dat文件全路径
                string curDatFullname = fInfo[d].FullName;

                Console.WriteLine(string.Format("*****************************{0}场次Start**************************", curDatFullname) + DateTime.Now);
                //！ 遍历每个省的网格模型
                foreach (var curDB in dbValues)
                {
                    //! key名称，省份的名称
                    string keyString = curDB.Key;

                    //！判断记录省份apppath的 pid列表是否为null，则说明省份执行结束了，否则进入等待
                    //1\查询pids是否为空
                    while (pids.Count > 0)
                    {
                        //! 执行等待，然后查询更新pids列表.等待5分钟
                        Console.WriteLine(string.Format("等待前一个省份区域进程计算完成并关闭，pid进程查询更新等待中...") + DateTime.Now);
                        System.Threading.Thread.Sleep(1000 * 60 * 1);

                        //！ 遍历pids，查询windows process中是否存在这个pid，不存在，则移除
                        int pidnum = pids.Count;
                        foreach (var item in pids.ToList())
                        {
                            int curPID = item.Value;
                            Process curProcss = null;
                            try
                            {
                                curProcss = Process.GetProcessById(curPID);
                            }
                            catch (Exception ex)
                            {
                                curProcss = null;
                            }
                            bool isInProcess = curProcss == null ? false : true;
                            if (!isInProcess)
                            {
                                pids.Remove(item.Key);
                            }
                        }

                        if (pids.Count == 0)
                        {
                            break;
                        }
                    }

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
                            //！执行python切片
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

                            //! 启动bat前每场的时间不同，要更新写出execsingle.bat
                            if (dbTableConfigs[keyString].Count > 0)
                            {
                                int appnum = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows.Count;
                                for (int a = 0; a < appnum; ++a)
                                {
                                    //!当前路径
                                    string apppath = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                                    string ComputeUnit = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
                                    //execbat路径
                                    string execpath = apppath + "execsingle.bat";
                                    if (apppath.EndsWith("/"))
                                    {
                                        execpath = apppath + "execsingle.bat";
                                    }
                                    else
                                    {
                                        execpath = apppath + "\\" + "execsingle.bat";
                                    }


                                    string outpath = HookHelper.rainTileDirectory;

                                    // 更新execpath的值
                                    bool isUpExec = false;
                                    //！覆盖更新通过模板文件
                                    string start = "2003-11-15T13:00";
                                    string end = "2003-11-15T13:00";
                                    string datnums = "95";

                                    string datPureName = System.IO.Path.GetFileNameWithoutExtension(curDatFullname);
                                    isUpExec = GenRainTile.UpdateExecBatFileByTemplateExecsingle(execpath, ComputeUnit, start, end, datnums, datPureName, HookHelper.rainTileDirectory);

                                    if (isUpExec)
                                    {
                                        Console.WriteLine(string.Format("{0}区域{1}文件exec.bat更新成功  ", keyString, apppath) + DateTime.Now);
                                    }
                                }

                            }

                            //12、遍历一个个启动
                            if (dbTableConfigs[keyString].Count > 0)
                            {
                                //！每个省下的所有app
                                int appnum = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows.Count;
                                for (int a = 0; a < appnum; ++a)
                                {
                                    //!当前路径
                                    string apppath = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                                    string ComputeUnit = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
                                    //execbat路径
                                    string execpath = apppath + "execsingle.bat";
                                    if (apppath.EndsWith("/"))
                                    {
                                        execpath = apppath + "execsingle.bat";
                                    }
                                    else
                                    {
                                        execpath = apppath + "\\" + "execsingle.bat";
                                    }


                                    //! 启动该exec.bat
                                    bool isOneStart = StartOneByOneExecsingle(execpath, ComputeUnit);
                                    if (isOneStart)
                                    {
                                        Console.WriteLine(string.Format("{0}区域{1}执行成功  ", keyString, execpath) + DateTime.Now);
                                    }
                                    else
                                    {
                                        Console.WriteLine(string.Format("{0}区域{1}执行失败  ", keyString, execpath) + DateTime.Now);
                                    }

                                }
                            }
                            else
                            {
                                Console.WriteLine(string.Format("{0}区域所有单元执行bat启动失败  ", keyString) + DateTime.Now);
                            }

                        }
                    }

                }

                Console.WriteLine(string.Format("{0}台风场所有省份计算完成  ", curDatFullname) + DateTime.Now);
                Console.WriteLine(string.Format("*****************************{0}场次END**************************", curDatFullname) + DateTime.Now);
            }

            return true;
        }
        
    }
}

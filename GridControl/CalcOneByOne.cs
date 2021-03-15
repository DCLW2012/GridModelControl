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
        //！ pid列表，通过定时查询用来判断是否清空key 是pid value是单元信息
        public static Dictionary<int, string> pids = new Dictionary<int, string>();

        public static bool StartOneByOneExecsingle(string appPath, string appunitInfo)
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
                if (!HookHelper.isshowchildprocess)
                {
                    myProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏黑屏，不让执行exe的黑屏弹出
                }

                myProcess.StartInfo = myProcessStartInfo;
                myProcess.StartInfo.Arguments = appPath;
                bool isStart = myProcess.Start();

                if (isStart)
                {
                    pids.Add(myProcess.Id, appunitInfo);
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
            // 更新execpath的值
            string start = "2000-01-01T00:00";
            string end = "2000-01-01T08:00";
            string datnums = "95";

            string yearmmddForID = "2000010100";

            //! 设置计时器，当前场次时间
            Stopwatch totalFolderDat = new Stopwatch();
            totalFolderDat.Start();
            //! 遍历指定目录下的降雨数据
            if (!Directory.Exists(HookHelper.rainSRCDirectory))
            {
                Console.WriteLine(string.Format("{0}台风场dat降雨目录不存在  ", HookHelper.rainSRCDirectory) + DateTime.Now);
                return false;
            }

            FileInfo[] fInfo = GenRainTileByPython.GetRaindatList();
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

                    if (!String.IsNullOrWhiteSpace(HookHelper.curProvince))
                    {
                        if (!keyString.ToUpper().Equals(HookHelper.curProvince.ToUpper()))
                        {
                            continue;
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
                                //! 设置计时器，当前场次时间
                                Stopwatch perChangci = new Stopwatch();
                                perChangci.Start();

                                bool isGenTilesucess = GenRainTileByCSharp.CreateTileByWATAByCSharpFromProvince(keyString, curDatFullname, ref start, ref end, ref datnums, ref yearmmddForID);


                                perChangci.Stop();
                                TimeSpan perChangciTime = perChangci.Elapsed;
                                if (!isGenTilesucess)
                                {
                                    //Console.WriteLine(string.Format("{0}区域降雨切片执行失败  ", HookHelper.computerNode) + DateTime.Now);
                                    continue;
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("网格{0}场次降雨切片执行耗时：{1}秒", curDatFullname, perChangciTime.TotalMilliseconds / 1000));
                                    HookHelper.Log += string.Format("网格{0}场次降雨切片执行耗时：{1}秒", curDatFullname, perChangciTime.TotalMilliseconds / 1000) + DateTime.Now + ";\r\n";
                                    Console.WriteLine(string.Format("{0}区域降雨切片执行成功  ", HookHelper.computerNode) + DateTime.Now);
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
                                    string datPureName = System.IO.Path.GetFileNameWithoutExtension(curDatFullname);
                                    isUpExec = WriteExecBatFile.UpdateExecBatFileByTemplateExecsingle(execpath, ComputeUnit, start, end, datnums, datPureName, HookHelper.rainTileDirectory, yearmmddForID);

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
                                //!判断appnum 是否超过了processnum，是则部分启动，等待
                                //!先根据个数分组，分组后，则循环，则可以等待了
                                int processGroup = (int)Math.Ceiling((float)appnum / (float)HookHelper.processnum);


                                for (int g = 0; g < processGroup; ++g)
                                {
                                    //!循环每个组，pid存在，则执行等待
                                    int perGroupCount = 0;
                                    while (pids.Count > 0)
                                    {
                                        //! 执行等待，然后查询更新pids列表.等待1分钟
                                        Console.WriteLine(string.Format("共{0}个分组", processGroup) + DateTime.Now);
                                        Console.WriteLine(string.Format("等待第{0}场{1}文件的第{2}进程组计算完成并关闭，pid进程查询更新等待中，等待时长15秒...", d + 1, curDatFullname, g) + DateTime.Now);
                                        System.Threading.Thread.Sleep(1000 * 15 * 1);

                                        //kill
                                        perGroupCount++;
                                        Console.WriteLine(string.Format("已经等待次数{0}次", perGroupCount) + DateTime.Now);
                                        if (perGroupCount >= HookHelper.waitcount)
                                        {
                                            //遍历强制关闭当前场次的所有pid程序
                                            foreach (var item in pids.ToList())
                                            {
                                                int curPID = item.Key;
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
                                                if (isInProcess)
                                                {
                                                    //curProcss.Kill();
                                                    HookHelper.KillProcessAndChildren(curPID);
                                                }
                                            }
                                        }

                                        //！ 遍历pids，查询windows process中是否存在这个pid，不存在，则移除
                                        int pidnum = pids.Count;
                                        foreach (var item in pids.ToList())
                                        {
                                            int curPID = item.Key;
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
                                            else
                                            {
                                                Console.WriteLine(string.Format("单元{0}所在分组{1}计算进行中......需继续等待......", item.Value, g) + DateTime.Now);
                                            }
                                        }

                                        if (pids.Count == 0)
                                        {
                                            break;
                                        }
                                    }

                                    //当前分组的起始值，和end值
                                    int startPROCESS = g * HookHelper.processnum;
                                    int endPROCESS = (g + 1) * HookHelper.processnum;
                                    if (g == processGroup - 1)
                                    {
                                        endPROCESS = appnum;
                                    }

                                    int validStartUnitModel = 0;
                                    for (int a = startPROCESS; a < endPROCESS; ++a)
                                    {
                                        //!当前路径
                                        string apppath = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                                        string ComputeUnit = dbTableConfigs[keyString]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
                                        string ComputeNode = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["ComputeNode"].ToString();
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
                                        //! 单元信息
                                        string appunitInfo = ComputeNode + "_" + ComputeUnit + "_" + apppath;
                                        bool isOneStart = StartOneByOneExecsingle(execpath, appunitInfo);
                                        if (isOneStart)
                                        {
                                            validStartUnitModel++;
                                            //Console.WriteLine(string.Format("{0}节点{1}单元{2}路径执行成功  ", HookHelper.computerNode, ComputeUnit, execpath) + DateTime.Now);
                                        }
                                        else
                                        {
                                            //Console.WriteLine(string.Format("{0}节点{1}单元{2}路径执行失败  ", HookHelper.computerNode, ComputeUnit, execpath) + DateTime.Now);
                                        }

                                    }
                                    Console.WriteLine(string.Format("{0}节点{1}个有效单元启动命令执行成功  ", HookHelper.computerNode, validStartUnitModel) + DateTime.Now);
                                }

                                //!循环每个组，pid存在，则执行等待，直至继续运行到下一步，代表一个场次计算结束
                                int perWaitCount = 0;  //如果等待超过1个小时，仍然无法计算，则跳过这个场次，并写出到log中
                                while (pids.Count > 0)
                                {
                                    //! 执行等待，然后查询更新pids列表.等待1分钟
                                    Console.WriteLine(string.Format("等待第{0}场{1}文件计算完成并关闭，pid进程查询更新等待中，等待时长15秒...", d + 1, curDatFullname) + DateTime.Now);
                                    System.Threading.Thread.Sleep(1000 * 15 * 1);
                                    perWaitCount++;
                                    Console.WriteLine(string.Format("已经等待次数{0}次", perWaitCount) + DateTime.Now);
                                    if (perWaitCount >= HookHelper.waitcount)
                                    {
                                        //遍历强制关闭当前场次的所有pid程序
                                        //将该场次值写出到log文件中
                                        string ignoreCCName = curDatFullname;
                                        WriteLog.AppendLogMethod(ignoreCCName, "datIgnore");
                                        foreach (var item in pids.ToList())
                                        {
                                            int curPID = item.Key;
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
                                            if (isInProcess)
                                            {
                                                //curProcss.Kill();
                                                HookHelper.KillProcessAndChildren(curPID);
                                            }
                                            else
                                            {
                                                Console.WriteLine(string.Format("最后一组单元{0}计算进行中......需继续等待......", item.Value) + DateTime.Now);
                                            }
                                        }
                                    }

                                    //！ 遍历pids，查询windows process中是否存在这个pid，不存在，则移除
                                    int pidnum = pids.Count;
                                    foreach (var item in pids.ToList())
                                    {
                                        int curPID = item.Key;
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
                                        else
                                        {
                                            Console.WriteLine(string.Format("最后一组单元{0}计算进行中......需继续等待......", item.Value) + DateTime.Now);
                                        }
                                    }

                                    if (pids.Count == 0)
                                    {
                                        break;
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

            totalFolderDat.Stop();
            TimeSpan totalFolderDatTime = totalFolderDat.Elapsed;
            Console.WriteLine(string.Format("{0}降雨目录下{1}个降雨文件从降雨切片->bat信息更新->等待网格流域计算，总耗时：{2}秒", HookHelper.rainSRCDirectory, datnum, totalFolderDatTime.TotalMilliseconds / 1000));
            HookHelper.Log += string.Format("{0}降雨目录下{1}个降雨文件从降雨切片->bat信息更新->等待网格流域计算，总耗时：{2}秒", HookHelper.rainSRCDirectory, datnum, totalFolderDatTime.TotalMilliseconds / 1000) + DateTime.Now + ";\r\n";
            return true;
        }
        
    }
}

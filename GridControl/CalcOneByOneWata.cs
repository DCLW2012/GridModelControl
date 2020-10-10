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
    public class CalcOneByOneWata
    {
        // 每个省对应一个数据库连接，每个连接里包含了降雨切片目录
        public static Dictionary<string, Dictionary<string, string>> dbValues = ClientConn.m_dbTableTypes;
        public static Dictionary<string, Dictionary<string, DataTable>> dbTableConfigs = ClientConn.m_dbTableConfig;
        //！ pid列表，通过定时查询用来判断是否清空
        public static Dictionary<int, int> pids = new Dictionary<int, int>();
        public static bool run()
        {
            int num = dbValues.Count;


            //! 启动bat前由于流域模式的 unit id和 省模式下id不一样，要更新
            if (dbTableConfigs["china"].Count > 0)
            {
                int appnum = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows.Count;

                //!判断appnum 是否超过了processnum，是则部分启动，等待
                //!先根据个数分组，分组后，则循环，则可以等待了
                int processGroup = (int)Math.Ceiling((float)appnum / (float)HookHelper.processnum);

                for (int g = 0; g < processGroup; ++g)
                {
                    //!循环每个组，pid存在，则执行等待
                    while (pids.Count > 0)
                    {
                        //! 执行等待，然后查询更新pids列表.等待1分钟
                        Console.WriteLine(string.Format("等待前一个流域进程组计算完成并关闭，pid进程查询更新等待中，等待时长60秒...") + DateTime.Now);
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

                    //当前分组的起始值，和end值
                    int start = g * HookHelper.processnum;
                    int end = (g + 1) * HookHelper.processnum;
                    if (g == processGroup - 1)
                    {
                        end = appnum;
                    }

                    for (int a = start; a < end; ++a)
                    {
                        //!当前路径
                        string apppath = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                        string ComputeUnit = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
                        string provinceName = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["province"].ToString();

                        //execbat路径
                        string execpath = apppath + "execcctable.bat";
                        if (apppath.EndsWith("/"))
                        {
                            execpath = apppath + "execcctable.bat";
                        }
                        else
                        {
                            execpath = apppath + "\\" + "execcctable.bat";
                        }

                        string outrainTilepath = dbValues[provinceName]["rainTileFolder"];
                        bool isUpExec = false;
                        //！execcctable.bat文件
                        isUpExec = WriteExecBatFile.UpdateexeccctableBatFileByWATAChina(execpath, ComputeUnit, outrainTilepath);

                        if (isUpExec)
                        {
                            Console.WriteLine(string.Format("{0}区域{1}execcctable.bat更新成功  ", "china", apppath) + DateTime.Now);
                        }
                    }

                    if (dbTableConfigs["china"].Count > 0)
                    {
                        //！分组所有app
                        for (int a = start; a < end; ++a)
                        {
                            //!当前路径
                            string apppath = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                            string ComputeUnit = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
                            //execbat路径
                            string execpath = apppath + "execcctable.bat";
                            if (apppath.EndsWith("/"))
                            {
                                execpath = apppath + "execcctable.bat";
                            }
                            else
                            {
                                execpath = apppath + "\\" + "execcctable.bat";
                            }


                            //! 启动该exec.bat
                            bool isOneStart = StartOneByOneExec(execpath, ComputeUnit);
                            if (isOneStart)
                            {
                                Console.WriteLine(string.Format("{0}节点{1}单元{2}路径执行成功  ", HookHelper.computerNode, ComputeUnit, execpath) + DateTime.Now);
                            }
                            else
                            {
                                Console.WriteLine(string.Format("{0}节点{1}单元{2}路径执行失败  ", HookHelper.computerNode, ComputeUnit, execpath) + DateTime.Now);
                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0}区域所有单元执行失败  ", HookHelper.computerNode) + DateTime.Now);
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
            //!! 场次信息回传
            // 更新execpath的值
            string start = "2003-11-15T13:00";
            string end = "2003-11-15T13:00";
            string datnums = "95";

            //! 遍历指定目录下的降雨数据
            //! 设置计时器，当前场次时间
            Stopwatch totalFolderDat = new Stopwatch();
            totalFolderDat.Start();
            if (!Directory.Exists(HookHelper.rainSRCDirectory))
            {
                Console.WriteLine(string.Format("{0}台风场dat降雨目录不存在  ", HookHelper.rainSRCDirectory) + DateTime.Now);
                return false;
            }

            FileInfo[] fInfo = GenRainTileByCSharp.GetRaindatList();
            int datnum = fInfo.Length;
            for (int d = 0; d < datnum; ++d)
            {
                //! 当前dat文件全路径
                //! 设置计时器，当前场次时间
                Stopwatch oneDat = new Stopwatch();
                oneDat.Start();
                string curDatFullname = fInfo[d].FullName;

                Console.WriteLine(string.Format("****************************降雨目录下第{0}场*********A************", d+1) + DateTime.Now);
                Console.WriteLine(string.Format("*****************************************************AAA***********") + DateTime.Now);
                Console.WriteLine(string.Format("*****************************{0}场次Start***********AAAAA**********", curDatFullname) + DateTime.Now);
                Console.WriteLine(string.Format("***************************************************AAAAAAA*********") + DateTime.Now);
                Console.WriteLine(string.Format("**************************************************AAAAAAAAA********") + DateTime.Now);
                //！ 遍历每个流域的网格模型
                //！执行python切片
                //!1、执行切片，调用python执行
                if (HookHelper.isgenraintile)
                {
                    //! 设置计时器，当前场次时间
                    Stopwatch perChangci = new Stopwatch();
                    perChangci.Start();
                    bool isGenTilesucess = GenRainTileByCSharp.CreateTileByWATAByCSharp(curDatFullname, ref start, ref end, ref datnums);
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
                if (dbTableConfigs["china"].Count > 0)
                {
                    int appnum = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows.Count;
                    for (int a = 0; a < appnum; ++a)
                    {
                        //!当前路径
                        string apppath = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                        string ComputeUnit = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
                        string provinceName = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["province"].ToString();

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


                        string outrainTilepath = dbValues[provinceName]["rainTileFolder"];

                        // 更新execpath的值
                        bool isUpExec = false;
                        //！覆盖更新通过指定参数到execsingle.bat文件

                        string datPureName = System.IO.Path.GetFileNameWithoutExtension(curDatFullname);
                        isUpExec = WriteExecBatFile.UpdateExecBatFileByTemplateExecsingle(execpath, ComputeUnit, start, end, datnums, datPureName, outrainTilepath);

                        if (isUpExec)
                        {
                            Console.WriteLine(string.Format("{0}区域{1}文件exec.bat更新成功  ", "china", apppath) + DateTime.Now);
                        }else
                        {
                            Console.WriteLine(string.Format("{0}区域{1}文件exec.bat更新失败  ", "china", apppath) + DateTime.Now);
                        }
                    }

                    //!判断appnum 是否超过了processnum，是则部分启动，等待
                    //!先根据个数分组，分组后，则循环，则可以等待了
                    int processGroup = (int)Math.Ceiling((float)appnum / (float)HookHelper.processnum);

                    for (int g = 0; g < processGroup; ++g)
                    {
                        //!循环每个组，pid存在，则执行等待
                        while (pids.Count > 0)
                        {
                            //! 执行等待，然后查询更新pids列表.等待1分钟
                            Console.WriteLine(string.Format("等待第{0}场{1}文件的第{2}进程组计算完成并关闭，pid进程查询更新等待中，等待时长60秒...", d+1, curDatFullname, g+1) + DateTime.Now);
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

                        //当前分组的起始值，和end值
                        int startPROCESS = g * HookHelper.processnum;
                        int endPROCESS = (g + 1) * HookHelper.processnum;
                        if (g == processGroup - 1)
                        {
                            endPROCESS = appnum;
                        }

                        for (int a = 0; a < appnum; ++a)
                        {
                            //!当前路径
                            string apppath = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                            string ComputeUnit = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
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
                                Console.WriteLine(string.Format("{0}节点{1}单元{2}路径执行成功  ", HookHelper.computerNode, ComputeUnit, execpath) + DateTime.Now);
                            }
                            else
                            {
                                Console.WriteLine(string.Format("{0}节点{1}单元{2}路径执行失败  ", HookHelper.computerNode, ComputeUnit, execpath) + DateTime.Now);
                            }

                        }
                    }
                }

                //!循环每个组，pid存在，则执行等待，直至继续运行到下一步，代表一个场次计算结束
                while (pids.Count > 0)
                {
                    //! 执行等待，然后查询更新pids列表.等待1分钟
                    Console.WriteLine(string.Format("等待第{0}场{1}文件计算完成并关闭，pid进程查询更新等待中，等待时长60秒...",d+1, curDatFullname) + DateTime.Now);
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
                Console.WriteLine(string.Format("{0}台风场所有流域计算完成  ", curDatFullname) + DateTime.Now);
                HookHelper.Log += string.Format("{0}台风场所有流域计算完成  ", curDatFullname) + DateTime.Now + ";\r\n";
                Console.WriteLine(string.Format("******************************************************A************") + DateTime.Now);
                Console.WriteLine(string.Format("*****************************************************AAA***********") + DateTime.Now);
                Console.WriteLine(string.Format("*****************************{0}场次END*************AAAAA**********", curDatFullname) + DateTime.Now);
                Console.WriteLine(string.Format("***************************************************AAAAAAA*********") + DateTime.Now);
                Console.WriteLine(string.Format("**************************************************AAAAAAAAA********") + DateTime.Now);
                Console.WriteLine(string.Format("****************************降雨目录下第{0}场结束******************", d+1) + DateTime.Now);
                oneDat.Stop();
                TimeSpan oneDatTime = oneDat.Elapsed;
                Console.WriteLine(string.Format("网格{0}场次降雨切片->bat信息更新->等待网格流域计算，单场次全流程耗时：{1}秒", curDatFullname, oneDatTime.TotalMilliseconds / 1000));
                HookHelper.Log += string.Format("网格{0}场次降雨切片->bat信息更新->等待网格流域计算，单场次全流程耗时：{1}秒", curDatFullname, oneDatTime.TotalMilliseconds / 1000) + DateTime.Now + ";\r\n";
            }

            Console.WriteLine(string.Format("{0}场台风场次逐场次流域计算完成  ", datnum) + DateTime.Now);
            HookHelper.Log += string.Format("{0}场台风场次逐场次流域计算完成  ", datnum) + DateTime.Now + ";\r\n";
            Console.WriteLine(string.Format("*********(-_-) **************{0}场台风场次逐场次流域计算完成*******..~^_^~..***********", datnum) + DateTime.Now);
            Console.WriteLine(string.Format("*********(-_-) ***************{0}场台风场次逐场次流域计算完成*******..~^_^~..**********", datnum) + DateTime.Now);
            Console.WriteLine(string.Format("*********(-_-) ***************{0}场台风场次逐场次流域计算完成*******..~^_^~..**********", datnum) + DateTime.Now);
            Console.WriteLine(string.Format("********(-_-) **************{0}场台风场次逐场次流域计算完成*******..~^_^~..**********", datnum) + DateTime.Now);
            Console.WriteLine(string.Format("********(-_-) ***************{0}场台风场次逐场次流域计算完成*******..~^_^~..*********", datnum) + DateTime.Now);
            Console.WriteLine(string.Format("********(-_-) ****************{0}场台风场次逐场次流域计算完成*******..~^_^~..**********", datnum) + DateTime.Now);
            totalFolderDat.Stop();
            TimeSpan totalFolderDatTime = totalFolderDat.Elapsed;
            Console.WriteLine(string.Format("{0}降雨目录下{1}个降雨文件从降雨切片->bat信息更新->等待网格流域计算，总耗时：{2}秒", HookHelper.rainSRCDirectory, datnum, totalFolderDatTime.TotalMilliseconds / 1000));
            HookHelper.Log += string.Format("{0}降雨目录下{1}个降雨文件从降雨切片->bat信息更新->等待网格流域计算，总耗时：{2}秒", HookHelper.rainSRCDirectory, datnum, totalFolderDatTime.TotalMilliseconds / 1000) + DateTime.Now + ";\r\n";
            return true;
        }

    }
}

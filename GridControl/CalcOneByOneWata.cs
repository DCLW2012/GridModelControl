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


            //! 遍历全国表中computernode指定的所有单元
            //12、写出bat，执行,传入bat批量启动全路径
            if (dbTableConfigs["china"].Count > 0)
            {
                //！每个省下的所有app
                int appnum = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows.Count;
                for (int a = 0; a < appnum; ++a)
                {
                    //!当前路径
                    string apppath = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["AppPath"].ToString();
                    string ComputeUnit = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows[a]["ComputeUnit"].ToString();
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
                        Console.WriteLine(string.Format("{0}区域{1}执行成功  ", HookHelper.computerNode, execpath) + DateTime.Now);
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0}区域{1}执行失败  ", HookHelper.computerNode, execpath) + DateTime.Now);
                    }

                }
            }
            else
            {
                Console.WriteLine(string.Format("{0}区域所有单元执行失败  ", HookHelper.computerNode) + DateTime.Now);
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
                //！ 遍历每个流域的网格模型
                ////! 只有配置了raintile目录才执行计算
                //string keyTableType = curTBType.Key;
                //string folderPath = curTBType.Value;
                //HookHelper.rainTileDirectory = "";
                //if (keyTableType.ToUpper() == "RAINTILEFOLDER")
                //{
                    

                //}
                //HookHelper.rainTileDirectory = folderPath;
                //！执行python切片
                //!1、执行切片，调用python执行
                if (HookHelper.isgenraintile)
                {
                    bool isGenTilesucess = GenRainTile.CreateTileByWATA(curDatFullname);

                    if (!isGenTilesucess)
                    {
                        Console.WriteLine(string.Format("{0}区域降雨切片执行失败  ", HookHelper.computerNode) + DateTime.Now);
                        continue;
                    }
                    else
                    {
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


                        string outpath = HookHelper.rainTileDirectory;

                        // 更新execpath的值
                        //二进制的形式读取dat文件，获取文件头内容
                        // 读取文件
                        BinaryReader br;
                        string start = "2003-11-15T13:00";
                        string end = "2003-11-15T13:00";
                        string datnums = "95";
                        try
                        {
                            br = new BinaryReader(new FileStream(curDatFullname,
                                            FileMode.Open));
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine(string.Format("{0}台风场文件解析场次信息失败，继续下一个", curDatFullname) + DateTime.Now);
                            continue;
                        }
                        try
                        {
                            //! 第一部分数据 年(year)、月日时(mdh)、该台风总时次(times) 均为整型  3 * 4 个字节
                            //inFile.read((char*)&datStruct.headerone[0], 3 * sizeof(int));
                            int year = br.ReadInt32();
                            int mdh = br.ReadInt32();
                            int times = br.ReadInt32();

                            string mdhSt = mdh.ToString();
                            if (mdhSt.Length == 5)
                            {
                                mdhSt = String.Format("0{0}", mdhSt);
                            }

                            string ymdhstr = String.Format("{0}{1}", year, mdhSt);
                            DateTime dt = Convert.ToDateTime(ymdhstr.Substring(0, 4) + "-" + ymdhstr.Substring(4, 2) + "-" + ymdhstr.Substring(6, 2) + " " + ymdhstr.Substring(8, 2) + ":00:00");
                            start = dt.ToString("yyyy-MM-ddThh:mm");
                            end = (dt.AddHours(times - 1)).ToString("yyyy-MM-ddThh:mm");
                            datnums = times.ToString();
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine(string.Format("{0}台风场文件解析场次信息失败，继续下一个", curDatFullname) + DateTime.Now);
                            continue;
                        }
                        br.Close();

                        bool isUpExec = false;
                        //！覆盖更新通过指定参数到execsingle.bat文件

                        string datPureName = System.IO.Path.GetFileNameWithoutExtension(curDatFullname);
                        isUpExec = GenRainTile.UpdateExecBatFileByTemplateExecsingle(execpath, ComputeUnit, start, end, datnums, datPureName);

                        if (isUpExec)
                        {
                            Console.WriteLine(string.Format("{0}区域{1}文件exec.bat更新成功  ", "china", apppath) + DateTime.Now);
                        }
                    }

                }

                //12、遍历一个个启动
                if (dbTableConfigs["china"].Count > 0)
                {
                    //！每个省下的所有app
                    int appnum = dbTableConfigs["china"]["HSFX_ComputeUnit"].Rows.Count;
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
                            Console.WriteLine(string.Format("{0}区域{1}执行成功  ", HookHelper.computerNode, execpath) + DateTime.Now);
                        }
                        else
                        {
                            Console.WriteLine(string.Format("{0}区域{1}执行失败  ", HookHelper.computerNode, execpath) + DateTime.Now);
                        }

                    }
                }
                else
                {
                    Console.WriteLine(string.Format("{0}区域所有单元执行bat启动失败  ", HookHelper.computerNode) + DateTime.Now);
                }

                Console.WriteLine(string.Format("{0}台风场所有流域计算完成  ", curDatFullname) + DateTime.Now);
                Console.WriteLine(string.Format("*****************************{0}场次END**************************", curDatFullname) + DateTime.Now);
            }

            return true;
        }

    }
}

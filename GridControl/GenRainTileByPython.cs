using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Common;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SysDAL;
using System.Data;

namespace GridControl
{
    public class GenRainTileByPython
    {
        // 每个省对应一个数据库连接，每个连接里包含了降雨切片目录
        public static Dictionary<string, Dictionary<string, string>> dbValues = ClientConn.m_dbTableTypes;
        public static Dictionary<string, Dictionary<string, DataTable>> dbTableConfigs = ClientConn.m_dbTableConfig;

        //！根据传入的输出目录，调用python
        public static bool CreateTile(string datFullname, string ouput)
        {
            //！写出bat调用python脚本
            string path1 = System.IO.Directory.GetCurrentDirectory();
            #region 生成执行文件
            string p = System.IO.Directory.GetDirectoryRoot(path1);
            string Contents = p.Substring(0, p.Length - 1) + "\r\ncd " + path1 + "//script\r\n" + path1 + "//script//tile.bat";
            string path = path1 + "//script//Execution.bat";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            FileStream fs2 = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            StreamWriter sw2 = new StreamWriter(fs2, System.Text.Encoding.GetEncoding("GB2312"));
            //sw2.Write(Contents);
            sw2.WriteLine(Contents);
            sw2.Close();
            fs2.Close();

            #endregion

            #region window专用
            Process myProcess = new Process();
            string fileName = path1 + "//script//Execution.bat";
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(fileName);
            if (!HookHelper.isshowchildprocess)
            {
                myProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏黑屏，不让执行exe的黑屏弹出
            }
            myProcess.StartInfo = myProcessStartInfo;
            myProcess.Start();
            myProcess.WaitForExit();
            #endregion

            //string path1 = System.IO.Directory.GetCurrentDirectory();
            //string pythonFullname = path1 + "//script//" + HookHelper.raindataForPython;
            //Process p = new Process(); // create process (i.e., the python program
            //p.StartInfo.FileName = "python.exe";
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.UseShellExecute = false; // make sure we can read the output from stdout
            //p.StartInfo.Arguments = pythonFullname + datFullname; // start the python program with two parameters
            //p.Start(); // start the process (the python program)

            //StreamReader s = p.StandardOutput;
            //String output = s.ReadToEnd();
            //string[] r = output.Split(new char[] { ' ' }); // get the parameter
            //Console.WriteLine(r[0]);

            //p.WaitForExit();
            return true;
        }

        public static bool CreateTileByWATA(string datFullname)
        {
            //！写出bat调用python脚本
            string path1 = System.IO.Directory.GetCurrentDirectory();
            #region 生成执行文件
            string p = System.IO.Directory.GetDirectoryRoot(path1);
            string Contents = p.Substring(0, p.Length - 1) + "\r\ncd " + path1 + "//script\r\n" + path1 + "//script//tile.bat";
            string path = path1 + "//script//Execution.bat";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            FileStream fs2 = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            StreamWriter sw2 = new StreamWriter(fs2, System.Text.Encoding.GetEncoding("GB2312"));
            //sw2.Write(Contents);
            sw2.WriteLine(Contents);
            sw2.Close();
            fs2.Close();

            #endregion

            #region window专用
            Process myProcess = new Process();
            string fileName = path1 + "//script//Execution.bat";
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(fileName);
            if (!HookHelper.isshowchildprocess)
            {
                myProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏黑屏，不让执行exe的黑屏弹出
            }
            myProcess.StartInfo = myProcessStartInfo;
            myProcess.Start();
            myProcess.WaitForExit();
            #endregion

            //string path1 = System.IO.Directory.GetCurrentDirectory();
            //string pythonFullname = path1 + "//script//" + HookHelper.raindataForPython;
            //Process p = new Process(); // create process (i.e., the python program
            //p.StartInfo.FileName = "python.exe";
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.UseShellExecute = false; // make sure we can read the output from stdout
            //p.StartInfo.Arguments = pythonFullname + datFullname; // start the python program with two parameters
            //p.Start(); // start the process (the python program)

            //StreamReader s = p.StandardOutput;
            //String output = s.ReadToEnd();
            //string[] r = output.Split(new char[] { ' ' }); // get the parameter
            //Console.WriteLine(r[0]);

            //p.WaitForExit();
            return true;
        }


        public static bool StartCurDBBatGroup(string batRootPath, bool isRewrite)
        {
            if (isRewrite)
            {
                //获取应用程序的当前工作目录。 
                string path1 = System.IO.Directory.GetCurrentDirectory();
                #region 生成执行文件
                string p = System.IO.Directory.GetDirectoryRoot(path1);
                string Contents = p.Substring(0, p.Length - 1) + "\r\ncd " + path1 + "\\script\r\n" + "python " + HookHelper.raindataForPython;
                string path = path1 + "//script//tile.bat";
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                FileStream fs2 = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw2 = new StreamWriter(fs2, System.Text.Encoding.GetEncoding("GB2312"));
                //sw2.Write(Contents);
                sw2.WriteLine(Contents);
                sw2.Close();
                fs2.Close();

                #endregion

                try
                {
                    #region window专用
                    Process myProcess = new Process();
                    string fileName = path1 + "//script//tile.bat";
                    ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(fileName);
                    if (!HookHelper.isshowchildprocess)
                    {
                        myProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏黑屏，不让执行exe的黑屏弹出
                    }
                    myProcess.StartInfo = myProcessStartInfo;
                    bool isStart = myProcess.Start();
                    myProcess.WaitForExit();
                    #endregion
                }
                catch (Exception exp)
                {
                    //MessageBox.Show(exp.Message, exp.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }else
            {
                //Process myProcess = new Process();
                //string fileName = batRootPath + "\\" + HookHelper.rubbatForDOS;

                //if (!File.Exists(fileName)) {
                //    return false;
                //}

                //ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(fileName);
                //myProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏黑屏，不让执行exe的黑屏弹出
                //myProcess.StartInfo = myProcessStartInfo;
                //bool isStart = myProcess.Start();
                //myProcess.WaitForExit();

                string path1 = batRootPath;
                if (!Directory.Exists(batRootPath))
                {
                    return false;
                }

                #region 生成执行文件
                string p = System.IO.Directory.GetDirectoryRoot(path1);
                string Contents = p.Substring(0, p.Length - 1) + "\r\ncd " + path1 + "\r\n" + path1 + "//" + HookHelper.rubbatForDOS;
                string path = path1 + "//Execution.bat";
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                FileStream fs2 = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw2 = new StreamWriter(fs2, System.Text.Encoding.GetEncoding("GB2312"));
                //sw2.Write(Contents);
                sw2.WriteLine(Contents);
                sw2.Close();
                fs2.Close();

                #endregion

                #region window专用
                Process myProcess = new Process();
                string fileName = path1 + "//Execution.bat";
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(fileName);

                if(!HookHelper.isshowchildprocess)
                {
                    myProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏黑屏，不让执行exe的黑屏弹出
                }
                
                myProcess.StartInfo = myProcessStartInfo;
                myProcess.Start();
                myProcess.WaitForExit();
                #endregion
            }

            return true;
        }

        public static FileInfo[] GetRaindatList()
        {
            DirectoryInfo pDirectoryInfo = new DirectoryInfo(HookHelper.rainSRCDirectory);
            FileInfo[] ArrayileInfo = pDirectoryInfo.GetFiles("*.dat");
            if (ArrayileInfo.Length < 1)
            {
                return ArrayileInfo;
            }
            Array.Sort(ArrayileInfo, new FileNameSort());


            return ArrayileInfo;
        }

    }
}

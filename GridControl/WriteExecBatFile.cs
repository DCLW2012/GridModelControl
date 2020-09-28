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
    public class WriteExecBatFile
    {
        public static bool UpdateExecBatFileByTemplateExecsingle(string fileName, string ComputeUnit, string start, string end, string timeNums, string datName, string outrainTilepath)
        {
            DirectoryInfo info = new DirectoryInfo(fileName);
            String apppath = info.Parent.FullName;

            if (!Directory.Exists(apppath))
            {
                return false;
            }


            //！ 写之前先创建
            FileStream fileStreamWriter = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            StreamWriter streamWriter = new StreamWriter(fileStreamWriter, Encoding.GetEncoding("GB2312"));

            string p = System.IO.Directory.GetDirectoryRoot(fileName);
            string Contents = p.Substring(0, p.Length - 1) + "\r\ncd " + apppath;
            //! 写出到输出文件流中。
            streamWriter.WriteLine(Contents);

            //! 写参数
            string newLine = String.Format("DCFDProc.exe {0} 60 60 1 1 ", ComputeUnit);
            string parasLine = String.Format("-m grid -exec false -t forecast -usegroovy true -methodtopo wata -computernode {0} -s {1} -c {2} -datTimes {3} -curDatGridName {4} -gridRainRoot {5} -gridFBCRoot {6}",
                                             HookHelper.computerNode, start, end, timeNums, datName, outrainTilepath, HookHelper.rainSRCDirectory.Replace('\\', '/'));

            streamWriter.WriteLine(newLine + parasLine);
            //! 结束----
            //! 结束读取流
            streamWriter.Close();
            fileStreamWriter.Close();

            return true;
        }

        public static bool UpdateexeccctableBatFileByWATAChina(string fileName, string ComputeUnit, string outrainTilepath)
        {
            DirectoryInfo info = new DirectoryInfo(fileName);
            String apppath = info.Parent.FullName;

            if (!Directory.Exists(apppath))
            {
                return false;
            }


            //！ 写之前先创建
            FileStream fileStreamWriter = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            StreamWriter streamWriter = new StreamWriter(fileStreamWriter, Encoding.GetEncoding("GB2312"));

            string p = System.IO.Directory.GetDirectoryRoot(fileName);
            string Contents = p.Substring(0, p.Length - 1) + "\r\ncd " + apppath;
            //! 写出到输出文件流中。
            streamWriter.WriteLine(Contents);

            //! 写参数
            string newLine = String.Format("DCFDProc.exe {0} 60 60 1 1 ", ComputeUnit);
            string parasLine = String.Format("-m grid -exec false -t forecast -usegroovy true -methodtopo wata -computernode {0} -cctable true -gridRainRoot {1} -gridFBCRoot {2}",
                                             HookHelper.computerNode, outrainTilepath, HookHelper.rainSRCDirectory.Replace('\\', '/'));

            streamWriter.WriteLine(newLine + parasLine);
            //! 结束----
            //! 结束读取流
            streamWriter.Close();
            fileStreamWriter.Close();

            return true;
        }


        public static bool UpdateExecBatFileByTemplate(string fileName, string ComputeUnit)
        {
            DirectoryInfo info = new DirectoryInfo(fileName);
            String apppath = info.Parent.FullName;

            if (!Directory.Exists(apppath))
            {
                return false;
            }

            string directoryRoot = System.IO.Directory.GetCurrentDirectory();
            string templateTempCopy = directoryRoot + "\\template" + "\\templateexec.bat";

            // 不存在说明需要创建，则根据模板复制一份
            if (!File.Exists(templateTempCopy))
            {
                return false;
            }

            //！以上步骤备份完了当前exec.bat，则读取template，覆盖写出到filename中删除
            //! 开始----定义读文件操作
            //! 读
            StreamReader streamRead = new StreamReader(templateTempCopy, Encoding.GetEncoding("GB2312"));

            //！ 写之前先创建
            FileStream fileStreamWriter = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            StreamWriter streamWriter = new StreamWriter(fileStreamWriter, Encoding.GetEncoding("GB2312"));

            //！ 读取并写出，读一行，写一行
            while (streamRead.Peek() >= 0)
            {
                string curLine = streamRead.ReadLine();

                //更新cd行
                if (curLine.Contains("CD ") || curLine.Contains("cd "))
                {
                    curLine = String.Format("cd {0}", apppath);
                }

                //更新当前行参数,从模板中的第行，第6个位置更新
                if (curLine.Contains("DCFDProc.exe"))
                {
                    //! 分割存储当前行，
                    string[] curlineList = curLine.Split(' ');
                    string newLine = String.Format("DCFDProc.exe {0} 60 60 1 1 ", ComputeUnit);
                    //组装当前行,从6开始
                    for (int i = 6; i < curlineList.Length; ++i)
                    {
                        string temp = "";
                        if (i != curlineList.Length - 1)
                        {
                            temp = curlineList[i] + " ";
                        }
                        else
                        {
                            temp = curlineList[i];
                        }
                        newLine += temp;
                    }

                    curLine = newLine;
                }

                //! 写出到输出文件流中。
                streamWriter.WriteLine(curLine);
            }

            //! 结束----
            //! 结束读取流
            streamWriter.Close();
            fileStreamWriter.Close();
            streamRead.Close();

            return true;
        }


        public static bool UpdateExecBatFile(string fileName, string rainTilePath)
        {
            //! 打开文件读
            if (!File.Exists(fileName))
            {
                return false;
            }


            DirectoryInfo info = new DirectoryInfo(fileName);
            String directoryRoot = info.Parent.FullName;
            string templateTempCopy = directoryRoot + "\\" + "templateexec.bat";

            // 不存在说明需要创建，则根据模板复制一份
            if (File.Exists(templateTempCopy))
            {
                File.Delete(templateTempCopy);
            }
            System.IO.File.Copy(fileName, templateTempCopy, true);

            //！以上步骤备份完了当前exec.bat，则读取template，覆盖写出到filename中删除
            //! 开始----定义读文件操作
            //! 读
            StreamReader streamRead = new StreamReader(templateTempCopy, Encoding.GetEncoding("GB2312"));

            //！ 写之前先创建
            FileStream fileStreamWriter = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            StreamWriter streamWriter = new StreamWriter(fileStreamWriter, Encoding.GetEncoding("GB2312"));

            //！ 读取并写出，读一行，写一行
            while (streamRead.Peek() >= 0)
            {
                string curLine = streamRead.ReadLine();

                //更新当前行参数
                if (curLine.Contains("DCFDProc.exe"))
                {
                    //! 分割存储当前行，
                    string[] curlineList = curLine.Split(' ');
                    string newLine = "";
                    //组装当前行
                    for (int i = 0; i < curlineList.Length; ++i)
                    {
                        string temp = "";
                        //存在则更新参数值，不存在则追加
                        if (curlineList[i].Contains("-gridRainRoot"))
                        {
                            if (i + 1 < curlineList.Length)
                            {
                                curlineList[i + 1] = rainTilePath;
                            }
                        }

                        if (i != curlineList.Length - 1)
                        {
                            temp = curlineList[i] + " ";
                        }
                        else
                        {
                            temp = curlineList[i];
                        }
                        newLine += temp;
                    }

                    curLine = newLine;
                }

                //! 写出到输出文件流中。
                streamWriter.WriteLine(curLine);
            }

            //! 结束----
            //! 结束读取流
            streamWriter.Close();
            fileStreamWriter.Close();
            streamRead.Close();

            return true;
        }

        
    }

}

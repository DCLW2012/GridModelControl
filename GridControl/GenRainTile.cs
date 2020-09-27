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
    public class FileNameSort : IComparer<object>
    {
        //调用DLL
        [System.Runtime.InteropServices.DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string param1, string param2);


        //前后文件名进行比较。
        public int Compare(object name1, object name2)
        {
            if (null == name1 && null == name2)
            {
                return 0;
            }
            if (null == name1)
            {
                return -1;
            }
            if (null == name2)
            {
                return 1;
            }
            return StrCmpLogicalW(name1.ToString(), name2.ToString());
        }
    }

    //! 先行后列
    public class DatFileStruct
    {
        //! 第一部分数据 年(year)、月日时(mdh)、该台风总时次(times) 均为整型  3 * 4 个字节
        public int[] headerone;

        //! 第二部分数据，纬度Lat0(times)、经度Lon0(times)”，均为8位double型数据。
        //先Lat0，后Lon0，循环写入，直至写到最后一个台风时次
        public double[] Lats;
        public double[] Lons;

        //! 辅助变量，例如网格的行和列， 分辨率，当前场次索引号
        public int curRainIndex;
        public int row;
        public int col;
        public double fbl;

        //! 第三部分数据，不存储全部，如果时间场次过多，则会导致内存过大。故只存储对应times的一份数据
        //! rain(1001,1001,ti)
        public float[,,] rain;

        public double xllcorner;
        public double yllcorner;

        public double xmaxcorner;
        public double ymaxcorner;

        public double cellsize;

        public  DatFileStruct()
        {
            headerone = new int[3];

            xllcorner = 0;
            xmaxcorner = 0;
            yllcorner = 0;
            ymaxcorner = 0;
            cellsize = 0;
            row = 0;
            col = 0;
        }

         ~DatFileStruct()
        {
            headerone = null;
            rain = null;
            Lons = null;
            Lats = null;
            xllcorner = 0;
            xmaxcorner = 0;
            yllcorner = 0;
            ymaxcorner = 0;
            cellsize = 0;
            row = 0;
            col = 0;
        }

    };

    public class GenRainTile
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
            if (!HookHelper.isshowcmd)
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
            if (!HookHelper.isshowcmd)
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

        public static bool WriteAscFileByParams(string datPureName, string provinceName, string groovyName, string startTimeCurDat, DatFileStruct datStruct, DataTable paramsUnitDT)
        {
            //当前单元输出路径
            string outrainTilepath = dbValues[provinceName]["rainTileFolder"];
            string unitOutdir = outrainTilepath + "\\" + datPureName + "\\" + groovyName;

            for (int t = 0; t < datStruct.headerone[2]; ++t)
            {
                //!当前文件名
                string curWriteFileName = String.Format("{0}\\{1}-{2}.asc", unitOutdir, startTimeCurDat, t);
                //! 使用c#写出

            }
            //@ 判断当前时段的降雨数据是否 对 当前传入的计算单元有降雨，有则写出，无则跳过；
            // 模型在执行计算的时候会搜索对应的降雨，找不到则自动跳过计算
            /*float fbl = dStruct.fbl;
            float stLon = dStruct.Lons[dStruct.curRainIndex];
            float stLat = dStruct.Lats[dStruct.curRainIndex];*/
            //double xa1 = dStruct.Lons[dStruct.curRainIndex]; double ya1 = dStruct.Lats[dStruct.curRainIndex];
            //double xa2 = xa1 + dStruct.fbl * (dStruct.col - 1); double ya2 = ya1 + dStruct.fbl * (dStruct.row - 1);

            ///*float outStLon = params.left.toFloat();
            //float outStLat = params.bottom.toFloat();*/
            //double xb1 = params.left.toDouble(); double yb1 = params.bottom.toDouble();
            //double xb2 = xb1 + dStruct.fbl * (params.ncols.toInt() - 1); double yb2 = yb1 + dStruct.fbl * (params.nrows.toInt() - 1);

            //bool ret = overlap(xa1, ya1, xa2, ya2, xb1, yb1, xb2, yb2);

            //if (ret == 0)
            //{
            //    Logger::Message(QStringLiteral("不存在有效的降雨数据！！！"));
            //    return false;
            //}

            ////写出数据
            //QFile file(fileName);
            //if (!file.open(QFile::WriteOnly | QFile::Truncate))
            //{
            //    return false;
            //}
            //QTextStream stream(&file);
            ////设置格式控制
            //stream.setRealNumberNotation(QTextStream::FixedNotation);
            //stream.setRealNumberPrecision(6);

            ////! 1、先写出文件头，起点投影坐标xy以及行列号
            ////ncols         25
            ////nrows         16
            ////xllcorner     2093613.7816920
            ////yllcorner     311816.2681279
            ////cellsize      1086.3543389
            ////NODATA_value - 9999 
            //stream << "ncols" << " " << params.ncols.toInt() << "\n";
            //stream << "nrows" << " " << params.nrows.toInt() << "\n";
            //stream << "xllcorner" << " " << params.xllcorner << "\n";
            //stream << "yllcorner" << " " << params.yllcorner << "\n";
            //stream << "cellsize" << " " << params.cellsize << "\n";
            //stream << "NODATA_value" << " " << QString("%1").arg(-9999, 0, 10) << "\n";
            ////1 根据每个单元的参数记录的起点经纬度，行列数，遍历计算当前点在台风场数据中的索引号，从中取出对应的值
            ////! 如果计算出来的索引号行或者列为负值，则说明不在范围内，赋值为0
            //int NODATA_value = -9999;
            //int outRow = params.nrows.toInt();
            //int outCol = params.ncols.toInt();
            //float outStLon = params.left.toFloat();
            //float outStLat = params.bottom.toFloat();

            //float fbl = dStruct.fbl;
            //float stLon = dStruct.Lons[dStruct.curRainIndex];
            //float stLat = dStruct.Lats[dStruct.curRainIndex];

            ////! 先写出一行，再写一行
            //QString lines;
            ////! 由于asc中文件的参数信息是做下脚，但是数据是先存左上开始
            //for (int r = outRow - 1; r >= 0; --r)
            //{
            //    QString line;

            //    for (int c = 0; c < outCol; ++c)
            //    {
            //        //! 坐标索引转换，根据起点坐标 分辨率，行列号，计算当前点位经纬度，根据台风场的经纬度值，计算在台风场中的行列号 ，赋值即可
            //        //! 当前坐标
            //        float curLon = outStLon + fbl * c;
            //        float curLat = outStLat + fbl * r;

            //        //! 在台风场中的索引号
            //        int originRow = ceil((curLat - stLat) * (1 / fbl)); ;
            //        int originCol = ceil((curLon - stLon) * (1 / fbl));

            //        float curRain = 0.0;
            //        if (originRow >= 0 && originCol >= 0 && originRow <= dStruct.row - 1 && originCol <= dStruct.col - 1)
            //        {
            //            curRain = dStruct.rain[originRow * dStruct.col + originCol];
            //            if (curRain < 0 || curRain == NODATA_value)
            //            {
            //                curRain = NODATA_value;
            //            }
            //        }

            //        line.append(QString("%1").arg(curRain, 0, 'f', 3));
            //        if (c == outCol - 1)
            //        {
            //            line.append("\n"); //添加分隔符
            //        }
            //        else
            //        {
            //            line.append(" "); //添加分隔符
            //        }


            //    }
            //    lines += line;
            //}

            //stream << lines;
            //file.close();

            return true;
        }


        public static bool CreateTileByWATAByCSharp(string curDatFullname, ref string start, ref string end, ref string datnums)
        {
            string datPureName = System.IO.Path.GetFileNameWithoutExtension(curDatFullname);

            //！解析当前dat文件
            //！创建数据存储结构
            DatFileStruct datStruct = new DatFileStruct();
            datStruct.col = 1001;
            datStruct.row = 1001;
            datStruct.fbl = 0.01;
            

            // 读取文件
            BinaryReader br;
            string startTimeCurDat = "2013111513";

            try
            {
                br = new BinaryReader(new FileStream(curDatFullname,
                                FileMode.Open));
            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("{0}台风场文件解析场次信息失败，继续下一个", curDatFullname) + DateTime.Now);
                return false;
            }
            try
            {
                //! 第一部分数据 年(year)、月日时(mdh)、该台风总时次(times) 均为整型  3 * 4 个字节
                //inFile.read((char*)&datStruct.headerone[0], 3 * sizeof(int));
                int year = br.ReadInt32();
                int mdh = br.ReadInt32();
                int times = br.ReadInt32();

                datStruct.headerone[0] = year;
                datStruct.headerone[1] = mdh;
                datStruct.headerone[2] = times;

                string mdhSt = mdh.ToString();
                if (mdhSt.Length == 5)
                {
                    mdhSt = String.Format("0{0}", mdhSt);
                }

                string ymdhstr = String.Format("{0}{1}", year, mdhSt);
                DateTime dt = Convert.ToDateTime(ymdhstr.Substring(0, 4) + "-" + ymdhstr.Substring(4, 2) + "-" + ymdhstr.Substring(6, 2) + " " + ymdhstr.Substring(8, 2) + ":00:00");
                startTimeCurDat = ymdhstr.Substring(0, 4) + ymdhstr.Substring(4, 2) + ymdhstr.Substring(6, 2) + ymdhstr.Substring(8, 2);
                start = dt.ToString("yyyy-MM-ddThh:mm");
                end = (dt.AddHours(times - 1)).ToString("yyyy-MM-ddThh:mm");
                datnums = times.ToString();

                //！2、第二部分，是各个场次经纬度列表
                datStruct.Lats = new double[times];
                datStruct.Lons = new double[times];
                for (int tindex = 0; tindex < datStruct.headerone[2]; ++tindex)
                {
                    double lat = br.ReadDouble();
                    double lon = br.ReadDouble();
                    datStruct.Lats[tindex] = lat;
                    datStruct.Lons[tindex] = lon;
                }

                //!3、第三部分，是所有场次的网格数据存储，三维数组存放每个时间的网格数据
                datStruct.rain = new float[times, datStruct.row, datStruct.col];
                for (int tindex = 0; tindex < datStruct.headerone[2]; ++tindex)
                {
                    datStruct.curRainIndex = tindex;

                    for (int r = 0; r < datStruct.row; ++r)
                    {
                        for (int c = 0; c < datStruct.col; ++c)
                        {
                            float val = br.ReadSingle();
                            datStruct.rain[tindex, r,c] = val;
                        }
                    }
                }

            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("{0}台风场文件解析场次信息失败，继续下一个", curDatFullname) + DateTime.Now);
                br.Close();
                return false;
            }
            br.Close();

            //！22 数据读取完成，则需要插值到各个计算单元，然后写出
            //! 遍历所有的计算单元信息表，写出数据
            //! 遍历每个计算单元，然后在其中遍历每个场次的数据
            int unitNUM = dbTableConfigs["china"]["GRID_HSFX_UNIT"].Rows.Count;
            //unit 表
            DataTable grid_unit_tables = dbTableConfigs["china"]["GRID_HSFX_UNIT"];

            for (int i = 0; i < unitNUM; ++i)
            {
                //！单元的信息
                string provinceName = grid_unit_tables.Rows[i]["province"].ToString();
                string groovyName = grid_unit_tables.Rows[i]["GroovyName"].ToString();

                //!当前场次下某个单元的所有时间文件写出
                bool status = WriteAscFileByParams(datPureName, provinceName, groovyName, startTimeCurDat, datStruct, grid_unit_tables);
                if (status)
                {
                    Console.WriteLine(string.Format("{0}台风场文件在{1}省下{2}单元目录切片成功", curDatFullname, provinceName, groovyName) + DateTime.Now);
                }else
                {
                    Console.WriteLine(string.Format("{0}台风场文件在{1}省下{2}单元目录切片失败", curDatFullname, provinceName, groovyName) + DateTime.Now);
                }
            }

            datStruct.headerone = null;
            datStruct.rain = null;
            datStruct.Lons = null;
            datStruct.Lats = null;
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
                    if (!HookHelper.isshowcmd)
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

                if(!HookHelper.isshowcmd)
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
            string parasLine = String.Format("-m grid -exec false -t forecast -usegroovy true -methodtopo wata -s {0} -c {1} -datTimes {2} -curDatGridName {3} -gridRainRoot {4} -gridFBCRoot {5}",
                                             start, end, timeNums, datName, outrainTilepath, HookHelper.rainSRCDirectory.Replace('\\', '/'));

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
            string parasLine = String.Format("-m grid -exec false -t forecast -usegroovy true -methodtopo wata -cctable true -gridRainRoot {0} -gridFBCRoot {1}",
                                             outrainTilepath, HookHelper.rainSRCDirectory.Replace('\\', '/'));

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
                if (curLine.Contains("CD ")  || curLine.Contains("cd "))
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
                            if (i+1 < curlineList.Length)
                            {
                                curlineList[i + 1] = rainTilePath;
                            }
                        }

                        if (i != curlineList.Length-1)
                        {
                            temp = curlineList[i] + " ";
                        }else
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

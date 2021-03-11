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

        public DatFileStruct()
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

    public class GenRainTileByCSharp
    {
        // 每个省对应一个数据库连接，每个连接里包含了降雨切片目录
        public static Dictionary<string, Dictionary<string, string>> dbValues = ClientConn.m_dbTableTypes;
        public static Dictionary<string, Dictionary<string, DataTable>> dbTableConfigs = ClientConn.m_dbTableConfig; //可以得到计算单元数


        public static int between(double d1, double d2, double d3)
        {

            if (d1 < d2)
            {
                if (d1 <= d3 && d3 <= d2)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }


            }
            else
            {
                if (d2 <= d3 && d3 <= d1)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }

            }

            return 0;

        }

        public static int overlap(double xa1, double ya1, double xa2, double ya2, double xb1, double yb1, double xb2, double yb2)
        {

            /* 1 */

            if (between(xa1, xa2, xb1) > 0 && between(ya1, ya2, yb1) > 0)

                return 1;

            if (between(xa1, xa2, xb2) > 0 && between(ya1, ya2, yb2) > 0)

                return 1;

            if (between(xa1, xa2, xb1) > 0 && between(ya1, ya2, yb2) > 0)

                return 1;

            if (between(xa1, xa2, xb2) > 0 && between(ya1, ya2, yb1) > 0)

                return 1;

            /* 2 */

            if (between(xb1, xb2, xa1) > 0 && between(yb1, yb2, ya1) > 0)

                return 1;

            if (between(xb1, xb2, xa2) > 0 && between(yb1, yb2, ya2) > 0)

                return 1;



            /* 3 */

            if ((between(ya1, ya2, yb1) > 0 && between(ya1, ya2, yb2) > 0)

                && (between(xb1, xb2, xa1) > 0 && between(xb1, xb2, xa2) > 0))

                return 1;



            /* 4 */

            if ((between(xa1, xa2, xb1) > 0 && between(xa1, xa2, xb2) > 0)

                && (between(yb1, yb2, ya1) > 0 && between(yb1, yb2, ya2) > 0))

                return 1;



            return 0;

        }

        public static bool WriteAscFileByParams(string datPureName, string provinceName, string groovyName, string startTimeCurDat, DatFileStruct dStruct, DataRow paramsUnitDT)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //当前单元输出路径
            if (!dbValues.ContainsKey(provinceName))
            {
                return false;
            }
            string outrainTilepath = dbValues[provinceName]["rainTileFolder"];
            string unitOutdir = outrainTilepath + "\\" + datPureName + "\\" + groovyName;
            bool isHaveRainCurUnit = false;

            float destDegFbl = 0.01f;
            for (int t = 0; t < dStruct.headerone[2]; ++t)
            {
                dStruct.curRainIndex = t;
                //!当前文件名
                string curWriteFileName = String.Format("{0}\\{1}-{2}.asc", unitOutdir, datPureName, t);

                if (curWriteFileName.Contains("WHF66_3_4"))
                {
                    int a = 0;
                }

                //! 使用c#写出
                //@ 判断当前时段的降雨数据是否 对 当前传入的计算单元有降雨，有则写出，无则跳过；
                // 模型在执行计算的时候会搜索对应的降雨，找不到则自动跳过计算
                double xa1 = dStruct.Lons[dStruct.curRainIndex]; double ya1 = dStruct.Lats[dStruct.curRainIndex];
                double xa2 = xa1 + dStruct.fbl * (dStruct.col); double ya2 = ya1 + dStruct.fbl * (dStruct.row );

                int NODATA_value = -9999;
                double xb1 = double.Parse(paramsUnitDT["left"].ToString());
                double yb1 = double.Parse(paramsUnitDT["bottom"].ToString());

                double xllcorner = double.Parse(paramsUnitDT["xllcorner"].ToString());
                double yllcorner = double.Parse(paramsUnitDT["yllcorner"].ToString());
                double cellsize = double.Parse(paramsUnitDT["cellsize"].ToString());

                int unitCols = int.Parse(paramsUnitDT["ncols"].ToString());
                int unitRows = int.Parse(paramsUnitDT["nrows"].ToString());

                double xb2 = xb1 + destDegFbl * (unitCols - 1);
                double yb2 = yb1 + destDegFbl * (unitRows - 1);

                int ret = overlap(xa1, ya1, xa2, ya2, xb1, yb1, xb2, yb2);

                if (ret == 0)
                {
                    //Console.WriteLine(string.Format("{0}不存在有效的降雨数据！！！", curWriteFileName) + DateTime.Now);
                    continue;
                }

                //! 有数据才创建目录
                if (!Directory.Exists(unitOutdir))
                {
                    Directory.CreateDirectory(unitOutdir);
                }

                //写出数据
                StringBuilder lines = new StringBuilder();
                //! 1、先写出文件头，起点投影坐标xy以及行列号
                //stream << "ncols" << " " << params.ncols.toInt() << "\n";
                //stream << "nrows" << " " << params.nrows.toInt() << "\n";
                //stream << "xllcorner" << " " << params.xllcorner << "\n";
                //stream << "yllcorner" << " " << params.yllcorner << "\n";
                //stream << "cellsize" << " " << params.cellsize << "\n";
                //stream << "NODATA_value" << " " << QString("%1").arg(-9999, 0, 10) << "\n";

                lines.Append(String.Format("ncols {0}\n", unitCols));
                lines.Append(String.Format("nrows {0}\n", unitRows));
                lines.Append(String.Format("xllcorner {0}\n", xllcorner));
                lines.Append(String.Format("yllcorner {0}\n", yllcorner));
                lines.Append(String.Format("cellsize {0}\n", cellsize));
                lines.Append(String.Format("NODATA_value {0}\n", NODATA_value));
                //1 根据每个单元的参数记录的起点经纬度，行列数，遍历计算当前点在台风场数据中的索引号，从中取出对应的值
                //! 如果计算出来的索引号行或者列为负值，则说明不在范围内，赋值为0
                int outRow = unitRows;
                int outCol = unitCols;
                float outStLon = (float)(xb1);
                float outStLat = (float)(yb1); 

                float stLon = (float)dStruct.Lons[dStruct.curRainIndex];
                float stLat = (float)dStruct.Lats[dStruct.curRainIndex];

                //! 先写出一行，再写一行
                //! 由于asc中文件的参数信息是做下脚，但是数据是先存左上开始
                for (int r = outRow - 1; r >= 0; --r)
                {
                    for (int c = 0; c < outCol; ++c)
                    {
                        //! 坐标索引转换，根据起点坐标 分辨率，行列号，计算当前点位经纬度，根据台风场的经纬度值，计算在台风场中的行列号 ，赋值即可
                        //! 当前坐标
                        float curLon = outStLon + destDegFbl * c;
                        float curLat = outStLat + destDegFbl * r;

                        //! 在台风场中的索引号
                        int originRow = (int)Math.Ceiling((curLat - stLat) * (1 / dStruct.fbl)); ;
                        int originCol = (int)Math.Ceiling((curLon - stLon) * (1 / dStruct.fbl));

                        float curRain = 0.0f;
                        if (originRow >= 0 && originCol >= 0 && originRow <= dStruct.row - 1 && originCol <= dStruct.col - 1)
                        {
                            curRain = dStruct.rain[t, originRow, originCol];
                            if (curRain < 0 || curRain == NODATA_value || double.IsNaN(curRain))
                            {
                                curRain = 0.0f;
                            }
                        }

                        lines.Append(curRain);
                        if (c == outCol - 1)
                        {
                            lines.Append("\n"); //添加分隔符
                        }
                        else
                        {
                            lines.Append(" "); //添加分隔符
                        }
                    }
                }

                isHaveRainCurUnit = true;

                //stopwatch.Stop();
                //Console.WriteLine("--计算" + stopwatch.ElapsedMilliseconds);
                //stopwatch.Restart();

                FileStream fs = new FileStream(curWriteFileName, FileMode.Create, FileAccess.Write);//定义写入方式
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GB2312"));//写入文件格式

                sw.Write(lines.ToString());
                sw.Close();
                fs.Close();

                //stopwatch.Stop();
                //Console.WriteLine("--写入" + stopwatch.ElapsedMilliseconds);
                //stopwatch.Restart();

            }

            return isHaveRainCurUnit;
        }


        public static bool CreateTileByWATAByCSharp(string curDatFullname, ref string start, ref string end, ref string datnums, ref string yearmmddForID)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string[] gridlist = HookHelper.gridsize.Split(',');

            int gridrow = 1001;
            int gridcol = 1001;
            float rainSRCFBL = 0.01f;

            if (gridlist.Count() == 2)
            {
                gridrow = int.Parse(gridlist[0]);
                gridcol = int.Parse(gridlist[1]);
            }

            if (gridlist.Count() == 3)
            {
                gridrow = int.Parse(gridlist[0]);
                gridcol = int.Parse(gridlist[1]);
                rainSRCFBL = float.Parse(gridlist[2]);
            }

            string datPureName = System.IO.Path.GetFileNameWithoutExtension(curDatFullname);

            //！解析当前dat文件
            //！创建数据存储结构
            DatFileStruct datStruct = new DatFileStruct();
            datStruct.col = gridcol;
            datStruct.row = gridrow;
            datStruct.fbl = rainSRCFBL;


            // 读取文件
            BinaryReader br;

            try
            {
                br = new BinaryReader(new FileStream(curDatFullname,
                                FileMode.Open, FileAccess.Read, FileShare.Read));
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
                //////
                string yearStr = year.ToString();


                year = int.Parse(yearStr);
                ///////
                datStruct.headerone[0] = year;
                datStruct.headerone[1] = mdh;
                datStruct.headerone[2] = times;

                string mdhSt = mdh.ToString();
                if (mdhSt.Length == 5)
                {
                    mdhSt = String.Format("0{0}", mdhSt);
                }
                //System.String.Substring(Int32 startIndex, Int32 length)
                string ymdhstr = String.Format("{0}{1}", yearStr, mdhSt);

                //1,2,3位的year前边必须补0，不然会识别错误， 5位的不识别.传入程序的时间以4位为准，统一从2000年开始，追加，模型输出时候纠正时间
                string yearStrForCalc = "2020";
                //DateTime dt = Convert.ToDateTime(yearStrForCalc + "-" + mdhSt.Substring(0, 2) + "-" + mdhSt.Substring(2, 2) + " " + mdhSt.Substring(4, 2) + ":00:00");
                DateTime dt = Convert.ToDateTime(yearStrForCalc + "-" + "01" + "-" + "01" + " " + "00" + ":00:00");
                //! 传入到模型中的时间值，用来计算该时间段的水文结果
                start = dt.ToString("yyyy-MM-ddTHH:mm");
                end = (dt.AddHours(times - 1)).ToString("yyyy-MM-ddTHH:mm");
                datnums = times.ToString();

                //该变量是根据时间值组合是数字串，后续作为降雨及计算结果的输出文件名称前缀，year前自动补0，与模型计算中更新rainfile中规则一致
                yearmmddForID = yearStr + mdhSt.Substring(0, 2) + mdhSt.Substring(2, 2) + mdhSt.Substring(4, 2);

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

                    byte[] datbytes = br.ReadBytes(datStruct.row * datStruct.col * 4);

                    for (int r = 0; r < datStruct.row; ++r)
                    {
                        for (int c = 0; c < datStruct.col; ++c)
                        {
                            //float val = br.ReadSingle();
                            //datStruct.rain[tindex, r, c] = val;
                            datStruct.rain[tindex, r, c] = BitConverter.ToSingle(datbytes, (r * datStruct.row + c) * 4);
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
            stopwatch.Stop();
            Console.WriteLine(string.Format("读取{0}台风场文件耗时{1}", curDatFullname, stopwatch.ElapsedMilliseconds));
            stopwatch.Restart();

            //！22 数据读取完成，则需要插值到各个计算单元，然后写出
            //! 遍历所有的计算单元信息表，写出数据
            //! 遍历每个计算单元，然后在其中遍历每个场次的数据
            int unitNUM = dbTableConfigs["china"]["GRID_HSFX_UNIT"].Rows.Count;
            //unit 表
            DataTable grid_unit_tables = dbTableConfigs["china"]["GRID_HSFX_UNIT"];

            int countOfHaveRain = 0;
            for (int i = 0; i < unitNUM; ++i)
            {
                //！单元的信息
                string provinceName = grid_unit_tables.Rows[i]["province"].ToString();
                string groovyName = grid_unit_tables.Rows[i]["GroovyName"].ToString();

                //!当前场次下某个单元的所有时间文件写出
                if (provinceName.Equals("hainan"))
                {
                    int aa = 9;
                }
                bool status = WriteAscFileByParams(datPureName, provinceName, groovyName, yearmmddForID, datStruct, grid_unit_tables.Rows[i]);
                if (status)
                {
                    countOfHaveRain++;
                    //Console.WriteLine(string.Format("{0}台风场文件在{1}省下{2}单元目录切片成功", curDatFullname, provinceName, groovyName) + DateTime.Now);
                }
                else
                {
                    //Console.WriteLine(string.Format("{0}台风场文件在{1}省下{2}单元目录切片失败", curDatFullname, provinceName, groovyName) + DateTime.Now);
                }
                //stopwatch.Stop();
                //Console.WriteLine(string.Format("写入{0}台风场第{1}个计算单元耗时{2}", curDatFullname, i, stopwatch.ElapsedMilliseconds));
                //stopwatch.Restart();
            }
            stopwatch.Stop();
            Console.WriteLine(string.Format("写入{0}台风场文件耗时{1}", curDatFullname, stopwatch.ElapsedMilliseconds));

            Console.WriteLine(string.Format("{0}台风场文件在{1}节点下共有{2}个计算单元，其中{3}个计算单元中有有效降雨", curDatFullname, HookHelper.computerNode, unitNUM, countOfHaveRain) + DateTime.Now);

            //CSVLog
            if (HookHelper.useCSVLOG.Equals("true"))
            {
                CSVData.addData(CSVData.GetRowNumber(), "HostName", System.Net.Dns.GetHostName());
                var serverIP = Program.GetLocalIP(HookHelper.serachIP);
                CSVData.addData(CSVData.GetRowNumber(), "服务器IP", serverIP);
                CSVData.addData(CSVData.GetRowNumber(), "计算节点", HookHelper.computerNode);
                CSVData.addData(CSVData.GetRowNumber(), "eventId", Path.GetFileNameWithoutExtension(curDatFullname));
                CSVData.addData(CSVData.GetRowNumber(), "计算单元个数", unitNUM);
                CSVData.addData(CSVData.GetRowNumber(), "有效降雨单元个数", countOfHaveRain);

            }
                
            



            datStruct.headerone = null;
            datStruct.rain = null;
            datStruct.Lons = null;
            datStruct.Lats = null;
            return true;
        }

        //method为省时候调用，只切割指定的省份
        public static bool CreateTileByWATAByCSharpFromProvince(string keyString, string curDatFullname, ref string start, ref string end, ref string datnums, ref string yearmmddForID)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string[] gridlist = HookHelper.gridsize.Split(',');

            int gridrow = 1001;
            int gridcol = 1001;
            float rainSRCFBL = 0.01f;

            if (gridlist.Count() == 2)
            {
                gridrow = int.Parse(gridlist[0]);
                gridcol = int.Parse(gridlist[1]);
            }

            if (gridlist.Count() == 3)
            {
                gridrow = int.Parse(gridlist[0]);
                gridcol = int.Parse(gridlist[1]);
                rainSRCFBL = float.Parse(gridlist[2]);
            }

            string datPureName = System.IO.Path.GetFileNameWithoutExtension(curDatFullname);

            //！解析当前dat文件
            //！创建数据存储结构
            DatFileStruct datStruct = new DatFileStruct();
            datStruct.col = gridcol;
            datStruct.row = gridrow;
            datStruct.fbl = rainSRCFBL;


            // 读取文件
            BinaryReader br;

            try
            {
                br = new BinaryReader(new FileStream(curDatFullname,
                                FileMode.Open, FileAccess.Read, FileShare.Read));
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
                //////
                string yearStr = year.ToString();


                year = int.Parse(yearStr);
                ///////
                datStruct.headerone[0] = year;
                datStruct.headerone[1] = mdh;
                datStruct.headerone[2] = times;

                string mdhSt = mdh.ToString();
                if (mdhSt.Length == 5)
                {
                    mdhSt = String.Format("0{0}", mdhSt);
                }
                //System.String.Substring(Int32 startIndex, Int32 length)
                string ymdhstr = String.Format("{0}{1}", yearStr, mdhSt);

                //1,2,3位的year前边必须补0，不然会识别错误， 5位的不识别.传入程序的时间以4位为准，统一从2000年开始，追加，模型输出时候纠正时间
                string yearStrForCalc = "2020";
                //DateTime dt = Convert.ToDateTime(yearStrForCalc + "-" + mdhSt.Substring(0, 2) + "-" + mdhSt.Substring(2, 2) + " " + mdhSt.Substring(4, 2) + ":00:00");
                DateTime dt = Convert.ToDateTime(yearStrForCalc + "-" + "01" + "-" + "01" + " " + "00" + ":00:00");
                //! 传入到模型中的时间值，用来计算该时间段的水文结果
                start = dt.ToString("yyyy-MM-ddTHH:mm");
                end = (dt.AddHours(times - 1)).ToString("yyyy-MM-ddTHH:mm");
                datnums = times.ToString();

                //该变量是根据时间值组合是数字串，后续作为降雨及计算结果的输出文件名称前缀，year前自动补0，与模型计算中更新rainfile中规则一致
                yearmmddForID = yearStr + mdhSt.Substring(0, 2) + mdhSt.Substring(2, 2) + mdhSt.Substring(4, 2);

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

                    byte[] datbytes = br.ReadBytes(datStruct.row * datStruct.col * 4);

                    for (int r = 0; r < datStruct.row; ++r)
                    {
                        for (int c = 0; c < datStruct.col; ++c)
                        {
                            //float val = br.ReadSingle();
                            //datStruct.rain[tindex, r, c] = val;
                            datStruct.rain[tindex, r, c] = BitConverter.ToSingle(datbytes, (r * datStruct.col + c) * 4);
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
            stopwatch.Stop();
            Console.WriteLine(string.Format("读取{0}台风场文件耗时{1}", curDatFullname, stopwatch.ElapsedMilliseconds));
            stopwatch.Restart();

            //！22 数据读取完成，则需要插值到各个计算单元，然后写出
            //! 遍历所有的计算单元信息表，写出数据
            //! 遍历每个计算单元，然后在其中遍历每个场次的数据
            int unitNUM = dbTableConfigs[keyString]["GRID_HSFX_UNIT"].Rows.Count;
            //unit 表
            DataTable grid_unit_tables = dbTableConfigs[keyString]["GRID_HSFX_UNIT"];

            int countOfHaveRain = 0;
            for (int i = 0; i < unitNUM; ++i)
            {
                //！单元的信息
                string provinceName = keyString;
                string groovyName = grid_unit_tables.Rows[i]["GroovyName"].ToString();

                //!当前场次下某个单元的所有时间文件写出
                bool status = WriteAscFileByParams(datPureName, provinceName, groovyName, yearmmddForID, datStruct, grid_unit_tables.Rows[i]);
                if (status)
                {
                    countOfHaveRain++;
                    //Console.WriteLine(string.Format("{0}台风场文件在{1}省下{2}单元目录切片成功", curDatFullname, provinceName, groovyName) + DateTime.Now);
                }
                else
                {
                    //Console.WriteLine(string.Format("{0}台风场文件在{1}省下{2}单元目录切片失败", curDatFullname, provinceName, groovyName) + DateTime.Now);
                }
                //stopwatch.Stop();
                //Console.WriteLine(string.Format("写入{0}台风场第{1}个计算单元耗时{2}", curDatFullname, i, stopwatch.ElapsedMilliseconds));
                //stopwatch.Restart();
            }
            stopwatch.Stop();
            Console.WriteLine(string.Format("写入{0}台风场文件耗时{1}", curDatFullname, stopwatch.ElapsedMilliseconds));

            Console.WriteLine(string.Format("{0}台风场文件在{1}节点下共有{2}个计算单元，其中{3}个计算单元中有有效降雨", curDatFullname, HookHelper.computerNode, unitNUM, countOfHaveRain) + DateTime.Now);

            //CSVLog
            if (HookHelper.useCSVLOG.Equals("true"))
            {
                CSVData.addData(CSVData.GetRowNumber(), "HostName", System.Net.Dns.GetHostName());
                var serverIP = Program.GetLocalIP(HookHelper.serachIP);
                CSVData.addData(CSVData.GetRowNumber(), "服务器IP", serverIP);
                CSVData.addData(CSVData.GetRowNumber(), "计算节点", HookHelper.computerNode);
                CSVData.addData(CSVData.GetRowNumber(), "eventId", Path.GetFileNameWithoutExtension(curDatFullname));
                CSVData.addData(CSVData.GetRowNumber(), "计算单元个数", unitNUM);
                CSVData.addData(CSVData.GetRowNumber(), "有效降雨单元个数", countOfHaveRain);

            }





            datStruct.headerone = null;
            datStruct.rain = null;
            datStruct.Lons = null;
            datStruct.Lats = null;
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

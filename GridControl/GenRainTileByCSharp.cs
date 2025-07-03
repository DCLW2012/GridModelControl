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
using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OSR;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

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
        public double nodata;

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

                            if (curRain > 0.5 && curRain < 1)
                            {
                                int aaa = 9;
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
        public static bool WriteAscFileByParamsOneTime(string datPureName, string provinceName, string groovyName, string startTimeCurDat, DatFileStruct dStruct, DataRow paramsUnitDT, int tIndex)
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
            //for (int t = 0; t < dStruct.headerone[2]; ++t)
            {
                dStruct.curRainIndex = tIndex;
                //!当前文件名
                string curWriteFileName = String.Format("{0}\\{1}-{2}.asc", unitOutdir, datPureName, tIndex);

                if (curWriteFileName.Contains("WHF66_3_4"))
                {
                    int a = 0;
                }

                //! 使用c#写出
                //@ 判断当前时段的降雨数据是否 对 当前传入的计算单元有降雨，有则写出，无则跳过；
                // 模型在执行计算的时候会搜索对应的降雨，找不到则自动跳过计算
                double xa1 = dStruct.Lons[dStruct.curRainIndex]; double ya1 = dStruct.Lats[dStruct.curRainIndex];
                double xa2 = xa1 + dStruct.fbl * (dStruct.col); double ya2 = ya1 + dStruct.fbl * (dStruct.row);

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
                    return false;
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
                            curRain = dStruct.rain[0, originRow, originCol];
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

        //降雨数据写出到asc文件4326坐标系
        static public bool WriteRainAscFileByParamsWithMinMax(DatFileStruct datStruct, int index, string fileName,
                        ref float minValue,
                        ref float maxValue)
        {
            try
            {
                // 检查输出目录是否存在，不存在则创建
                string directory = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    // 写出 ASC 文件头
                    writer.WriteLine($"ncols {@datStruct.row}");
                    writer.WriteLine($"nrows {@datStruct.col}");
                    writer.WriteLine($"xllcorner {@datStruct.Lons[index]:F6}");
                    writer.WriteLine($"yllcorner {@datStruct.Lats[index]:F6}");
                    writer.WriteLine($"cellsize {@datStruct.fbl:F6}");
                    writer.WriteLine("NODATA_value -9999");

                    int outRow = datStruct.row;
                    int outCol = datStruct.col;

                    const int NODATA_VALUE = -9999;

                    // 行倒序写入（从最后一行开始）
                    for (int r = outRow - 1; r >= 0; r--)
                    {
                        StringBuilder line = new StringBuilder();

                        for (int c = 0; c < outCol; c++)
                        {
                            float curRain = datStruct.rain[index, r, c];

                            // 忽略无效值参与最值统计
                            if (curRain >= NODATA_VALUE)
                            {
                                if (curRain < minValue && curRain > NODATA_VALUE)
                                    minValue = curRain;

                                if (curRain > maxValue)
                                    maxValue = curRain;
                            }

                            // 保留三位小数
                            line.AppendFormat("{0:F3}", curRain);

                            if (c != outCol - 1)
                                line.Append(" ");
                        }

                        writer.WriteLine(line.ToString());
                    }
                }

                //写出同名的prj文件
                string prjFileName = fileName.Replace(".asc", ".prj");
                //Projection GEOGRAPHIC
                //Datum WGS84
                //Spheroid WGS84
                //Units DD
                //Zunits NO
                //Parameters
                using (StreamWriter prjWriter = new StreamWriter(prjFileName))
                {
                    prjWriter.WriteLine("Projection GEOGRAPHIC");
                    prjWriter.WriteLine("Datum WGS84");
                    prjWriter.WriteLine("Spheroid WGS84");
                    prjWriter.WriteLine("Units DD");
                    prjWriter.WriteLine("Zunits NO");
                    prjWriter.WriteLine("Parameters");
                }




                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入文件失败: {ex.Message}");
                return false;
            }
        }

        static Rgba32 GenRGBColor(float value, float curMinValue, float curMaxValue)
        {
            //创建十个元素的list List<float>
            List<float> _cValues = new List<float>(10);
            for (int i = 0; i < 10; ++i)
            {
                _cValues.Add(0.0f);
            }


            //根据最小值，最大值，生成_cValues 区间。
            for (int i = 0; i < 10; ++i)
            {
                if (i == 0)
                {
                    _cValues[0] = curMinValue;
                }

                if (i == 9)
                {
                    _cValues[9] = curMaxValue;
                }

                if (i > 0 && i < 9)
                {
                    _cValues[i] = (float)(curMinValue + (curMaxValue - curMinValue + 1.0e-6) / 10.0 * (float)i);
                }
            }

            List<Rgba32> _cColors = new List<Rgba32>();
            _cColors.Add(new Rgba32(255, 255, 255, 255));
            _cColors.Add(new Rgba32(166, 242, 242, 255));
            _cColors.Add(new Rgba32(61, 184, 63, 255));
            _cColors.Add(new Rgba32(98, 184, 255, 255));
            _cColors.Add(new Rgba32(0, 0, 253, 255));
            _cColors.Add(new Rgba32(249, 1, 249, 255));
            _cColors.Add(new Rgba32(127, 1, 64, 255));
            _cColors.Add(new Rgba32(244, 167, 0, 255));
            _cColors.Add(new Rgba32(235, 99, 0, 255));
            _cColors.Add(new Rgba32(220, 0, 0, 255));
            _cColors.Add(new Rgba32(147, 0, 0, 255));

            if (value <= _cValues[0])
            {
                return _cColors[0];
            }

            if (value > _cValues[9])
            {
                return _cColors[10];
            }

            int index = 0;
            for (int i = 0; i < _cValues.Count; ++i)
            {
                if (value > _cValues[i] && value <= _cValues[i + 1])
                {
                    index = i;
                    break;
                }
            }

            return _cColors[index + 1];

        }

        static public Rgba32 GenRGBColorByLegend_rain(float value, float curMinValue, float curMaxValue)
        {
            //0
            //0-10，10-20，20-50，50-100，100-200，200-500，500-1000，1000-2000，2000-5000，5000-10000，10000以上
            //创建十个元素的list List<float>
            List<float> _cValues = new List<float>(10);
            _cValues.Add(0.0f); // 0
            _cValues.Add(1.0f); // 1
            _cValues.Add(2.0f); // 2
            _cValues.Add(5.0f); // 3
            _cValues.Add(10.0f); // 4
            _cValues.Add(15f); // 5
            _cValues.Add(20.0f); // 6
            _cValues.Add(50.0f); // 7
            _cValues.Add(100.0f); // 8
            _cValues.Add(150.0f); // 9
            _cValues.Add(200.0f); // 10


            List<Rgba32> _cColors = new List<Rgba32>();
            _cColors.Add(new Rgba32(115, 223, 255, 128));
            _cColors.Add(new Rgba32(166, 242, 242, 128));
            _cColors.Add(new Rgba32(61, 184, 63, 128));
            _cColors.Add(new Rgba32(98, 184, 255, 128));
            _cColors.Add(new Rgba32(0, 0, 253, 128));
            _cColors.Add(new Rgba32(249, 1, 249, 128));
            _cColors.Add(new Rgba32(127, 1, 64, 128));
            _cColors.Add(new Rgba32(244, 167, 0, 128));
            _cColors.Add(new Rgba32(235, 99, 0, 128));
            _cColors.Add(new Rgba32(220, 0, 0, 128));
            _cColors.Add(new Rgba32(147, 0, 0, 128));

            if (value <= _cValues[0])
            {
                return new Rgba32(255, 255, 255, 128);
            }

            if (value > _cValues[10])
            {
                return _cColors[10];
            }

            int index = 0;
            for (int i = 0; i < _cValues.Count - 1; ++i)
            {
                if (value > _cValues[i] && value <= _cValues[i + 1])
                {
                    index = i;
                    break;
                }
            }

            return _cColors[index];
        }

        static public bool AscDemToColorPng(string inascfile, string outpngfile, ref float curMinValue, ref float curMaxValue)
        {
            try
            {
                // 检查输入文件是否存在
                if (!File.Exists(inascfile))
                {
                    Console.WriteLine($"输入的ASC文件不存在: {inascfile}");
                    return false;
                }
                // 检查输出目录是否存在，不存在则创建
                string directory = Path.GetDirectoryName(outpngfile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                //读取tif文件inascfile 从中获取行列数和波段1的最值范围
                // 注册所有驱动程序
                GdalBase.ConfigureAll();
                Gdal.AllRegister();

                using (Dataset dataset = Gdal.Open(inascfile, Access.GA_ReadOnly))
                {
                    if (dataset == null)
                    {
                        Console.WriteLine("无法打开指定的DEM文件");
                        return false;
                    }

                    // 获取第一个波段
                    Band band = dataset.GetRasterBand(1);

                    // 获取波段统计信息，如果尚未计算，则自动计算
                    double[] minMax = new double[2];
                    band.ComputeRasterMinMax(minMax, 1);

                    curMinValue = (float)minMax[0]; // 最小值
                    curMaxValue = (float)minMax[1]; // 最大值

                    int cols = dataset.RasterXSize; // 列数（宽度）
                    int rows = dataset.RasterYSize; // 行数（高度）
                    Console.WriteLine($"行列数: {cols} 列 x {rows} 行");

                    // 获取数据类型（如 GDT_Float32、GDT_Int16 等）
                    var dataType = band.DataType;

                    double noDataValue;
                    int hasNoData;

                    band.GetNoDataValue(out noDataValue, out hasNoData);

                    // 分配一个缓冲区来读取整张图像
                    float[] buffer = new float[cols * rows];

                    // 读取波段数据到缓冲区
                    band.ReadRaster(
                        0, 0, cols, rows,
                        buffer, cols, rows, 0, 0);
                    Image<Rgba32> image = new Image<Rgba32>(cols, rows);
                    // 遍历每个像素并输出高程值
                    for (int row = 0; row < rows; row++)
                    {
                        for (int col = 0; col < cols; col++)
                        {
                            float value = buffer[row * cols + col];
                            var temp = Math.Abs(value - noDataValue) < 0.0001; // 检查是否为无效值
                            if (hasNoData > 0 && temp)
                            {
                                image[col, row] = new Rgba32(0, 0, 0, 0); // 设置为完全透明
                            }
                            else
                            {
                                Rgba32 color = GenRGBColorByLegend_rain(value, curMinValue, curMaxValue);
                                image[col, row] = color; // 根据值映射到灰度
                            }
                        }
                    }

                    var options = new PngEncoder
                    {
                        ColorType = PngColorType.RgbWithAlpha,
                        BitDepth = PngBitDepth.Bit8
                    };

                    image.Save(outpngfile, options);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成PNG失败: {ex.Message}");
                return false;
            }
        }

        static public bool MergeASCFilesToTifAndPng(String srcEPSG, String targetEPSG, List<string> inputFiles, String outputTifFile, String pngOutputFile, double[] outputBounds, ref float curMinValue, ref float curMaxValue)
        {
            //// 输入文件列表（替换为实际路径）
            //var inputFiles = new List<string>
            //{
            //    @"D:\\output\\2025061100-10-r4000-c4000-d1\\henan\\output\\txt\\2025061100-10-r4000-c4000-d1-discharge-henan-000.asc",
            //    @"D:\\output\\2025061100-10-r4000-c4000-d1\\shandong\\output\\txt\\2025061100-10-r4000-c4000-d1-discharge-shandong-000.asc"
            //};
            // 检查输出目录是否存在，不存在则创建
            string directory = Path.GetDirectoryName(outputTifFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            directory = Path.GetDirectoryName(pngOutputFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            // 初始化GDAL
            GdalBase.ConfigureAll();
            Gdal.AllRegister();
            // 目标坐标系 (EPSG:4326 - WGS84)
            SpatialReference targetSrs = new SpatialReference("");
            targetSrs.ImportFromEPSG(4326);
            // 目标分辨率（按需调整）
            double targetResolution = 0.01; // 单位：度
            string dstSrsWkt;
            targetSrs.ExportToWkt(out dstSrsWkt, null);


            // 步骤1: 预处理每个文件（重投影+重采样）
            var processedFiles = new List<string>();

            string tempDir = Path.GetTempPath();

            foreach (var file in inputFiles)
            {
                string tempFile = Path.Combine(tempDir, $"{Guid.NewGuid()}.tif");
                processedFiles.Add(tempFile);

                using (var srcDs = Gdal.Open(file, Access.GA_ReadOnly))
                {
                    // 创建重投影选项

                    var warpOptions = new string[]
                    {
                        "-t_srs", dstSrsWkt,
                        "-tr", $"{targetResolution}", $"{targetResolution}",
                        "-r", "bilinear",       // 重采样方法：双线性插值
                        "-of", "GTiff",         // 输出格式
                        "-overwrite"
                    };
                    if (!String.IsNullOrEmpty(srcEPSG))
                    {
                        warpOptions = new string[]
                        {
                            "-s_srs", srcEPSG,      // 源坐标系
                            "-t_srs", dstSrsWkt,
                            "-tr", $"{targetResolution}", $"{targetResolution}",
                            "-r", "near",       // 重采样方法：双线性插值
                            "-of", "GTiff",         // 输出格式
                            "-overwrite"
                        };
                    }

                    // 执行重投影和重采样
                    Dataset tempsr = Gdal.Warp(tempFile, new Dataset[] { srcDs }, new GDALWarpAppOptions(warpOptions), null, "");
                    tempsr.Dispose();
                }
            }

            // 步骤2: 合并所有预处理后的文件
            var vrtFile = Path.Combine(tempDir, $"{Guid.NewGuid()}.vrt");
            using (var vrtDs = Gdal.BuildVRT(vrtFile, processedFiles.ToArray(), null, null, ""))
            {
                // 转换为最终输出文件
                //Dataset temp = Gdal.wrapper_GDALTranslate(tempoutmergeFile, vrtDs, new GDALTranslateOptions(new string[] { "-of", "GTiff" }), null, "");
                //temp.Dispose();
                // 假设已初始化GDAL并打开srcDs，已设置dstSrsWkt
                //double[] outputBounds = { 89.705, 17.339, 138.997, 55.238 }; // 指定输出范围

                string[] warpOptions = new string[]
                {
                $"-t_srs", dstSrsWkt,
                "-r", "near", // 最近邻插值
                "-of", "GTiff",
                "-te", outputBounds[0].ToString(), outputBounds[1].ToString(), outputBounds[2].ToString(), outputBounds[3].ToString() // 范围
                };
                // 调用Gdal.Warp
                Dataset dstDs = Gdal.Warp(outputTifFile, new Dataset[] { vrtDs }, new GDALWarpAppOptions(warpOptions), null, "");
                dstDs.Dispose();
            }
            Console.WriteLine($"Merged output: {outputTifFile}");

            // 清理临时文件
            foreach (var file in processedFiles) File.Delete(file);
            File.Delete(vrtFile);

            //tempoutmergeFile tif文件写出为png文件
            if (File.Exists(outputTifFile))
            {
                AscDemToColorPng(outputTifFile, pngOutputFile, ref curMinValue, ref curMaxValue);
            }
            else
            {
                Console.WriteLine("输出文件不存在，请检查路径和文件名。");
            }

            return true;
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

            //动态根据文件名修正行列数2023021608-24-r1040-c1600-d6.dat
            string datPureName = System.IO.Path.GetFileNameWithoutExtension(curDatFullname);
            string[] namelist = datPureName.Split('-');
            if (namelist.Length == 5 && namelist[0].Length == 10)
            {
                gridrow = int.Parse(namelist[2].Replace("r", ""));
                gridcol = int.Parse(namelist[3].Replace("c", ""));
                rainSRCFBL = float.Parse(namelist[4].Replace("d", "")) / 100;
            }

            //！解析当前dat文件
            //！创建数据存储结构
            DatFileStruct datStruct = new DatFileStruct();
            datStruct.col = gridcol;
            datStruct.row = gridrow;
            datStruct.fbl = rainSRCFBL;
            DateTime begin_time = DateTime.Now;

            Dictionary<String, List<WaterDeep>> gridResultFieldURL = new Dictionary<string, List<WaterDeep>>();
            gridResultFieldURL.Add("rain", new List<WaterDeep>());

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
                begin_time = dt;
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
            Console.WriteLine(string.Format("读取{0}台风场文件耗时{1}秒", curDatFullname, stopwatch.ElapsedMilliseconds / 1000));
            stopwatch.Restart();

            //！22 数据读取完成，则需要插值到各个计算单元，然后写出
            //首先要把降雨数据存储到结果目录中，判断结果目录是否存在，不存在则创建
            //对齐输出文件索引
            String rainFolder = Path.Combine(datPureName, "rain", "txt");
            String pngFloder = Path.Combine(datPureName, "rain", "png");
            String pngFloder4326 = Path.Combine(datPureName, "rain", "png4326");

            String curJsonFilefullpath = Path.Combine(HookHelper.IISRootDirectory, datPureName, "rain.json");
            String curGifFilefullpath = Path.Combine(HookHelper.IISRootDirectory, datPureName, "rain.gif");

            for (int tindex = 0; tindex < datStruct.headerone[2]; ++tindex)
            {
                String outFormatIndex = tindex.ToString("D3");
                String curCCTimeOutdir = String.Format("{0}/{1}/{2}-{3}.asc", HookHelper.IISRootDirectory, rainFolder, datPureName, tindex.ToString("D3"));
                String curCCTimepngFloder4326Outdir = String.Format("{0}/{1}/{2}-{3}.png", HookHelper.IISRootDirectory, pngFloder4326, datPureName, tindex.ToString("D3"));

                //！ curCCTimepngFloder4326Outdir 文件存在则跳过
                if (File.Exists(curCCTimepngFloder4326Outdir))
                {
                    continue;
                }

                DateTime curTime = begin_time.AddHours(tindex);
                String curFrameTime = curTime.ToString("yyyyMMddHHmm");
                //写出数据为png降雨图片到curCCTimepngFloder4326Outdir文件中
                double[] outBounds = new double[4];
                outBounds[0] = double.Parse(datStruct.Lons[tindex].ToString());
                outBounds[1] = double.Parse(datStruct.Lats[tindex].ToString());
                outBounds[2] = outBounds[0] + datStruct.col * datStruct.fbl;
                outBounds[3] = outBounds[1] + datStruct.row * datStruct.fbl;

                float curMinvalue = 9999.0f;
                float curMaxValue = -9999.0f;
                bool isPngOK32649 = WriteRainAscFileByParamsWithMinMax(datStruct, tindex, curCCTimeOutdir, ref curMinvalue, ref curMaxValue);
                if (isPngOK32649)
                {
                    String curoutReporjectTifFile = Path.Combine(Path.GetDirectoryName(curCCTimeOutdir), "4326", Path.GetFileNameWithoutExtension(curCCTimeOutdir) + ".tif");
                    List<String> listForMerge = new List<String>();
                    listForMerge.Add(curCCTimeOutdir);
                    String srcEPSG = String.Format("EPSG:{0}", "4326");
                    bool isrpro4326 = MergeASCFilesToTifAndPng(srcEPSG, "", listForMerge, curoutReporjectTifFile, curCCTimepngFloder4326Outdir, outBounds, ref curMinvalue, ref curMaxValue);
                    //再写出份4326的 
                    //curCCTimeOutPng4326Filename 中获取带扩展名的文件名
                    String extStr = Path.GetFileName(curCCTimepngFloder4326Outdir);

                    //为对应的指标添加url
                    WaterDeep wd = new WaterDeep();
                    wd.url = String.Format("/rain/png4326/{0}", extStr);
                    wd.time = curFrameTime;
                    wd.area = 0; //淹没面积
                    wd.isHavarecord = "1"; //有数据
                    if (!gridResultFieldURL.ContainsKey("rain"))
                    {
                        gridResultFieldURL["rain"] = new List<WaterDeep>();
                    }
                    gridResultFieldURL["rain"].Add(wd);
                    ReadWriteJSONFile.Write(gridResultFieldURL["rain"], curJsonFilefullpath);

                    //Logger::Message(QStringLiteral("%1场次时间%2的字段%3在%4省份png写出成功").arg(curCCname).arg(indexNumber).arg(gridResultFieldName[g]).arg(proName));


                    //如果是最后一个时间，则在当前png目录下直接输出gif文件，合并png
                    if (tindex == datStruct.headerone[2] - 1)
                    {
                        //合并当前省份下的所有png文件
                        String pngFullFloder = Path.Combine(HookHelper.IISRootDirectory, pngFloder4326);

                        bool isGifOK = MergeTileToIISFolder.CreateGif(pngFullFloder, curGifFilefullpath, 1000);
                        if (isGifOK)
                        {
                            //输出gif成功
                            Console.WriteLine("全区域合并成功");
                        }
                    }
                }

            }


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
            Console.WriteLine(string.Format("写入{0}台风场文件耗时{1}秒", curDatFullname, stopwatch.ElapsedMilliseconds / 1000));

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

        //读取一个时间，写出一个时间，避免数据大，没法一次读取
        public static bool CreateTileByWATAByCSharpOneTimeByOne(string curDatFullname, ref string start, ref string end, ref string datnums, ref string yearmmddForID)
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
                // 数据量大的时候无法分配内存，加载所有时间场次的数据，故改为读一个小时的写一个小时的
                datStruct.rain = new float[1, datStruct.row, datStruct.col];
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
                            datStruct.rain[0, r, c] = BitConverter.ToSingle(datbytes, (r * datStruct.col + c) * 4);
                        }
                    }

                    //写出到每个目录切片
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
                        bool status = WriteAscFileByParamsOneTime(datPureName, provinceName, groovyName, yearmmddForID, datStruct, grid_unit_tables.Rows[i], tindex);
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

                    Console.WriteLine(string.Format("{0}台风场文件在{1}节点下共有{2}个计算单元，其中第{3}个时刻{4}个计算单元中有有效降雨", curDatFullname, HookHelper.computerNode, unitNUM, tindex, countOfHaveRain) + DateTime.Now);

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

            datStruct.headerone = null;
            datStruct.rain = null;
            datStruct.Lons = null;
            datStruct.Lats = null;
            return true;
        }

        //根据nc目录，获取起始结束事件等场次信息，并切片到单元上。当目录为空或者目录内nc个数少于2个，则返回false
        public static bool CreateTileByWATAByCSharpFromNCFolder(string curNCFFoldername, ref string start, ref string end, ref string datnums, ref string yearmmddForID)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            FileInfo[] fInfo = GenRainTileByCSharp.GetRainNCFilesList(curNCFFoldername);

            datnums = fInfo.Length.ToString();
            if (fInfo.Length < 2)
            {
                Console.WriteLine(string.Format("{0}台风场文件解析场次信息失败，没有有效的nc文件，继续下一个", curNCFFoldername) + DateTime.Now);
                return false;
            }

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

            string datPureName = System.IO.Path.GetFileNameWithoutExtension(curNCFFoldername);

            //！解析当前nc文件
            //！创建数据存储结构
            DatFileStruct datStruct = new DatFileStruct();
            datStruct.col = gridcol;
            datStruct.row = gridrow;
            datStruct.fbl = rainSRCFBL;


            // 读取dat文件改为遍历nc文件，生成datStruct结构，继续下一步的切片写出
            try
            {
                //! 第一部分数据 年(year)、月日时(mdh)、该台风总时次(times) 均为整型  3 * 4 个字节
                //文件夹内子文件第一个文件名字为时间字符串
                String[] firstNCNameStr = fInfo[0].ToString().Split('_');
                String firstNCName = firstNCNameStr[firstNCNameStr.Length-1];
                int year = int.Parse(firstNCName.Substring(0,4));
                int mdh = int.Parse(firstNCName.Substring(4, 6));
                int times = fInfo.Length;
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
                datPureName = yearmmddForID;
                //！2、第二部分，是各个场次经纬度列表，
                datStruct.Lats = new double[times];
                datStruct.Lons = new double[times];

                //!3、第三部分，是所有场次的网格数据存储，三维数组存放每个时间的网格数据
                datStruct.rain = null;
                //遍历每个nc文件，读取解析存储信息
                for (int tindex = 0; tindex < datStruct.headerone[2]; ++tindex)
                {
                    //使用netcdf读取nc文件
                    ReadWriteNCIO readwritencio = new ReadWriteNCIO();
                    bool isReadNCSucess = readwritencio.ReadNCFileSingleTime(curNCFFoldername + "//" + fInfo[tindex].ToString());
                    if (!isReadNCSucess)
                    {
                        Console.WriteLine(string.Format("{0}的nc文件读取失败，跳过计算", curNCFFoldername + "//" + fInfo[tindex].ToString()) + DateTime.Now);
                        return false;
                    }

                    //更新经纬度起点坐标，左下角
                    datStruct.col = readwritencio.XDimension.DimLength;
                    datStruct.row = readwritencio.YDimension.DimLength;
                    datStruct.fbl = readwritencio.XDelt;
                    datStruct.Lats[tindex] = readwritencio.YDimension.GetValues()[0];
                    datStruct.Lons[tindex] = readwritencio.XDimension.GetValues()[0];

                    if (datStruct.rain == null)
                    {
                        datStruct.rain = new float[times, datStruct.row, datStruct.col];
                    }

                    //更新栅格值
                    datStruct.curRainIndex = tindex;

                    for (int r = 0; r < datStruct.row; ++r)
                    {
                        for (int c = 0; c < datStruct.col; ++c)
                        {
                            datStruct.rain[tindex, r, c] = (float)readwritencio.GRIDData[r,c];
                        }
                    }
                }

            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("{0}台风场文件解析场次信息失败，继续下一个", curNCFFoldername) + DateTime.Now);
                return false;
            }
            stopwatch.Stop();
            Console.WriteLine(string.Format("读取{0}台风场文件耗时{1}", curNCFFoldername, stopwatch.ElapsedMilliseconds));
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
                    //Console.WriteLine(string.Format("{0}台风场文件在{1}省下{2}单元目录切片成功", curNCFFoldername, provinceName, groovyName) + DateTime.Now);
                }
                else
                {
                    //Console.WriteLine(string.Format("{0}台风场文件在{1}省下{2}单元目录切片失败", curNCFFoldername, provinceName, groovyName) + DateTime.Now);
                }
                //stopwatch.Stop();
                //Console.WriteLine(string.Format("写入{0}台风场第{1}个计算单元耗时{2}", curNCFFoldername, i, stopwatch.ElapsedMilliseconds));
                //stopwatch.Restart();
            }
            stopwatch.Stop();
            Console.WriteLine(string.Format("写入{0}台风场文件耗时{1}", curNCFFoldername, stopwatch.ElapsedMilliseconds));

            Console.WriteLine(string.Format("{0}台风场文件在{1}节点下共有{2}个计算单元，其中{3}个计算单元中有有效降雨", curNCFFoldername, HookHelper.computerNode, unitNUM, countOfHaveRain) + DateTime.Now);

            //CSVLog
            if (HookHelper.useCSVLOG.Equals("true"))
            {
                CSVData.addData(CSVData.GetRowNumber(), "HostName", System.Net.Dns.GetHostName());
                var serverIP = Program.GetLocalIP(HookHelper.serachIP);
                CSVData.addData(CSVData.GetRowNumber(), "服务器IP", serverIP);
                CSVData.addData(CSVData.GetRowNumber(), "计算节点", HookHelper.computerNode);
                CSVData.addData(CSVData.GetRowNumber(), "eventId", Path.GetFileNameWithoutExtension(curNCFFoldername));
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

        public static bool CreateTileByWATAByCSharpFromProvinceFromNC(string keyString, string curNCFFoldername, ref string start, ref string end, ref string datnums, ref string yearmmddForID)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            FileInfo[] fInfo = GenRainTileByCSharp.GetRainNCFilesList(curNCFFoldername);

            datnums = fInfo.Length.ToString();
            if (fInfo.Length < 2)
            {
                Console.WriteLine(string.Format("{0}台风场文件解析场次信息失败，没有有效的nc文件，继续下一个", curNCFFoldername) + DateTime.Now);
                return false;
            }

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

            string datPureName = System.IO.Path.GetFileNameWithoutExtension(curNCFFoldername);

            //！解析当前dat文件
            //！创建数据存储结构
            DatFileStruct datStruct = new DatFileStruct();
            datStruct.col = gridcol;
            datStruct.row = gridrow;
            datStruct.fbl = rainSRCFBL;


            // 读取dat文件改为遍历nc文件，生成datStruct结构，继续下一步的切片写出
            try
            {
                //! 第一部分数据 年(year)、月日时(mdh)、该台风总时次(times) 均为整型  3 * 4 个字节
                //文件夹内子文件第一个文件名字为时间字符串
                String[] firstNCNameStr = fInfo[0].ToString().Split('_');
                String firstNCName = firstNCNameStr[firstNCNameStr.Length - 1];
                int year = int.Parse(firstNCName.Substring(0, 4));
                int mdh = int.Parse(firstNCName.Substring(4, 6));
                int times = fInfo.Length;
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
                datPureName = yearmmddForID;
                //！2、第二部分，是各个场次经纬度列表，
                datStruct.Lats = new double[times];
                datStruct.Lons = new double[times];

                //!3、第三部分，是所有场次的网格数据存储，三维数组存放每个时间的网格数据
                datStruct.rain = null;
                //遍历每个nc文件，读取解析存储信息
                for (int tindex = 0; tindex < datStruct.headerone[2]; ++tindex)
                {
                    //使用netcdf读取nc文件
                    ReadWriteNCIO readwritencio = new ReadWriteNCIO();
                    bool isReadNCSucess = readwritencio.ReadNCFileSingleTime(curNCFFoldername + "//" + fInfo[tindex].ToString());
                    if (!isReadNCSucess)
                    {
                        Console.WriteLine(string.Format("{0}的nc文件读取失败，跳过计算", curNCFFoldername + "//" + fInfo[tindex].ToString()) + DateTime.Now);
                        return false;
                    }

                    //更新经纬度起点坐标，左下角
                    datStruct.col = readwritencio.XDimension.DimLength;
                    datStruct.row = readwritencio.YDimension.DimLength;
                    datStruct.fbl = readwritencio.XDelt;
                    datStruct.Lats[tindex] = readwritencio.YDimension.GetValues()[0];
                    datStruct.Lons[tindex] = readwritencio.XDimension.GetValues()[0];

                    if (datStruct.rain == null)
                    {
                        datStruct.rain = new float[times, datStruct.row, datStruct.col];
                    }

                    //更新栅格值
                    datStruct.curRainIndex = tindex;

                    for (int r = 0; r < datStruct.row; ++r)
                    {
                        for (int c = 0; c < datStruct.col; ++c)
                        {
                            datStruct.rain[tindex, r, c] = (float)readwritencio.GRIDData[r, c];
                        }
                    }
                }

            }
            catch (IOException e)
            {
                Console.WriteLine(string.Format("{0}台风场文件解析场次信息失败，继续下一个", curNCFFoldername) + DateTime.Now);
                return false;
            }
            stopwatch.Stop();
            Console.WriteLine(string.Format("读取{0}台风场文件耗时{1}", curNCFFoldername, stopwatch.ElapsedMilliseconds));
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
            Console.WriteLine(string.Format("写入{0}台风场文件耗时{1}", curNCFFoldername, stopwatch.ElapsedMilliseconds));

            Console.WriteLine(string.Format("{0}台风场文件在{1}节点下共有{2}个计算单元，其中{3}个计算单元中有有效降雨", curNCFFoldername, HookHelper.computerNode, unitNUM, countOfHaveRain) + DateTime.Now);
            
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
        public static DirectoryInfo[] GetRainNCFolderList()
        {
            DirectoryInfo pDirectoryInfo = new DirectoryInfo(HookHelper.rainSRCDirectory);
            DirectoryInfo[] ArrayileInfo = pDirectoryInfo.GetDirectories();
            if (ArrayileInfo.Length < 1)
            {
                return ArrayileInfo;
            }
            Array.Sort(ArrayileInfo, new FileNameSort());


            return ArrayileInfo;
        }

        public static FileInfo[] GetRainNCFilesList(String folder)
        {
            DirectoryInfo pDirectoryInfo = new DirectoryInfo(folder);
            FileInfo[] ArrayileInfo = pDirectoryInfo.GetFiles("*.nc");
            if (ArrayileInfo.Length < 1)
            {
                return ArrayileInfo;
            }
            Array.Sort(ArrayileInfo, new FileNameSort());


            return ArrayileInfo;
        }
    }


}

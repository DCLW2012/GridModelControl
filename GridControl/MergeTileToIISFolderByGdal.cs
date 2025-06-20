using Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SixLabors.ImageSharp.Processing;
using SysDAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OSR;
using System.Security.Cryptography.Xml;
using MaxRev.Gdal.Core;

namespace GridControl
{
    internal class MergeTileToIISFolderByGdal
    {
        //成员变量
        private string _fileNameWithoutExtension;
        private String _iisRootDirectory;
        DataTable _stpoinginfo;
        Dictionary<String, int> _srcEPSGINFO;
        DataTable _srcSizeINFO;
        //各个省份的四至信息
        DataTable _extentInfo;
        int _provinceNum;
        DataTable _unitsinfo;
        DataTable _taifenginfoForcalc;

        //构造函数
        public MergeTileToIISFolderByGdal(String fileNameWithoutExtension, DataTable taifenginfoForcalc)
        {
            _taifenginfoForcalc = taifenginfoForcalc;

            //当前场次的文件名不带扩展名
            _fileNameWithoutExtension = fileNameWithoutExtension;

            //IIS根目录
            _iisRootDirectory = HookHelper.IISRootDirectory;

            //从china表中执行查找
            String keyString = "china";
            String stpoinginfosql = "select min([left]), min(bottom), min(degfbl), min(xllcorner), min(yllcorner), min(cellsize), max([right]), max([top]) from GRID_HSFX_UNIT";
            _stpoinginfo = Dal_Rain.GetDataBySql(keyString, stpoinginfosql);

            String srcEPSGINFOSQL = "SELECT * from ChinaCoordinate ORDER BY Coordinate desc";
            DataTable srcEPSGINFO = Dal_Rain.GetDataBySql(keyString, srcEPSGINFOSQL);
            //遍历
            _srcEPSGINFO = new Dictionary<string, int>();
            foreach (DataRow row in srcEPSGINFO.Rows)
            {
                String proName = row["province"].ToString();
                int epsgCode = int.Parse(row["Coordinate"].ToString());
                if (!_srcEPSGINFO.ContainsKey(proName))
                {
                    _srcEPSGINFO.Add(proName, epsgCode);
                }
            }


            String srcSizeINFOSQL = "SELECT * from ChinaCoordinate ORDER BY Coordinate desc";
            _srcSizeINFO = Dal_Rain.GetDataBySql(keyString, srcSizeINFOSQL);

            String srcExtentINFOSQL = "SELECT * from grid_taifeng_province_extent ORDER BY order_num asc";
            _extentInfo = Dal_ThirdWeb.GetDataBySql(srcExtentINFOSQL);

            int _provinceNum = _srcSizeINFO.Rows.Count;

            if (_srcEPSGINFO.Count == 0)
            {
                //输出提示信息
                Console.WriteLine("数据库表中没有查询到有效的区域坐标系信息!");
                return;
            }
            double mfbl = double.Parse(_stpoinginfo.Rows[0][5].ToString());

            //"where T1.UNITCD = 'WHE13_4_1'"
            //"where T2.ComputerIP = '172.16.43.2' "
            // 结果目录列表根据组号和node id获取当前主机路径
            String folderNamelistSql = @"SELECT DISTINCT
	                                        T1.UNITCD,
	                                        T1.province,
	                                        T1.bswatacd,
	                                        T2.ComputeNode,
	                                        T2.APPPath,
	                                        T2.ComputerIP,
	                                        T2.ComputerName 
                                        FROM
	                                        GRID_HSFX_UNIT T1
	                                        INNER JOIN (
	                                        SELECT
		                                        a.*,
		                                        b.bswatacd,
		                                        b.ComputerName,
		                                        b.ComputerIP 
	                                        FROM
		                                        HSFX_ComputeUnit a
		                                        LEFT JOIN HSFX_COMPUTER_Group b ON a.ComputeNode = b.ComputeNode 
	                                        WHERE
		                                        b.ComputerGroup = 'group1' 
	                                        ) T2 ON T1.GroupID = T2.ComputeUnit 
	                                        AND T1.bswatacd = T2.bswatacd 
                                        ORDER BY
	                                        T2.ComputerIP ASC";


            //所有计算单元的列表
            if (!string.IsNullOrEmpty(HookHelper.curProvince))
            {
                //如果省份名称中包含逗号，则表示是多个省份
                List<string> listmultipro = new List<string>();
                listmultipro = HookHelper.curProvince.Split(',').ToList();
                //每个元素用单引号包裹并用逗号连接
                for (int j = 0; j < listmultipro.Count; j++)
                {
                    listmultipro[j] = "'" + listmultipro[j] + "'";
                }
                String new_provincename = string.Join(",", listmultipro);
                folderNamelistSql = String.Format(@"SELECT DISTINCT
                                                    T1.UNITCD,
                                                    T1.province,
                                                    T1.bswatacd,
                                                    T2.ComputeNode,
                                                    T2.APPPath,
                                                    '192.168.1.171' AS ComputerIP,
                                                    T3.ComputerName
                                            FROM
                                                    GRID_HSFX_UNIT T1
                                            INNER JOIN (
                                                    SELECT DISTINCT
                                                            a.*, b.bswatacd,
                                                            b.ComputerName
                                                    FROM
                                                            HSFX_ComputeUnit a
                                                    LEFT JOIN HSFX_COMPUTER b ON a.ComputeNode = b.ComputeNode
                                            ) T2 ON T1.GroupID = T2.ComputeUnit
                                            AND T1.bswatacd = T2.bswatacd
                                            AND T1.province in ({0})
                                            LEFT JOIN HSFX_COMPUTER T3 ON T1.bswatacd = T3.bswatacd", new_provincename);
            }
            _unitsinfo = Dal_Rain.GetDataBySql(keyString, folderNamelistSql);

            if (_unitsinfo.Rows.Count == 0)
            {
                //提示信息
                Console.WriteLine("没有查询到有效的计算单元信息，请检查数据库连接和数据表内容!");
                return;
            }

            
        }

        public void ReadGridDataFromAsc(string fileName, ref DatFileStruct dt)
        {
            dt.col = 0;
            dt.row = 0;

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"当前查询搜索的文件--{fileName}--不存在");
                return;
            }

            string[] lines = File.ReadAllLines(fileName);
            int count = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = Regex.Split(line, @"\s+|,");
                if (parts.Length < 2) continue;

                switch (count)
                {
                    case 0 when parts[0].Equals("ncols", StringComparison.OrdinalIgnoreCase):
                        if (int.TryParse(parts[1], out var col))
                            dt.col = col;
                        break;

                    case 1 when parts[0].Equals("nrows", StringComparison.OrdinalIgnoreCase):
                        if (int.TryParse(parts[1], out var row))
                            dt.row = row;
                        break;

                    case 2 when parts[0].Equals("xllcorner", StringComparison.OrdinalIgnoreCase):
                        if (double.TryParse(parts[1], out var xll))
                            dt.xllcorner = xll;
                        dt.xmaxcorner = dt.xllcorner;
                        break;

                    case 3 when parts[0].Equals("yllcorner", StringComparison.OrdinalIgnoreCase):
                        if (double.TryParse(parts[1], out var yll))
                            dt.yllcorner = yll;
                        dt.ymaxcorner = dt.yllcorner;
                        break;

                    case 4 when parts[0].Equals("cellsize", StringComparison.OrdinalIgnoreCase):
                        if (double.TryParse(parts[1], out var cellsize))
                            dt.cellsize = cellsize;
                        break;

                    case 5 when parts[0].Equals("nodata_value", StringComparison.OrdinalIgnoreCase) ||
                                 parts[0].Equals("NODATA_VALUE", StringComparison.OrdinalIgnoreCase):
                        if (double.TryParse(parts[1], out var nodata))
                            dt.nodata = nodata;
                        break;
                }

                count++;
            }

            // 分配内存
            if (dt.col <= 0 || dt.row <= 0)
            {
                Console.WriteLine("列或行数无效，无法继续解析数据");
                return;
            }

            // 分配降雨数据数组
            dt.rain = new float[dt.row, dt.col, 1];


            int rowIndex = 0;
            for (int i = 6; i < lines.Length && rowIndex < dt.row; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] values = Regex.Split(line, @"\s+|,");
                if (values.Length != dt.col)
                {
                    Console.WriteLine($"第 {i + 1} 行数据列数不匹配，跳过该行");
                    continue;
                }

                for (int j = 0; j < dt.col; j++)
                {
                    if (float.TryParse(values[j], out var val))
                    {
                        dt.rain[rowIndex, j, 0] = val;
                    }
                    else
                    {
                        dt.rain[rowIndex, j, 0] = (float)dt.nodata;
                    }
                }

                rowIndex++;
            }
            if(dt.rain != null && dt.rain.Length > 0)
            {
                dt.xmaxcorner = dt.xllcorner + dt.cellsize * (dt.col);
                dt.ymaxcorner = dt.yllcorner + dt.cellsize * (dt.row);
            }
        }

        /// <summary>
        /// 写出 .prj 地理投影文件（UTM格式）
        /// </summary>
        /// <param name="fileName">输出文件路径</param>
        /// <param name="utmNumber">UTM带号（字符串）</param>
        /// <returns>是否写入成功</returns>
        public bool WriteResultProjAscFileByParams(string fileName, string utmNumber)
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
                    writer.WriteLine("Projection UTM");
                    writer.WriteLine($"Zone {utmNumber}");
                    writer.WriteLine("Datum WGS84");
                    writer.WriteLine("Spheroid WGS84");
                    writer.WriteLine("Units METERS");
                    writer.WriteLine("Zunits NO");
                    writer.WriteLine("Parameters");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入投影文件失败: {ex.Message}");
                return false;
            }
        }

        public bool WriteResultAscFileByParamsWithMinMax( string fileName,
                        float[,,] data,
                        HSFX_UNIT_Grid @params,
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
                    writer.WriteLine($"ncols {@params.ncols}");
                    writer.WriteLine($"nrows {@params.nrows}");
                    writer.WriteLine($"xllcorner {@params.xllcorner:F6}");
                    writer.WriteLine($"yllcorner {@params.yllcorner:F6}");
                    writer.WriteLine($"cellsize {@params.cellsize:F6}");
                    writer.WriteLine("NODATA_value -9999");

                    int outRow = int.Parse(@params.nrows);
                    int outCol = int.Parse(@params.ncols);

                    const int NODATA_VALUE = -9999;

                    // 行倒序写入（从最后一行开始）
                    for (int r = outRow - 1; r >= 0; r--)
                    {
                        StringBuilder line = new StringBuilder();

                        for (int c = 0; c < outCol; c++)
                        {
                            float curRain = data[r, c,0];

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

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入文件失败: {ex.Message}");
                return false;
            }
        }


        public bool CreateGif(String pngFloder, string outputGifPath, int delayMilliseconds)
        {
            // 检查输出目录是否存在，不存在则创建
            string directory = Path.GetDirectoryName(outputGifPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            List<string> imagePaths = new List<string>();
            //获取 pngFloder 目录下的所有png文件，并按名字升序排列
            if (Directory.Exists(pngFloder))
            {
                imagePaths = Directory.GetFiles(pngFloder, "*.png")
                                    .OrderBy(f => f)
                                    .ToList();
            }
            if(imagePaths.Count == 0)
            {
                return false;
            }

            // 加载所有PNG图像
            var frames = new List<Image>();
            foreach (var path in imagePaths)
            {
                frames.Add(Image.Load(path));
            }

            int repeatCount = 0;
            // 创建GIF（使用第一帧初始化）
            using var gif = new Image<Rgba32>(frames[0].Width, frames[0].Height);
            var gifMeta = gif.Metadata.GetGifMetadata();
            gifMeta.RepeatCount = (ushort)repeatCount;

            // 添加所有帧
            foreach (var frame in frames)
            {
                // 克隆帧并设置延迟
                var clonedFrame = frame.Clone(ctx => ctx.Resize(gif.Size)); // 确保尺寸一致
                var frameMeta = clonedFrame.Frames.RootFrame.Metadata.GetGifMetadata();
                frameMeta.FrameDelay = delayMilliseconds / 10; // 转换为GIF时间单位（1单位=10ms）
                //上边的单位是10ms，所以这里除以10

                // 添加到GIF
                gif.Frames.AddFrame(clonedFrame.Frames.RootFrame);
            }

            // 移除初始空白帧
            gif.Frames.RemoveFrame(0);

            // 保存GIF
            var encoder = new GifEncoder
            {
                ColorTableMode = GifColorTableMode.Global,
                Quantizer = new OctreeQuantizer() // 优化颜色
            };
            gif.Save(outputGifPath, encoder);

            Console.WriteLine("✅ GIF 创建完成！");
            return true;
        }
        public Rgba32 GenRGBColorByLegend(float value, float curMinValue, float curMaxValue)
        {
            //0
            //0-10，10-20，20-50，50-100，100-200，200-500，500-1000，1000-2000，2000-5000，5000-10000，10000以上
            //创建十个元素的list List<float>
            List<float> _cValues = new List<float>(10);
            _cValues.Add(0.0f); // 0
            _cValues.Add(10.0f); // 1
            _cValues.Add(20.0f); // 2
            _cValues.Add(50.0f); // 3
            _cValues.Add(100.0f); // 4
            _cValues.Add(200.0f); // 5
            _cValues.Add(500.0f); // 6
            _cValues.Add(1000.0f); // 7
            _cValues.Add(2000.0f); // 8
            _cValues.Add(5000.0f); // 9
            _cValues.Add(10000.0f); // 10


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

        public Rgba32 GenRGBColorByLegend_water(float value, float curMinValue, float curMaxValue)
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
            _cValues.Add(20.0f); // 5
            _cValues.Add(50.0f); // 6
            _cValues.Add(100.0f); // 7
            _cValues.Add(200.0f); // 8
            _cValues.Add(500.0f); // 9
            _cValues.Add(1000.0f); // 10


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

        public bool AscDemToColorPng(string inascfile, string outpngfile, ref float curMinValue, ref float curMaxValue)
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
                                Rgba32 color = GenRGBColorByLegend(value, curMinValue, curMaxValue);
                                if (outpngfile.Contains("-water_depth-"))
                                {
                                    color = GenRGBColorByLegend_water(value, curMinValue, curMaxValue);
                                }
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

        public bool MergeASCFilesToTifAndPng(String srcEPSG, String targetEPSG, List<string> inputFiles, String outputTifFile, String pngOutputFile, double[] outputBounds, ref float curMinValue, ref float curMaxValue)
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

        //执行合并
        //合并当前主机下所有模型目录下的结果
        //各个省份分目录存储
        public bool DoASCMergeGridPerProvinceLocal_usegdal()
        {
            //! 网格模型out中输出文件的英文名称
            List<String> gridResultFieldName = new List<string>();
            Dictionary<String, List<WaterDeep>> gridResultFieldURL = new Dictionary<string, List<WaterDeep>>();

            //为每个指标添加对应的url列表
            gridResultFieldName.Add("water_depth");
            gridResultFieldName.Add("discharge");
            //gridResultFieldName.push_back("velocity");
            //gridResultFieldName.push_back("precip");

            //key的名字 需要是省份名字+字段名
            //gridResultFieldURL.Add("water_depth", new List<WaterDeep>());
            //gridResultFieldURL.Add("discharge", new List<WaterDeep>());

            int NOData = -9999;

            //执行数据合并写出
            String curCCname = _fileNameWithoutExtension;

            DateTime begin_time = Convert.ToDateTime(_taifenginfoForcalc.Rows[0]["starttime"]);
            String startTimeCurDat = begin_time.ToString("yyyy-MM-dd HH:mm:ss");
            //获取 "yyyyMMddhh"
            String startTimeFromSrc = begin_time.ToString("yyyyMMddHH");

            //! 遍历合并指定场次的信息，需要遍历的目录个数，目录路径，输出目录
            int totalUnitsnum = _unitsinfo.Rows.Count;

            int totalTimeNum = int.Parse(_taifenginfoForcalc.Rows[0]["times"].ToString());
            double mfbl = double.Parse(_stpoinginfo.Rows[0][5].ToString());
            //遍历每个时间
            for (int t = 0; t < totalTimeNum; t = t + 1){
                //begin_time 增加t天
                DateTime curTime = begin_time.AddHours(t);
                String curFrameTime = curTime.ToString("yyyyMMddHHmm");
                for (int g = 0; g < gridResultFieldName.Count; ++g)
                {
                    //存储当前dat场次降雨最大最小值
                    DatInfoRecord diRecordmin = new DatInfoRecord();
                    diRecordmin.value = 99999;
                    DatInfoRecord diRecordmax = new DatInfoRecord();
                    diRecordmax.value = -99999;
                    Dictionary<String, DatInfoRecord> minmaxCurField = new Dictionary<string, DatInfoRecord>();
                    minmaxCurField.Add("min", diRecordmin);
                    minmaxCurField.Add("max", diRecordmax);

                    //遍历 srcSizeINFO
                    for (int p = 0; p < _srcSizeINFO.Rows.Count; p++)
                    {
                        bool isDataUpdate = false;

                        //对 时间 t索引 大于0小于10 则输出1位， 大于等于10且小于100输出2位 其他则输出3位
                        String indexNumber = t.ToString("D3");
                        if (totalTimeNum > 0 && totalTimeNum < 10)
                        {
                            indexNumber = t.ToString("D1");
                        }
                        else if (totalTimeNum >= 10 && totalTimeNum < 100)
                        {
                            indexNumber = t.ToString("D2");
                        }

                        //省名字
                        String proName = _srcSizeINFO.Rows[p]["province"].ToString();
                        mfbl = double.Parse(_srcSizeINFO.Rows[p]["cellsize"].ToString());

                        //! 当前省份的四至信息
                        DataRow[] dataRows = _extentInfo.Select($"province = '{proName}'");
                        if(dataRows.Length == 0)
                        {
                            Console.WriteLine($"没有找到省份 {proName} 的四至信息，跳过该省份的处理。");
                            continue;
                        }

                        //坐标系信息
                        if (!_srcEPSGINFO.ContainsKey(proName))
                        {
                            continue;
                        }

                        //提前确定好目标区域大小，将找到的单元，计算行列数后，放到这个数组中
                        double xllcorner = double.Parse(dataRows[0]["xllcorner"].ToString());
                        double yllcorner = double.Parse(dataRows[0]["yllcorner"].ToString());
                        int ncols = int.Parse(dataRows[0]["ncols"].ToString());
                        int nrows = int.Parse(dataRows[0]["nrows"].ToString());
                        double cellsize = double.Parse(dataRows[0]["cellsize"].ToString());

                        //double数组4个元素，存放四至信息
                        double[] outBounds = new double[4];
                        outBounds[0] = double.Parse(dataRows[0]["left"].ToString());
                        outBounds[1] = double.Parse(dataRows[0]["bottom"].ToString());
                        outBounds[2] = double.Parse(dataRows[0]["right"].ToString());
                        outBounds[3] = double.Parse(dataRows[0]["top"].ToString());

                        //每种指标对应的json索引文件名
                        String curJsonFilefullpath = Path.Combine(_iisRootDirectory, curCCname, proName, gridResultFieldName[g] + ".json");

                        //每种指标对应的当前省的 gif文件名
                        String curgifproFilefullpath = Path.Combine(_iisRootDirectory, curCCname, proName, gridResultFieldName[g] + ".gif");

                        //对齐输出文件索引
                        String txtFolder = Path.Combine(curCCname, proName, "output", "txt");
                        String pngFloder = Path.Combine(curCCname, proName, "output", "png", gridResultFieldName[g]);
                        String pngFloder4326 = Path.Combine(curCCname, proName, "output", "png4326", gridResultFieldName[g]);

                        String outFormatIndex = t.ToString("D3");
                        String curCCTimeOutdir = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.asc", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        String curCCTimeOutPngFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.png", _iisRootDirectory, pngFloder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        String curCCTimeOutPng4326Filename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.png", _iisRootDirectory, pngFloder4326, curCCname, gridResultFieldName[g], proName, outFormatIndex);

                        String curCCTimeOutMinmaxFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.minmax", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        String curCCOutProjfileName = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.prj", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        //遍历每个单元
                        int isstop = 0;
                        List<String> curCCTimeOutFileList = new List<string>();
                        for (int i = 0; i < totalUnitsnum; ++i)
                        {
                            //! 当前目录,跳过不是当前省份的数据，只合并当前省份
                            if (!proName.Equals(_unitsinfo.Rows[i]["province"].ToString()))
                            {
                                continue;
                            }
                            if (isstop == 1)
                            {
                                //break;
                            }
                            //QString curSearchDatFile = QString("%1/GRIDEXE/output/%2/%3/out/%4%5.txt").arg(unitsinfo[i].APPPath).arg(curCCname).arg(unitsinfo[i].UNITCD).arg(gridResultFieldName[g]).arg(indexNumber);
                            //改为netcre
                            String curSearchDatFile = String.Format("{0}/GRIDEXE/output/{1}/{2}/out/{3}{4}.txt", _unitsinfo.Rows[i]["APPPath"], curCCname, _unitsinfo.Rows[i]["UNITCD"], gridResultFieldName[g], indexNumber);
                            //判断curSearchDatFile 文件是否存在
                            if (!File.Exists(curSearchDatFile))
                            {
                                continue;
                            }
                            curCCTimeOutFileList.Add(curSearchDatFile);
                        }
                        if (curCCTimeOutFileList.Count > 0)
                        {
                            //提示信息，当前省份切片asc文件合并完成
                            Console.WriteLine($"当前省份 {proName} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} asc文件列表收集完成，开始转换写出");
                            float curMinvalue = 0.0f;
                            float curMaxValue = 0.0f;
                            String curoutReporjectTifFile = Path.Combine(Path.GetDirectoryName(curCCTimeOutdir), "4326", Path.GetFileNameWithoutExtension(curCCTimeOutdir) + ".tif");
                            String srcEPSG = String.Format("EPSG:{0}", _srcEPSGINFO[proName]);
                            bool status = MergeASCFilesToTifAndPng(srcEPSG, "", curCCTimeOutFileList, curoutReporjectTifFile, curCCTimeOutPng4326Filename, outBounds, ref curMinvalue, ref curMaxValue);
                            Console.WriteLine($"当前省份 {proName} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 写出合并后asc文件完成");
                            if (status)
                            {
                                {
                                    //curCCTimeOutPngFilename 中获取带扩展名的文件名
                                    String extStr = Path.GetFileName(curCCTimeOutPngFilename);

                                    //为对应的指标添加url
                                    WaterDeep wd = new WaterDeep();
                                    wd.url = String.Format("/output/png4326/{0}/{1}", gridResultFieldName[g], extStr);
                                    wd.time = curFrameTime;
                                    wd.area = 0; //淹没面积
                                    wd.isHavarecord = "1"; //有数据
                                    String proKeyWithField = $"{proName}-{gridResultFieldName[g]}"; //使用省份名和字段名作为key
                                    if (!gridResultFieldURL.ContainsKey(proKeyWithField))
                                    {
                                        gridResultFieldURL.Add(proKeyWithField, new List<WaterDeep>());
                                    }
                                    gridResultFieldURL[proKeyWithField].Add(wd);
                                    ReadWriteJSONFile.Write(gridResultFieldURL[proKeyWithField], curJsonFilefullpath);

                                    //Logger::Message(QStringLiteral("%1场次时间%2的字段%3在%4省份png写出成功").arg(curCCname).arg(indexNumber).arg(gridResultFieldName[g]).arg(proName));
                                    //写出提示信息
                                    Console.WriteLine($"场次 {curCCname} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 在 {proName} 省份的PNG写出成功");

                                    //如果是最后一个时间，则在当前省份目录下直接输出gif文件，合并png
                                    if (t == totalTimeNum - 1)
                                    {
                                        //合并当前省份下的所有png文件
                                        String pngFullFloder = Path.Combine(_iisRootDirectory, pngFloder4326);

                                        bool isGifOK = CreateGif(pngFullFloder, curgifproFilefullpath, 1000);
                                        if (isGifOK)
                                        {
                                            //输出gif成功
                                            Console.WriteLine($"场次 {curCCname} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 在 {proName} 省份的GIF写出成功");
                                        }
                                    }
                                }
                            }

                            //写出min max值
                            if (status)
                            {
                                //更新最值
                                if (curMinvalue < minmaxCurField["min"].value)
                                {
                                    minmaxCurField["min"].value = curMinvalue;
                                    minmaxCurField["min"].tm = outFormatIndex;
                                }
                                if (curMaxValue > minmaxCurField["max"].value)
                                {
                                    minmaxCurField["max"].value = curMaxValue;
                                    minmaxCurField["max"].tm = outFormatIndex;
                                }
                                //输出文件名
                                String fiName = curCCTimeOutMinmaxFilename;
                                //写出min maxvalue到finame中
                                using (StreamWriter writer = new StreamWriter(fiName))
                                {
                                    //stream << "min" << "," << minmaxCurField["min"].value << "," << "time" << "," << minmaxCurField["min"].tm << "\n";
                                    //stream << "max" << "," << minmaxCurField["max"].value << "," << "time" << "," << minmaxCurField["max"].tm << "\n";
                                    //写出上边两行
                                    writer.WriteLine($"min,{minmaxCurField["min"].value},time,{minmaxCurField["min"].tm}");
                                    writer.WriteLine($"max,{minmaxCurField["max"].value},time,{minmaxCurField["max"].tm}");
                                }
                            }
                        }
                    }
                }
                
            }

            return true;
        }

        //自己读取文本模式
        public bool DoASCMergeGridPerProvinceLocal()
        {
            //! 网格模型out中输出文件的英文名称
            List<String> gridResultFieldName = new List<string>();
            Dictionary<String, List<WaterDeep>> gridResultFieldURL = new Dictionary<string, List<WaterDeep>>();

            //为每个指标添加对应的url列表
            gridResultFieldName.Add("water_depth");
            gridResultFieldName.Add("discharge");
            //gridResultFieldName.push_back("velocity");
            //gridResultFieldName.push_back("precip");

            //key的名字 需要是省份名字+字段名
            //gridResultFieldURL.Add("water_depth", new List<WaterDeep>());
            //gridResultFieldURL.Add("discharge", new List<WaterDeep>());

            int NOData = -9999;

            //执行数据合并写出
            String curCCname = _fileNameWithoutExtension;

            DateTime begin_time = Convert.ToDateTime(_taifenginfoForcalc.Rows[0]["starttime"]);
            String startTimeCurDat = begin_time.ToString("yyyy-MM-dd HH:mm:ss");
            //获取 "yyyyMMddhh"
            String startTimeFromSrc = begin_time.ToString("yyyyMMddHH");

            //! 遍历合并指定场次的信息，需要遍历的目录个数，目录路径，输出目录
            int totalUnitsnum = _unitsinfo.Rows.Count;

            int totalTimeNum = int.Parse(_taifenginfoForcalc.Rows[0]["times"].ToString());
            double mfbl = double.Parse(_stpoinginfo.Rows[0][5].ToString());
            //遍历每个时间
            for (int t = 0; t < totalTimeNum; t = t + 1)
            {
                //begin_time 增加t天
                DateTime curTime = begin_time.AddHours(t);
                String curFrameTime = curTime.ToString("yyyyMMddHHmm");
                for (int g = 0; g < gridResultFieldName.Count; ++g)
                {
                    //存储当前dat场次降雨最大最小值
                    DatInfoRecord diRecordmin = new DatInfoRecord();
                    diRecordmin.value = 99999;
                    DatInfoRecord diRecordmax = new DatInfoRecord();
                    diRecordmax.value = -99999;
                    Dictionary<String, DatInfoRecord> minmaxCurField = new Dictionary<string, DatInfoRecord>();
                    minmaxCurField.Add("min", diRecordmin);
                    minmaxCurField.Add("max", diRecordmax);

                    //遍历 srcSizeINFO
                    for (int p = 0; p < _srcSizeINFO.Rows.Count; p++)
                    {
                        bool isDataUpdate = false;

                        //对 时间 t索引 大于0小于10 则输出1位， 大于等于10且小于100输出2位 其他则输出3位
                        String indexNumber = t.ToString("D3");
                        if (totalTimeNum > 0 && totalTimeNum < 10)
                        {
                            indexNumber = t.ToString("D1");
                        }
                        else if (totalTimeNum >= 10 && totalTimeNum < 100)
                        {
                            indexNumber = t.ToString("D2");
                        }

                        //省名字
                        String proName = _srcSizeINFO.Rows[p]["province"].ToString();
                        mfbl = double.Parse(_srcSizeINFO.Rows[p]["cellsize"].ToString());

                        //! 当前省份的四至信息
                        DataRow[] dataRows = _extentInfo.Select($"province = '{proName}'");
                        if (dataRows.Length == 0)
                        {
                            Console.WriteLine($"没有找到省份 {proName} 的四至信息，跳过该省份的处理。");
                            continue;
                        }
                        //提前确定好目标区域大小，将找到的单元，计算行列数后，放到这个数组中
                        double xllcorner = double.Parse(dataRows[0]["xllcorner"].ToString());
                        double yllcorner = double.Parse(dataRows[0]["yllcorner"].ToString());
                        int ncols = int.Parse(dataRows[0]["ncols"].ToString());
                        int nrows = int.Parse(dataRows[0]["nrows"].ToString());
                        double cellsize = double.Parse(dataRows[0]["cellsize"].ToString());

                        //double数组4个元素，存放四至信息
                        double[] outBounds = new double[4];
                        outBounds[0] = double.Parse(dataRows[0]["left"].ToString()); ;
                        outBounds[1] = double.Parse(dataRows[0]["bottom"].ToString()); ;
                        outBounds[2] = double.Parse(dataRows[0]["right"].ToString()); ;
                        outBounds[3] = double.Parse(dataRows[0]["top"].ToString()); ;

                        //每次读取asc数据后，膨胀这个变量
                        //修改为根据每个省份的四至信息来处理
                        DatFileStruct lastDt = new DatFileStruct();
                        lastDt.xllcorner = xllcorner;
                        lastDt.yllcorner = yllcorner;
                        lastDt.xmaxcorner = xllcorner + ncols * cellsize;
                        lastDt.ymaxcorner = yllcorner + nrows * cellsize;
                        lastDt.col = ncols;
                        lastDt.row = nrows;
                        lastDt.cellsize = cellsize;
                        lastDt.nodata = NOData;
                        lastDt.rain = new float[nrows, ncols, 1];
                        //初始值设置为 NOData
                        for (int r = 0; r < nrows; r++)
                        {
                            for (int c = 0; c < ncols; c++)
                            {
                                lastDt.rain[r, c, 0] = NOData;
                            }
                        }

                        //每种指标对应的json索引文件名
                        String curJsonFilefullpath = Path.Combine(_iisRootDirectory, curCCname, proName, gridResultFieldName[g] + ".json");

                        //每种指标对应的当前省的 gif文件名
                        String curgifproFilefullpath = Path.Combine(_iisRootDirectory, curCCname, proName, gridResultFieldName[g] + ".gif");

                        //对齐输出文件索引
                        String txtFolder = Path.Combine(curCCname, proName, "output", "txt");
                        String pngFloder = Path.Combine(curCCname, proName, "output", "png", gridResultFieldName[g]);
                        String pngFloder4326 = Path.Combine(curCCname, proName, "output", "png4326", gridResultFieldName[g]);

                        String outFormatIndex = t.ToString("D3");
                        String curCCTimeOutdir = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.asc", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        String curCCTimeOutPngFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.png", _iisRootDirectory, pngFloder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        String curCCTimeOutPng4326Filename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.png", _iisRootDirectory, pngFloder4326, curCCname, gridResultFieldName[g], proName, outFormatIndex);

                        String curCCTimeOutMinmaxFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.minmax", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        String curCCOutProjfileName = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.prj", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        //遍历每个单元
                        int isstop = 0;
                        for (int i = 0; i < totalUnitsnum; ++i)
                        {
                            //! 当前目录,跳过不是当前省份的数据，只合并当前省份
                            if (!proName.Equals(_unitsinfo.Rows[i]["province"].ToString()))
                            {
                                continue;
                            }
                            if (isstop == 1)
                            {
                                //break;
                            }
                            //QString curSearchDatFile = QString("%1/GRIDEXE/output/%2/%3/out/%4%5.txt").arg(unitsinfo[i].APPPath).arg(curCCname).arg(unitsinfo[i].UNITCD).arg(gridResultFieldName[g]).arg(indexNumber);
                            //改为netcre
                            String curSearchDatFile = String.Format("{0}/GRIDEXE/output/{1}/{2}/out/{3}{4}.txt", _unitsinfo.Rows[i]["APPPath"], curCCname, _unitsinfo.Rows[i]["UNITCD"], gridResultFieldName[g], indexNumber);

                            //输出目录
                            String outdir = _iisRootDirectory + Path.DirectorySeparatorChar + curCCname;
                            //判断目录不存在则创建
                            if (!Directory.Exists(outdir))
                            {
                                Directory.CreateDirectory(outdir);
                            }

                            //执行读写
                            List<datInfo> curDats = new List<datInfo>();

                            // 打开读取这个文件
                            DatFileStruct curDt = new DatFileStruct();
                            ReadGridDataFromAsc(curSearchDatFile, ref curDt);
                            isstop = 1;
                            //更新数据
                            if (curDt.rain == null)
                            {
                                continue;
                            }

                            isDataUpdate = true;



                            //！根据当前curDt，计算其在lastDt中的行列号
                            for (int dtR = 0; dtR < curDt.row; dtR++)
                            {
                                for (int dtC = 0; dtC < curDt.col; dtC++)
                                {
                                    double curLon = curDt.xllcorner + mfbl * (dtC);
                                    double curLat = curDt.yllcorner + mfbl * (dtR);

                                    int globalR = (int)Math.Floor((curLat - lastDt.yllcorner) * (1 / mfbl) + 1E-6);
                                    int globalC = (int)Math.Floor((curLon - lastDt.xllcorner) * (1 / mfbl) + 1E-6);

                                    if (globalR >= 0 && globalC >= 0 && globalR < lastDt.row && globalC < lastDt.col)
                                    {

                                        if (curDt.rain[(curDt.row - 1 - dtR), dtC, 0] != NOData)
                                        {
                                            lastDt.rain[globalR, globalC, 0] = curDt.rain[(curDt.row - 1 - dtR), dtC, 0];
                                        }

                                    }
                                }
                            }

                        }

                        //提示信息，当前省份所有单元切片asc文件读取完成，开始写出到文件并输出png图片
                        //提示信息，当前省份所有单元切片asc文件读取完成，开始写出到文件并输出png文件
                        Console.WriteLine($"当前省份 {proName} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 所有单元切片asc文件读取完成，开始写出到文件并输出png文件");

                        //写出到文件
                        HSFX_UNIT_Grid paramsgrid = new HSFX_UNIT_Grid();
                        paramsgrid.ncols = lastDt.col.ToString(CultureInfo.InvariantCulture);
                        paramsgrid.nrows = lastDt.row.ToString(CultureInfo.InvariantCulture);
                        paramsgrid.xllcorner = lastDt.xllcorner.ToString("f6", CultureInfo.InvariantCulture);
                        paramsgrid.yllcorner = lastDt.yllcorner.ToString("f6", CultureInfo.InvariantCulture);
                        paramsgrid.cellsize = mfbl.ToString("f6", CultureInfo.InvariantCulture);
                        if (isDataUpdate)
                        {
                            float curMinvalue = 0.0f;
                            float curMaxValue = 0.0f;
                            bool status = WriteResultAscFileByParamsWithMinMax(curCCTimeOutdir, lastDt.rain, paramsgrid, ref curMinvalue, ref curMaxValue);

                            //! 写出同名proj文件curCCOutProjfileName
                            int utmNumber = _srcEPSGINFO[proName];
                            String utmIndexStr = (utmNumber - 32600).ToString();
                            bool statusProj = WriteResultProjAscFileByParams(curCCOutProjfileName, utmIndexStr);
                            Console.WriteLine($"当前省份 {proName} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 写出合并后asc文件完成");
                            if (status)
                            {
                                String curoutReporjectTifFile = Path.Combine(Path.GetDirectoryName(curCCTimeOutdir), "4326", Path.GetFileNameWithoutExtension(curCCTimeOutdir) + ".tif");
                                List<String> listForMerge = new List<String>();
                                listForMerge.Add(curCCTimeOutdir);
                                String srcEPSG = String.Format("EPSG:{0}", _srcEPSGINFO[proName]);
                                bool isrpro4326 = MergeASCFilesToTifAndPng(srcEPSG, "",listForMerge, curoutReporjectTifFile, curCCTimeOutPng4326Filename, outBounds, ref curMinvalue, ref curMaxValue);
                                Console.WriteLine($"当前省份 {proName} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 写出合并后tif及png文件完成");
                                if (isrpro4326)
                                {
                                    //curCCTimeOutPngFilename 中获取带扩展名的文件名
                                    String extStr = Path.GetFileName(curCCTimeOutPngFilename);

                                    //为对应的指标添加url
                                    WaterDeep wd = new WaterDeep();
                                    wd.url = String.Format("/output/png4326/{0}/{1}", gridResultFieldName[g], extStr);
                                    wd.time = curFrameTime;
                                    wd.area = 0; //淹没面积
                                    wd.isHavarecord = "1"; //有数据
                                    String proKeyWithField = $"{proName}-{gridResultFieldName[g]}"; //使用省份名和字段名作为key
                                    if (!gridResultFieldURL.ContainsKey(proKeyWithField))
                                    {
                                        gridResultFieldURL.Add(proKeyWithField, new List<WaterDeep>());
                                    }
                                    gridResultFieldURL[proKeyWithField].Add(wd);
                                    ReadWriteJSONFile.Write(gridResultFieldURL[proKeyWithField], curJsonFilefullpath);

                                    //Logger::Message(QStringLiteral("%1场次时间%2的字段%3在%4省份png写出成功").arg(curCCname).arg(indexNumber).arg(gridResultFieldName[g]).arg(proName));
                                    //写出提示信息
                                    Console.WriteLine($"场次 {curCCname} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 在 {proName} 省份的PNG写出成功");

                                    //如果是最后一个时间，则在当前省份目录下直接输出gif文件，合并png
                                    if (t == totalTimeNum - 1)
                                    {
                                        //合并当前省份下的所有png文件
                                        String pngFullFloder = Path.Combine(_iisRootDirectory, pngFloder4326);

                                        bool isGifOK = CreateGif(pngFullFloder, curgifproFilefullpath, 1000);
                                        if (isGifOK)
                                        {
                                            //输出gif成功
                                            Console.WriteLine($"场次 {curCCname} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 在 {proName} 省份的GIF写出成功");
                                        }
                                    }
                                }
                            }

                            //写出min max值
                            if (status)
                            {
                                //更新最值
                                if (curMinvalue < minmaxCurField["min"].value)
                                {
                                    minmaxCurField["min"].value = curMinvalue;
                                    minmaxCurField["min"].tm = outFormatIndex;
                                }
                                if (curMaxValue > minmaxCurField["max"].value)
                                {
                                    minmaxCurField["max"].value = curMaxValue;
                                    minmaxCurField["max"].tm = outFormatIndex;
                                }
                                //输出文件名
                                String fiName = curCCTimeOutMinmaxFilename;
                                //写出min maxvalue到finame中
                                using (StreamWriter writer = new StreamWriter(fiName))
                                {
                                    //stream << "min" << "," << minmaxCurField["min"].value << "," << "time" << "," << minmaxCurField["min"].tm << "\n";
                                    //stream << "max" << "," << minmaxCurField["max"].value << "," << "time" << "," << minmaxCurField["max"].tm << "\n";
                                    //写出上边两行
                                    writer.WriteLine($"min,{minmaxCurField["min"].value},time,{minmaxCurField["min"].tm}");
                                    writer.WriteLine($"max,{minmaxCurField["max"].value},time,{minmaxCurField["max"].tm}");
                                }
                            }
                        }
                    }
                }

            }

            return true;
        }

        //合并各个省份到全国
        public bool DoASCMergeGridAllProvinceLocalToOne()
        {
            //! 网格模型out中输出文件的英文名称
            List<String> gridResultFieldName = new List<string>();
            Dictionary<String, List<WaterDeep>> gridResultFieldURL = new Dictionary<string, List<WaterDeep>>();

            //为每个指标添加对应的url列表
            gridResultFieldName.Add("water_depth");
            gridResultFieldName.Add("discharge");
            //gridResultFieldName.push_back("velocity");
            //gridResultFieldName.push_back("precip");

            gridResultFieldURL.Add("water_depth", new List<WaterDeep>());
            gridResultFieldURL.Add("discharge", new List<WaterDeep>());

            int NOData = -9999;

            //执行数据合并写出
            String curCCname = _fileNameWithoutExtension;

            DateTime begin_time = Convert.ToDateTime(_taifenginfoForcalc.Rows[0]["starttime"]);
            String startTimeCurDat = begin_time.ToString("yyyy-MM-dd HH:mm:ss");
            //获取 "yyyyMMddhh"
            String startTimeFromSrc = begin_time.ToString("yyyyMMddHH");

            int totalTimeNum = int.Parse(_taifenginfoForcalc.Rows[0]["times"].ToString());
            double mfbl = double.Parse(_stpoinginfo.Rows[0][5].ToString());

            //! 当前省份的四至信息
            DataRow[] dataRows = _extentInfo.Select($"province = 'china'");
            if (dataRows.Length == 0)
            {
                Console.WriteLine($"没有找到省份 china 的四至信息，跳过该省份的处理。");
                return false;
            }
            //提前确定好目标区域大小，将找到的单元，计算行列数后，放到这个数组中
            //double数组4个元素，存放四至信息
            double[] outBounds = new double[4];
            outBounds[0] = double.Parse(dataRows[0]["left"].ToString()); ;
            outBounds[1] = double.Parse(dataRows[0]["bottom"].ToString()); ;
            outBounds[2] = double.Parse(dataRows[0]["right"].ToString()); ;
            outBounds[3] = double.Parse(dataRows[0]["top"].ToString()); ;
            //遍历每个时间
            for (int t = 0; t < totalTimeNum; t = t + 1)
            {
                //begin_time 增加t天
                DateTime curTime = begin_time.AddHours(t);
                String curFrameTime = curTime.ToString("yyyyMMddHHmm");
                for (int g = 0; g < gridResultFieldName.Count; ++g)
                {
                    //存储当前dat场次降雨最大最小值
                    DatInfoRecord diRecordmin = new DatInfoRecord();
                    diRecordmin.value = 99999;
                    DatInfoRecord diRecordmax = new DatInfoRecord();
                    diRecordmax.value = -99999;
                    Dictionary<String, DatInfoRecord> minmaxCurField = new Dictionary<string, DatInfoRecord>();
                    minmaxCurField.Add("min", diRecordmin);
                    minmaxCurField.Add("max", diRecordmax);
                    //每种指标对应的json索引文件名
                    String curJsonFilefullpath = Path.Combine(_iisRootDirectory, curCCname, gridResultFieldName[g] + ".json");

                    //每种指标对应的当前省的 gif文件名
                    String curgifproFilefullpath = Path.Combine(_iisRootDirectory, curCCname, gridResultFieldName[g] + ".gif");

                    //对齐输出文件索引
                    String txtFolder = Path.Combine(curCCname, "output", "txt");
                    String pngFloder = Path.Combine(curCCname, "output", "png", gridResultFieldName[g]);
                    String pngFloder4326 = Path.Combine(curCCname, "output", "png4326", gridResultFieldName[g]);

                    String outFormatIndex = t.ToString("D3");
                    String curCCTimeOutdir = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.tif", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], "all", outFormatIndex);
                    String curCCTimeOutPngFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.png", _iisRootDirectory, pngFloder, curCCname, gridResultFieldName[g], "all", outFormatIndex);
                    String curCCTimeOutPng4326Filename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.png", _iisRootDirectory, pngFloder4326, curCCname, gridResultFieldName[g], "all", outFormatIndex);

                    String curCCTimeOutMinmaxFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.minmax", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], "all", outFormatIndex);
                    String curCCOutProjfileName = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.prj", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], "all", outFormatIndex);
                    //遍历 srcSizeINFO
                    //当前时间下的所有省的待合并文件列表
                    List<String> srcProAscFileList = new List<string>();
                    for (int p = 0; p < _srcSizeINFO.Rows.Count; p++)
                    {

                        //省名字
                        String proName = _srcSizeINFO.Rows[p]["province"].ToString();
                        mfbl = double.Parse(_srcSizeINFO.Rows[p]["cellsize"].ToString());

                        String txtFolderCurPro = Path.Combine(curCCname, proName, "output", "txt", "4326");
                        String curSearchDatFile = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.tif", _iisRootDirectory, txtFolderCurPro, curCCname, gridResultFieldName[g], proName, outFormatIndex);

                        //判断curSearchDatFile 文件是否存在
                        if (!File.Exists(curSearchDatFile))
                        {
                            continue;
                        }
                        srcProAscFileList.Add(curSearchDatFile);
                    }
                    if (srcProAscFileList.Count != 0)
                    {
                        float curMinvalue = 0.0f;
                        float curMaxValue = 0.0f;
                        bool status = MergeASCFilesToTifAndPng("", "", srcProAscFileList, curCCTimeOutdir, curCCTimeOutPng4326Filename, outBounds, ref curMinvalue, ref curMaxValue);
                        
                        if (status)
                        {
                            Console.WriteLine($"场次 {curCCname} 时间 {outFormatIndex} 的字段 {gridResultFieldName[g]} 在全国的PNG写出成功，共{totalTimeNum}个时间");
                            //再写出份4326的 
                            //curCCTimeOutPng4326Filename 中获取带扩展名的文件名
                            String extStr = Path.GetFileName(curCCTimeOutPng4326Filename);

                            //为对应的指标添加url
                            WaterDeep wd = new WaterDeep();
                            wd.url = String.Format("/output/png4326/{0}/{1}", gridResultFieldName[g], extStr);
                            wd.time = curFrameTime;
                            wd.area = 0; //淹没面积
                            wd.isHavarecord = "1"; //有数据
                            if (!gridResultFieldURL.ContainsKey(gridResultFieldName[g]))
                            {
                                gridResultFieldURL[gridResultFieldName[g]] = new List<WaterDeep>();
                            }
                            gridResultFieldURL[gridResultFieldName[g]].Add(wd);
                            ReadWriteJSONFile.Write(gridResultFieldURL[gridResultFieldName[g]], curJsonFilefullpath);

                            //Logger::Message(QStringLiteral("%1场次时间%2的字段%3在%4省份png写出成功").arg(curCCname).arg(indexNumber).arg(gridResultFieldName[g]).arg(proName));


                            //如果是最后一个时间，则在当前png目录下直接输出gif文件，合并png
                            if (t == totalTimeNum - 1)
                            {
                                //合并当前省份下的所有png文件
                                String pngFullFloder = Path.Combine(_iisRootDirectory, pngFloder4326);

                                bool isGifOK = CreateGif(pngFullFloder, curgifproFilefullpath, 1000);
                                if (isGifOK)
                                {
                                    //输出gif成功
                                    Console.WriteLine("全区域合并成功");
                                }
                            }

                        }

                        //写出min max值
                        if (status)
                        {
                            //更新最值
                            if (curMinvalue < minmaxCurField["min"].value)
                            {
                                minmaxCurField["min"].value = curMinvalue;
                                minmaxCurField["min"].tm = outFormatIndex;
                            }
                            if (curMaxValue > minmaxCurField["max"].value)
                            {
                                minmaxCurField["max"].value = curMaxValue;
                                minmaxCurField["max"].tm = outFormatIndex;
                            }
                            //输出文件名
                            String fiName = curCCTimeOutMinmaxFilename;
                            //写出min maxvalue到finame中
                            using (StreamWriter writer = new StreamWriter(fiName))
                            {
                                //stream << "min" << "," << minmaxCurField["min"].value << "," << "time" << "," << minmaxCurField["min"].tm << "\n";
                                //stream << "max" << "," << minmaxCurField["max"].value << "," << "time" << "," << minmaxCurField["max"].tm << "\n";
                                //写出上边两行
                                writer.WriteLine($"min,{minmaxCurField["min"].value},time,{minmaxCurField["min"].tm}");
                                writer.WriteLine($"max,{minmaxCurField["max"].value},time,{minmaxCurField["max"].tm}");
                            }
                        }
                    }
                }

            }

            return true;
        }
    }
}

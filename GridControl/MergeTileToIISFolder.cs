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

namespace GridControl
{
    

    public class HSFX_UNIT_Grid
    {
	    public String UNITCD;
        public String UNITNM;
        public String FCD;
        public String OCD;
        public String BSCD;
        public String ONDCD;
        public String Remark;
        public String NDX;
        public String NDY;
        public String GroupID;
        public String GroovyName;
        public String left;
        public String bottom;
        public String top;
        public String right;
        public String ncols;
        public String nrows;
        public String xllcorner;
        public String yllcorner;
        public String cellsize;
        public String dfbl;
        public String apppath;
        public bool isValid;

    };

    class datInfo
    {
        public float lon;
        public float lat;
        public float depth;
        public float liusu;
    };

    class DatInfoRecord
    {
        public DatInfoRecord()
        {
            tm = "2020-01-01";
            value = 0.0f;
        }

        public String tm;
        public float value;
    };

    internal class MergeTileToIISFolder
    {
        //成员变量
        private string _fileNameWithoutExtension;
        private String _iisRootDirectory;
        DataTable _stpoinginfo;
        Dictionary<String, int> _srcEPSGINFO;
        DataTable _srcSizeINFO;
        int _provinceNum;
        DataTable _unitsinfo;
        DataTable _taifenginfoForcalc;

        //构造函数
        public MergeTileToIISFolder(String fileNameWithoutExtension, DataTable taifenginfoForcalc)
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

        Rgba32 GenRGBColor(float value, float curMinValue, float curMaxValue)
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
        public bool AscDemToColorPng(string inascfile, string outpngfile, float curMinValue, float curMaxValue)
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
                DatFileStruct curDt = new DatFileStruct();
                ReadGridDataFromAsc(inascfile, ref curDt);

                // 创建 ImageSharp 图像
                using (Image<Rgba32> image = new Image<Rgba32>(curDt.col, curDt.row))
                {
                    for (int y = 0; y < curDt.row; y++)
                    {
                        for (int x = 0; x < curDt.col; x++)
                        {
                            float value = curDt.rain[y, x, 0];
                            if (value == curDt.nodata)
                            {
                                image[x, y] = new Rgba32(0, 0, 0, 0); // 设置为完全透明
                            }
                            else
                            {
                                Rgba32 color = GenRGBColor(value, curMinValue, curMaxValue);
                                image[x, y] = color; // 根据值映射到灰度
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


        //执行合并
        //合并当前主机下所有模型目录下的结果
        //各个省份分目录存储
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

            gridResultFieldURL.Add("water_depth", new List<WaterDeep>());
            gridResultFieldURL.Add("discharge", new List<WaterDeep>());

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
                        //每次读取asc数据后，膨胀这个变量
                        DatFileStruct lastDt = new DatFileStruct();

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

                        //每种指标对应的json索引文件名
                        String curJsonFilefullpath = Path.Combine(_iisRootDirectory, curCCname, proName, gridResultFieldName[g] + ".json");

                        //每种指标对应的当前省的 gif文件名
                        String curgifproFilefullpath = Path.Combine(_iisRootDirectory, curCCname, proName, gridResultFieldName[g] + ".gif");

                        //对齐输出文件索引
                        String txtFolder = Path.Combine(curCCname, proName, "output", "txt");
                        String pngFloder = Path.Combine(curCCname, proName, "output", "png", gridResultFieldName[g]);
                        String outFormatIndex = t.ToString("D3");
                        String curCCTimeOutdir = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.asc", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
                        String curCCTimeOutPngFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.png", _iisRootDirectory, pngFloder, curCCname, gridResultFieldName[g], proName, outFormatIndex);
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
                            if(isstop == 1)
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
                            List<datInfo > curDats = new List<datInfo>();

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

                            //！根据当前curDt和 更新已有的expand
                            DatFileStruct expandDt = new DatFileStruct();
                            if (lastDt.rain == null)
                            {
                                expandDt.xllcorner = curDt.xllcorner;
                                expandDt.yllcorner = curDt.yllcorner;
                                expandDt.xmaxcorner = curDt.xmaxcorner;
                                expandDt.ymaxcorner = curDt.ymaxcorner;
                            }
                            else
                            {
                                expandDt.xllcorner = (curDt.xllcorner <= lastDt.xllcorner) ? curDt.xllcorner : lastDt.xllcorner;
                                expandDt.yllcorner = (curDt.yllcorner <= lastDt.yllcorner) ? curDt.yllcorner : lastDt.yllcorner;
                                expandDt.xmaxcorner = (curDt.xmaxcorner > lastDt.xmaxcorner) ? curDt.xmaxcorner : lastDt.xmaxcorner;
                                expandDt.ymaxcorner = (curDt.ymaxcorner > lastDt.ymaxcorner) ? curDt.ymaxcorner : lastDt.ymaxcorner;
                            }

                            //！扩展后的行列数
                            expandDt.col = (int)Math.Floor((expandDt.xmaxcorner - expandDt.xllcorner) / mfbl + 1E-6);
                            expandDt.row = (int)Math.Floor((expandDt.ymaxcorner - expandDt.yllcorner) / mfbl + 1E-6);

                            // 创建一个新的expand后的data数组，行列初始为NOData，循环找到对应文件更新data中的值，写出
                            float[,,] data = new float[expandDt.row, expandDt.col, 1];
                            //！初始化所有值为-9999
                            for (int r = 0; r < expandDt.row; r++)
                            {
                                for (int c = 0; c < expandDt.col; c++)
                                {
                                    data[r, c, 0] = NOData;
                                }
                            }

                            //！1、上次原有的数据放入到expand中
                            for (int lastR = 0; lastR < lastDt.row; lastR++)
                            {
                                for (int lastC = 0; lastC < lastDt.col; lastC++)
                                {
                                    double curLon = lastDt.xllcorner + mfbl * (lastC);
                                    double curLat = lastDt.yllcorner + mfbl * (lastR);

                                    int globalR = (int)Math.Floor((curLat - expandDt.yllcorner) * (1 / mfbl) + 1E-6);
                                    int globalC = (int)Math.Floor((curLon - expandDt.xllcorner) * (1 / mfbl) + 1E-6);

                                    if (globalR >= 0 && globalC >= 0 && globalR < expandDt.row && globalC < expandDt.col)
                                    {

                                        if (lastDt.rain[lastR, lastC, 0] != NOData)
                                        {
                                            data[globalR, globalC, 0] = lastDt.rain[lastR, lastC, 0];
                                        }
                                        else
                                        {
                                            int gg = 9;
                                        }

                                    }
                                }
                            }

                            //！2、新解析的文件写入到expand中
                            //！计算行列号，以及左下角起点，并将新的和原有的，放到expand后的
                            //! expandDt是新的，包含lastDt 和 curDt
                            //！扩展后的行列数
                            for (int dtR = 0; dtR < curDt.row; dtR++)
                            {
                                for (int dtC = 0; dtC < curDt.col; dtC++)
                                {
                                    double curLon = curDt.xllcorner + mfbl * (dtC);
                                    double curLat = curDt.yllcorner + mfbl * (dtR);

                                    int globalR = (int)Math.Floor((curLat - expandDt.yllcorner) * (1 / mfbl) + 1E-6);
                                    int globalC = (int)Math.Floor((curLon - expandDt.xllcorner) * (1 / mfbl) + 1E-6);

                                    if (globalR >= 0 && globalC >= 0 && globalR < expandDt.row && globalC < expandDt.col)
                                    {

                                        if (curDt.rain[(curDt.row - 1 - dtR), dtC, 0] != NOData)
                                        {
                                            data[globalR, globalC, 0] = curDt.rain[(curDt.row - 1 - dtR), dtC, 0];
                                        }

                                    }
                                }
                            }

                            //释放lastDt.rain
                            if (lastDt.rain != null)
                            {
                                lastDt.rain = null;
                            }

                            //更新lastDt 用expandDt
                            lastDt.xllcorner = expandDt.xllcorner;
                            lastDt.yllcorner = expandDt.yllcorner;
                            lastDt.xmaxcorner = expandDt.xmaxcorner;
                            lastDt.ymaxcorner = expandDt.ymaxcorner;
                            lastDt.col = expandDt.col;
                            lastDt.row = expandDt.row;
                            lastDt.cellsize = expandDt.cellsize;
                            lastDt.nodata = expandDt.nodata;
                            lastDt.rain = data;


                        }

                        //写出到文件
                        //                  HSFX_UNIT_Grid params;
                        //params.ncols = QString::number(lastDt.col);
                        //params.nrows = QString::number(lastDt.row);
                        //params.xllcorner = QString::number(lastDt.xllcorner, 'f', 6);
                        //params.yllcorner = QString::number(lastDt.yllcorner, 'f', 6);
                        //params.cellsize = QString::number(mfbl, 'f', 6);
                        //以上写为netcore
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
                            if (status)
                            {
                                bool isPngOK = AscDemToColorPng(curCCTimeOutdir, curCCTimeOutPngFilename, curMinvalue, curMaxValue);
                                if (isPngOK)
                                {
                                    //curCCTimeOutPngFilename 中获取带扩展名的文件名
                                    String extStr = Path.GetFileName(curCCTimeOutPngFilename);

                                    //为对应的指标添加url
                                    WaterDeep wd = new WaterDeep();
                                    wd.url = String.Format("/output/png/{0}/{1}", gridResultFieldName[g], extStr);
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
                                    //写出提示信息
                                    Console.WriteLine($"场次 {curCCname} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 在 {proName} 省份的PNG写出成功");

                                    //如果是最后一个时间，则在当前省份目录下直接输出gif文件，合并png
                                    if (t == totalTimeNum - 1)
                                    {
                                        //合并当前省份下的所有png文件
                                        String pngFullFloder = Path.Combine(_iisRootDirectory, pngFloder);
                                        
                                        bool isGifOK = CreateGif(pngFullFloder, curgifproFilefullpath, 1000);
                                        if (isGifOK)
                                        {
                                            //输出gif成功
                                            Console.WriteLine($"场次 {curCCname} 时间 {indexNumber} 的字段 {gridResultFieldName[g]} 在 {proName} 省份的GIF写出成功");
                                        }
                                    }
                                }
                            }
                            //! 写出同名proj文件curCCOutProjfileName
                            int utmNumber = _srcEPSGINFO[proName];
                            String utmIndexStr = (utmNumber - 32600).ToString();
                            bool statusProj = WriteResultProjAscFileByParams(curCCOutProjfileName, utmIndexStr);
                            if (status)
                            {
                                //Logger::Message(QStringLiteral("%1场次时间%2的字段%3在%4省份写出成功").arg(curCCname).arg(indexNumber).arg(gridResultFieldName[g]).arg(proName));
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

                    //每次读取asc数据后，膨胀这个变量
                    DatFileStruct lastDt = new DatFileStruct();

                    bool isDataUpdate = false;

                    //每种指标对应的json索引文件名
                    String curJsonFilefullpath = Path.Combine(_iisRootDirectory, curCCname, gridResultFieldName[g] + ".json");

                    //每种指标对应的当前省的 gif文件名
                    String curgifproFilefullpath = Path.Combine(_iisRootDirectory, curCCname, gridResultFieldName[g] + ".gif");

                    //对齐输出文件索引
                    String txtFolder = Path.Combine(curCCname, "output", "txt");
                    String pngFloder = Path.Combine(curCCname, "output", "png", gridResultFieldName[g]);
                    String outFormatIndex = t.ToString("D3");
                    String curCCTimeOutdir = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.asc", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], "all", outFormatIndex);
                    String curCCTimeOutPngFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.png", _iisRootDirectory, pngFloder, curCCname, gridResultFieldName[g], "all", outFormatIndex);
                    String curCCTimeOutMinmaxFilename = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.minmax", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], "all", outFormatIndex);
                    String curCCOutProjfileName = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.prj", _iisRootDirectory, txtFolder, curCCname, gridResultFieldName[g], "all", outFormatIndex);
                    //遍历 srcSizeINFO
                    for (int p = 0; p < _srcSizeINFO.Rows.Count; p++)
                    {

                        //省名字
                        String proName = _srcSizeINFO.Rows[p]["province"].ToString();
                        mfbl = double.Parse(_srcSizeINFO.Rows[p]["cellsize"].ToString());

                        String txtFolderCurPro = Path.Combine(curCCname, proName, "output", "txt");
                        String curSearchDatFile = String.Format("{0}/{1}/{2}-{3}-{4}-{5}.asc", _iisRootDirectory, txtFolderCurPro, curCCname, gridResultFieldName[g], proName, outFormatIndex);

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
                        //更新数据
                        if (curDt.rain == null)
                        {
                            continue;
                        }

                        isDataUpdate = true;

                        //！根据当前curDt和 更新已有的expand
                        DatFileStruct expandDt = new DatFileStruct();
                        if (lastDt.rain == null)
                        {
                            expandDt.xllcorner = curDt.xllcorner;
                            expandDt.yllcorner = curDt.yllcorner;
                            expandDt.xmaxcorner = curDt.xmaxcorner;
                            expandDt.ymaxcorner = curDt.ymaxcorner;
                        }
                        else
                        {
                            expandDt.xllcorner = (curDt.xllcorner <= lastDt.xllcorner) ? curDt.xllcorner : lastDt.xllcorner;
                            expandDt.yllcorner = (curDt.yllcorner <= lastDt.yllcorner) ? curDt.yllcorner : lastDt.yllcorner;
                            expandDt.xmaxcorner = (curDt.xmaxcorner > lastDt.xmaxcorner) ? curDt.xmaxcorner : lastDt.xmaxcorner;
                            expandDt.ymaxcorner = (curDt.ymaxcorner > lastDt.ymaxcorner) ? curDt.ymaxcorner : lastDt.ymaxcorner;
                        }

                        //！扩展后的行列数
                        expandDt.col = (int)Math.Floor((expandDt.xmaxcorner - expandDt.xllcorner) / mfbl + 1E-6);
                        expandDt.row = (int)Math.Floor((expandDt.ymaxcorner - expandDt.yllcorner) / mfbl + 1E-6);

                        // 创建一个新的expand后的data数组，行列初始为NOData，循环找到对应文件更新data中的值，写出
                        float[,,] data = new float[expandDt.row, expandDt.col, 1];
                        //！初始化所有值为-9999
                        for (int r = 0; r < expandDt.row; r++)
                        {
                            for (int c = 0; c < expandDt.col; c++)
                            {
                                data[r, c, 0] = NOData;
                            }
                        }

                        //！1、上次原有的数据放入到expand中
                        for (int lastR = 0; lastR < lastDt.row; lastR++)
                        {
                            for (int lastC = 0; lastC < lastDt.col; lastC++)
                            {
                                double curLon = lastDt.xllcorner + mfbl * (lastC);
                                double curLat = lastDt.yllcorner + mfbl * (lastR);

                                int globalR = (int)Math.Floor((curLat - expandDt.yllcorner) * (1 / mfbl) + 1E-6);
                                int globalC = (int)Math.Floor((curLon - expandDt.xllcorner) * (1 / mfbl) + 1E-6);

                                if (globalR >= 0 && globalC >= 0 && globalR < expandDt.row && globalC < expandDt.col)
                                {

                                    if (lastDt.rain[lastR, lastC, 0] != NOData)
                                    {
                                        data[globalR, globalC, 0] = lastDt.rain[lastR, lastC, 0];
                                    }
                                    else
                                    {
                                        int gg = 9;
                                    }

                                }
                            }
                        }

                        //！2、新解析的文件写入到expand中
                        //！计算行列号，以及左下角起点，并将新的和原有的，放到expand后的
                        //! expandDt是新的，包含lastDt 和 curDt
                        //！扩展后的行列数
                        for (int dtR = 0; dtR < curDt.row; dtR++)
                        {
                            for (int dtC = 0; dtC < curDt.col; dtC++)
                            {
                                double curLon = curDt.xllcorner + mfbl * (dtC);
                                double curLat = curDt.yllcorner + mfbl * (dtR);

                                int globalR = (int)Math.Floor((curLat - expandDt.yllcorner) * (1 / mfbl) + 1E-6);
                                int globalC = (int)Math.Floor((curLon - expandDt.xllcorner) * (1 / mfbl) + 1E-6);

                                if (globalR >= 0 && globalC >= 0 && globalR < expandDt.row && globalC < expandDt.col)
                                {

                                    if (curDt.rain[(curDt.row - 1 - dtR), dtC, 0] != NOData)
                                    {
                                        data[globalR, globalC, 0] = curDt.rain[(curDt.row - 1 - dtR), dtC, 0];
                                    }

                                }
                            }
                        }

                        //释放lastDt.rain
                        if (lastDt.rain != null)
                        {
                            lastDt.rain = null;
                        }

                        //更新lastDt 用expandDt
                        lastDt.xllcorner = expandDt.xllcorner;
                        lastDt.yllcorner = expandDt.yllcorner;
                        lastDt.xmaxcorner = expandDt.xmaxcorner;
                        lastDt.ymaxcorner = expandDt.ymaxcorner;
                        lastDt.col = expandDt.col;
                        lastDt.row = expandDt.row;
                        lastDt.cellsize = expandDt.cellsize;
                        lastDt.nodata = expandDt.nodata;
                        lastDt.rain = data; 
                    }

                    //写出到文件
                    //                  HSFX_UNIT_Grid params;
                    //params.ncols = QString::number(lastDt.col);
                    //params.nrows = QString::number(lastDt.row);
                    //params.xllcorner = QString::number(lastDt.xllcorner, 'f', 6);
                    //params.yllcorner = QString::number(lastDt.yllcorner, 'f', 6);
                    //params.cellsize = QString::number(mfbl, 'f', 6);
                    //以上写为netcore
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
                        if (status)
                        {
                            bool isPngOK = AscDemToColorPng(curCCTimeOutdir, curCCTimeOutPngFilename, curMinvalue, curMaxValue);
                            if (isPngOK)
                            {
                                //curCCTimeOutPngFilename 中获取带扩展名的文件名
                                String extStr = Path.GetFileName(curCCTimeOutPngFilename);

                                //为对应的指标添加url
                                WaterDeep wd = new WaterDeep();
                                wd.url = String.Format("/output/png/{0}/{1}", gridResultFieldName[g], extStr);
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
                                    String pngFullFloder = Path.Combine(_iisRootDirectory, pngFloder);

                                    bool isGifOK = CreateGif(pngFullFloder, curgifproFilefullpath, 1000);
                                    if (isGifOK)
                                    {
                                        //输出gif成功
                                        Console.WriteLine("全区域合并成功");
                                    }
                                }
                            }
                        }
                        //! 写出同名proj文件curCCOutProjfileName
                        int utmNumber = _srcEPSGINFO[_srcSizeINFO.Rows[0]["province"].ToString()];
                        String utmIndexStr = (utmNumber - 32600).ToString();
                        bool statusProj = WriteResultProjAscFileByParams(curCCOutProjfileName, utmIndexStr);
                        if (status)
                        {
                            //Logger::Message(QStringLiteral("%1场次时间%2的字段%3在%4省份写出成功").arg(curCCname).arg(indexNumber).arg(gridResultFieldName[g]).arg(proName));
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

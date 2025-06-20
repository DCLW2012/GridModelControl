using System.Collections.Generic;
using System.Data;
using System.IO;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OSR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;


namespace GdalAscMerger
{
    class Program
    {

        static public bool ReSampleToDestAndReprojectToChinaUTM49(string srcPath, string dstPath, String targetSrs,
                                 double minX, double minY, double maxX, double maxY, double resolution)
        {
            
            // 初始化GDAL
            GdalBase.ConfigureAll();
            Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            Gdal.AllRegister();

            // 打开源数据
            Dataset srcDs = Gdal.Open(srcPath, Access.GA_ReadOnly);

            // 设置目标投影（以 WGS84 为例，EPSG:4326）
            string dstSrsWkt;
            SpatialReference dstSrs = new SpatialReference("");
            dstSrs.ImportFromEPSG(int.Parse(targetSrs.Split(":")[1]));
            dstSrs.ExportToWkt(out dstSrsWkt, null);

            // 重投影
            // 假设已初始化GDAL并打开srcDs，已设置dstSrsWkt
            double[] outputBounds = { minX, minY, maxX, maxY }; // 指定输出范围

            string[] warpOptions = new string[]
            {
                $"-t_srs", dstSrsWkt,
                "-r", "near", // 最近邻插值
                "-of", "GTiff",
                "-te", outputBounds[0].ToString(), outputBounds[1].ToString(), outputBounds[2].ToString(), outputBounds[3].ToString() // 范围
            };

            Driver drv = Gdal.GetDriverByName("AAIGrid");
            if (drv == null)
            {
                Console.WriteLine("无法找到AAIGrid驱动");
                return false;
            }
            // 调用Gdal.Warp
            Dataset dstDs = Gdal.Warp(dstPath, new Dataset[] { srcDs }, new GDALWarpAppOptions(warpOptions), null, "");

            // 释放资源
            srcDs.Dispose();
            dstDs.Dispose();
            drv.Dispose();
            return true;
        }

        static Rgba32 GenRGBColorByLegend(float value, float curMinValue, float curMaxValue)
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

        static public bool AscDemToColorPng(string inascfile, string outpngfile, float curMinValue, float curMaxValue)
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
                    Console.WriteLine($"数据类型: {dataType}");

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
                            if (hasNoData>0 && temp)
                            {
                                image[col, row] = new Rgba32(0, 0, 0, 0); // 设置为完全透明
                            }
                            else
                            {
                                Rgba32 color = GenRGBColorByLegend(value, curMinValue, curMaxValue);
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

        static void Main(string[] args)
        {
            // 输入文件列表（替换为实际路径）
            var inputFiles = new List<string>
            {
                @"D://HSFXHeNanGRID//HSFXGrid3//GRIDEXE//output//2025061100-10-r4000-c4000-d1//WCF10_henan_1//out//water_depth00.txt"
            };

            // 初始化GDAL
            GdalBase.ConfigureAll();
            Gdal.AllRegister();

            // 输出文件
            string outputascFile = @"D:\output\china_merged.tif";

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
                        "-s_srs", "EPSG:32649", // 源坐标系（假设输入文件是WGS84坐标系）
                        "-t_srs", dstSrsWkt,
                        "-tr", $"{targetResolution}", $"{targetResolution}",
                        "-r", "bilinear",       // 重采样方法：双线性插值
                        "-of", "GTiff",         // 输出格式
                        "-overwrite"
                    };

                    // 执行重投影和重采样
                    Dataset tempsr = Gdal.Warp(tempFile, new Dataset[] { srcDs }, new GDALWarpAppOptions(warpOptions), null, "");
                    tempsr.Dispose();
                }
                Console.WriteLine($"Processed: {file}");
            }

            // 步骤2: 合并所有预处理后的文件
            var vrtFile = Path.Combine(tempDir, $"{Guid.NewGuid()}.vrt");
            string tempoutmergeFile = Path.Combine(tempDir, $"{Guid.NewGuid()}.tif");
            using (var vrtDs = Gdal.BuildVRT(vrtFile, processedFiles.ToArray(), null, null, ""))
            {
                // 转换为最终输出文件
                //Dataset temp = Gdal.wrapper_GDALTranslate(tempoutmergeFile, vrtDs, new GDALTranslateOptions(new string[] { "-of", "GTiff" }), null, "");
                //temp.Dispose();
                // 假设已初始化GDAL并打开srcDs，已设置dstSrsWkt
                double[] outputBounds = { 89.705, 17.339, 138.997, 55.238 }; // 指定输出范围

                string[] warpOptions = new string[]
                {
                $"-t_srs", dstSrsWkt,
                "-r", "near", // 最近邻插值
                "-of", "GTiff",
                "-te", outputBounds[0].ToString(), outputBounds[1].ToString(), outputBounds[2].ToString(), outputBounds[3].ToString() // 范围
                };
                // 调用Gdal.Warp
                Dataset dstDs = Gdal.Warp(tempoutmergeFile, new Dataset[] { vrtDs }, new GDALWarpAppOptions(warpOptions), null, "");
                dstDs.Dispose();
            }
            Console.WriteLine($"Merged output: {tempoutmergeFile}");

            // 清理临时文件
            foreach (var file in processedFiles) File.Delete(file);
            File.Delete(vrtFile);

            // 方法1: warp重采样
            //ReSampleToDestAndReprojectToChinaUTM49(tempoutmergeFile, outputascFile, "EPSG:32649", -908745.868177, 2007145.71554, 2408568.131823, 6121255.71554, 1001);
            //ReSampleToDestAndReprojectToChinaUTM49(tempoutmergeFile, outputascFile, "EPSG:4326", 89.705, 17.339, 138.997, 55.238, 0.01);
            //清理tempoutmergeFile
            if (File.Exists(tempoutmergeFile))
            {
                try
                {
                    File.Delete(tempoutmergeFile);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"清理临时文件失败: {ex.Message}");
                }
            }

            //tempoutmergeFile tif文件写出为png文件
            if (File.Exists(outputascFile))
            {
                string pngOutputFile = Path.ChangeExtension(outputascFile, ".png");
                AscDemToColorPng(outputascFile, pngOutputFile, 0, 1000);
            }
            else
            {
                Console.WriteLine("输出文件不存在，请检查路径和文件名。");
            }
        }
    }
}
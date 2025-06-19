using System.Collections.Generic;
using System.Data;
using System.IO;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace GdalAscMerger
{
    class Program
    {

        static public bool ReSampleToDestAndReprojectToChinaUTM49(string srcPath, string dstPath, String sourceSrs, String targetSrs)
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
            double chinaMinX = 73.0;  // 最西经度
            double chinaMaxX = 135.0; // 最东经度
            double chinaMinY = 18.0;  // 最南纬度
            double chinaMaxY = 54.0;  // 最北纬度
            double xRes = 1001; // 目标分辨率（单位：投影坐标系单位）
            double yRes = 1001;
            double[] outputBounds = { -908745.868177, 2007145.71554, 2408568.131823, 6121255.71554 }; // 指定输出范围

            string[] warpOptions = new string[]
            {
                $"-t_srs", dstSrsWkt,
                "-r", "near", // 最近邻插值
                "-of", "AAIGrid",
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
        // 方法1: 使用VRT中间文件（推荐）
        static void MergeUsingVRT(String tempoutmergeFile, string outputFile,
                                 double minX, double minY, double maxX, double maxY, double resolution)
        {
            try
            {
                // 使用gdal打开tif文件
                using (var dataset = Gdal.Open(tempoutmergeFile, Access.GA_ReadOnly))
                { 
                    if (dataset == null)
                    {
                        throw new Exception($"无法打开输入文件: {tempoutmergeFile}");
                    }

                    // 1. 获取输入数据的实际范围和坐标系
                    double[] geoTransform = new double[6];
                    dataset.GetGeoTransform(geoTransform);
                    string inputSrsWkt = dataset.GetProjection();

                    Console.WriteLine($"输入数据范围:");
                    Console.WriteLine($"  左上: ({geoTransform[0]}, {geoTransform[3]})");
                    Console.WriteLine($"  右下: ({geoTransform[0] + geoTransform[1] * dataset.RasterXSize}, " +
                                      $"{geoTransform[3] + geoTransform[5] * dataset.RasterYSize})");
                    Console.WriteLine($"输入坐标系: {inputSrsWkt}");

                    // 设置Warp选项
                    string dstSrsWkt;
                    SpatialReference dstSrs = new SpatialReference("");
                    dstSrs.ImportFromEPSG(4326);
                    dstSrs.ExportToWkt(out dstSrsWkt, null);
                    var warpOptions = new GDALWarpAppOptions(new string[] {
                        "-te", $"{minX}", $"{minY}", $"{maxX}", $"{maxY}", // 目标范围
                        "-tr", $"{resolution}", $"{resolution}",          // 分辨率
                        "-t_srs", dstSrsWkt,                           // 目标坐标系
                        "-r", "bilinear",                                // 重采样方法
                        "-dstnodata", "-9999",                           // NoData值
                        "-overwrite",
                        "-of", "GTiff",
                        "-co", "COMPRESS=LZW",
                        "-co", "TILED=YES"
                    });

                    
                    // 执行Warp操作
                    using (var outputDs = Gdal.Warp(outputFile, new Dataset[] { dataset }, warpOptions, null, ""))
                    {
                        if (outputDs == null)
                        {
                            throw new Exception($"合并失败: {Gdal.GetLastErrorMsg()}");
                        }
                        Console.WriteLine($"成功合并到全国底图! 输出文件: {outputFile}");
                        //释放资源
                        outputDs.Dispose();
                        dataset.Dispose();
                        dstSrs.Dispose();
                    }
                }
                
                
            }
            finally
            {
                
            }
        }


        static void Main(string[] args)
        {
            // 输入文件列表（替换为实际路径）
            var inputFiles = new List<string>
            {
                @"D:\\output\\2025061100-10-r4000-c4000-d1\\henan\\output\\txt\\2025061100-10-r4000-c4000-d1-discharge-henan-000.asc",
                @"D:\\output\\2025061100-10-r4000-c4000-d1\\shandong\\output\\txt\\2025061100-10-r4000-c4000-d1-discharge-shandong-000.asc"
            };

            // 初始化GDAL
            GdalBase.ConfigureAll();
            Gdal.AllRegister();

            // 输出文件
            string outputFile = @"D:\output\china_merged.tif";
            string outputascFile = @"D:\output\china_merged.asc";

            // 全国范围定义 (中国大致地理范围)
            double chinaMinX = 73.0;  // 最西经度
            double chinaMaxX = 135.0; // 最东经度
            double chinaMinY = 18.0;  // 最南纬度
            double chinaMaxY = 54.0;  // 最北纬度

            // 全国底图分辨率 (0.01度 ≈ 1公里)
            double nationalRes = 0.01;

            // 目标坐标系 (EPSG:4326 - WGS84)
            SpatialReference targetSrs = new SpatialReference("");
            targetSrs.ImportFromEPSG(32649);
            string dstSrsWkt;
            targetSrs.ExportToWkt(out dstSrsWkt, null);
            // 目标分辨率（按需调整）
            double targetResolution = 1001; // 单位：度

            // 步骤1: 预处理每个文件（重投影+重采样）
            var processedFiles = new List<string>();
            foreach (var file in inputFiles)
            {
                string tempFile = Path.GetTempFileName() + ".tif";
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

                    // 执行重投影和重采样
                    Dataset tempsr = Gdal.Warp(tempFile, new Dataset[] { srcDs }, new GDALWarpAppOptions(warpOptions), null, "");
                    tempsr.Dispose();
                }
                Console.WriteLine($"Processed: {file}");
            }

            // 步骤2: 合并所有预处理后的文件
            var vrtFile = Path.GetTempFileName() + ".vrt";
            string tempoutmergeFile = Path.GetTempFileName() + ".tif";
            using (var vrtDs = Gdal.BuildVRT(vrtFile, processedFiles.ToArray(), null, null, ""))
            {
                // 转换为最终输出文件
                Dataset temp = Gdal.wrapper_GDALTranslate(tempoutmergeFile, vrtDs, new GDALTranslateOptions(new string[] { "-of", "GTiff" }), null, "");
                temp.Dispose();
            }
            Console.WriteLine($"Merged output: {tempoutmergeFile}");

            // 清理临时文件
            foreach (var file in processedFiles) File.Delete(file);
            File.Delete(vrtFile);

            // 方法1: warp重采样
            MergeUsingVRT(tempoutmergeFile, outputFile, chinaMinX, chinaMinY, chinaMaxX, chinaMaxY, nationalRes);
            ReSampleToDestAndReprojectToChinaUTM49(tempoutmergeFile, outputascFile, "EPSG:32649", "EPSG:32649");
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
        }
    }
}
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
                Dataset temp = Gdal.wrapper_GDALTranslate(tempoutmergeFile, vrtDs, new GDALTranslateOptions(new string[] { "-of", "GTiff" }), null, "");
                temp.Dispose();
            }
            Console.WriteLine($"Merged output: {tempoutmergeFile}");

            // 清理临时文件
            foreach (var file in processedFiles) File.Delete(file);
            File.Delete(vrtFile);

            // 方法1: warp重采样
            //ReSampleToDestAndReprojectToChinaUTM49(tempoutmergeFile, outputascFile, "EPSG:32649", -908745.868177, 2007145.71554, 2408568.131823, 6121255.71554, 1001);
            ReSampleToDestAndReprojectToChinaUTM49(tempoutmergeFile, outputascFile, "EPSG:4326", 89.705, 17.339, 138.997, 55.238, 0.01);
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
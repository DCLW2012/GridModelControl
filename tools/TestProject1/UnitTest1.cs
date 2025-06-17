using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OSR;
using System.ComponentModel.DataAnnotations;
using System.Data;
using DataType = OSGeo.GDAL.DataType;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            GdalBase.ConfigureAll();
            Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            Gdal.AllRegister();

            // 假设 data 是你的二维数组，行数为 rows，列数为 cols
            int rows = 100;
            int cols = 200;
            float[,] data = new float[rows, cols];
            // 填充数据
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    data[i, j] = (float)(i + j);

            // 创建 ASC 数据集
            Driver drv = Gdal.GetDriverByName("AAIGrid");
            string outPath = "output.asc";
            string[] options = new string[] { "FORMAT=AAIGrid", "BYTEORDER=LSB" }; // 可选参数
            Dataset ds = drv.Create(outPath, cols, rows, 1, DataType.GDT_Float32, options);

            // 写入数据
            Band band = ds.GetRasterBand(1);
            float[] buffer = new float[cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    buffer[j] = data[i, j];
                band.WriteRaster(0, i, cols, 1, buffer, cols, 1, 0, 0);
            }

            // 设置地理参考（可选）
            double[] geoTransform = new double[] { 0, 1, 0, 0, 0, -1 }; // 左上角X, 像元宽度, 旋转, 左上角Y, 旋转, 像元高度
            ds.SetGeoTransform(geoTransform);

            // 设置投影（可选）
            SpatialReference srs = new SpatialReference("");
            srs.ImportFromEPSG(4326); // WGS84
            String dstSrsWkt;
            srs.ExportToWkt(out dstSrsWkt, null);
            ds.SetProjection(dstSrsWkt);

            // 释放资源
            band.Dispose();
            ds.Dispose();
            drv.Dispose();
        }
    }
}
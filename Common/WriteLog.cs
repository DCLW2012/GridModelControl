using Common;
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Common
{
    public class WriteLog
    {
        public static void WriteLogMethod(string content, string fileBaseName = "gridControl")
        {
            string sysPath = System.IO.Directory.GetCurrentDirectory();
            string basepath = sysPath + "//Log//";//文件存放地址
            string name = DateTime.Now.ToString("yyyyMMddHH").Replace("/", "-").Replace(":", "").Replace(" ", "").Trim() + "-" + fileBaseName + ".txt";//组装的文件名称

            if (!Directory.Exists(basepath)) {
                Directory.CreateDirectory(basepath);
            }

            string Contents = content;
            string path = basepath + name;//地址全称
            if (File.Exists(path))// 判断文件是否存在
            {
                File.Delete(path);//删除文件
            }
            FileStream fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write);//定义写入方式
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GB2312"));//写入文件格式
            sw.Write(Contents);//写入文件
            sw.Close();
            fs.Close();
            // 删除上次已经生成的OUT文件
        }


        public static void AppendLogMethod(string content, string fileBaseName = "gridControl")
        {
            string sysPath = System.IO.Directory.GetCurrentDirectory();
            string basepath = sysPath + "//Log//";//文件存放地址
            string name = fileBaseName + ".txt";//组装的文件名称
            string Contents = content;
            string path = basepath + name;//地址全称

            FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);//定义写入方式
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("GB2312"));//写入文件格式
            sw.Write(Contents);//写入文件
            sw.Close();
            fs.Close();
        }

        //！ 日志过期处理
        public static void DeleteLog()
        {
            int logNumExpire = 30;
            if (HookHelper.AppSettings.Build()["logNumExpire"] != null)
            {
                logNumExpire = int.Parse(HookHelper.AppSettings.Build()["logNumExpire"].ToString());
            }

            string path = Environment.CurrentDirectory + "//Log//";//文件存放地址;
            string pattern = "*.txt";
            //判断path路径是否存在
            if (Directory.Exists(path))
            {
                string[] strFileName = Directory.GetFiles(path, pattern);
                //! 日志文件个数超过多少条，执行删除
                if (strFileName.Length > logNumExpire)
                {
                    foreach (var item in strFileName)
                    {
                        File.Delete(item);
                    }
                }
            }
            
        }
    }
}


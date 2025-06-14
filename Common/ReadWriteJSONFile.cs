using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ReadWriteJSONFile
    {
        /// <summary>
        /// 把对象写入到json文件中
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static void Write(List<WaterDeep> jsonData, string strFileName)
        {
            string directory = Path.GetDirectoryName(strFileName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string ListJson = JsonConvert.SerializeObject(jsonData);

            writeJsonFile(strFileName, ListJson);

            //将序列化的json字符串内容写入Json文件，并且保存
            void writeJsonFile(string path, string jsonConents)
            {
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    //如果json文件中有中文数据，可能会出现乱码的现象，那么需要加上如下代码
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("GB2312")))
                    {
                        sw.WriteLine(jsonConents);
                    }
                }
            }
        }

        /// <summary>
        /// 获取到本地的Json文件并且解析返回对应的json字符串
        /// </summary>
        /// <param name="filepath">文件路径</param>
        /// <returns></returns>
        public static string GetJsonFile(string filepath)
        {
            string json = string.Empty;
            using (FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    json = sr.ReadToEnd().ToString();
                }
            }
            return json;
        }

    }
}

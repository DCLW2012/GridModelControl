using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class WaterDeep
    {
        //构造函数
        public WaterDeep()
        {
            this.url = "";
            this.time = "";
            this.area = 0;
            this.isHavarecord = "1";
        }

        public string url { get; set; }
        public string time { get; set; }
        //淹没面积
        public decimal area { get; set; }
        //是否空文件，用来让前端判断是否有数据
        public String isHavarecord { get; set; }

    }
}

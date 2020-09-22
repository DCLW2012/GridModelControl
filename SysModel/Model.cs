using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SysModel
{

    /// <summary>
    /// 站点
    /// </summary>
    public class Site
    {
        /// <summary>
        /// 测站编码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 流域
        /// </summary>
        public string wsnm { get; set; }
        /// <summary>
        /// 水系
        /// </summary>
        public string rsnm { get; set; }
        /// <summary>
        /// 河名
        /// </summary>
        public string rvnm { get; set; }
        /// <summary>
        /// 站名
        /// </summary>
        public string znm { get; set; }
        /// <summary>
        /// 断面地点
        /// </summary>
        public string hsaddress { get; set; }
        /// <summary>
        /// 经度
        /// </summary>
        public string lgtd { get; set; }
        /// <summary>
        /// 纬度
        /// </summary>
        public string lttd { get; set; }
        /// <summary>
        /// 集水面积
        /// </summary>
        public string waterarea { get; set; }
        /// <summary>
        /// 设站日期
        /// </summary>
        public string settime { get; set; }
        /// <summary>
        /// 停测日期
        /// </summary>
        public string gaptime { get; set; }
        /// <summary>
        /// 观测年限
        /// </summary>
        public string observeyear { get; set; }
        /// <summary>
        /// 站别
        /// </summary>
        public string type { get; set; }

    }

    /// <summary>
    /// 雨量
    /// </summary>
    public class YLZJY
    {
        /// <summary>
        /// 编码
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// 雨量编码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime dtime { get; set; }
        /// <summary>
        /// 雨量值
        /// </summary>
        public string val { get; set; }
        /// <summary>
        /// 时段长
        /// </summary>
        public string intv { get; set; }
    }

    /// <summary>
    /// 水文站-流量
    /// </summary>
    public class Swzll
    {
        /// <summary>
        /// 流量编码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 日期
        /// </summary>
        public string dtime { get; set; }
        /// <summary>
        /// 流量值
        /// </summary>
        public string val { get; set; }
    }
}

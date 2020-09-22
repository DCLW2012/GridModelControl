using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SysModel
{
    #region 流域、河道、节点拓扑关系
    /// <summary>
    /// 流域
    /// </summary>
    public class WataTopology
    {
        /// <summary>
        /// ID
        /// </summary>
        public string GID { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        public string WSCD { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public string WSTYPE { get; set; }
        /// <summary>
        /// 上游编号
        /// </summary>
        public string IWSCD { get; set; }
        /// <summary>
        /// 出流流域编码
        /// </summary>
        public string OWSCD { get; set; }
        /// <summary>
        /// 面积
        /// </summary>
        public string WSAREA { get; set; }
        /// <summary>
        /// 平均比降
        /// </summary>
        public string WSSLP { get; set; }
        /// <summary>
        /// 最长汇流路径长度
        /// </summary>
        public string MAXLEN { get; set; }
        /// <summary>
        /// 最长汇流路径比降
        /// </summary>
        public string MAXLSLP { get; set; }

        /// <summary>
        /// 中心点经度
        /// </summary>
        public string CenterX { get; set; }
        /// <summary>
        /// 中心点维度
        /// </summary>
        public string CenterY { get; set; }
        /// <summary>
        /// 中心点高程
        /// </summary>
        public string CenterELV { get; set; }
        /// <summary>
        /// 节点类型
        /// </summary>
        public string NDTYPE { get; set; }
        /// <summary>
        /// 节点编号
        /// </summary>
        public string NDCD { get; set; }
        /// <summary>
        /// 流域坐标串
        /// </summary>
        public string XYS { get; set; }
    }
    /// <summary>
    /// 河道拓扑关系
    /// </summary>
    public class RivlTopology {

        /// <summary>
        /// 河段编号
        /// </summary>
        public string RVCD { get; set; }
        /// <summary>
        /// 上游河段编号
        /// </summary>
        public string FRVCD { get; set; }
        /// <summary>
        /// 河段长度
        /// </summary>
        public string RVLEN { get; set; }
        /// <summary>
        /// 河段比降
        /// </summary>
        public string RVSLP { get; set; }
        /// <summary>
        /// 下游节点类型
        /// </summary>
        public string NDTYPE { get; set; }
        /// <summary>
        /// 节点编号
        /// </summary>
        public string NDCD { get; set; }
        /// <summary>
        /// 河段坐标串
        /// </summary>
        public string XYS { get; set; }

        public string BWSCD { get; set; }
    
    }

    /// <summary>
    /// 节点拓扑关系类
    /// </summary>
    public class NodeTopology
    {
        /// <summary>
        /// 汇流点编号
        /// </summary>
        public string NDCD { get; set; }
        /// <summary>
        /// 河段编号
        /// </summary>
        public string RVCD { get; set; }
    }
    #endregion

}

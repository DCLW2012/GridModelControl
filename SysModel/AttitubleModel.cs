using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace SysModel
{
    public class AttitubleModel
    {

    }
    #region 流域
    /// <summary>
    /// 流域
    /// </summary>
    public class Wata
    {
        /// <summary>
        /// 所在在流域ID,只用于arcgis自提参数
        /// </summary>
        public string drainid { get; set; }
        public string ID { get; set; }
        public string wscd { get; set; }
        public string wsnm { get; set; }
        public string iwscd { get; set; }
        public string owscd { get; set; }
        public string ondcd { get; set; }
        public string wsarea { get; set; }
        public string wsslp { get; set; }
        public string maxlen { get; set; }
        public string maxlslp { get; set; }
        public string centerx { get; set; }
        public string centery { get; set; }
        public string centerelv { get; set; }
        public string the_geom { get; set; }
        public string firstlen { get; set; }
        public string firstlslp { get; set; }
        public string secondlen { get; set; }
        public string secondlslp { get; set; }
        public string threelen { get; set; }
        public string threelslp { get; set; }
        /// <summary>
        /// 流域拆分时使用，拆分的原流域
        /// </summary>
        public string pwscd { get; set; }
       
        public string impervious { get; set; }
       
        /// <summary>
        /// 滞后时间
        /// </summary>
        public string lagtime { get; set; }
        /// <summary>
        /// 汇流时间
        /// </summary>
        public string concentrationtime { get; set; }
      
        /// <summary>
        /// //曼宁系数，用于计算流域汇流时间
        /// </summary>
        public string manning { get; set; }

        /// <summary>
        /// 用于拆分时的排序
        /// </summary>
        public int Ord { get; set; }
       
        
    }
    /// <summary>
    /// 流域提取的基本参数
    /// </summary>
    public class WataParamDefault
    {
        public string ID { get; set; }
        public string wscd { get; set; }

        /// <summary>
        /// 初损
        /// </summary>
        public string initialloss { get; set; }
        /// <summary>
        /// 稳渗
        /// </summary>
        public string constantrate { get; set; }
        /// <summary>
        /// 不透水面积比重
        /// </summary>
        public string impervious { get; set; }
        /// <summary>
        /// 初始含水量
        /// </summary>
        public string initialcontent { get; set; }
        /// <summary>
        /// 饱和含水量
        /// </summary>
        public string saturatedcontent { get; set; }
        /// <summary>
        /// 湿润锋水吸力
        /// </summary>
        public string suction { get; set; }
        /// <summary>
        /// 水力传导度
        /// </summary>
        public string conductivity { get; set; }

        /// <summary>
        /// 滞后时间
        /// </summary>
        public string lagtime { get; set; }
        /// <summary>
        /// 汇流时间
        /// </summary>
        public string concentrationtime { get; set; }
        /// <summary>
        /// 存储系数
        /// </summary>
        public string storagecoefficient { get; set; }

    }

    public class WataChart
    {
        public DateTime Category { get; set; }
        public double Value { get; set; }
    }
    #endregion

    #region 河道
    /// <summary>
    /// 河道
    /// </summary>
    public class Rivl
    {
        /// <summary>
        /// 所在流域ID,只用于arcgis自提参数
        /// </summary>
        public string drainid { get; set; }
        public string the_geom { get; set; }
        /// <summary>
        /// 出口点编号
        /// </summary>
        public string OutCode { get; set; }
        /// <summary>
        /// 河段编码
        /// </summary>
        public string rvcd { get; set; }
        /// <summary>
        /// 河段名称
        /// </summary>
        public string rvnm { get; set; }
        /// <summary>
        /// 上接河段编码
        /// </summary>
        public string frvcd { get; set; }
        /// <summary>
        /// 下接河段编码
        /// </summary>
        public string trvcd { get; set; }
        /// <summary>
        /// 入流节点编码
        /// </summary>
        public string indcd { get; set; }
        /// <summary>
        /// 出流节点编码
        /// </summary>
        public string ondcd { get; set; }
        /// <summary>
        /// 所在流域编码
        /// </summary>
        public string bwscd { get; set; }
        /// <summary>
        /// 水系名称
        /// </summary>
        public string rsnm { get; set; }
        /// <summary>
        /// 河段长度
        /// </summary>
        public string rvlen { get; set; }
        /// <summary>
        /// 河段级别
        /// </summary>
        public string rvcs { get; set; }
        /// <summary>
        /// 河段比降
        /// </summary>
        public string rvslp { get; set; }

        /// <summary>
        /// 拆分的原始河道
        /// </summary>
        public string prvcd { get; set; }

        public string ID { get; set; }
        /// <summary>
        /// 用于拆分时的排序
        /// </summary>
        public int Ord { get; set; }
    }
    public enum Shape
    {
        圆形,
        长方形,
        梯形,
        三角形
    }
    #endregion

    #region 河道节点对应关系类,导入arcgis提取河道使用
    public class RivlNode
    {
        /// <summary>
        /// 上级河道
        /// </summary>
        public string frvcd { get; set; }
        public string ndcd { get; set; }
        //下级河道
        public string trvcd { get; set; }
    }
    #endregion

    #region 节点
    /// <summary>
    /// 节点
    /// </summary>
    public class Node
    {
        /// <summary>
        /// 所在流域ID,只用于arcgis自提参数
        /// </summary>
        public string drainid { get; set; }
        public string ndcd { get; set; }
        public string ndnm { get; set; }
        public string fndcd { get; set; }
        public string tndcd { get; set; }
        public string ndx { get; set; }
        public string ndy { get; set; }
        public string ndtype { get; set; }

        public string ID { get; set; }

       
        public string pndcd { get; set; }
        /// <summary>
        /// 用于拆分时的排序
        /// </summary>
        public int Ord { get; set; }
        /// <summary>
        /// 拆分点对应的沿河村落
        /// </summary>
        public string SADCD { get; set; }
    }
    #endregion

    #region 流域中心点，arcgis提取的流域型心点
    public class CenterPoint
    {
        public string drainid { get; set; }
        public string centerx { get; set; }
        public string centery { get; set; }
    }
    #endregion

    #region 站点相关信息
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
        /// <summary>
        /// 备注
        /// </summary>
        public string remark { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        public string id { get; set; }
    }
    #endregion

    #region 气象站点（流域拟合为站点）相关信息
    /// <summary>
    /// 流域拟合为站点
    /// </summary>
    public class MeteoWataSite
    {
        public string Code { get; set; }
        public string Lgtd { get; set; }
        public string lttd { get; set; }
        public string CalculatingZone { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        public string id { get; set; }
    }

    /// <summary>
    /// 雨量
    /// </summary>
    public class YlZRainfall
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
        public string dtime { get; set; }
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
    /// 流量
    /// </summary>
    public class SWZFlow
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

    #endregion

    #region 泥沙含量
    /// <summary>
    /// 泥沙含量
    /// </summary>
    public class SedCharge
    {
        /// <summary>
        /// 泥沙监测站编码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 日期
        /// </summary>
        public string dtime { get; set; }
        /// <summary>
        /// 泥沙含量值
        /// </summary>
        public string val { get; set; }
    }

    #endregion

    #region 小流域与级配曲线的关联关系
    /// <summary>
    /// 小流域与级配曲线的关联关系
    /// </summary>
    public class WataPairedLine
    {
        /// <summary>
        /// 小流域编码
        /// </summary>
        public string wscd { get; set; }
        /// <summary>
        /// 级配曲线名称
        /// </summary>
        public string pairedLine { get; set; }
    }
    #endregion

    #region 级配曲线表对应数据
    /// <summary>
    /// 级配曲线表对应数据
    /// </summary>
    public class PairedData
    {
        /// <summary>
        /// 级配曲线表名称
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 粒径
        /// </summary>
        public string Diameter { get; set; }
        /// <summary>
        /// 小于（或大于）某粒径的土重（累计百分）含量
        /// </summary>
        public string PercentFiner { get; set; }
    }
    #endregion

    #region 级配曲线表表格基本信息
    /// <summary>
    /// 级配曲线表基本信息
    /// </summary>
    public class PairedTable
    {
        public string id { get; set; }

        /// <summary>
        /// 级配曲线表名称
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 内容描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 数据来源
        /// </summary>
        public string DataSource { get; set; }
        /// <summary>
        /// 计算单位
        /// </summary>
        public string Units { get; set; }
        /// <summary>
        /// 计算单位
        /// </summary>
        public string CalculatingZone { get; set; }
    }
    #endregion


    public class NodeLL
    {
        public DateTime Category { get; set; }
        public double Val { get; set; }
        public double OutVal { get; set; }
        public double CombinVal { get; set; }
    }
    public class RivlLL
    {
        public DateTime Category { get; set; }
        /// <summary>
        /// 出口流量
        /// </summary>
        public double Val { get; set; }
        /// <summary>
        /// 入口流量
        /// </summary>
        public double CombinVal { get; set; }
    }
    public class YLZ
    {
        public string name { get; set; }
    }

    public class YLZChart
    {
        public DateTime Category { get; set; }
        public double Value { get; set; }
    }

    public class ChartValue
    {
        public DateTime Category { get; set; }
        public double Value { get; set; }
    }

    public class ChartDoubleValue
    {
        public double Category { get; set; }
        public double Value { get; set; }
    }

    public class HSJSLDParamMain
    {
        public string ID { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// //流域或河道
        /// </summary>
        public string LDType { get; set; }
        /// <summary>
        /// //流域或河道编号
        /// </summary>
        public string LDCode { get; set; }

        public string LDMainID { get; set; }

        public List<HSLDParam> HSLDParamList = new List<HSLDParam>();
    }

    #region 控制断面
    /// <summary>
    /// 控制断面
    /// </summary>
    public class Section
    {
        public string hecd { get; set; }
        public string the_geom { get; set; }
    }

    #endregion

    public class HSLDParam
    {
        public string ID { get; set; }
        public string ParamType { get; set; }
        public string NomalVal { get; set; }
        public string MaxVal { get; set; }
        public string MinVal { get; set; }
        public string ParamMainName { get; set; }
    }

    #region 率定的参数,流域
    /// <summary>
    /// 率定的参数,流域
    /// </summary>
    public class HSJSLDWataParam
    {
        public string wscd { get; set; }

        public string cs_initial { get; set; }
        public string cs_min { get; set; }
        public string cs_max { get; set; }

        public string ws_initial { get; set; }
        public string ws_min { get; set; }
        public string ws_max { get; set; }

        public string cshsl_initial { get; set; }
        public string cshsl_min { get; set; }
        public string cshsl_max { get; set; }

        public string bhhsl_initial { get; set; }
        public string bhhsl_min { get; set; }
        public string bhhsl_max { get; set; }

        public string xl_initial { get; set; }
        public string xl_min { get; set; }
        public string xl_max { get; set; }

        public string slcdd_initial { get; set; }
        public string slcdd_min { get; set; }
        public string slcdd_max { get; set; }

        public string zhsj_initial { get; set; }
        public string zhsj_min { get; set; }
        public string zhsj_max { get; set; }

        public string hlsj_initial { get; set; }
        public string hlsj_min { get; set; }
        public string hlsj_max { get; set; }

        public string ccxs_initial { get; set; }
        public string ccxs_min { get; set; }
        public string ccxs_max { get; set; }
        public static ObservableCollection<HSJSLDWataParam> ListDefaultWataParam { get; set; }
        public static ObservableCollection<HSJSLDWataParam> GetListDefaultWataParam()
        {
            ListDefaultWataParam = new ObservableCollection<HSJSLDWataParam>();
            ListDefaultWataParam.Add(new HSJSLDWataParam()
            {
                cs_min = "0.001",
                cs_max = "1000",
                ws_min = "0.001",
                ws_max = "300",
                cshsl_min = "0.001",
                cshsl_max = "0.5",
                bhhsl_min = "0.001",
                bhhsl_max = "0.6",
                xl_min = "0.001",
                xl_max = "4000",
                slcdd_min = "0.001",
                slcdd_max = "250",
                zhsj_min = "0.001",
                zhsj_max = "30000",
                hlsj_min = "0.0167",
                hlsj_max = "1000",
                ccxs_min = "0.01",
                ccxs_max = "1000"
            });
            return ListDefaultWataParam;
        }
    }
    #endregion

    #region 率定的参数,河道
    /// <summary>
    /// 率定的参数,河道
    /// </summary>
    public class HSJSLDRivlParam
    {
        public string rvcd { get; set; }

        public string hdds_initial { get; set; }
        public string hdds_min { get; set; }
        public string hdds_max { get; set; }

        public string mnxs_initial { get; set; }
        public string mnxs_min { get; set; }
        public string mnxs_max { get; set; }

        public string zxcs_initial { get; set; }
        public string zxcs_min { get; set; }
        public string zxcs_max { get; set; }

        public string qzyz_initial { get; set; }
        public string qzyz_min { get; set; }
        public string qzyz_max { get; set; }

        public string zhsj_initial { get; set; }
        public string zhsj_min { get; set; }
        public string zhsj_max { get; set; }
        public static ObservableCollection<HSJSLDRivlParam> ListDefaultRivlParam { get; set; }
        public static ObservableCollection<HSJSLDRivlParam> GetListDefaultRivlParam()
        {
            ListDefaultRivlParam = new ObservableCollection<HSJSLDRivlParam>();
            ListDefaultRivlParam.Add(new HSJSLDRivlParam()
            {

                hdds_min = "1",
                hdds_max = "100",
                mnxs_min = "0.0001",
                mnxs_max = "1",
                zxcs_min = "0.001",
                zxcs_max = "150",
                qzyz_min = "0.001",
                qzyz_max = "0.5",
                zhsj_min = "0.001",
                zhsj_max = "30000"
            });
            return ListDefaultRivlParam;
        }
    }
    #endregion

    #region 土壤侵蚀参数
    /// <summary>
    /// 土壤侵蚀参数
    /// </summary>
    public class SoilErosionParam
    {
        public string ID { get; set; }
        //流域名称
        public string Name { get; set; }
        //河道输沙能力计算方法
        public string TransportPotential { get; set; }
        //内聚势
        public string CohesivePotential { get; set; }
        //泥沙比重
        public string SpecificGravity { get; set; }
        //粘土密度
        public string GlaryDry { get; set; }
        //壤土密度
        public string SiltDry { get; set; }
        //砂砾密度
        public string SandDry { get; set; }
        //泥沙颗粒沉降计算方法
        public string FallVelocity { get; set; }
        //粒径分组标准
        public string GradeScale { get; set; }
        //计算单元
        public string CalculatingZone { get; set; }
        //场次ID
        public int FloodEventID { get; set; }

    }
    #endregion

    #region 洪水场次
    public class HSCC
    {
        public string hsccid { get; set; }
        public string name { get; set; }
        public string starttime { get; set; }
        public string endtime { get; set; }
        public string timeinterval { get; set; }
        public string ylinterval { get; set; }
        public string llinterval { get; set; }
        public string Localtion { get; set; }
        public int jylType { get; set; }
        public string meteo_starttime { get; set; }
        public string meteo_endtime { get; set; }
    }
    #endregion

    #region 洪水场次流域算法
    public class HSCC_WATA : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string id { get; set; }
        public string ccid { get; set; }
        public string wscd { get; set; }
        public string canopy { get; set; }
        public string surface { get; set; }
        public string loss { get; set; }
        public string transform { get; set; }
        public string baseflow { get; set; }
        public string canopyname { get; set; }
        public string surfacename { get; set; }
        public string lossname { get; set; }
        public string transformname { get; set; }
        public string baseflowname { get; set; }

        public string ischeck = "False";
        public string IsCheck
        {
            get { return ischeck; }
            set
            {
                ischeck = value;
                if (PropertyChanged != null)
                {

                    PropertyChanged(this, new PropertyChangedEventArgs("IsCheck"));

                }

            }
        }


    }
    #endregion

    #region 洪水场次河道算法
    public class HSCC_RIVL : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string id { get; set; }
        public string ccid { get; set; }
        public string rvcd { get; set; }
        public string routing { get; set; }
        public string loss { get; set; }
        public string routingname { get; set; }
        public string lossname { get; set; }

        public string ischeck = "False";
        public string IsCheck
        {
            get { return ischeck; }
            set
            {
                ischeck = value;
                if (PropertyChanged != null)
                {

                    PropertyChanged(this, new PropertyChangedEventArgs("IsCheck"));
                }

            }
        }
    }
    #endregion

    #region 洪水场次水库算法
    public class HSCC_Reservoir
    {
        public string ID { get; set; }
        public string Ccid { get; set; }
        public string Code { get; set; }
        public string Method { get; set; }
        public string StorageMethod { get; set; }
        public string InitialCondition { get; set; }
    }
    #endregion

    #region 算法(流域、河道)
    public class Method
    {
        public string id { get; set; }
        public string english { get; set; }
        public string chinese { get; set; }
        public string mtype { get; set; }
        public string fid { get; set; }

        public string paramtable { get; set; }
    }

    #endregion

    #region 水库算法
    public class ResMethod
    {
        public string id { get; set; }
        public string english { get; set; }
        public string chinese { get; set; }
        public string mtype { get; set; }
        public string pid { get; set; }

        public string ord { get; set; }

    }

    #endregion

    public class OutPoint
    {
        public string id { get; set; }
        public string name { get; set; }
        public string lgtd { get; set; }
        public string lttd { get; set; }
    }

    #region 图例点
    public class DrawPoint
    {
        /// <summary>
        /// 横坐标
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// 纵坐标
        /// </summary>
        public double Y { set; get; }
    }
    #endregion
    #region 水源
    public class WaterSource
    {
        public string rvcd { get; set; }
        public string x { get; set; }
        public string y { get; set; }
        public string starttime { get; set; }
        public string endtime { get; set; }
        public string interval { get; set; }
        public string name { get; set; }
        public string hsccid { get; set; }
    }
    public class SourceFlow
    {
        public string dtime { get; set; }
        public string flow { get; set; }
    }
    #endregion

    /// <summary>
    /// 按沿河村落进行拆分的流域
    /// </summary>
    public class SplitWataClass
    {
        public string wscd { get; set; }
        public List<Wata> SplitWata { get; set; }

    }
    /// <summary>
    /// 按沿河村落进行拆分的河道
    /// </summary>
    public class SplitRivlClass
    {
        public string rvcd { get; set; }
        public List<Rivl> SplitRivl { get; set; }

    }
    /// <summary>
    /// 按沿河村落拆分后新增的节点
    /// </summary>
    public class SplitNodeClass
    {
        public string ndcd { get; set; }
        public List<Node> SplitNode { get; set; }

    }
}
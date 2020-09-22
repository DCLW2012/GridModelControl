using SysModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;

namespace SysDAL
{
    public class FloodCalculateDAL
    {
        #region 添加洪水场次
        /// <summary>
        /// 添加洪水场次
        /// </summary>
        public static int InsertHSCC(HSCC hscc, string CzoneID)
        {
            int result = -1;

            string sql = "";
            if (hscc != null)
            {
                try
                {
                    string starttime = "";
                    string endtime = "";
                    //根据降雨量数据来源不同，取不同的数据值
                    switch (hscc.jylType)
                    {
                        case 0:
                            starttime = hscc.starttime;
                            endtime = hscc.endtime;
                            break;
                        case 1:
                            starttime = hscc.meteo_starttime;
                            endtime = hscc.meteo_endtime;
                            break;
                    }

                    sql = "insert into hsjscc(name,starttime,endtime,timeinterval,ylinterval,llinterval,localtion,CalculatingZone,jylType) values('" 
                        + hscc.name + "','" + starttime + "','" + endtime + "','" + hscc.timeinterval + "','"
                        + hscc.ylinterval + "','" + hscc.llinterval + "','" + hscc.Localtion + "','" + CzoneID + "','" + hscc.jylType
                        + "');select last_insert_rowid()";
                    result = PublicDAL.ExecuteScalarSql(sql);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;
        }
        #endregion

        #region 更新洪水场次
        /// <summary>
        /// 更新洪水场次
        /// </summary>
        /// <param name="hscc"></param>
        /// <returns></returns>
        public static int UpdateHSCC(HSCC hscc)
        {
            int result = 0;
            try
            {
                string starttime = "";
                string endtime = "";
                //根据降雨量数据来源不同，取不同的数据值
                switch (hscc.jylType)
                {
                    case 0:
                        starttime = hscc.starttime;
                        endtime = hscc.endtime;
                        break;
                    case 1:
                        starttime = hscc.meteo_starttime;
                        endtime = hscc.meteo_endtime;
                        break;
                }

                string sql = "update hsjscc set name='" + hscc.name + "',starttime='" + starttime 
                    + "',endtime='" + endtime + "',timeinterval='" + hscc.timeinterval
                    + "',ylinterval='" + hscc.ylinterval + "',llinterval='" + hscc.llinterval
                    + "',localtion='" + hscc.Localtion + "',jylType='" + hscc.jylType + "' where id=" + hscc.hsccid + "";
                result = PublicDAL.ExecuteSql(sql);

            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                PublicDAL.RollbackTransaction();
            }

            return result;


        }
        #endregion

        #region 查询洪水场次
        /// <summary>
        /// 获取洪水场次数据
        /// </summary>
        /// <returns></returns>
        public static List<HSCC> GetHSCC(string hsccid, string CZoneID)
        {
            List<HSCC> hsccList = new List<HSCC>();
            string where = " where 1=1 and CalculatingZone='" + CZoneID + "'";
            if (hsccid != "0")
            {
                where += " and id = '" + hsccid + "'";
            }
            try
            {
                string sql = "select * from hsjscc " + where;
                DataTable dt = PublicDAL.GetData(sql);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    HSCC hscc = new HSCC();
                    hscc.hsccid = dt.Rows[i]["id"].ToString().Trim();
                    hscc.name = dt.Rows[i]["name"].ToString().Trim();
                    hscc.timeinterval = dt.Rows[i]["timeinterval"].ToString().Trim();
                    hscc.ylinterval = dt.Rows[i]["ylinterval"].ToString().Trim();
                    hscc.llinterval = dt.Rows[i]["llinterval"].ToString().Trim();
                    hscc.Localtion = dt.Rows[i]["Localtion"].ToString().Trim();
                    
                    if(dt.Rows[i]["jylType"].ToString().Trim() == "")
                    {
                        hscc.jylType = 0;
                    }
                    else
                    {
                        hscc.jylType = int.Parse(dt.Rows[i]["jylType"].ToString().Trim());
                    }
                    //根据降雨量数据来源不同，取不同的数据值
                    switch (hscc.jylType)
                    {
                        case 0:
                            hscc.starttime = dt.Rows[i]["starttime"].ToString().Trim();
                            hscc.endtime = dt.Rows[i]["endtime"].ToString().Trim();
                            break;
                        case 1:
                            hscc.meteo_starttime = dt.Rows[i]["starttime"].ToString().Trim();
                            hscc.meteo_endtime = dt.Rows[i]["endtime"].ToString().Trim();
                            break;
                    }
                    
                    hsccList.Add(hscc);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return hsccList;

        }
        #endregion

        #region 判断洪水场次是否已经存在
        /// <summary>
        /// 判断是否已经存在洪水场次
        /// </summary>
        /// <returns></returns>
        public static bool IsHaveHSCC(string name)
        {
            bool result = false;
            string sql = "select name from hsjscc where name='" + name + "'";
            DataTable dt = PublicDAL.GetData(sql);
            if (dt.Rows.Count > 0)
            {
                result = true;
            }
            return result;
        }
        #endregion

        #region 创建默认所有流域选择的方法
        /// <summary>
        /// 创建默认所有流域选择的方法
        /// </summary>
        /// <param name="hsccid"></param>
        /// <returns></returns>
        public static int InsertHSCCWata(int hsccid, string CZoneID)
        {
            int result = -1;

            string sql = "";
            if (hsccid != 0)
            {
                try
                {
                    // 导入之前清空数据库表
                    string sql_del = "delete from hscc_wata where ccid='" + hsccid + "'";
                    PublicDAL.ExecuteSql(sql_del);
                    //sql = "insert into hscc_wata(CCID,WSCD,Canopy,Surface,Loss,Transform,BaseFlow) select " + hsccid + ",wscd,0,0,0,0,0  from wata";
                    //先创建洪水场次对应的响应单元
                    string sql_hr = "insert into hscc_responseunits(ccid,wscd,ucode) select " + hsccid + ",wscd,ucode from wata_responseunits where CalculatingZone='" + CZoneID + "'";
                    PublicDAL.ExecuteSql(sql_hr);
                    //创建响应单元对应的算法
                    sql = "insert into hscc_wata(CCID,WSCD,Canopy,Surface,Loss,Transform,BaseFlow) "
                          + " select t1.ccid,t1.wscd,t2.canopy,t2.surface,t2.loss,t2.Transform,t2.baseflow from  hscc_responseunits t1, responseunits t2 "
                          + " where t1.ucode = t2.Code and t1.ccid='" + hsccid + "' order by t1.id;";
                    result = PublicDAL.ExecuteSql(sql);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;

        }
        #endregion

        #region 创建同上次参数的流域方法
        /// <summary>
        /// 创建默认所有流域选择的方法
        /// </summary>
        /// <param name="hsccid"></param>
        /// <returns></returns>
        public static int InsertHSCCWataByLast(int hsccid, int lasthsccid)
        {
            int result = -1;

            string sql = "";
            if (hsccid != 0)
            {
                try
                {
                    // 导入之前清空数据库表
                    string sql_del = "delete from hscc_wata where ccid='" + hsccid + "'";
                    PublicDAL.ExecuteSql(sql_del);

                    //先创建洪水场次对应的响应单元
                    string sql_hr = "insert into hscc_responseunits(ccid,wscd,ucode) select " + hsccid + ",wscd,ucode from hscc_responseunits where ccid='" + lasthsccid + "'";
                    PublicDAL.ExecuteSql(sql_hr);
                    //创建响应单元对应的算法
                    sql = @"insert into hscc_wata(CCID,WSCD,Canopy,Surface,Loss,Transform,BaseFlow) 
select '" + hsccid + @"',wscd,canopy,surface,loss,Transform,baseflow from  hscc_wata 
 where  ccid='" + lasthsccid + "' order by id";
                    result = PublicDAL.ExecuteSql(sql);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;

        }
        #endregion

        #region 更新洪水场次对应的响应单元
        /// <summary>
        /// 更新洪水场次对应的响应单元
        /// </summary>
        /// <param name="hsccid"></param>
        /// <returns></returns>
        public static int UpdateHSCCResponseUnits(List<HSCC_ResponseUnits> HSCCResponseUnitsList)
        {
            int result = -1;
            if (HSCCResponseUnitsList.Count > 0)
            {
                try
                {
                    PublicDAL.BeginTransaction();
                    string wscds = "";
                    for (int i = 0; i < HSCCResponseUnitsList.Count; i++)
                    {
                        string sql_hr = "update  hscc_responseunits set ucode='" + HSCCResponseUnitsList[i].ucode + "' where wscd='" + HSCCResponseUnitsList[i].wscd + "' and ccid='" + HSCCResponseUnitsList[i].ccid + "'";
                        wscds += "'" + HSCCResponseUnitsList[i].wscd + "',";
                        result += PublicDAL.ExecuteSql(sql_hr);
                    }
                    PublicDAL.CommitTransacton();



                    ///更新响应单元对应的算法
                    string sql = @"update hscc_wata set Canopy=(select t2.canopy from  hscc_responseunits t1, responseunits t2
 where t1.ucode = t2.Code and t1.ccid = '" + HSCCResponseUnitsList[0].ccid + @"' and wscd = t1.wscd and ccid = t1.ccid ),Surface = (select t2.Surface from  hscc_responseunits t1, responseunits t2
         where t1.ucode = t2.Code and t1.ccid = '" + HSCCResponseUnitsList[0].ccid + @"' and wscd = t1.wscd and ccid = t1.ccid ),
Loss = (select t2.Loss from  hscc_responseunits t1, responseunits t2
   where t1.ucode = t2.Code and t1.ccid = '" + HSCCResponseUnitsList[0].ccid + @"' and wscd = t1.wscd and ccid = t1.ccid ),
Transform = (select t2.Transform from  hscc_responseunits t1, responseunits t2
   where t1.ucode = t2.Code and t1.ccid = '" + HSCCResponseUnitsList[0].ccid + @"' and wscd = t1.wscd and ccid = t1.ccid ),
BaseFlow = (select t2.BaseFlow from  hscc_responseunits t1, responseunits t2
   where t1.ucode = t2.Code and t1.ccid = '" + HSCCResponseUnitsList[0].ccid + @"' and wscd = t1.wscd and ccid = t1.ccid )";
                    result += PublicDAL.ExecuteSql(sql);
                    ///获取更新的关联ID
                    wscds = wscds.Substring(0, wscds.Length - 1);
                    string sql_hmids = "select id from hscc_wata where wscd in(" + wscds + ") and ccid='" + HSCCResponseUnitsList[0].ccid + "'";
                    DataTable dt_hmids = PublicDAL.GetData(sql_hmids);
                    string hmids = "";
                    for (int i = 0; i < dt_hmids.Rows.Count; i++)
                    {
                        hmids += dt_hmids.Rows[i]["id"].ToString() + ",";
                    }
                    hmids = hmids.Substring(0, hmids.Length - 1);
                    ///更新流域的选择的参数
                    result += InsertHSCCWataParam(hmids, int.Parse(HSCCResponseUnitsList[0].ccid));
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    PublicDAL.RollbackTransaction();
                }

            }
            return result;

        }
        #endregion

        #region 创建流域对应的方法对应的参数
        /// <summary>
        /// 创建方法对应的默认参数
        /// </summary>
        /// <returns></returns>
        public static int InsertHSCCWataParam(int ccid)
        {
            int result = -1;
            if (ccid != 0)
            {
                try
                {
                    #region 清空参数数据
                    string sql_del = "delete from L_InitialConstant where ccid in(" + ccid + ");delete from L_GreenAmpt where  ccid in(" + ccid + ");delete from L_SCSCurveNumber where ccid in(" + ccid + ");delete from L_SoilMoistureAcco where ccid in(" + ccid + "); delete from T_ClarkUnit where ccid in(" + ccid + ");delete from T_SCSUnit where ccid in(" + ccid + ");delete from T_UserSpecifiedUni where ccid in(" + ccid + ");delete from C_SimpleCanopy where ccid in(" + ccid + ");delete from S_SimpleSurface where ccid in(" + ccid + ");delete from B_ConstantMonthly where ccid in(" + ccid + ");delete from B_LinearReservoir where ccid in(" + ccid + ");";
                    PublicDAL.ExecuteSql(sql_del);
                    #endregion
                    #region 根据洪水场次查询 流域使用方法
                    string sql = "select * from hscc_wata where ccid=" + ccid + "";
                    DataTable dt_hsccwata = PublicDAL.GetData(sql);

                    result = InsertWataParamData(ccid, result, dt_hsccwata);

                    #endregion

                }
                catch (Exception ex)
                {

                    throw ex;
                }
                finally
                {
                    PublicDAL.RollbackTransaction();


                }
            }
            return result;
        }

        /// <summary>
        /// 向数据库添加土壤侵蚀因子
        /// </summary>
        /// <param name="cZoneID"></param>
        /// <param name="ccID"></param>
        /// <returns></returns>
        public static int InsertSoilErosionFactor(string cZoneID, int ccID)
        {
            int result = -1;

            if (ccID != 0)
            {
                try
                {
                    // 导入之前清空数据库表
                    string sql_del = "delete from Soil_WataErosionfactor where ccid='" + ccID + "'";
                    PublicDAL.ExecuteSql(sql_del);
                    //sql = "insert into hscc_wata(CCID,WSCD,Canopy,Surface,Loss,Transform,BaseFlow) select " + hsccid + ",wscd,0,0,0,0,0  from wata";
                    //先创建洪水场次对应的响应单元
                    string sql_insert = "INSERT INTO Soil_WataErosionfactor (ccid, wscd) SELECT '" + ccID + "', wscd FROM Wata WHERE CalculatingZone = '" + cZoneID + "';";

                    result = PublicDAL.ExecuteSql(sql_insert);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;

        }

        /// <summary>
        /// 插入洪水场次对应的土壤侵蚀参数及计算方法
        /// </summary>
        /// <param name="seParam"></param>
        /// <returns></returns>
        public static int InsertSoilErosionParameters(SoilErosionParam seParam)
        {
            int result = -1;

            string sql = "";
            if (seParam != null)
            {
                try
                {
                   sql = "INSERT INTO Soil_BasinParam "
                        +" ( ID, Name, TransportPotential, CohesivePotential, SpecificGravity, GlaryDry, "
                        + " SiltDry, SandDry, FallVelocity, GradeScale, CalculatingZone, FloodEventID) "
                        + " VALUES ((SELECT count(*) FROM Soil_BasinParam) + 1 ,'" + seParam.Name + "','" + seParam.TransportPotential + "','"
                        + seParam.CohesivePotential + "','" + seParam.SpecificGravity + "','" + seParam.GlaryDry + "','"  
                        + seParam.SiltDry + "','" + seParam.SandDry + "','" + seParam.FallVelocity + "','"
                        + seParam.GradeScale + "','" + seParam.CalculatingZone + "','" + seParam.FloodEventID + "');";

                   result = PublicDAL.ExecuteSql(sql);                   
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;
        }

        public static int UpdateSoilErosionParamters(SoilErosionParam seParam)
        {
            int result = -1;

            string sql = "";
            if (seParam != null)
            {
                try
                {

                    sql = "UPDATE Soil_BasinParam SET "
                        + " Name='" + seParam.Name + "', TransportPotential='" + seParam.TransportPotential + "', CohesivePotential='"
                        + seParam.CohesivePotential + "'," + " SpecificGravity=" + seParam.SpecificGravity + ", GlaryDry="
                        + seParam.GlaryDry + ", SiltDry=" + seParam.SiltDry + ", SandDry=" + seParam.SandDry + ","
                        + " FallVelocity='" + seParam.FallVelocity + "', GradeScale='" + seParam.GradeScale + "'"
                        + " WHERE CalculatingZone=" + seParam.CalculatingZone + " and FloodEventID=" + seParam.FloodEventID + ";";

                    result = PublicDAL.ExecuteSql(sql);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;
        }
        /// <summary>
        ///   创建对应的方法对应的参数,重构针对参数修改
        /// </summary>
        /// <returns></returns>
        public static int InsertHSCCWataParam(string hmids, int ccid)
        {
            int result = -1;
            if (hmids != "")
            {
                try
                {
                    #region 清空参数数据
                    string sql_del = "delete from L_InitialConstant where hmid in(" + hmids + ");delete from L_GreenAmpt where  hmid in(" + hmids + ");delete from L_SCSCurveNumber where hmid in(" + hmids + ");delete from L_SoilMoistureAcco where hmid in(" + hmids + "); delete from T_ClarkUnit where hmid in(" + hmids + ");delete from T_SCSUnit where hmid in(" + hmids + ");delete from T_UserSpecifiedUni where hmid in(" + hmids + ");delete from C_SimpleCanopy where hmid in(" + hmids + ");delete from S_SimpleSurface where hmid in(" + hmids + ");delete from B_ConstantMonthly where hmid in(" + hmids + ");delete from B_LinearReservoir where hmid in(" + hmids + ");";
                    PublicDAL.ExecuteSql(sql_del);
                    #endregion
                    #region 根据洪水场次查询 流域使用方法
                    string sql = "select * from hscc_wata where id in(" + hmids + ")";
                    DataTable dt_hsccwata = PublicDAL.GetData(sql);
                    result = InsertWataParamData(ccid, result, dt_hsccwata);
                    #endregion

                }
                catch (Exception ex)
                {

                    throw ex;
                }
                finally
                {
                    PublicDAL.RollbackTransaction();


                }
            }
            return result;
        }



        /// <summary>
        /// 插入参数数据
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="result"></param>
        /// <param name="dt_hsccwata"></param>
        /// <returns></returns>
        public static int InsertWataParamData(int ccid, int result, DataTable dt_hsccwata)
        {
            DataTable dt_paramdefault = GetWataParamDefault();

            if (dt_hsccwata.Rows.Count > 0)
            {
                PublicDAL.BeginTransaction();

                for (int i = 0; i < dt_hsccwata.Rows.Count; i++)
                {
                    string wscd = dt_hsccwata.Rows[i]["wscd"].ToString();
                    string id = dt_hsccwata.Rows[i]["id"].ToString();
                    string canopy = dt_hsccwata.Rows[i]["canopy"].ToString();
                    string surface = dt_hsccwata.Rows[i]["surface"].ToString();
                    string loss = dt_hsccwata.Rows[i]["loss"].ToString();
                    string transform = dt_hsccwata.Rows[i]["transform"].ToString();
                    string baseflow = dt_hsccwata.Rows[i]["baseflow"].ToString();

                    DataRow[] paramdefault = dt_paramdefault.Select("wscd='" + wscd + "'");//默认参数

                    #region 写入植被截留参数（canopy）
                    result += InsertCanopyDefault(ccid, id, canopy);

                    #endregion

                    #region 写入洼蓄参数（surface）
                    result += InsertSurfaceDefault(ccid, id, surface);


                    #endregion

                    #region 写入扣损参数（loss）
                    result += InsertLossDefault(ccid, id, loss, paramdefault);
                    #endregion

                    #region 写入汇流参数（transform）
                    result += InsertTransformDefault(ccid, id, transform, paramdefault);
                    #endregion

                    #region 写入基流参数（baseflow）
                    result += InsertBaseFlowDefault(ccid, id, baseflow);
                    #endregion


                }
                PublicDAL.CommitTransacton();
            }

            return result;
        }
        /// <summary>
        /// 导入基流默认参数
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <param name="baseflow"></param>
        /// <returns></returns>
        public static int InsertBaseFlowDefault(int ccid, string id, string baseflow)
        {
            int result = 0;
            string sql_baseflow = "";
            switch (baseflow)
            {
                case "0": //无
                    break;
                case "1"://单月常数
                    sql_baseflow = "insert into B_ConstantMonthly(hmid,ccid) values('" + id + "','" + ccid + "')";
                    break;
                case "2"://线性水库
                    sql_baseflow = "insert into B_LinearReservoir(hmid,ccid) values('" + id + "','" + ccid + "')";
                    break;

                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_baseflow);
            return result;
        }
        /// <summary>
        /// 导入汇流默认
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="result"></param>
        /// <param name="id"></param>
        /// <param name="transform"></param>
        /// <param name="paramdefault"></param>
        /// <returns></returns>
        public static int InsertTransformDefault(int ccid, string id, string transform, DataRow[] paramdefault)
        {
            int result = 0;
            string sql_transform = "";
            switch (transform)
            {
                case "0":
                    break;

                case "1"://SCS单位线
                    sql_transform = "insert into T_SCSUnit(lagtime,hmid,ccid) values ('" + paramdefault[0]["lagtime"].ToString() + "','" + id + "'," + ccid + ")";
                    break;

                case "2"://用户自定义单位线
                    sql_transform = "insert into T_UserSpecifiedUni(hmid,ccid) values('" + id + "','" + ccid + "')";
                    break;

                case "3"://克拉克单位线
                    sql_transform = "insert into T_ClarkUnit(concentrationtime,storagecoefficient,hmid,ccid) values ('" + paramdefault[0]["concentrationtime"].ToString() + "','" + paramdefault[0]["storagecoefficient"].ToString() + "','" + id + "','" + ccid + "')";
                    break;
                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_transform);
            return result;
        }
        /// <summary>
        /// 导入扣损默认参数
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <param name="loss"></param>
        /// <param name="paramdefault"></param>
        /// <returns></returns>
        public static int InsertLossDefault(int ccid, string id, string loss, DataRow[] paramdefault)
        {
            int result = 0;
            string sql_loss = "";
            switch (loss)
            {
                case "0": //无

                    break;
                case "1": //初损后损
                    sql_loss = "insert into L_InitialConstant(initialloss,constantrate,impervious,hmid,ccid) values('" + paramdefault[0]["initialloss"].ToString() + "','" + paramdefault[0]["constantrate"].ToString() + "','" + paramdefault[0]["impervious"].ToString() + "','" + id + "','" + ccid + "')";
                    break;
                case "2": //格林安普
                    sql_loss = "insert into L_GreenAmpt(initialcontent,saturatedcontent,suction,conductivity,impervious,hmid,ccid) values('" + paramdefault[0]["initialcontent"].ToString() + "','" + paramdefault[0]["saturatedcontent"].ToString() + "','" + paramdefault[0]["suction"].ToString() + "','" + paramdefault[0]["conductivity"].ToString() + "','" + paramdefault[0]["impervious"].ToString() + "','" + id + "','" + ccid + "')";
                    break;
                case "3"://SCS指数法
                    sql_loss = "insert into L_SCSCurveNumber(hmid,ccid) values('" + id + "','" + ccid + "')";
                    break;
                case "4"://土壤湿度考虑法
                    sql_loss = "insert into L_SoilMoistureAcco(hmid,ccid) values('" + id + "','" + ccid + "')";
                    break;

                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_loss);
            return result;
        }
        /// <summary>
        /// 导入洼蓄默认值
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <param name="surface"></param>
        /// <returns></returns>
        public static int InsertSurfaceDefault(int ccid, string id, string surface)
        {
            int result = 0;
            string sql_surface = "";
            switch (surface)
            {
                case "0": //无
                    break;
                case "1"://简单洼蓄
                    sql_surface = "insert into S_SimpleSurface(hmid,ccid) values('" + id + "','" + ccid + "')";
                    break;

                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_surface);
            return result;
        }
        /// <summary>
        /// 导入植被截留默认值
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <param name="canopy"></param>
        /// <returns></returns>
        public static int InsertCanopyDefault(int ccid, string id, string canopy)
        {
            int result = 0;
            string sql_canopy = "";
            switch (canopy)
            {
                case "0": //无

                    break;
                case "1"://简单截留
                    sql_canopy = "insert into C_SimpleCanopy(hmid,ccid) values('" + id + "','" + ccid + "')";
                    break;

                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_canopy);
            return result;
        }
        /// <summary>
        /// 获取流域默认参数列表
        /// </summary>
        /// <returns></returns>
        public static DataTable GetWataParamDefault()
        {
            ///流域默认参数表
            string sql_paramdefault = "select * from wata_paramdefault";
            DataTable dt_paramdefault = PublicDAL.GetData(sql_paramdefault);
            return dt_paramdefault;
        }


        #endregion
        #region 创建同上次洪水场次方法对应的参数
        public static int InsertHSCCWataParamByLast(int ccid, int lastccid)
        {
            int result = -1;
            if (ccid != 0)
            {
                try
                {
                    #region 清空参数数据
                    string sql_del = "delete from L_InitialConstant where ccid in(" + ccid + ");delete from L_GreenAmpt where  ccid in(" + ccid + ");delete from L_SCSCurveNumber where ccid in(" + ccid + ");delete from L_SoilMoistureAcco where ccid in(" + ccid + "); delete from T_ClarkUnit where ccid in(" + ccid + ");delete from T_SCSUnit where ccid in(" + ccid + ");delete from T_UserSpecifiedUni where ccid in(" + ccid + ");delete from C_SimpleCanopy where ccid in(" + ccid + ");delete from S_SimpleSurface where ccid in(" + ccid + ");delete from B_ConstantMonthly where ccid in(" + ccid + ");delete from B_LinearReservoir where ccid in(" + ccid + ");";
                    PublicDAL.ExecuteSql(sql_del);
                    #endregion
                    #region 根据洪水场次查询 流域使用方法
                    string sql = @"select tt1.wscd as wscd,tt1.id as hmidold,tt2.id as hmidnew,tt2.canopy as canopy,tt2.surface as surface,tt2.loss as loss,tt2.Transform as Transform,tt2.baseflow as baseflow from 
(select id,wscd from  hscc_wata where ccid='" + lastccid + @"')tt1
,
(select id,wscd,canopy,surface,loss,Transform,baseflow from hscc_wata where ccid='" + ccid + @"')tt2
where tt1.wscd=tt2.wscd";
                    DataTable dt_hsccwata = PublicDAL.GetData(sql);

                    result = InsertWataParamDataByLast(ccid, result, dt_hsccwata);

                    #endregion

                }
                catch (Exception ex)
                {

                    //throw ex;
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    PublicDAL.RollbackTransaction();


                }
            }
            return result;
        }
        /// <summary>
        /// 插入参数数据
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="result"></param>
        /// <param name="dt_hsccwata"></param>
        /// <returns></returns>
        public static int InsertWataParamDataByLast(int ccid, int result, DataTable dt_hsccwata)
        {
            DataTable dt_paramdefault = GetWataParamDefault();

            if (dt_hsccwata.Rows.Count > 0)
            {
                PublicDAL.BeginTransaction();

                for (int i = 0; i < dt_hsccwata.Rows.Count; i++)
                {
                    string wscd = dt_hsccwata.Rows[i]["wscd"].ToString();
                    string hmidold = dt_hsccwata.Rows[i]["hmidold"].ToString();
                    string hmidnew = dt_hsccwata.Rows[i]["hmidnew"].ToString();
                    string canopy = dt_hsccwata.Rows[i]["canopy"].ToString();
                    string surface = dt_hsccwata.Rows[i]["surface"].ToString();
                    string loss = dt_hsccwata.Rows[i]["loss"].ToString();
                    string transform = dt_hsccwata.Rows[i]["transform"].ToString();
                    string baseflow = dt_hsccwata.Rows[i]["baseflow"].ToString();

                    #region 写入植被截留参数（canopy）
                    result += InsertCanopyDefaultByLast(ccid, hmidnew, canopy, hmidold);

                    #endregion

                    #region 写入洼蓄参数（surface）
                    result += InsertSurfaceDefaultByLast(ccid, hmidnew, surface, hmidold);


                    #endregion

                    #region 写入扣损参数（loss）
                    result += InsertLossDefaultByLast(ccid, hmidnew, loss, hmidold);
                    #endregion

                    #region 写入汇流参数（transform）
                    result += InsertTransformDefaultByLast(ccid, hmidnew, transform, hmidold);
                    #endregion

                    #region 写入基流参数（baseflow）
                    result += InsertBaseFlowDefaultByLast(ccid, hmidnew, baseflow, hmidold);
                    #endregion


                }
                PublicDAL.CommitTransacton();
            }

            return result;
        }
        /// <summary>
        /// 导入基流默认参数
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <param name="baseflow"></param>
        /// <returns></returns>
        public static int InsertBaseFlowDefaultByLast(int ccid, string id, string baseflow, string oldid)
        {
            int result = 0;
            string sql_baseflow = "";
            switch (baseflow)
            {
                case "0": //无
                    break;
                case "1"://单月常数
                    sql_baseflow = "insert into B_ConstantMonthly(hmid,ccid,January,February,March, April,May, June,July,August,September,October,November, December) select " + id + "," + ccid + ",January,February,March, April,May, June,July,August,September,October,November, December  from B_ConstantMonthly  where hmid=" + oldid;
                    break;
                case "2"://线性水库
                    sql_baseflow = "insert into B_LinearReservoir(hmid,ccid,initialtype,gw1initial,gw2initial,gw1coefficient,gw1reservoirs,gw2coefficient,gw2reservoirs)  select " + id + "," + ccid + ",initialtype,gw1initial,gw2initial,gw1coefficient,gw1reservoirs,gw2coefficient,gw2reservoirs from B_LinearReservoir where hmid=" + oldid;
                    break;

                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_baseflow);
            return result;
        }
        /// <summary>
        /// 导入汇流默认
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="result"></param>
        /// <param name="id"></param>
        /// <param name="transform"></param>
        /// <param name="paramdefault"></param>
        /// <returns></returns>
        public static int InsertTransformDefaultByLast(int ccid, string id, string transform, string oldid)
        {
            int result = 0;
            string sql_transform = "";
            switch (transform)
            {
                case "0":
                    break;

                case "1"://SCS单位线
                    sql_transform = "insert into T_SCSUnit(hmid,ccid,lagtime) select " + id + "," + ccid + ", lagtime from  T_SCSUnit where hmid=" + oldid;
                    break;

                case "2"://用户自定义单位线
                    sql_transform = "insert into T_UserSpecifiedUni(hmid,ccid,unithydrograph) select " + id + "," + ccid + ",unithydrograph from T_UserSpecifiedUni where hmid=" + oldid;
                    break;

                case "3"://克拉克单位线
                    sql_transform = "insert into T_ClarkUnit(hmid,ccid,concentrationtime,storagecoefficient) select " + id + "," + ccid + ",concentrationtime,storagecoefficient from T_ClarkUnit where hmid=" + oldid;
                    break;
                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_transform);
            return result;
        }
        /// <summary>
        /// 导入扣损默认参数
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <param name="loss"></param>
        /// <param name="paramdefault"></param>
        /// <returns></returns>
        public static int InsertLossDefaultByLast(int ccid, string id, string loss, string oldid)
        {
            int result = 0;
            string sql_loss = "";
            switch (loss)
            {
                case "0": //无

                    break;
                case "1": //初损后损
                    sql_loss = "insert into L_InitialConstant(hmid,ccid,initialloss,constantrate,impervious) select " + id + "," + ccid + ",initialloss,constantrate,impervious from  L_InitialConstant where hmid=" + oldid;
                    break;
                case "2": //格林安普
                    sql_loss = "insert into L_GreenAmpt(hmid,ccid,initialcontent,saturatedcontent,suction,conductivity,impervious) select " + id + "," + ccid + ",initialcontent,saturatedcontent,suction,conductivity,impervious from  L_GreenAmpt where hmid=" + oldid;
                    break;
                case "3"://SCS指数法
                    sql_loss = "insert into L_SCSCurveNumber(hmid,ccid,initialabstraction,curvenumber,impervious) select " + id + "," + ccid + ",initialabstraction,curvenumber,impervious from  L_SCSCurveNumber where hmid=" + oldid;
                    break;
                case "4"://土壤湿度考虑法
                    sql_loss = "insert into L_SoilMoistureAcco(hmid,ccid,soil,groundwater1,groundwater2,maxinfiltration,impervious,soilstorage,tensionstorage,soilpercolation,gw1storage,gw1percolation,gw1coefficient,gw2storage,gw2percolation,gw2coefficient) select " + id + "," + ccid + ",soil,groundwater1,groundwater2,maxinfiltration,impervious,soilstorage,tensionstorage,soilpercolation,gw1storage,gw1percolation,gw1coefficient,gw2storage,gw2percolation,gw2coefficient from L_SoilMoistureAcco where hmid=" + oldid;
                    break;

                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_loss);
            return result;
        }
        /// <summary>
        /// 导入洼蓄默认值
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <param name="surface"></param>
        /// <returns></returns>
        public static int InsertSurfaceDefaultByLast(int ccid, string id, string surface, string oldid)
        {
            int result = 0;
            string sql_surface = "";
            switch (surface)
            {
                case "0": //无
                    break;
                case "1"://简单洼蓄
                    sql_surface = "insert into S_SimpleSurface(hmid,ccid,initialstroage,maxstroage) select " + id + "," + ccid + ",initialstroage,maxstroage from S_SimpleSurface where hmid=" + oldid;
                    break;

                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_surface);
            return result;
        }
        /// <summary>
        /// 导入植被截留默认值
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="id"></param>
        /// <param name="canopy"></param>
        /// <returns></returns>
        public static int InsertCanopyDefaultByLast(int ccid, string id, string canopy, string oldid)
        {
            int result = 0;
            string sql_canopy = "";
            switch (canopy)
            {
                case "0": //无

                    break;
                case "1"://简单截留
                    sql_canopy = "insert into C_SimpleCanopy(hmid,ccid,initialstroage,maxstroage,cropcoefficient,uptakemehtod) select " + id + "," + ccid + ",initialstroage,maxstroage,cropcoefficient,uptakemehtod from c_simplecanopy where hmid=" + oldid;
                    break;

                default:
                    break;
            }
            result = PublicDAL.ExecuteSql(sql_canopy);
            return result;
        }
        #endregion

        #region 创建默认所有河道选择的方法
        /// <summary>
        /// 创建默认所有河道选择的方法
        /// </summary>
        /// <param name="hsccid"></param>
        /// <returns></returns>
        public static int InsertHSCCRivl(int hsccid, string CZoneID)
        {
            int result = -1;

            string sql = "";
            if (hsccid != 0)
            {
                try
                {
                    // 导入之前清空数据库表
                    string sql_del = "delete from hscc_rivl where ccid='" + hsccid + "'";
                    PublicDAL.ExecuteSql(sql_del);

                    sql = "insert into hscc_rivl(CCID,rvcd,Routing,Loss) select " + hsccid + ",rvcd,1,0 from rivl where rivl.CalculatingZone='" + CZoneID + "'";
                    result = PublicDAL.ExecuteSql(sql);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;

        }
        #endregion
        #region 创建同参数的河道选择的方法
        /// <summary>
        /// 创建默认所有河道选择的方法
        /// </summary>
        /// <param name="hsccid"></param>
        /// <returns></returns>
        public static int InsertHSCCRivlByLast(int hsccid, int lasthsccid)
        {
            int result = -1;

            string sql = "";
            if (hsccid != 0)
            {
                try
                {
                    // 导入之前清空数据库表
                    string sql_del = "delete from hscc_rivl where ccid='" + hsccid + "'";
                    PublicDAL.ExecuteSql(sql_del);

                    sql = "insert into hscc_rivl(CCID,rvcd,Routing,Loss) select " + hsccid + ",rvcd,Routing,Loss from hscc_rivl where ccid='" + lasthsccid + "'";
                    result = PublicDAL.ExecuteSql(sql);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return result;

        }
        #endregion

        #region 修改河道对应的算法
        /// <summary>
        /// 修改河道对应的算法
        /// </summary>
        /// <param name="HSCCWataList"></param>
        /// <returns></returns>
        public static int UpdateHSCCRivl(List<HSCC_RIVL> HSCCRivlList)
        {

            int result = -1;
            try
            {
                string sql = "";
                if (HSCCRivlList != null)
                {
                    string hmids = "";
                    PublicDAL.BeginTransaction();
                    for (int i = 0; i < HSCCRivlList.Count; i++)
                    {
                        hmids += HSCCRivlList[i].id + ",";
                        sql = string.Format("update hscc_rivl set Routing='{0}',Loss='{1}' where ID={2}", HSCCRivlList[i].routing, HSCCRivlList[i].loss, HSCCRivlList[i].id);
                        result += PublicDAL.ExecuteSql(sql);

                    }
                    PublicDAL.CommitTransacton();
                    hmids = hmids.Substring(0, hmids.Length - 1);

                    result += FloodCalculateDAL.InsertHSCCRivlParam(hmids, int.Parse(HSCCRivlList[0].ccid));
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                PublicDAL.RollbackTransaction();
            }

            return result;

        }


        #endregion

        #region 创建河道使用参数

        /// <summary>
        /// 创建河道默认使用参数
        /// </summary>
        /// <returns></returns>
        public static int InsertHSCCRivlParam(int ccid)
        {
            int result = -1;
            if (ccid != 0)
            {
                try
                {
                    string sql_del = "delete from R_KinematicWave where ccid='" + ccid + "';delete from R_Muskingum where ccid='" + ccid + "';delete from R_Lag where ccid='" + ccid + "';";
                    PublicDAL.ExecuteSql(sql_del);
                    string sql_hsccrivl = "select * from hscc_rivl where ccid='" + ccid + "'";
                    DataTable dt_hsccrivl = PublicDAL.GetData(sql_hsccrivl);

                    result = InsertRivParamlData(ccid, result, dt_hsccrivl);
                }

                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    PublicDAL.RollbackTransaction();
                }

            }
            return result;


        }
        /// <summary>
        /// 创建河道默认使用参数
        /// </summary>
        /// <returns></returns>
        public static int InsertHSCCRivlParam(string hmids, int ccid)
        {
            int result = -1;
            if (hmids != "")
            {
                try
                {
                    string sql_del = "delete from R_KinematicWave where hmid in(" + hmids + ");delete from R_Muskingum where hmid in(" + hmids + ");delete from R_Lag where hmid in(" + hmids + ")";
                    PublicDAL.ExecuteSql(sql_del);
                    string sql_hsccrivl = "select * from hscc_rivl where id in(" + hmids + ")";
                    DataTable dt_hsccrivl = PublicDAL.GetData(sql_hsccrivl);

                    result = InsertRivParamlData(ccid, result, dt_hsccrivl);
                }

                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    PublicDAL.RollbackTransaction();
                }

            }
            return result;


        }
        /// <summary>
        /// 更新河道使用参数
        /// </summary>
        /// <param name="ccid"></param>
        /// <param name="result"></param>
        /// <param name="dt_hsccrivl"></param>
        /// <returns></returns>
        public static int InsertRivParamlData(int ccid, int result, DataTable dt_hsccrivl)
        {
            string sql_rivl = "select rvcd,rvlen,rvslp from rivl";
            DataTable dt_rivl = PublicDAL.GetData(sql_rivl);
            if (dt_hsccrivl.Rows.Count > 0)
            {
                PublicDAL.BeginTransaction();
                for (int i = 0; i < dt_hsccrivl.Rows.Count; i++)
                {
                    string id = dt_hsccrivl.Rows[i]["id"].ToString();
                    string rvcd = dt_hsccrivl.Rows[i]["rvcd"].ToString();
                    string routing = dt_hsccrivl.Rows[i]["routing"].ToString();
                    DataRow[] rivlparam = dt_rivl.Select("rvcd='" + rvcd + "'");
                    #region 河道演进方法选择
                    string insert_routing = "";
                    switch (routing)
                    {
                        case "0":
                            break;
                        case "1"://运动波,其他参数全部默认了，如有修改再次添加字段
                            insert_routing = "insert into R_KinematicWave(rvlen,rvslp,hmid,ccid) values ('" + rivlparam[0]["rvlen"].ToString() + "','" + rivlparam[0]["rvslp"].ToString() + "','" + id + "','" + ccid + "')";
                            break;
                        case "2"://马斯京根
                            insert_routing = "insert into R_Muskingum(hmid,ccid) values ('" + id + "','" + ccid + "')";
                            break;
                        case "3"://滞后演算 update by chen 2016/11/5
                            insert_routing = @"insert into R_Lag(hmid,ccid,lagtime) select " + id + @"," + ccid + @" t3.lagtime from 
(select rvcd from hscc_rivl where ccid='1' and id='1') t1,
(select rvcd,bwscd from rivl) t2,
(select wscd,lagtime from wata) t3
where t1.rvcd=t2.rvcd and t2.bwscd=t3.wscd";
                            break;
                        default:
                            break;
                    }
                    result += PublicDAL.ExecuteSql(insert_routing);
                    #endregion
                }
                PublicDAL.CommitTransacton();
            }

            return result;
        }

        #endregion

        #region 创建同参数场次河道使用参数
        /// <summary>
        /// 创建河道默认使用参数
        /// </summary>
        /// <returns></returns>
        public static int InsertHSCCRivlParamByLast(int ccid, int lasthsccid)
        {
            int result = -1;
            if (ccid != 0)
            {
                try
                {
                    string sql_del = "delete from R_KinematicWave where ccid='" + ccid + "';delete from R_Muskingum where ccid='" + ccid + "';delete from R_Lag where ccid='" + ccid + "';";
                    PublicDAL.ExecuteSql(sql_del);
                    string sql_hsccrivl = @"select tt1.rvcd as rvcd,tt1.id as oldhmid,tt2.id as newhmid,tt2.routing as routing,tt2.loss as loss from 
(select * from hscc_rivl where ccid='" + lasthsccid + @"')tt1
,
(select * from hscc_rivl where ccid='" + ccid + @"')tt2
where tt1.rvcd=tt2.rvcd";
                    DataTable dt_hsccrivl = PublicDAL.GetData(sql_hsccrivl);

                    result = InsertRivParamlDataByLast(ccid, result, dt_hsccrivl);
                }

                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    PublicDAL.RollbackTransaction();
                }

            }
            return result;


        }
        public static int InsertRivParamlDataByLast(int ccid, int result, DataTable dt_hsccrivl)
        {

            if (dt_hsccrivl.Rows.Count > 0)
            {
                PublicDAL.BeginTransaction();
                for (int i = 0; i < dt_hsccrivl.Rows.Count; i++)
                {
                    string oldhmid = dt_hsccrivl.Rows[i]["oldhmid"].ToString();
                    string newhmid = dt_hsccrivl.Rows[i]["newhmid"].ToString();
                    string rvcd = dt_hsccrivl.Rows[i]["rvcd"].ToString();
                    string routing = dt_hsccrivl.Rows[i]["routing"].ToString();
                    #region 河道演进方法选择
                    string insert_routing = "";
                    switch (routing)
                    {
                        case "0":
                            break;
                        case "1"://运动波,其他参数全部默认了，如有修改再次添加字段
                            insert_routing = "insert into R_KinematicWave(hmid,ccid,rvlen,rvslp,subreaches,shape,diameter,width,sideslope,manning) select " + newhmid + "," + ccid + ",rvlen,rvslp,subreaches,shape,diameter,width,sideslope,manning from R_KinematicWave where hmid=" + oldhmid;
                            break;
                        case "2"://马斯京根
                            insert_routing = "insert into R_Muskingum(hmid,ccid,muskingumk,muskingumx,subreaches) select " + newhmid + "," + ccid + ",muskingumk,muskingumx,subreaches from R_Muskingum where hmid=" + oldhmid;
                            break;
                        case "3"://滞后演算
                            insert_routing = "insert into R_Lag(hmid,ccid,lagtime) select " + newhmid + "," + ccid + ",lagtime from  R_Lag where hmid=" + oldhmid;
                            break;
                        default:
                            break;
                    }
                    result += PublicDAL.ExecuteSql(insert_routing);
                    #endregion
                }
                PublicDAL.CommitTransacton();
            }

            return result;
        }
        #endregion

        #region 删除洪水场次，同时清空该洪水场次下的所以参数数据
        /// <summary>
        /// 删除洪水场次，同时清空该洪水场次下的所以参数数据
        /// </summary>
        /// <returns></returns>
        public static int DelHSCC(string ccid)
        {
            int result;
            try
            {
                string del_sql = "delete from hsjscc where id='" + ccid + @"';
                                  delete from HSCC_Reservoir where Ccid='" + ccid + @"';
                                  delete from HSCC_ResponseUnits where ccid='" + ccid + @"';                                      
                                  delete from HSCC_Rivl where CCID='" + ccid + @"';
                                  delete from HSCC_Wata where CCID='" + ccid + @"';
                                  delete from L_InitialConstant where ccid='" + ccid + @"';
                                  delete from L_GreenAmpt where ccid='" + ccid + @"';
                                  delete from L_SCSCurveNumber where ccid='" + ccid + @"';
                                  delete from L_SoilMoistureAcco where ccid='" + ccid + @"';
                                  delete from L_GreenAmpt_Trials where Ccid='" + ccid + @"';
                                  delete from L_InitialConstant_Trials where Ccid='" + ccid + @"';
                                  delete from L_SCSCurveNumber_Trials where Ccid='" + ccid + @"';
                                  delete from L_SoilMoistureAcco_Trials where Ccid='" + ccid + @"';
                                  delete from T_ClarkUnit where ccid='" + ccid + @"';
                                  delete from T_SCSUnit where ccid='" + ccid + @"';
                                  delete from T_UserSpecifiedUni where ccid='" + ccid + @"';
                                  delete from T_ClarkUnit_Trials where Ccid='" + ccid + @"';
                                  delete from T_SCSUnit_Trials where Ccid='" + ccid + @"';
                                  delete from C_SimpleCanopy where ccid='" + ccid + @"';
                                  delete from C_SimpleCanopy_Trials where Ccid='" + ccid + @"';
                                  delete from S_SimpleSurface where ccid='" + ccid + @"';
                                  delete from S_SimpleSurface_Trials where Ccid='" + ccid + @"';
                                  delete from B_ConstantMonthly where ccid='" + ccid + @"';
                                  delete from B_LinearReservoir where ccid='" + ccid + @"';
                                  delete from B_LinearReservoir_Trials where Ccid='" + ccid + @"';
                                  delete from Reservoir_InitalCondition where Ccid='" + ccid + @"';
                                  delete from R_KinematicWave where ccid='" + ccid + @"';
                                  delete from R_Muskingum where ccid='" + ccid + @"';
                                  delete from R_KinematicWave_Trials where Ccid='" + ccid + @"';
                                  delete from R_Lag_Trials where Ccid='" + ccid + @"';
                                  delete from R_Muskingum_Trials where Ccid='" + ccid + @"';
                                  delete from RivlParamHSCC where hsccID='" + ccid + @"';
                                  delete from R_Lag where ccid='" + ccid + "';";
                result = PublicDAL.ExecuteSql(del_sql);
            }
            catch (Exception ex)
            {
                result = 0;
                throw ex;
            }
            return result;


        }
        #endregion

        #region 查询流域响应单元
        /// <summary>
        /// 查询流域对应的响应单元
        /// </summary>
        /// <returns></returns>
        public static ObservableCollection<HSCC_ResponseUnits> GetHSCCResponseUnitsList(string ccid)
        {
            ObservableCollection<HSCC_ResponseUnits> UnitsList = new ObservableCollection<HSCC_ResponseUnits>();
            try
            {
                string sql = "select t1.*,t2.name as unit from hscc_responseunits t1,responseunits t2 where t1.[ucode]=t2.[code] and ccid='" + ccid + "'";
                DataTable dt = PublicDAL.GetData(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    UnitsList.Add(new HSCC_ResponseUnits()
                    {
                        ccid = dt.Rows[i]["ccid"].ToString(),
                        wscd = dt.Rows[i]["wscd"].ToString(),
                        ucode = dt.Rows[i]["ucode"].ToString(),
                        unit = dt.Rows[i]["unit"].ToString(),
                        ischeck = "False"
                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return UnitsList;

        }
        #endregion

        #region 查询响应单元类型
        /// <summary>
        /// 查询响应单元类型
        /// </summary>
        /// <returns></returns>
        public static DataTable GetResponseUnitsType()
        {
            DataTable dt = new DataTable();//数据列表
            try
            {
                string sql = "";
                sql = "select code,name from responseunits ";
                dt = PublicDAL.GetData(sql);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;//返回数据列表
        }
        #endregion

        #region 流域重提时清空相关所有数据
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">计算单元ID</param>
        /// <returns></returns>
        public static int DeleteData(string id)
        {
            int result = -1;
            DataTable dt = IngenieurinformatikDAL.GetHSCCID(id);
            try
            {
                string sql = "delete from Wata where CalculatingZone='" + id + @"';
                              delete from Wata_ResponseUnits where CalculatingZone='" + id + @"';
                              delete from Wata_USLU_SLTA where CalculatingZone='" + id + @"';
                              delete from Rivl where CalculatingZone='" + id + @"';
                              delete from Node where CalculatingZone='" + id + @"';                             
                              delete from SLTAAreaPro where CalculatingZone='" + id + @"';
                              delete from USLUAreaPro where CalculatingZone='" + id + @"';
                              delete from Wata_ParamDefault where CalculatingZone='" + id + @"';
                              delete from HSJSLD where CalculatingZone='" + id + @"';
                              delete from HSJSCC where CalculatingZone='" + id + "';";
                PublicDAL.ExecuteSql(sql);

                string hsccid = "";
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        hsccid = "'" + dt.Rows[i]["ID"].ToString() + "',";
                    }
                    hsccid = hsccid.Substring(0, hsccid.Length - 1);
                    string hsccsql = "delete from HSCC_Reservoir where Ccid in (" + hsccid + @");
                                      delete from HSCC_ResponseUnits where ccid in (" + hsccid + @");                                      
                                      delete from HSCC_Rivl where CCID in (" + hsccid + @");
                                      delete from HSCC_Wata where CCID in (" + hsccid + @");
                                      delete from L_InitialConstant where ccid in (" + hsccid + @");
                                      delete from L_GreenAmpt where ccid in (" + hsccid + @");
                                      delete from L_SCSCurveNumber where ccid in (" + hsccid + @");
                                      delete from L_SoilMoistureAcco where ccid in (" + hsccid + @");
                                      delete from L_GreenAmpt_Trials where Ccid in (" + hsccid + @");
                                      delete from L_InitialConstant_Trials where Ccid in (" + hsccid + @");
                                      delete from L_SCSCurveNumber_Trials where Ccid in (" + hsccid + @");
                                      delete from L_SoilMoistureAcco_Trials where Ccid in (" + hsccid + @");
                                      delete from T_ClarkUnit where ccid in (" + hsccid + @");
                                      delete from T_SCSUnit where ccid in (" + hsccid + @");
                                      delete from T_UserSpecifiedUni where ccid in (" + hsccid + @");
                                      delete from T_ClarkUnit_Trials where Ccid in (" + hsccid + @");
                                      delete from T_SCSUnit_Trials where Ccid in (" + hsccid + @");
                                      delete from C_SimpleCanopy where ccid in (" + hsccid + @");
                                      delete from C_SimpleCanopy_Trials where Ccid in (" + hsccid + @");
                                      delete from S_SimpleSurface where ccid in (" + hsccid + @");
                                      delete from S_SimpleSurface_Trials where Ccid in (" + hsccid + @");
                                      delete from B_ConstantMonthly where ccid in (" + hsccid + @");
                                      delete from B_LinearReservoir where ccid in (" + hsccid + @");
                                      delete from B_LinearReservoir_Trials where Ccid in (" + hsccid + @");
                                      delete from Reservoir_InitalCondition where Ccid in (" + hsccid + @");
                                      delete from R_KinematicWave where ccid in (" + hsccid + @");
                                      delete from R_Muskingum where ccid in (" + hsccid + @");
                                      delete from R_KinematicWave_Trials where Ccid in (" + hsccid + @");
                                      delete from R_Lag_Trials where Ccid in (" + hsccid + @");
                                      delete from R_Muskingum_Trials where Ccid in (" + hsccid + @");
                                      delete from RivlParamHSCC where hsccID in (" + hsccid + @");
                                      delete from R_Lag where ccid in (" + hsccid + @");";
                    result = PublicDAL.ExecuteSql(hsccsql);
                }
                string sql_VACUUM = "VACUUM [" + ClientConn.DataBaseName + "]";
                PublicDAL.ExecuteSql(sql_VACUUM);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;

        }
        #endregion


    }
}



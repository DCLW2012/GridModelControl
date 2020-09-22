using DBSupport;
using SysModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SysDAL
{
    public class Dal_GX
    {
        public static DataTable GetData(string sql)
        {
            DataTable dt = new DataTable();
            ClientConn.m_dataAccess.Open();
            try
            {
                dt = ClientConn.m_dataAccess.ExecuteDataSet(sql).Tables[0];
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                ClientConn.m_dataAccess.Close();
            }
            return dt;
        }

        public static int ExecuteSql(string sql)
        {
            int reslut;
            reslut = ClientConn.m_dataAccess.ExecuteNonQuery(sql);
            return reslut;
        }
        #region 流域

        #region 获取流域的算法
        /// <summary>
        /// 获取流域的算法
        /// </summary>
        /// <returns></returns>
        public static List<WATA_METHOD> GetWataMethodList(string czoneid)
        {
            List<WATA_METHOD> WATA_METHODList = new List<WATA_METHOD>();
            try
            {
                string sql = @"select t1.* from GX_WATA_METHOD t1,GX_WATA t2 where t1.wscd=t2.wscd and t2.UNITID='" + czoneid + "'";
                DataTable dt = GetData(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    WATA_METHODList.Add(new WATA_METHOD()
                    {
                        id = dt.Rows[i]["id"].ToString().Trim(),
                        canopy = dt.Rows[i]["canopy"].ToString().Trim(),
                        surface = dt.Rows[i]["surface"].ToString().Trim(),
                        loss = dt.Rows[i]["loss"].ToString().Trim(),
                        transform = dt.Rows[i]["transform"].ToString().Trim(),
                        baseflow = dt.Rows[i]["baseflow"].ToString().Trim(),
                        wscd = dt.Rows[i]["wscd"].ToString().Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return WATA_METHODList;
        }
        #endregion

        #region 植被截留参数
        /// <summary>
        /// 获取植被截留参数
        /// </summary>
        /// <returns></returns>
        public static DataTable GetCanopyParam(string wscd, string paramtype)
        {
            DataTable dt = new DataTable();//数据列表
            try
            {
                string sql = "";
                switch (paramtype)
                {
                    case "1":
                        sql = "select * from GX_C_SimpleCanopy where wscd='" + wscd + "'";
                        break;
                    default:
                        break;
                }

                dt = GetData(sql);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;//返回数据列表

        }
        #endregion

        #region 洼蓄参数
        /// <summary>
        /// 获取洼蓄参数列表
        /// </summary>
        /// <returns></returns>
        public static DataTable GetSurfaceParam(string wscd, string paramtype)
        {

            DataTable dt = new DataTable();//数据列表
            try
            {
                string sql = "";
                switch (paramtype)
                {
                    case "1":
                        sql = "select * from GX_S_SimpleSurface where wscd='" + wscd + "'";//简单洼蓄
                        break;
                    default:
                        break;
                }

                dt = GetData(sql);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;//返回数据列表
        }
        #endregion

        #region 扣损参数
        /// <summary>
        /// 获取扣损参数列表
        /// </summary>
        /// <returns></returns>
        public static DataTable GetLossParam(string wscd, string paramtype)
        {

            DataTable dt = new DataTable();//数据列表
            try
            {
                string sql = "";
                switch (paramtype)
                {
                    case "1":

                        sql = "select * from GX_L_InitialConstant where wscd='" + wscd + "'";//初损后损

                        break;
                    case "2":

                        sql = "select * from GX_L_GreenAmpt where wscd='" + wscd + "'";//格林安普

                        break;
                    case "3":

                        sql = "select * from GX_L_SoilMoistureAcco where wscd='" + wscd + "'";//土壤湿度考虑法

                        break;
                    case "4":

                        sql = "select * from GX_L_SCSCurveNumber where wscd='" + wscd + "'";//SCS指数法

                        break;


                    default:
                        break;
                }

                dt = GetData(sql);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;//返回数据列表

        }
        #endregion

        #region 汇流参数
        /// <summary>
        /// 获取汇流参数列表
        /// <returns></returns>
        public static DataTable GetTransformParam(string wscd, string paramtype)
        {

            DataTable dt = new DataTable();//数据列表
            try
            {
                string sql = "";
                switch (paramtype)
                {
                    case "1"://scs单位线

                        sql = "select * from GX_T_SCSUnit where wscd='" + wscd + "'";//scs单位线

                        break;
                    case "2"://用户自定义单位线

                        sql = "select * from GX_T_UserSpecifiedUni where wscd='" + wscd + "'";//用户自定义单位线

                        break;
                    case "3"://克拉克单位线

                        sql = "select * from GX_T_ClarkUnit where wscd='" + wscd + "'";//克拉克

                        break;


                    default:
                        break;
                }

                dt = GetData(sql);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;//返回数据列表

        }
        #endregion

        #region 基流参数
        /// <summary>
        /// 获取基流参数列表
        /// </summary>
        /// <returns></returns>
        public static DataTable GetBaseflowParam(string wscd, string paramtype)
        {

            DataTable dt = new DataTable();//数据列表
            try
            {
                string sql = "";
                switch (paramtype)
                {
                    case "1"://单月常数

                        sql = "select * from GX_B_ConstantMonthly where wscd='" + wscd + "'";//单月常数

                        break;
                    case "2"://线性水库

                        sql = "select * from GX_B_LinearReservoir where wscd='" + wscd + "'";//线性水库

                        break;

                    default:
                        break;
                }

                dt = GetData(sql);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;//返回数据列表

        }

        #endregion
        #endregion

        #region 河道
        #region 获取河道的算法
        /// <summary>
        /// 获取河道的算法
        /// </summary>
        /// <returns></returns>
        public static List<RIVL_METHOD> GetRivlMethodList(string czoneid)
        {
            List<RIVL_METHOD> RivlMethodList = new List<RIVL_METHOD>();
            try
            {
                string sql = @"select t1.* from GX_RIVL_METHOD t1,GX_RIVL t2 where t1.rvcd=t2.rvcd and t2.UNITID='" + czoneid + "' ";
                DataTable dt = GetData(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    RivlMethodList.Add(new RIVL_METHOD()
                    {
                        id = dt.Rows[i]["id"].ToString().Trim(),
                        routing = dt.Rows[i]["routing"].ToString().Trim(),
                        rvcd = dt.Rows[i]["rvcd"].ToString().Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return RivlMethodList;
        }
        #endregion
        #region 河道算法参数
        /// <summary>
        /// 获取河道演进参数列表
        /// </summary>
        /// <returns></returns>
        public static DataTable GetRoutingParam(string rvcd, string paramtype)
        {

            DataTable dt = new DataTable();//数据列表
            try
            {
                string sql = "";
                switch (paramtype)
                {
                    case "1":

                        sql = "select * from GX_R_KinematicWave where rvcd='" + rvcd + "'";//运动波

                        break;
                    case "2":

                        sql = "select * from GX_R_Muskingum where rvcd='" + rvcd + "'";//马斯京根

                        break;
                    case "3":

                        sql = "select * from GX_R_Lag where rvcd='" + rvcd + "'";//滞后演算

                        break;
                    default:
                        break;
                }

                dt = GetData(sql);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;//返回数据列表

        }
        #endregion
        #region 获取河道点坐标
        /// <summary>
        /// 获取河道点坐标
        /// </summary>
        /// <param name="rvcd"></param>
        /// <returns></returns>
        public static string GetRivlPoints(string rvcd)
        {
            DataTable dt = new DataTable();//数据列表
            string rivlpoints = "";
            try
            {
                string sql = "select geom.STAsText() as geom from GX_RIVL where rvcd='" + rvcd + "'";//查找河道点坐标

                dt = GetData(sql);
                rivlpoints = dt.Rows[0]["geom"].ToString();
                rivlpoints = rivlpoints.Replace("LINESTRING (", "").Replace(")", "");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return rivlpoints;
        }
        #endregion
        #endregion
        #region 节点
        /// <summary>
        /// 获取节点数据
        /// </summary>
        /// <returns></returns>
        public static List<Node> GetNode(string czoneid)
        {
            List<Node> NodeList = new List<Node>();
            DataTable dt = new DataTable();//数据列表
            try
            {
                string sql = "select * from GX_NODE where UNITID='" + czoneid + "'";//滞后演算

                dt = GetData(sql);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                NodeList.Add(new Node()
               {
                   fndcd = dt.Rows[i]["fndcd"].ToString().Trim().Trim(),
                   ndcd = dt.Rows[i]["ndcd"].ToString().Trim().Trim(),
                   ndnm = dt.Rows[i]["ndnm"].ToString().Trim(),
                   ndx = dt.Rows[i]["ndx"].ToString().Trim(),
                   ndy = dt.Rows[i]["ndy"].ToString().Trim(),
                   tndcd = dt.Rows[i]["tndcd"].ToString().Trim(),
                   ID = dt.Rows[i]["ID"].ToString().Trim()
               });

            }
            return NodeList;//返回数据列表
        }

        #endregion
        #region 查询计算单元内所有的雨量站
        /// <summary>
        /// 查询所有的雨量站
        /// </summary>
        public static List<Site> GetSite(string czoneid)
        {
            List<Site> SiteList = new List<Site>();
            try
            {
                string sql = "select t1.stcd,lgtd,lttd from ST_STBPRP_B t1,GX_UNIT_PPTN t2 where t1.STCD=t2.STCD and t2.UNITID='" + czoneid + "' and t1.sttp='PP'";

                DataTable dt = GetData(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    SiteList.Add(new Site()
                    {
                        code = dt.Rows[i]["stcd"].ToString(),
                        lgtd = dt.Rows[i]["lgtd"].ToString(),
                        lttd = dt.Rows[i]["lttd"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return SiteList;
        }
        #endregion

        public static DataTable GetNodeADCD(string ndcd)
        {
            DataTable dt = new DataTable();
            string sql = "select ndcd,adcd from GX_node_ad  where ndcd='" + ndcd + "'";
            try
            {
                dt = GetData(sql);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;

        }
        /// <summary>
        /// 查询该计算单元下水位流量关系
        /// </summary>
        /// <param name="ndcd">节点</param>
        /// <returns></returns>
        public static DataTable GetStageDischarge(string ndcd)
        {
            DataTable dt = new DataTable();
            string sql = "select t1.ndcd,t1.adcd,t2.h,t2.f from GX_node_ad t1,ia_m_wflow t2 where t1.adcd=t2.adcd and t1.ndcd='" + ndcd + "'order by adcd,h";
            try
            {
                dt = GetData(sql);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;
        }
        /// <summary>
        /// 获取水位预警阀值信息
        /// </summary>
        /// <returns></returns>
        public static DataTable GetStageWarning(string adcd)
        {
            DataTable dt = new DataTable();
            string sql = @"select tt1.ndcd,tt1.adcd,tt1.wlevel as warninglevel,tt2.wlevel as dangerlevel from 
(select t1.ndcd,t2.adcd,t2.wlevel from GX_NODE_AD t1, GX_ADWWarnRule t2 where t1.adcd=t2.adcd and WarnGradeID='20' and t1.adcd='" + adcd + @"') tt1,
(select t1.ndcd,t2.adcd,t2.wlevel from GX_NODE_AD t1, GX_ADWWarnRule t2 where t1.adcd=t2.adcd and WarnGradeID='21' and t1.adcd='" + adcd + @"') tt2 where tt1.adcd=tt2.adcd and tt1.ndcd=tt2.ndcd 
";
            try
            {
                dt = GetData(sql);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;
        }
        /// <summary>
        /// 获取流量预警阀值信息
        /// </summary>
        /// <returns></returns>
        public static DataTable GetDischargeWarning(string adcd)
        {
            DataTable dt = new DataTable();
            string sql = @"select tt1.ndcd,tt1.adcd,tt1.flow as warningflow,tt2.flow as dangerflow from 
(select t1.ndcd,t2.adcd,t2.flow from GX_NODE_AD t1, GX_ADFWarnRule t2 where t1.adcd=t2.adcd and WarnGradeID='30' and t1.adcd='" + adcd + @"') tt1,
(select t1.ndcd,t2.adcd,t2.flow from GX_NODE_AD t1, GX_ADFWarnRule t2 where t1.adcd=t2.adcd and WarnGradeID='31' and t1.adcd='" + adcd + @"') tt2 where tt1.adcd=tt2.adcd and tt1.ndcd=tt2.ndcd";
            try
            {
                dt = GetData(sql);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dt;
        }
        /// <summary>
        /// 测试数据库连接放方法
        /// </summary>
        /// <returns></returns>
        public static string GetTest()
        {
            DataTable dt = ClientConn.m_dataAccess_rain.ExecuteDataSet("select count(1) noum from GX_NODE_FLOW ").Tables[0];
            Console.WriteLine(dt.Rows[0]["NOUM"]);
            return dt.Rows[0]["NOUM"] + "";
        }

        #region 导入沿河村落流量状态数据
        /// <summary>
        /// 导入沿河村落流量状态数据
        /// </summary>
        public static void insertFState(List<AdcdFState> AdcdFStateList)
        {
            try
            {
                int result = 0;
                string sql = "";

                if (AdcdFStateList.Count > 0)
                {
                    for (int i = 0; i < AdcdFStateList.Count; i++)
                    {
                        sql += string.Format("insert into GX_FState (warnstate,warndt,warnflow,flowdt,flowval,caldt,adcd,pubcode,currentstate)values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}');", AdcdFStateList[i].state, AdcdFStateList[i].warndt, AdcdFStateList[i].warnvalue, AdcdFStateList[i].waterdt, AdcdFStateList[i].waterflow, AdcdFStateList[i].jstime, AdcdFStateList[i].adcd, AdcdFStateList[i].pubcode, AdcdFStateList[i].currentstate);
                        result++;
                    }
                }
                if (sql != "")
                {
                    Dal.ExecuteSql(sql);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {

            }
        }
        #endregion

        #region 导入沿河村落水位状态数据
        /// <summary>
        /// 导入沿河村落水位状态数据
        /// </summary>
        public static void insertWState(List<AdcdWState> AdcdWStateList)
        {
            try
            {
                int result = 0;
                string sql = "";
                if (AdcdWStateList.Count > 0)
                {
                    for (int i = 0; i < AdcdWStateList.Count; i++)
                    {
                        sql += string.Format("insert into GX_WState (warnstate,warndt,warnwater,waterdt,waterval,caldt,adcd,pubcode,currentstate)values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}');", AdcdWStateList[i].state, AdcdWStateList[i].warndt, AdcdWStateList[i].warnvalue, AdcdWStateList[i].waterdt, AdcdWStateList[i].waterlevel, AdcdWStateList[i].jstime, AdcdWStateList[i].adcd, AdcdWStateList[i].pubcode, AdcdWStateList[i].currentstate);
                        result++;
                    }
                }
                if (sql != "")
                {
                    Dal.ExecuteSql(sql);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {

            }
        }
        #endregion

        /// <summary>
        /// 删除计算流量水位
        /// </summary>
        public static void DelStageDischarge(string pubcode)
        {
            string sql_del = "delete from GX_ADCD_FWL where pubcode='" + pubcode + "'";
            Dal.ExecuteSql(sql_del);
        }

        #region 导入节点数据
        /// <summary>
        /// 导入节点结果
        /// </summary>
        public static void insertNodeResult(List<NodeResult> NodeResultList,string pubcode)
        {
            try
            {
                int result = 0;
                string sql = "";
                Console.WriteLine(NodeResultList.Count);
                if (NodeResultList.Count > 0)
                {
                    for (int i = 0; i < NodeResultList.Count; i++)
                    {
                        sql += string.Format("insert into GX_NODE_FLOW (node,dt,flow,waterlevel,pubcode)values ('{0}','{1}','{2}','{3}','{4}');", NodeResultList[i].ndcd, NodeResultList[i].dtime, NodeResultList[i].OutVal, NodeResultList[i].OutLevel, pubcode);
                        result++;
                    }
                }
                if (sql != "")
                {
                    Dal.ExecuteSql(sql);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {

            }
        }
        #endregion
        #region 导入节点对应沿河村落结果
        /// <summary>
        /// 导入节点对应沿河村落结果
        /// </summary>
        public static void insertADCDResult(List<ADCDResult> ADCDResultList,string pubcode)
        {
            try
            {
                int result = 0;
                string sql = "";
                Console.WriteLine(ADCDResultList.Count);
                if (ADCDResultList.Count > 0)
                {
                    for (int i = 0; i < ADCDResultList.Count; i++)
                    {
                        sql += string.Format("insert into GX_ADCD_FWL (adcd,dt,flow,waterlevel,pubcode)values ('{0}','{1}','{2}','{3}','{4}');", ADCDResultList[i].adcd, ADCDResultList[i].dtime, ADCDResultList[i].OutVal, ADCDResultList[i].OutLevel, pubcode);
                        result++;
                    }
                }
                if (sql != "")
                {
                    Dal.ExecuteSql(sql);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {

            }
        }
        #endregion

        /// <summary>
        /// 删除节点流量
        /// </summary>
        public static void DelNodeFlow(string pubcode)
        {
            string sql_del = "delete from GX_NODE_FLOW where pubcode='" + pubcode + "'";
            Dal.ExecuteSql(sql_del);
        }
        
        /// <summary>
        /// 获取断面点坐标
        /// </summary>
        /// <returns></returns>
        public static DataTable GetCrossPoint(string ndcd)
        {
            DataTable dt = new DataTable();
            string sql = "select t2.adcd,t1.hecd,t1.ele,t1.lgtd,t1.lttd,coeff from ia_m_hspoint t1, ia_m_hsurface t2,gx_node_ad t3 where t1.hecd=t2.hecd and t2.adcd=t3.adcd and t3.ndcd='"+ndcd+"'";
            try
            {
                dt = GetData(sql);
            }
            catch (Exception)
            {
                
                throw;
            }
            return dt;
        }
        /// <summary>
        /// 查询纵断面
        /// </summary>
        /// <param name="ndcd"></param>
        /// <returns></returns>
        public static DataTable GetVCrossPoint(string adcd)
        {
            DataTable dt = new DataTable();
            string sql = "select t2.adcd,t1.ele,t1.cdistance from ia_m_vspoint t1, ia_m_vsurface t2 where t1.vecd=t2.vecd and t2.adcd='"+adcd+"'";
            try
            {
                dt = GetData(sql);
            }
            catch (Exception)
            {

                throw;
            }
            return dt;
        }

        #region 导入节点数据
        /// <summary>
        /// 导入节点结果
        /// </summary>
        public static void insertNodeResult_N(List<NodeResult> NodeResultList, string pubcode)
        {
            try
            {
                int result = 0;
                string sql = "";
               // string resulttext = "";
                string node = "";
                Console.WriteLine(NodeResultList.Count);
                if (NodeResultList.Count > 0)
                {
                    string resulttext = "";
                    for (int i = 0; i < NodeResultList.Count; i++)
                    {
                        node = NodeResultList[i].ndcd.ToString();
                        if (i==0)
                        {
                            resulttext = NodeResultList[i].OutVal + "," + NodeResultList[i].dtime + ";";
                        }
                        else 
                        {
                            if (node==NodeResultList[i-1].ndcd)
                            {
                                resulttext += NodeResultList[i].OutVal + "," + NodeResultList[i].dtime + ";";
                                if (i==NodeResultList.Count-1)
                                {
                                    sql += string.Format("insert into GX_NODE_FLOW_N (NODE,PubCode,Result)values ('{0}','{1}','{2}');", NodeResultList[i - 1].ndcd, pubcode, resulttext);  
                                }
                            }
                            else
                            {
                                
                                sql += string.Format("insert into GX_NODE_FLOW_N (NODE,PubCode,Result)values ('{0}','{1}','{2}');", NodeResultList[i - 1].ndcd, pubcode, resulttext);
                                resulttext = "";
                                resulttext = NodeResultList[i].OutVal + "," + NodeResultList[i].dtime + ";";
                                if (i == NodeResultList.Count - 1)
                                {
                                    sql += string.Format("insert into GX_NODE_FLOW_N (NODE,PubCode,Result)values ('{0}','{1}','{2}');", NodeResultList[i - 1].ndcd, pubcode, resulttext);
                                }
                            }
                        }
                        //sql += string.Format("insert into GX_NODE_FLOW (node,dt,flow,waterlevel,pubcode)values ('{0}','{1}','{2}','{3}','{4}');", NodeResultList[i].ndcd, NodeResultList[i].dtime, NodeResultList[i].OutVal, NodeResultList[i].OutLevel, pubcode);
                        //result++;
                    }
                }
                if (sql != "")
                {
                    Dal.ExecuteSql(sql);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {

            }
        }
        #endregion
    }
}

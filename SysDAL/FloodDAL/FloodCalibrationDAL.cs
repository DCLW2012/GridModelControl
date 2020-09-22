using SysModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SysDAL
{
    public class FloodCalibrationDAL
    {
        #region 判断率定项是否已经存在
        /// <summary>
        /// 判断率定项是否已经存在
        /// </summary>
        /// <returns></returns>
        public static bool IsHaveLD(string name)
        {
            bool result = false;
            string sql = "select name from hsjsld where name='" + name + "'";
            DataTable dt = PublicDAL.GetData(sql);
            if (dt.Rows.Count > 0)
            {
                result = true;
            }
            return result;
        }
        #endregion
        #region 创建流域率定使用参数
        /// <summary>
        /// 创建方法对应的默认参数
        /// </summary>
        /// <returns></returns>
        public static int InsertHSCCRatingParam(int ldid,int ccid)
        {
            int result = -1;
            if (ldid != 0)
            {
                try
                {
                    #region 清空参数数据
                    string sql_del = "delete from L_InitialConstant_Trials where RatingID in(" + ldid + ");delete from L_GreenAmpt_Trials where  RatingID in(" + ldid + ");delete from L_SCSCurveNumber_Trials where RatingID in(" + ldid + ");delete from L_SoilMoistureAcco_Trials where RatingID in(" + ldid + "); delete from T_ClarkUnit_Trials where RatingID in(" + ldid + ");delete from T_SCSUnit_Trials where RatingID in(" + ldid+ ");delete from C_SimpleCanopy_Trials where RatingID in(" + ldid+ ");delete from S_SimpleSurface_Trials where RatingID in(" + ldid+ ");delete from B_LinearReservoir_Trials where RatingID in(" + ldid+ ");";
                    PublicDAL.ExecuteSql(sql_del);
                    #endregion
                    #region 根据洪水场次查询 流域使用方法
                    

                    result = InsertRatingParamData(ccid,ldid,result);

                    #endregion

                }
                catch (Exception ex)
                {

                    throw ex;
                }
                finally
                {
                  
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
        private static int InsertRatingParamData(int ccid, int ldid,int result)
        {
             

                    #region 写入植被截留参数（canopy）
                    string sql_canopy = "";

                    sql_canopy = "insert into C_SimpleCanopy_Trials(TrialsID,RatingID,InitialStroage,Max_InitialStroage,Min_InitialStroage,MaxStroage,Max_MaxStroage,Min_MaxStroage) select hmid,"+ldid+",InitialStroage,100.00,0.001,MaxStroage,1500.0000,0.0010 from C_SimpleCanopy where ccid='" + ccid + "'";
                             
                    
                    result += PublicDAL.ExecuteSql(sql_canopy);

                    #endregion

                    #region 写入洼蓄参数（surface）
                    string sql_surface = "";

                    sql_surface = "insert into S_SimpleSurface_Trials(TrialsID,RatingID,InitialStroage,Max_InitialStroage,Min_InitialStroage,MaxStroage,Max_MaxStroage,Min_MaxStroage) select hmid," + ldid + ",InitialStroage,100.00,0.001,MaxStroage,1500.0000,0.0010 from S_SimpleSurface where ccid='" + ccid + "'";
                            

                       
                    result += PublicDAL.ExecuteSql(sql_surface);


                    #endregion

                    #region 写入扣损参数（loss）
                    string sql_loss = "";
                    
                          //初损后损
                    sql_loss += "insert into L_InitialConstant_Trials(initialloss,constantrate,TrialsID,RatingID,Max_Initialloss,Min_Initialloss,Max_Constantrate,Min_Constantrate)  select initialloss,constantrate,hmid," + ldid + ",1000.0000,0.0010,300.0000,0.001 from L_InitialConstant where ccid='" + ccid + "';";
                         
                         //格林安普
                    sql_loss += "insert into L_GreenAmpt_Trials(Initialcontent,Saturatedcontent,Suction,Conductivity,TrialsID,RatingID,Max_Initialcontent,Min_Initialcontent,Max_Saturatedcontent,Min_Saturatedcontent,Max_Suction,Min_Suction,Max_Conductivity,Min_Conductivity) select Initialcontent,Saturatedcontent,Suction,Conductivity,hmid," + ldid + ",0.5000,0.001,0.6000,0.001,4000.0000,0.001,250.0000,0.001 from L_GreenAmpt where ccid='" + ccid + "';";
                           
                         //SCS指数法
                    sql_loss += "insert into L_SCSCurveNumber_Trials(TrialsID,RatingID,InitialAbstraction, CurveNumber,Max_InitialAbstraction,Min_InitialAbstraction,Max_CurveNumber,Min_CurveNumber) select hmid," + ldid + ", InitialAbstraction, CurveNumber,500.0000,0.001,99.00,35 from L_SCSCurveNumber where ccid='" + ccid + "';";
                           
                         //土壤湿度考虑法
                    sql_loss += "insert into L_SoilMoistureAcco_Trials(TrialsID,RatingID,Soil,Groundwater1,Groundwater2,MaxInfiltration,SoilStorage,TensionStorage,SoilPercolation,GW1Storage,GW1Percolation,GW1Coefficient,GW2Storage,GW2Percolation,GW2Coefficient,Max_Soil,Min_Soil,Max_Groundwater1,Min_Groundwater1,Max_Groundwater2,Min_Groundwater2,Max_MaxInfiltration,Min_MaxInfiltration,Max_SoilStorage,Min_SoilStorage,Max_TensionStorage,Min_TensionStorage,Max_SoilPercolation,Min_SoilPercolation,Max_GW1Storage,Min_GW1Storage,Max_GW1Percolation,Min_GW1Percolation,Max_GW1Coefficient,Min_GW1Coefficient,Max_GW2Storage,Min_GW2Storage,Max_GW2Percolation,Min_GW2Percolation,Max_GW2Coefficient,Min_GW2Coefficient) select hmid," + ldid + ",Soil,Groundwater1,Groundwater2,MaxInfiltration,SoilStorage,TensionStorage,SoilPercolation,GW1Storage,GW1Percolation,GW1Coefficient,GW2Storage,GW2Percolation,GW2Coefficient,100.00,0.001,100.00,0.001,100.00,0.001,500.0000,0.01,1500.0000,0.01,1500.0000,0.01,500.0000,0.01,1500.0000,0.01,500.0000,0.01,10000.0000,0.01,1500.0000,0.01,500.0000,0.01,10000.0000,0.01 from L_SoilMoistureAcco where ccid='" + ccid + "';";
                            
                    result += PublicDAL.ExecuteSql(sql_loss);
                    #endregion

                    #region 写入汇流参数（transform）
                    string sql_transform = "";
                    //SCS单位线
                    sql_transform += "insert into T_SCSUnit_Trials(lagtime,TrialsID,RatingID,Max_LagTime,Min_LagTime) select lagtime,hmid," + ldid + ",30000.000,0.0010 from T_SCSUnit where ccid='"+ccid+"';";
                           

                        //用户自定义单位线

                           

                 //克拉克单位线
                    sql_transform += "insert into T_ClarkUnit_Trials(Concentrationtime,Storagecoefficient,TrialsID,RatingID,Max_Concentrationtime,Min_Concentrationtime,Max_Storagecoefficient,Min_Storagecoefficient) select Concentrationtime,Storagecoefficient,hmid," + ldid + ",1000.0000,0.0167,1000.0000,0.01 from T_ClarkUnit where ccid='"+ccid+"';";
                           
                      
                    result += PublicDAL.ExecuteSql(sql_transform);
                    #endregion

                    #region 写入基流参数（baseflow）
                    string sql_baseflow = "";
                     
                    
                         //无
                           
                        //单月常数

                           
                       //线性水库
                    sql_baseflow = "insert into B_LinearReservoir_Trials(TrialsID,RatingID,GW1Initial, GW1Coefficient, GW1Reservoirs,Max_GW1Initial,Min_GW1Initial,Max_GW1Coefficient,Min_GW1Coefficient,Max_GW1Reservoirs,Min_GW1Reservoirs) select hmid," + ldid + ", GW1Initial, GW1Coefficient, GW1Reservoirs,100000.0000,0.0000,10000.0000,0.0090,100,1 from B_LinearReservoir where ccid='" + ccid + "'";
                            
                   
                    result += PublicDAL.ExecuteSql(sql_baseflow);


                    #endregion


                
                PublicDAL.CommitTransacton();
           

            return result;
        }
        #endregion

        #region 创建河道率定使用参数

        /// <summary>
        /// 创建河道默认使用参数
        /// </summary>
        /// <returns></returns>
        public static int InsertHSCCRivlParam(int ldid,int ccid)
        {
            int result = -1;
            if (ldid != 0)
            {
                try
                {
                    string sql_del = "delete from R_KinematicWave_Trials where RatingID='" + ldid+ "';delete from R_Muskingum_Trials where RatingID='" + ldid+ "';delete from R_Lag_Trials where RatingID='" + ldid+ "';";
                    PublicDAL.ExecuteSql(sql_del);
                   
                    result = InsertRivParamlData(ldid, ccid, result);
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
        

        //}
        /// <summary>
        /// 更新河道使用参数
        /// </summary> 
        /// <returns></returns>
        public static int InsertRivParamlData(int ldid, int ccid, int result)
        {

            string insert_routing = "";
                        
                        //运动波, 
            insert_routing += "insert into R_KinematicWave_Trials(TrialsID,RatingID,manning,Min_manning,Max_manning) select hmid," + ldid + ",manning,0.0001000,1.0000000 from R_KinematicWave where ccid='" + ccid + "';";
                            //马斯京根
            insert_routing += "insert into R_Muskingum_Trials(TrialsID,RatingID,muskingumk,subreaches,muskingumx,Min_muskingumk,Max_muskingumk,Min_subreaches,Max_subreaches,Min_muskingumx,Max_muskingumx) select hmid,"+ldid+",muskingumk,subreaches,muskingumx,0.001,150.0000,1,100,0.001,0.5000 from R_Muskingum where ccid='" + ccid + "';";
                            
                       //滞后演算
            insert_routing += "insert into R_Lag_Trials(TrialsID,RatingID,lagtime,Min_lagtime,Max_lagtime) select hmid,"+ldid+",lagtime,0.001,30000.0000 from R_Lag where ccid='"+ccid+"';";
                           
                        
                    result += PublicDAL.ExecuteSql(insert_routing);
               

            return result;
        }

        #endregion

        

        #region 更新率定场次

        public static int UpdateLDCC(HSJSLD hsld)
         {
            int result = 0;
            try
            {
                string sql = "update HSJSLD set name='" + hsld.Name + "',hscc='" + hsld.HSCC + "',starttime='" + hsld.StartTime + "',endtime='" + hsld.EndTime + "',AllowError='" + hsld.AllowError + "',MaxIteration='" + hsld.MaxIteration + "',timeinterval='" + hsld.TimeInterval + "',ylinterval='" + hsld.YLInterval + "',llinterval='" + hsld.LLInterval + "',localtion='" + hsld.Localtion + "' where id=" + hsld.ID + "";
 
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

        #region 删除率定设定，根据率定ID删除
        /// <summary>
        /// 删除率定设定，根据率定ID删除
        /// </summary>
        /// <param name="ratingid"></param>
        /// <returns></returns>
        public static int DelHSLD(string ratingid)
        {
            int result;
            try
            {
                string del_sql = "delete from hsjsld where id='" + ratingid + @"';
                                  delete from L_GreenAmpt_Trials where RatingID='" + ratingid + @"';
                                  delete from L_InitialConstant_Trials where RatingID='" + ratingid + @"';
                                  delete from L_SCSCurveNumber_Trials where RatingID='" + ratingid + @"';
                                  delete from L_SoilMoistureAcco_Trials where RatingID='" + ratingid + @"';
                                  delete from T_ClarkUnit_Trials where RatingID='" + ratingid + @"';
                                  delete from T_SCSUnit_Trials where RatingID='" + ratingid + @"'; 
                                  delete from C_SimpleCanopy_Trials where RatingID='" + ratingid + @"'; 
                                  delete from S_SimpleSurface_Trials where RatingID='" + ratingid + @"'; 
                                  delete from B_LinearReservoir_Trials where RatingID='" + ratingid + @"';   
                                  delete from R_KinematicWave_Trials where RatingID='" + ratingid + @"';
                                  delete from R_Lag_Trials where RatingID='" + ratingid + @"';
                                  delete from R_Muskingum_Trials where RatingID='" + ratingid + @"'"; 
                                  
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



    }
}

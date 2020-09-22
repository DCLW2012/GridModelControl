using SysDAL;
using SysModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Common;
namespace Common
{
    /// <summary>
    /// 判断预警状态
    /// </summary>
    public class JudgeState
    {

        //public static List<AdcdFState> AdcdFStateList = new List<AdcdFState>();
        //public static List<AdcdWState> AdcdWStateList = new List<AdcdWState>();

        /// <summary>
        /// 进行判断
        /// </summary>
        public static void Judge(string adcd)
        {
            JudgeDischargeState(adcd);
            JudgeStageState(adcd);
        }

        /// <summary>
        /// 判断流量的预警状态
        /// </summary>
        public static void JudgeDischargeState(string adcd)
        {
            DataTable dt_warn = SysDAL.Dal.GetDischargeWarning(adcd);

            //！ 分析当前计算时间点的预警值及状态，分析峰值时间点对应的值及预警状态
            //！ 将这两种情况下的预警结果传给预警状态值中写入数据库flowstate、flowdttime、flow

            for (int i = 0; i < dt_warn.Rows.Count; i++)
            {
                double warningflow = double.Parse(dt_warn.Rows[i]["warningflow"].ToString());
                double dangerflow = double.Parse(dt_warn.Rows[i]["dangerflow"].ToString());

                string flowstate = "10";
                string flowdttime = "1900-01-01 00:00:00";
                string flow = "0";
                string ndcd = dt_warn.Rows[i]["ndcd"].ToString();
                List<ResulMax> listC = ResultImport.ResultMaxList.Where(o => o.code == ndcd).ToList();
               
               if(listC.Count == 0)
               {
                   continue;
               }

                string maxvalue = listC[0].maxval;
                string maxdt = listC[0].dtime;
                

                //! 沿河村落当前时间对应的计算结果----------------------------------------------------------
                string currentstate = "10";
                string currentFlowdttime = "1900-01-01 00:00:00";
                List<ADCDResult> ADCDResultListC = ResultImport.ADCDResultList.Where(o => DateTime.Parse(o.dtime).ToString("yyyy/MM/dd HH:mm") == DateTime.Parse(HookHelper.JSTime).ToString("yyyy/MM/dd HH:mm")).ToList();
                double val_currentVal = 0;
                if (ADCDResultListC.Count > 0)
                {
                    val_currentVal = double.Parse(ADCDResultListC[0].OutVal);
                    currentFlowdttime = ADCDResultListC[0].dtime;
                }
                if (val_currentVal >= dangerflow)
                {
                    currentstate = "31";
                }
                else if (val_currentVal >= warningflow)
                {
                    currentstate = "30";
                }
                else
                {
                    currentstate = "10";
                }
                //! 沿河村落当前时间对应的计算结果----------------------------------------------------------

                // 按流量值从大到小排序,计算峰值相关信息
                ResultImport.ADCDResultList.Sort(CompareByFlowVal);

                if (ResultImport.ADCDResultList.Count > 0)
                {
                    double val = double.Parse(ResultImport.ADCDResultList[0].OutVal);
                    if (val >= dangerflow)
                    {
                        flowstate = "31";
                        flowdttime = ResultImport.ADCDResultList[0].dtime;
                        flow = val.ToString();
                    }
                    else if (val >= warningflow)
                    {
                        flowstate = "30";
                        flowdttime = ResultImport.ADCDResultList[0].dtime;
                        flow = val.ToString();
                    }
                }

                //! 写出预警结果
                if (flowstate != "10")
                {
                    DataRow dr_fstate = ResultImport.FSateResultTable.NewRow();
                    dr_fstate[1] = int.Parse(flowstate);
                    dr_fstate[2] = DateTime.Parse(flowdttime);
                    dr_fstate[3] = float.Parse((flow == "") ? "0" : flow);
                    dr_fstate[4] = DateTime.Parse((maxdt == "") ? "1900-01-01 00:00:00" : maxdt);
                    dr_fstate[5] = float.Parse(maxvalue);
                    dr_fstate[6] = DateTime.Parse(HookHelper.JSTime);
                    dr_fstate[7] = HookHelper.PubCode;
                    dr_fstate[8] = adcd;
                    dr_fstate[9] = int.Parse(flowstate);
                    dr_fstate[10] = DateTime.Parse(HookHelper.JS_StartTime);
                    ResultImport.FSateResultTable.Rows.Add(dr_fstate);
                }

                

            }
        }

        /// <summary>
        /// 判断水位的预警状态
        /// </summary>
        public static void JudgeStageState(string adcd)
        {
            DataTable dt_warn = SysDAL.Dal.GetStageWarning(adcd);

            for (int i = 0; i < dt_warn.Rows.Count; i++)
            {
                double warninglevel = double.Parse(dt_warn.Rows[i]["warninglevel"].ToString());
                double dangerlevel = double.Parse(dt_warn.Rows[i]["dangerlevel"].ToString());
                string levelstate = "10";
                string leveldttime = "1900-01-01 00:00:00";
                string level = "0";
                string ndcd = dt_warn.Rows[i]["ndcd"].ToString();

                List<ResulMax> listC = ResultImport.ResultMaxList.Where(o => o.code == ndcd).ToList();

                if (listC.Count == 0)
                {
                    continue;
                }
                string maxvalue = listC[0].maxval.Trim();
                string maxdt = ResultImport.ResultMaxList.Where(o => o.code == ndcd).ToList()[0].dtime.Trim();
                List<ADCDResult> ADCDResultListC2 = ResultImport.ADCDResultList.Where(o => DateTime.Parse(o.dtime).ToString("yyyy/MM/dd HH:mm") == DateTime.Parse(HookHelper.JSTime).ToString("yyyy/MM/dd HH:mm")).ToList();
                string maxlevel = "0";
                if (ADCDResultListC2.Count > 0)
                {
                    maxlevel = ADCDResultListC2[0].OutLevel;

                }
                string currentstate = "";
                
                List<ADCDResult> ADCDResultListC = ResultImport.ADCDResultList.Where(o => DateTime.Parse(o.dtime).ToString("yyyy/MM/dd HH:mm") == DateTime.Parse(HookHelper.JSTime).ToString("yyyy/MM/dd HH:mm")).ToList();
                double val_currentstate = 0;
                if (ADCDResultListC.Count > 0)
                {
                    val_currentstate = double.Parse(ADCDResultListC[0].OutLevel);
                    leveldttime = ADCDResultListC[0].dtime;
                }

                if (val_currentstate >= dangerlevel)
                {
                    currentstate = "21";
                }
                else if (val_currentstate >= warninglevel)
                {
                    currentstate = "20";
                }
                else if (val_currentstate < warninglevel)
                {
                    currentstate = "10";
                }

                ResultImport.ADCDResultList.Sort(CompareByLevelVal);
                if (ResultImport.ADCDResultList.Count > 0)
                {
                    double val = double.Parse(ResultImport.ADCDResultList[0].OutLevel);
                    if (val >= dangerlevel)
                    {
                        levelstate = "21";
                        leveldttime = ResultImport.ADCDResultList[0].dtime;
                        level = val.ToString();
                    }
                    else if (val >= warninglevel)
                    {
                        levelstate = "20";
                        leveldttime = ResultImport.ADCDResultList[0].dtime;
                        level = val.ToString();
                    }
                }

                if (levelstate != "10")
                {
                    DataRow dr_wstate = ResultImport.WSateResultTable.NewRow();
                    dr_wstate[1] = int.Parse(levelstate);
                    dr_wstate[2] = DateTime.Parse(leveldttime);
                    dr_wstate[3] = float.Parse((level == "") ? "0" : level);
                    dr_wstate[4] = DateTime.Parse((maxdt == "") ? "1900-01-01 00:00:00" : maxdt);
                    dr_wstate[5] = float.Parse(maxlevel);
                    dr_wstate[6] = DateTime.Parse(HookHelper.JSTime);
                    dr_wstate[7] = HookHelper.PubCode;
                    dr_wstate[8] = adcd;
                    dr_wstate[9] = int.Parse(levelstate);
                    dr_wstate[10] = DateTime.Parse(HookHelper.JS_StartTime);
                    ResultImport.WSateResultTable.Rows.Add(dr_wstate);
                }

            }

        }
        /// <summary>
        /// 插入状态信息表
        /// </summary>
        //public static void InsertState()
        //{

        //    Dal.insertFState(AdcdFStateList, HookHelper.FStateTB);

        //    Dal.insertWState(AdcdWStateList, HookHelper.WstateTB);


        //}


        public static int CompareByFlowVal(ADCDResult x, ADCDResult y)// 流量从大到小排序器  
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }

                return 1;

            }
            if (y == null)
            {
                return -1;
            }
            int retval = y.OutVal.CompareTo(x.OutVal);
            return retval;
        }

        public static int CompareByLevelVal(ADCDResult x, ADCDResult y)// 水位从大到小排序器  
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }

                return 1;

            }
            if (y == null)
            {
                return -1;
            }
            int retval = y.OutLevel.CompareTo(x.OutLevel);
            return retval;
        }
    }
}

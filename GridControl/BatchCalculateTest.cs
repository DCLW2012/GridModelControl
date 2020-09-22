using CreateCalculateFile;
using SysDAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace WataFdCalculate
{
    /// <summary>
    /// 作为临时使用
    /// </summary>
    public class BatchCalculateTest
    {
        //查询计算单元
        //遍历计算单元，将是一组拓扑关系的放在一组中
        //组装遍历后的单元数组
        static List<UnitClass> UnitClassList = new List<UnitClass>();

        static DataTable dt_unit = new DataTable();
        public static void GetUnitList(string name)
        {
            string sql = "select * from hsfx_unit where remark like'%" + name + "%'";
            dt_unit = SysDAL.Dal.GetData(sql);
            if (dt_unit.Rows.Count == 0)
            {
                return;
            }
            DataRow[] dr_startuint = dt_unit.Select("fcd='-1'");
            List<string> unitidList_1 = new List<string>();
            for (int i = 0; i < dr_startuint.Count(); i++)
            {
                string unitid = dr_startuint[i]["id"].ToString();
                unitidList_1.Add(unitid);
            }
            UnitClassList.Add(new UnitClass()
            {
                unitids = unitidList_1,
                unittype = UnitClassList.Count + 1
            });

            AddUnitList(unitidList_1);


            #region 按级别从大到小排序，并去除重复排序单元
            UnitClassList.Sort(CompareByTypeDown);
            List<UnitClass> UnitClassListSort = new List<UnitClass>();

            for (int m = 0; m < UnitClassList.Count; m++)
            {
                if (m == 0)
                {
                    UnitClassListSort.Add(new UnitClass()
            {
                unitids = UnitClassList[m].unitids,
                unittype = UnitClassList[m].unittype
            });
                }
                if (m > 0)
                {
                    List<string> uidlist = UnitClassList[m].unitids;
                    List<string> unitidlistnew = new List<string>();
                    int lev = UnitClassList[m].unittype;
                    bool ishave = false;
                    for (int i = 0; i < uidlist.Count(); i++)
                    {
                        string id = uidlist[i].ToString();
                        for (int k = 0; k < UnitClassListSort.Count; k++)
                        {
                            if (UnitClassListSort[k].unitids.Contains(id))
                            {
                                ishave = true;
                            }
                        }
                        if (!ishave)
                        {
                            unitidlistnew.Add(id);
                            
                        }
                        ishave = false;
                    }
                    if (unitidlistnew.Count>0)
                    {
                        UnitClassListSort.Add(new UnitClass()
                        {
                            unitids = unitidlistnew,
                            unittype = lev
                        });
                    }
                   
                }

            }
            ///从小到大排序
            UnitClassListSort.Sort(CompareByTypeUp);

            //将值再赋值回去
            UnitClassList = UnitClassListSort;
            string _uid = "";
            for (int i = 0; i < UnitClassList.Count; i++)
            {
                string unitid = "";
                for (int k = 0; k < UnitClassList[i].unitids.Count; k++)
                {
                    unitid += UnitClassList[i].unitids[k] + ",";
                }
                _uid += unitid + "\r\n";
            }

            #endregion
            //for (int i = 0; i < dr_startuint.Count(); i++)
            //{
            //List<UnitID> unitidsList = new List<UnitID>();
            //string unitid = "0";
            //unitid = dr_startuint[i]["id"].ToString();
            //unitidsList.Add(new UnitID()
            //{
            //    unitid = unitid,
            //    lev = unitidsList.Count

            //});
            //string ocd = dr_startuint[i]["ocd"].ToString();
            //if (ocd == "-1")
            //{
            //    UnitClassList.Add(new UnitClass()
            //    {
            //        unitids = unitidsList,
            //        unittype = name
            //    });
            //}
            //else
            //{
            //    GetUnitIDTopology(dt_unit, unitidsList, ocd);

            //    UnitClassList.Add(new UnitClass()
            //    {
            //        unitids = unitidsList,
            //        unittype = name
            //    });
            //}
            //}
            //执行计算
        }

        private static void AddUnitList(List<string> unitidList_1)
        {
            List<string> unitidList_2 = new List<string>();
            for (int i = 0; i < unitidList_1.Count; i++)
            {
                string unitid = unitidList_1[i];
                DataRow[] dr_uint = dt_unit.Select("id='" + unitid + "'");
                if (dr_uint.Count() > 0)
                {
                    string ocd = dr_uint[0]["ocd"].ToString();
                    if (ocd != "-1")
                    {
                        DataRow[] dr_uintid = dt_unit.Select("unitcd='" + ocd + "'");
                        if (dr_uintid.Count() > 0)
                        {
                            string id = dr_uintid[0]["id"].ToString();
                            if (!unitidList_2.Contains(id))
                            {
                                unitidList_2.Add(id);
                            }

                        }
                    }
                }
            }
            UnitClassList.Add(new UnitClass()
            {
                unitids = unitidList_2,
                unittype = UnitClassList.Count + 1
            });
            if (unitidList_2.Count > 0)
            {
                AddUnitList(unitidList_2);
            }
        }
        /// <summary>
        /// 创建计算的数组
        /// </summary>
        public static void RunBatchCalculate(string ishavecc, string year, string unitidst, int index)
        {
            int st_i = 0;
            int st_m = 0;
            if (unitidst != "0")
            {
                for (int i = 0; i < UnitClassList.Count; i++)
                {
                    if (UnitClassList[i].unitids.Contains(unitidst))
                    {
                        st_i = i;
                        List<string> unitids = UnitClassList[i].unitids;//计算单元编号列表
                        for (int j = 0; j < unitids.Count; j++)
                        {
                            if (unitids[j] == unitidst)
                            {
                                st_m = j;
                            }
                        }
                    }
                }
            }
            for (int i = st_i; i < UnitClassList.Count; i++)
            {
                List<string> unitids = UnitClassList[i].unitids;//计算单元编号列表
                if (i != st_i)
                {
                    st_m = 0;
                }
                for (int m = st_m; m < unitids.Count; m++)
                {
                    string unitid = unitids[m].ToString();

                    Console.WriteLine(unitid + "开始计算的时间：" + DateTime.Now);
                    try
                    {
                        if (ishavecc == "1")
                        {
                            HookHelper.IsHaveCC = ishavecc;
                            DataTable dt_t = GetUnitSETime(unitid, "", year);
                            int pugnum = 0;
                            pugnum = index;
                            if (m != st_m)
                            {
                                pugnum = 0;
                            }
                            for (int n = pugnum; n < dt_t.Rows.Count; n++)
                            {
                                try
                                {

                                    HookHelper.JS_StartTime = dt_t.Rows[n]["CStartTime"].ToString();
                                    HookHelper.JS_EndTime = dt_t.Rows[n]["CEndTime"].ToString();
                                    HookHelper.PubCode = dt_t.Rows[n]["ccode"].ToString() + unitid;
                                    Console.WriteLine((n + 1) + " 场次：" + HookHelper.PubCode + "   " + DateTime.Now);
                                    ReadStartCondition.RunCalculateN(unitid, "60", true);
                                    Console.WriteLine("==============完成计算的场次：" + HookHelper.PubCode);
                                    Console.WriteLine("==============计算的第：" + (n + 1) + "个场次完成");

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(" " + (n + 1) + "场次出现异常");
                                    //n = n - 1;
                                    throw ex;
                                    //continue;
                                }

                            }
                        }


                        else
                        {
                            ReadStartCondition.RunCalculateN(unitid, "60", false);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine((m + 1) + " " + unitid + "单元出现异常");
                        throw ex;
                    }

                }
            }
        }

        /// <summary>
        /// 按计算单元编号计算洪水场次
        /// </summary>
        /// <param name="ishavecc"></param>
        public static void RunBatchCalculateByUintID(string ishavecc, string unitid, int index, string year)
        {
            Console.WriteLine("进入方法的时间：" + DateTime.Now);
            try
            {
                if (ishavecc == "1")
                {
                    HookHelper.IsHaveCC = ishavecc;
                    DataTable dt_t = GetUnitSETime(unitid, "", year);
                    for (int n = index; n < dt_t.Rows.Count; n++)
                    {
                        try
                        {
                            HookHelper.JS_StartTime = dt_t.Rows[n]["CStartTime"].ToString();
                            HookHelper.JS_EndTime = dt_t.Rows[n]["CEndTime"].ToString();
                            HookHelper.PubCode = dt_t.Rows[n]["ccode"].ToString() + unitid;
                            ReadStartCondition.RunCalculateN(unitid, "60", true);
                            Console.WriteLine("==============完成计算的场次：" + HookHelper.PubCode);
                            Console.WriteLine("==============计算的第：" + (n + 1) + "个场次完成");

                        }
                        catch (Exception)
                        {
                            n = n - 1;
                            throw;
                        }

                    }

                }
                else
                {
                    ReadStartCondition.RunCalculateN(unitid, "60", false);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }




        }

        /// <summary>
        /// 循环添加该计算单元的所有拓扑关系计算单元
        /// </summary>
        /// <param name="dt_unit"></param>
        /// <param name="unitidsList"></param>
        /// <param name="ocd"></param>
        private static void GetUnitIDTopology(DataTable dt_unit, List<UnitID> unitidsList, string ocd)
        {
            string[] ocdarray = ocd.Split(',');
            for (int m = 0; m < ocdarray.Count(); m++)
            {
                DataRow[] dr_unit = dt_unit.Select("unitcd='" + ocdarray[m] + "'");
                if (dr_unit.Count() > 0)
                {
                    string id = dr_unit[0]["id"].ToString();
                    string ocd_next = dr_unit[0]["ocd"].ToString();
                    unitidsList.Add(new UnitID()
                    {
                        unitid = id,
                        lev = unitidsList.Count

                    });
                    if (ocd_next != "-1")
                    {
                        GetUnitIDTopology(dt_unit, unitidsList, ocd_next);
                    }
                }
            }
        }
        /// <summary>
        /// 获取取计算单元的开始时间和结束时间
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DataTable GetUnitSETime(string id, string ccode, string year)
        {
            string where = "";
            if (id != "")
            {
                where += "unitid='" + id + "'";
            }
            if (ccode != "")
            {
                where += " and ccode='" + ccode + "' ";
            }
            if (year != "")
            {
                where += " and CCODE like'" + year + "%'";
            }
            DataTable dt = new DataTable();
            //string sql = "select * from hsfx_cc_unit where unitid='" + id + "' order by ccode desc";
            string sql = "select * from hsfx_cc_unit where " + where + " order by ccode desc";
            try
            {
                dt = SysDAL.Dal.GetData(sql);
            }
            catch (Exception)
            {

                throw;
            }
            return dt;

        }

        public static int CompareByTypeDown(UnitClass x, UnitClass y)// 流量从大到小排序器  
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
            int retval = y.unittype.CompareTo(x.unittype);
            return retval;
        }
        public static int CompareByTypeUp(UnitClass x, UnitClass y)// 流量从小到大排序器  
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
            int retval = x.unittype.CompareTo(y.unittype);
            return retval;
        }

    }




    public class UnitClass
    {
        /// <summary>
        /// 计算单元ID
        /// </summary>
        public List<string> unitids { get; set; }
        /// <summary>
        /// 计算单元等级
        /// </summary>
        public int unittype { get; set; }
    }

    public class UnitID
    {
        /// <summary>
        /// 计算单元ID
        /// </summary>
        public string unitid { get; set; }
        /// <summary>
        /// 计算单元等级
        /// </summary>
        public int lev { get; set; }

    }
}

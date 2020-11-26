using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common;
using SysDAL;

namespace GridControl
{
    public class WriteUnitInfo
    {

        public static Dictionary<string, Dictionary<string, string>> dbValues = ClientConn.m_dbTableTypes;
        public static Dictionary<string, Dictionary<string, DataTable>> dbTableConfigs = ClientConn.m_dbTableConfig;
        public static DataTable GetAllHsfxUnitTableByWATA()
        {
            DataTable dt_unit = new DataTable();
            if (dbTableConfigs["china"].Count > 0)
            {
                //！每个省下的所有app
                dt_unit = dbTableConfigs["china"]["GRID_HSFX_UNIT"].Copy();
                int unitnum = dbTableConfigs["china"]["GRID_HSFX_UNIT"].Rows.Count;
            }

            //! 对查询出来的表做依赖排序，越靠上游越在前
            DataTable dt_unitSort = new DataTable();
            dt_unitSort = dt_unit.Clone();//克隆All的结构传递给dt
            //! 遍历dt_unit，fcd=-1的，直接push到sort中
            while (dt_unit.Rows.Count > 0)
            {
                //! 每个当前循环必须，增加一个到sort中，同时清除自己
                for (int k = 0; k < dt_unit.Rows.Count; k++)
                {
                    string unitcd = dt_unit.Rows[k]["unitcd"].ToString();
                    string fcd = dt_unit.Rows[k]["fcd"].ToString();

                    //! fcd分隔
                    string[] fcd_array = fcd.Split(new char[2] { ',', '，' });

                    if (fcd_array[0].Equals("-1"))
                    {
                        DataRow dr1 = dt_unitSort.NewRow();
                        dr1 = dt_unit.Rows[k];
                        dt_unitSort.Rows.Add(dr1.ItemArray);
                        dt_unit.Rows.RemoveAt(k);

                        break;
                    }

                    //! 遍历fcd_array，如果dt_unit的个数还大于1，看看fcd_array里面有值在== dt_unit的索引号1 之后没
                    bool isPass = false;
                    for (int i = 0; i < fcd_array.Length; i++)
                    {
                        string curFCD = fcd_array[i].ToString();//上游单元编码
                        if (dt_unit.Rows.Count > 0)
                        {
                            for (int j = 0; j < dt_unit.Rows.Count; j++)
                            {
                                string unitcdAfter = dt_unit.Rows[j]["unitcd"].ToString();
                                if (curFCD.Equals(unitcdAfter))
                                {
                                    isPass = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!isPass)
                    {
                        //1 此分组中没有当前判断的上游节点了，可以追加到数组中
                        DataRow dr2 = dt_unitSort.NewRow();
                        dr2 = dt_unit.Rows[k];
                        dt_unitSort.Rows.Add(dr2.ItemArray);
                        dt_unit.Rows.RemoveAt(k);
                        break;
                    }
                }


            }

            //！输出排序后信息到文件中
            string sortLog = "";
            sortLog += String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23}", "id", "UNITCD", "UNITNM",
                     "FCD", "OCD", "BSCD", "ONDCD", "Remark", "NDX",
                     "NDY", "GroupID", "GroovyName", "left", "bottom", "top",
                     "right", "ncols", "nrows", "xllcorner", "yllcorner", "cellsize", "degfbl", "province", "bswatacd") + "\r\n";
            int count = 0;

            //@当前组所属的省
            string cruGroupProvince = dt_unitSort.Rows[0]["province"].ToString();

            //!! 单个组的基本个数
            int perGroupNum = 10;

            for (int i = 0; i < dt_unitSort.Rows.Count; i++)
            {

                int groupID = 101 + count;
                string curPro = dt_unitSort.Rows[i]["province"].ToString();

                //! 虽然没有达到10这个基数，但是已经变化了，则跳过
                if (!cruGroupProvince.Equals(curPro))
                {
                    cruGroupProvince = curPro;
                    count++;
                    groupID = 101 + count;
                    sortLog += String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23}", dt_unitSort.Rows[i]["id"], dt_unitSort.Rows[i]["UNITCD"], dt_unitSort.Rows[i]["UNITNM"],
                    dt_unitSort.Rows[i]["FCD"], dt_unitSort.Rows[i]["OCD"], dt_unitSort.Rows[i]["BSCD"], dt_unitSort.Rows[i]["ONDCD"], dt_unitSort.Rows[i]["Remark"], dt_unitSort.Rows[i]["NDX"],
                    dt_unitSort.Rows[i]["NDY"], groupID, dt_unitSort.Rows[i]["GroovyName"], dt_unitSort.Rows[i]["left"], dt_unitSort.Rows[i]["bottom"], dt_unitSort.Rows[i]["top"],
                    dt_unitSort.Rows[i]["right"], dt_unitSort.Rows[i]["ncols"], dt_unitSort.Rows[i]["nrows"], dt_unitSort.Rows[i]["xllcorner"], dt_unitSort.Rows[i]["yllcorner"], dt_unitSort.Rows[i]["cellsize"], dt_unitSort.Rows[i]["degfbl"], dt_unitSort.Rows[i]["province"], dt_unitSort.Rows[i]["bswatacd"]) + "\r\n";
                    continue;
                }else
                {
                    sortLog += String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23}", dt_unitSort.Rows[i]["id"], dt_unitSort.Rows[i]["UNITCD"], dt_unitSort.Rows[i]["UNITNM"],
                    dt_unitSort.Rows[i]["FCD"], dt_unitSort.Rows[i]["OCD"], dt_unitSort.Rows[i]["BSCD"], dt_unitSort.Rows[i]["ONDCD"], dt_unitSort.Rows[i]["Remark"], dt_unitSort.Rows[i]["NDX"],
                    dt_unitSort.Rows[i]["NDY"], groupID, dt_unitSort.Rows[i]["GroovyName"], dt_unitSort.Rows[i]["left"], dt_unitSort.Rows[i]["bottom"], dt_unitSort.Rows[i]["top"],
                    dt_unitSort.Rows[i]["right"], dt_unitSort.Rows[i]["ncols"], dt_unitSort.Rows[i]["nrows"], dt_unitSort.Rows[i]["xllcorner"], dt_unitSort.Rows[i]["yllcorner"], dt_unitSort.Rows[i]["cellsize"], dt_unitSort.Rows[i]["degfbl"], dt_unitSort.Rows[i]["province"], dt_unitSort.Rows[i]["bswatacd"]) + "\r\n";
                }

                

                if (i % perGroupNum == 9)
                {
                    count++;
                    cruGroupProvince = dt_unitSort.Rows[i]["province"].ToString();
                }
            }
            WriteLog.WriteLogMethod(sortLog, "gridUnitSortBYWATA");

            return dt_unitSort;
        }
    }
}

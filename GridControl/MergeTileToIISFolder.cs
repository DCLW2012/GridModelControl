using Common;
using SysDAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GridControl
{
    internal class MergeTileToIISFolder
    {
        //成员变量
        private string _fileNameWithoutExtension;
        private String _iisRootDirectory;
        DataTable _stpoinginfo;
        DataTable _srcEPSGINFO;
        DataTable _srcSizeINFO;
        int _provinceNum;
        DataTable _unitsinfo;

        //构造函数
        public MergeTileToIISFolder(String fileNameWithoutExtension)
        {
            //当前场次的文件名不带扩展名
            _fileNameWithoutExtension = fileNameWithoutExtension;

            //IIS根目录
            _iisRootDirectory = HookHelper.IISRootDirectory;

            //从china表中执行查找
            String keyString = "china";
            String stpoinginfosql = "select min([left]), min(bottom), min(degfbl), min(xllcorner), min(yllcorner), min(cellsize), max([right]), max([top]) from GRID_HSFX_UNIT";
            _stpoinginfo = Dal_Rain.GetDataBySql(keyString, stpoinginfosql);

            String srcEPSGINFOSQL = "SELECT * from ChinaCoordinate ORDER BY Coordinate desc";
            _srcEPSGINFO = Dal_Rain.GetDataBySql(keyString, srcEPSGINFOSQL);


            String srcSizeINFOSQL = "SELECT * from ChinaCoordinate ORDER BY Coordinate desc";
            _srcSizeINFO = Dal_Rain.GetDataBySql(keyString, srcSizeINFOSQL);


            int _provinceNum = _srcSizeINFO.Rows.Count;

            if (_srcEPSGINFO.Rows.Count == 0)
            {
                //输出提示信息
                Console.WriteLine("数据库表中没有查询到有效的区域坐标系信息!");
                return;
            }
            double mfbl = double.Parse(_stpoinginfo.Rows[0][5].ToString());

            //"where T1.UNITCD = 'WHE13_4_1'"
            //"where T2.ComputerIP = '172.16.43.2' "
            // 结果目录列表根据组号和node id获取当前主机路径
            String folderNamelistSql = @"SELECT DISTINCT
	                                        T1.UNITCD,
	                                        T1.province,
	                                        T1.bswatacd,
	                                        T2.ComputeNode,
	                                        T2.APPPath,
	                                        T2.ComputerIP,
	                                        T2.ComputerName 
                                        FROM
	                                        GRID_HSFX_UNIT T1
	                                        INNER JOIN (
	                                        SELECT
		                                        a.*,
		                                        b.bswatacd,
		                                        b.ComputerName,
		                                        b.ComputerIP 
	                                        FROM
		                                        HSFX_ComputeUnit a
		                                        LEFT JOIN HSFX_COMPUTER_Group b ON a.ComputeNode = b.ComputeNode 
	                                        WHERE
		                                        b.ComputerGroup = 'group1' 
	                                        ) T2 ON T1.GroupID = T2.ComputeUnit 
	                                        AND T1.bswatacd = T2.bswatacd 
                                        ORDER BY
	                                        T2.ComputerIP ASC";


            //所有计算单元的列表
            if (!string.IsNullOrEmpty(HookHelper.curProvince))
            {
                //如果省份名称中包含逗号，则表示是多个省份
                List<string> listmultipro = new List<string>();
                listmultipro = HookHelper.curProvince.Split(',').ToList();
                //每个元素用单引号包裹并用逗号连接
                for (int j = 0; j < listmultipro.Count; j++)
                {
                    listmultipro[j] = "'" + listmultipro[j] + "'";
                }
                String new_provincename = string.Join(",", listmultipro);
                folderNamelistSql = String.Format(@"SELECT DISTINCT
                                                    T1.UNITCD,
                                                    T1.province,
                                                    T1.bswatacd,
                                                    T2.ComputeNode,
                                                    T2.APPPath,
                                                    '192.168.1.171' AS ComputerIP,
                                                    T3.ComputerName
                                            FROM
                                                    GRID_HSFX_UNIT T1
                                            INNER JOIN (
                                                    SELECT DISTINCT
                                                            a.*, b.bswatacd,
                                                            b.ComputerName
                                                    FROM
                                                            HSFX_ComputeUnit a
                                                    LEFT JOIN HSFX_COMPUTER b ON a.ComputeNode = b.ComputeNode
                                            ) T2 ON T1.GroupID = T2.ComputeUnit
                                            AND T1.bswatacd = T2.bswatacd
                                            AND T1.province in ({0})
                                            LEFT JOIN HSFX_COMPUTER T3 ON T1.bswatacd = T3.bswatacd", new_provincename);
            }
            _unitsinfo = Dal_Rain.GetDataBySql(keyString, folderNamelistSql);

            if (_unitsinfo.Rows.Count == 0)
            {
                //提示信息
                Console.WriteLine("没有查询到有效的计算单元信息，请检查数据库连接和数据表内容!");
                return;
            }

            
        }

        //执行合并
        //合并当前主机下所有模型目录下的结果
        public bool DoASCMergeGridPerProvinceLocal()
        {

            return true;
        }
    }
}

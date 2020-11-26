using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GridControl
{
    public class CSVData
    {
        public static DataTable LogDataTable = new DataTable();
        public static int RowNumber = -1;
        public static string CSVFilePath;

        public static void CSVInit(string path)
        {
            CSVFilePath = path;

            LogDataTable.Columns.Add("HostName", System.Type.GetType("System.String"));
            LogDataTable.Columns.Add("服务器IP", System.Type.GetType("System.String"));
            LogDataTable.Columns.Add("计算节点", System.Type.GetType("System.String"));
            LogDataTable.Columns.Add("eventId", System.Type.GetType("System.String"));
            LogDataTable.Columns.Add("计算单元个数", System.Type.GetType("System.Int32"));
            LogDataTable.Columns.Add("有效降雨单元个数", System.Type.GetType("System.Int32"));
            LogDataTable.Columns.Add("切片时长", System.Type.GetType("System.Double"));
            LogDataTable.Columns.Add("单场计算时长", System.Type.GetType("System.Double"));
            LogDataTable.Columns.Add("单场台风时长", System.Type.GetType("System.Double"));
        }
        public static void addRow()
        {
            CSVFile.SaveCSV(LogDataTable, CSVFilePath);
            LogDataTable.Rows.Add();
            RowNumber++;
        }
        public static void addData<T>(int rowNumber,string ColumnName,T data)
        {
            LogDataTable.Rows[rowNumber][ColumnName] = data;
        }
        public static DataTable GetDataTable()
        {
            return LogDataTable;
        }
        public static int GetRowNumber()
        {
            return RowNumber;
        }
    }
}

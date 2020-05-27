using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using OfficeOpenXml;

namespace 문정고등학교_출석부
{
    public static class ExcelCreater
    {
        public static void FromDataTable(DataTable dt, string sheetName, Stream stream)
        {
            #region Exceptions
            if (dt == null)
            {
                throw new ArgumentNullException(nameof(dt));
            }

            if (string.IsNullOrEmpty(sheetName))
            {
                throw new ArgumentException("시트의 이름은 null이거나 빈 문자열 일 수 없습니다.", nameof(sheetName));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            } 
            #endregion

            using (ExcelPackage pck = new ExcelPackage(stream))
            {
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add(sheetName);
                ws.Cells["A1"].LoadFromDataTable(dt, true, OfficeOpenXml.Table.TableStyles.Light9);
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                pck.Save();
            }
        }

        /// <summary>
        /// heads는 각 tables 원소들 각각의 제목을 나타내는 배열.
        /// </summary>
        /// <param name="tables"></param>
        /// <param name="heads"></param>
        /// <param name="sheetName"></param>
        /// <param name="stream"></param>
        public static void FromDataTableArray(DataTable[] tables, string[] heads, string sheetName, Stream stream)
        {
            #region Exceptions
            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            if (heads == null)
            {
                throw new ArgumentNullException(nameof(heads));
            }

            if (string.IsNullOrEmpty(sheetName))
            {
                throw new ArgumentException("시트의 이름은 null이거나 빈 문자열 일 수 없습니다.", nameof(sheetName));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (tables.Length != heads.Length)
            {
                throw new ArgumentException("tables와 heads의 원소 수는 같아야 합니다.");
            } 
            #endregion

            using (ExcelPackage pck = new ExcelPackage(stream))
            {
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add(sheetName);
                int rowNum = 1;
                for (int i = 0; i < tables.Length; ++i)
                {
                    ExcelRange headRange = ws.Cells[rowNum, 1, rowNum, tables[i].Columns.Count];
                    headRange.Merge = true;
                    headRange.Value = heads[i];
                    headRange.Style.Font.Bold = true;
                    headRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    rowNum++;

                    ws.Cells[rowNum, 1].LoadFromDataTable(tables[i], true, OfficeOpenXml.Table.TableStyles.Light9);
                    rowNum += tables[i].Rows.Count + 2;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                pck.Save();
            }
        }
    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 문정고등학교_출석부
{
    public static class DataTableExtension
    {
        public static void ToExcel(this DataTable table, string fileName, string sheetName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("파일의 이름은 null이거나 빈 문자열일 수 없습니다.", nameof(fileName));
            }

            SaveFileDialog saveD = new SaveFileDialog();
            saveD.Title = "저장 경로 선택";
            saveD.FileName = fileName;
            saveD.Filter = $"Excel 통합문서 (.xlsx)|*.xlsx";

            FileStream fs = null;

            try
            {
                if (saveD.ShowDialog() == true)
                {
                    fs = saveD.OpenFile() as FileStream;

                    ExcelCreater.FromDataTable(table, sheetName, fs);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"xlsx 파일을 저장하는데 실패했습니다...\n\n추가 정보 : {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }
    }
}

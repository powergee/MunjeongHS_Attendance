using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace 문정고등학교_출석부
{
    /// <summary>
    /// AddStudentsWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AddStudentsWindow : Window
    {
        private StuListControl mStuList;
        private DataTable mTable;
        private string mGrade;
        private string mClass;
        private string[] mExistingNumbers;

        public AddStudentsWindow(StuListControl slc, string[] existingNumbers, string grade, string @class)
        {
            if (existingNumbers == null)
            {
                throw new ArgumentNullException(nameof(existingNumbers));
            }

            InitializeComponent();

            mStuList = slc;
            mGrade = grade;
            mClass = @class;
            mExistingNumbers = existingNumbers;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClassText.Text = $"{mGrade} - {mClass}";

            mTable = new DataTable();
            
            mTable.Columns.Add(new DataColumn("번호", typeof(string)));
            mTable.Columns.Add(new DataColumn("성명", typeof(string)));

            MainDataGrid.DataContext = mTable;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (mTable.Rows.Count == 0)
            {
                MessageBox.Show(this, "데이터가 입력되지 않았습니다.", "데이터가 입력되지 않았음", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<string> usedNumbers = new List<string>();

            foreach (DataRow row in mTable.Rows)
            {
                if (string.IsNullOrWhiteSpace(row["번호"].ToString()) || string.IsNullOrWhiteSpace(row["성명"].ToString()))
                {
                    MessageBox.Show(this, "입력되지 않은 데이터가 있습니다. 마지막 행의 두 칸을 제외한 모든 칸은 빈 문자열이거나 공백으로 이루어진 문자열이여선 안됩니다.", "빈 데이터 있음", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (mExistingNumbers.Contains(row["번호"].ToString()))
                {
                    MessageBox.Show(this, "입력한 번호가 이미 존재합니다.", "번호 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(row["번호"].ToString(), out int temp))
                {
                    MessageBox.Show(this, $"입력한 번호가 숫자가 아닙니다 : {row["번호"].ToString()}", "번호 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (usedNumbers.Where(x => x.Contains(row["번호"].ToString())).Count() != 0)
                {
                    MessageBox.Show(this, $"입력한 번호들 중 겹치는 수가 있습니다 : {row["번호"].ToString()}", "번호 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                usedNumbers.Add(row["번호"].ToString());
            }

            MessageBoxResult qResult = MessageBox.Show(this, $"총 {mTable.Rows.Count} 개의 학생이 추가됩니다.\n추가된 학생은 제거할 수 없습니다.\n계속하시겠습니까?", "준비 완료", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (qResult == MessageBoxResult.Yes)
            {
                if (!mStuList.Import(mTable))
                {
                    MessageBox.Show(this, "데이터를 저장하는데 실패하였습니다.", "저장 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    this.Close();
                }
            }
            else
            {
                return;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

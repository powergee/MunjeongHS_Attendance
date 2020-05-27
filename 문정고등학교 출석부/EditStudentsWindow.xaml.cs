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
    /// EditStudentsWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EditStudentsWindow : Window
    {
        private StuListControl mStuList;
        private string mGrade;
        private string mClass;
        private string[] mExistingNumbers;
        private DataRow mRowToEdit;
        private int mRowIndexToEdit;

        public EditStudentsWindow(StuListControl slc, string[] existingNumbers, string grade, string @class, DataRow rowToEdit, int rowIndexToEdit)
        {
            if (existingNumbers == null)
            {
                throw new ArgumentNullException(nameof(existingNumbers));
            }

            if (rowToEdit == null)
            {
                throw new ArgumentNullException(nameof(rowToEdit));
            }

            InitializeComponent();

            mStuList = slc;
            mGrade = grade;
            mClass = @class;
            mExistingNumbers = existingNumbers;
            mRowToEdit = rowToEdit;
            mRowIndexToEdit = rowIndexToEdit;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OriginalName.Text = mRowToEdit["성명"].ToString();
            OriginalNumber.Text = mRowToEdit["번호"].ToString();

            NewName.Text = mRowToEdit["성명"].ToString();
            NewNumber.Text = mRowToEdit["번호"].ToString();

            ClassText.Text = $"{mGrade} - {mClass}";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            NewNumber.Text = NewNumber.Text.Trim();
            NewName.Text = NewName.Text.Trim();

            if (string.IsNullOrWhiteSpace(NewNumber.Text) || string.IsNullOrWhiteSpace(NewName.Text))
            {
                MessageBox.Show(this, "입력되지 않은 데이터가 있습니다. 모든 칸은 빈 문자열이거나 공백으로 이루어진 문자열이여선 안됩니다.", "빈 데이터 있음", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (mExistingNumbers.Contains(NewNumber.Text) && OriginalNumber.Text != NewNumber.Text)
            {
                MessageBox.Show(this, "입력한 번호가 이미 존재합니다.", "번호 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(NewNumber.Text, out int temp))
            {
                MessageBox.Show(this, $"입력한 번호가 숫자가 아닙니다 : {NewNumber.Text}", "번호 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!mStuList.Update(mRowToEdit, NewNumber.Text, NewName.Text))
            {
                MessageBox.Show(this, "데이터를 저장하는데 실패하였습니다.", "저장 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

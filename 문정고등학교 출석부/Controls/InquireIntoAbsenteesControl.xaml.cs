using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TcpCore;
using static 문정고등학교_출석부.Attendance;

namespace 문정고등학교_출석부
{
    /// <summary>
    /// InquireIntoAbsenteesControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InquireIntoAbsenteesControl : UserControl, IClearable
    {
        public static readonly string ONLY_FAULTS_WHERE = @"(Not [0교시] = 1) And (Not [0교시] = 2) OR
(Not [1교시] = 1) And (Not [1교시] = 2) OR
(Not [2교시] = 1) And (Not [2교시] = 2) OR
(Not [3교시] = 1) And (Not [3교시] = 2) OR
(Not [4교시] = 1) And (Not [4교시] = 2) OR
(Not [5교시] = 1) And (Not [5교시] = 2) OR
(Not [6교시] = 1) And (Not [6교시] = 2) OR
(Not [7교시] = 1) And (Not [7교시] = 2) OR
(Not [8교시] = 1) And (Not [8교시] = 2)";

        public WorkWindow ParentWindow { get; private set; }
        public TcpDataSetRequirement RecentAttReq { get; private set; } = new TcpDataSetRequirement();

        private DataTable mTable;
        private DataTable mOriginal;
        private string mCurrentGrade, mCurrentClass;

        public InquireIntoAbsenteesControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as WorkWindow;
            DateControl.SelectedDate = DateTime.Now;
            ParentWindow.Client.ReceivedDataSetResult += Client_ReceivedDataSetResult;
        }

        private void Client_ReceivedDataSetResult(object sender, TcpCore.ReceivedDataSetResultEventArgs e)
        {
            if (!e.Result.Success)
                Debug.Error("서버로부터 데이터를 수신하는데 실패하였습니다.");

            if (RecentAttReq.Equals(e.Result.Requirement))
            {
                this.Dispatcher.Invoke(() =>
                {
                    // 출석부가 존재함. 준비 완료.
                    if (e.Result.Data.Tables[0].Rows.Count > 0)
                    {
                        ProcessAndShowData(e.Result.Data.Tables[0]);

                        this.IsEnabled = true;

                        ToExcel.IsEnabled = true;
                        ToExcel_Selected.IsEnabled = true;
                    }
                    // 출석부가 존재하지 않음. 근태 불량자가 없는것으로 취급.
                    else
                    {
                        ProcessAndShowData(null);
                        this.IsEnabled = true;

                        ToExcel.IsEnabled = false;
                        ToExcel_Selected.IsEnabled = false;
                    }
                });
            }
        }

        private void ProcessAndShowData(DataTable attData)
        {
            mTable = new DataTable();

            mTable.Columns.Add(new DataColumn("일자", typeof(string)));
            mTable.Columns.Add(new DataColumn("체크박스", typeof(CheckBox)));
            mTable.Columns.Add(new DataColumn("학년", typeof(string)));
            mTable.Columns.Add(new DataColumn("반", typeof(string)));
            mTable.Columns.Add(new DataColumn("번호", typeof(string)));
            mTable.Columns.Add(new DataColumn("성명", typeof(string)));
            mTable.Columns.Add(new DataColumn("해당사항", typeof(string)));
            mTable.Columns.Add(new DataColumn("벌점", typeof(string)));

            if (attData != null)
            {
                foreach (DataRow row in attData.Rows)
                {
                    AttOfDay attOfDay = new AttOfDay((DateTime)row["일자"]);

                    for (int period = 0; period <= 8; ++period)
                    {
                        AttEnum attEnum = (AttEnum)row[$"{period}교시"];
                        attOfDay.Add(attEnum);
                    }

                    if (attOfDay.HasFaults)
                    {
                        DataRow newRow = mTable.NewRow();

                        newRow["일자"] = ((DateTime)row["일자"]).ToShortDateString();
                        newRow["체크박스"] = new CheckBox();
                        newRow["학년"] = row["학년"].ToString();
                        newRow["반"] = row["반"].ToString();
                        newRow["번호"] = row["번호"].ToString();
                        newRow["성명"] = row["성명"].ToString();
                        newRow["해당사항"] = attOfDay.Description;
                        newRow["벌점"] = attOfDay.PenaltyPoints.ToString();

                        mTable.Rows.Add(newRow);
                    }
                }
            }

            if (mTable.Rows.Count == 0)
            {
                DataRow newRow = mTable.NewRow();
                newRow["해당사항"] = "근태 불량자 없음";
                mTable.Rows.Add(newRow);
            }

            MainDataGrid.DataContext = mTable;
            mOriginal = mTable.Copy();
        }

        private void TopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (mTable != null)
            {
                if (mTable.Rows[0]["해당사항"].ToString() == "근태 불량자 없음")
                    return;

                for (int i = 0; i < mTable.Rows.Count; ++i)
                {
                    (mTable.Rows[i]["체크박스"] as CheckBox).IsChecked = true;
                } 
            }
        }

        private void Inquire_Click(object sender, RoutedEventArgs e)
        {
            string gradeStr = null, classStr = null;
            ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                gradeStr = ParentWindow.GradeCombo.Text;
                classStr = ParentWindow.ClassCombo.Text;
            })).Wait();

            //TODO : 계정 권한에 따라 접근을 거부하는 코드 필요.
            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, gradeStr, classStr, AccessType.Inquire, ObjectToAccess.Attendance))
            {
                MessageBox.Show("데이터에 접근할 권한이 없습니다.\n\n특정 학급의 출석부를 조회하려면 그 학급의 담임이거나, 전 학년 총괄 관리자여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (DateControl.SelectedDate == null)
                return;

            mCurrentGrade = gradeStr;
            mCurrentClass = classStr;

            ParentWindow.ChildManager.TryToShow<LoadingWindow>("서버와 통신 중...", (WaitCallback)((o) =>
            {
                // 해당 날짜, 반에 대한 출석부가 생성되었는지 확인
                ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    TcpDataSetRequirement dataReq = new TcpDataSetRequirement(new string[] { "*" }, "[출석부]",
                    $"[일자]={DateControl.SelectedDate.Value.ToSQLString()} AND [학년]={mCurrentGrade.ToSQLString()} AND [반]={mCurrentClass.ToSQLString()} AND ({ONLY_FAULTS_WHERE})", "Val([번호])");
                    ParentWindow.Client.RequestDataSet(dataReq);

                    RecentAttReq = dataReq;
                })).Wait();
            }));

            this.IsEnabled = false;
        }

        private void TopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (mTable != null)
            {
                if (mTable.Rows[0]["해당사항"].ToString() == "근태 불량자 없음")
                    return;

                for (int i = 0; i < mTable.Rows.Count; ++i)
                {
                    (mTable.Rows[i]["체크박스"] as CheckBox).IsChecked = false;
                } 
            }
        }

        private List<int> GetCheckedRows()
        {
            List<int> rowList = new List<int>();

            for (int i = 0; i < mTable.Rows.Count; ++i)
            {
                CheckBox checkBox = mTable.Rows[i]["체크박스"] as CheckBox;

                if (checkBox.IsChecked == true)
                {
                    rowList.Add(i);
                }
            }

            return rowList;
        }

        private void ToExcel_Click(object sender, RoutedEventArgs e)
        {
            DataTable dataToProcess = mTable.Clone();
            dataToProcess.Columns.Remove("체크박스");
            dataToProcess.Columns["학년"].DataType = typeof(int);
            dataToProcess.Columns["반"].DataType = typeof(int);
            dataToProcess.Columns["번호"].DataType = typeof(int);
            dataToProcess.Columns["벌점"].DataType = typeof(int);

            foreach (DataRow row in mTable.Rows)
            {
                dataToProcess.ImportRow(row);
            }

            dataToProcess.ToExcel($"{mTable.Rows[0]["일자"].ToString()} {mCurrentGrade}학년 {mCurrentClass}반 근태 불량자.xlsx", "근태 불량자 명단");
        }

        private void ToExcel_Selected_Click(object sender, RoutedEventArgs e)
        {
            DataTable dataToProcess = mTable.Clone();
            dataToProcess.Columns.Remove("체크박스");
            dataToProcess.Columns["학년"].DataType = typeof(int);
            dataToProcess.Columns["반"].DataType = typeof(int);
            dataToProcess.Columns["번호"].DataType = typeof(int);
            dataToProcess.Columns["벌점"].DataType = typeof(int);

            foreach (int index in GetCheckedRows())
                dataToProcess.ImportRow(mTable.Rows[index]);

            dataToProcess.ToExcel($"{mTable.Rows[0]["일자"].ToString()} {mCurrentGrade}학년 {mCurrentClass}반 근태 불량자.xlsx", "근태 불량자 명단");
        }

        public void Clear()
        {
            mTable?.Clear();
            mOriginal?.Clear();
            mCurrentClass = null;
            mCurrentGrade = null;
            RecentAttReq = new TcpDataSetRequirement();
            DateControl.SelectedDate = DateTime.Now;
            ToExcel.IsEnabled = false;
            ToExcel_Selected.IsEnabled = false;
            TopCheckBox.IsChecked = false;
        }
    }
}

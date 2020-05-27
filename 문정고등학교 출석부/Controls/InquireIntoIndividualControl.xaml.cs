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
    /// InquireIntoIndividualControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InquireIntoIndividualControl : UserControl, IClearable
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
        public TcpDataSetRequirement RecentStuListReq { get; private set; } = new TcpDataSetRequirement();

        private DataTable mStuListTable;
        private DataTable mFaultsTable;
        private string mCurrentGrade, mCurrentClass;
        private DateTime mCurrentFirst, mCurrentSecond;
        private DataRow mCurrentInquiredRow;

        public InquireIntoIndividualControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as WorkWindow;
            FirstDateControl.SelectedDate = DateTime.Now;
            SecondDateControl.SelectedDate = DateTime.Now;
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
                    // 출석부 조회 성공.
                    if (e.Result.Data.Tables[0].Rows.Count > 0)
                    {
                        ShowFaultsList(e.Result.Data.Tables[0]);
                        this.IsEnabled = true;
                    }
                    // 출석부가 존재하지 않음. 벌점이 없는것으로 처리.
                    else
                    {
                        ShowEmptyFaultsList();
                        this.IsEnabled = true;
                    }
                });

                return;
            }

            if (RecentStuListReq.Equals(e.Result.Requirement))
            {
                this.Dispatcher.Invoke(() =>
                {
                    // 학생 목록을 가져왔음.
                    if (e.Result.Data.Tables[0].Rows.Count > 0)
                    {
                        ShowStuList(e.Result.Data.Tables[0]);
                        this.IsEnabled = true;

                        ToExcel.IsEnabled = true;

                        mFaultsTable?.Clear();
                    }
                    // 학생 목록 없음. 출석부 조회 불가.
                    else
                    {
                        ParentWindow.Dispatcher.Invoke(() => MessageBox.Show("서버에 기간에 맞는 학급 학생명단이 입력되지 않았습니다.", "학생 명단 없음", MessageBoxButton.OK, MessageBoxImage.Warning));
                        this.IsEnabled = true;

                        ToExcel.IsEnabled = true;
                    }
                });

                return;
            }
        }

        private void ShowStuList(DataTable stuTable)
        {
            mStuListTable = stuTable.Clone();
            mStuListTable.Columns.Add(new DataColumn("체크박스", typeof(CheckBox)));
            mStuListTable.Columns.Remove("등록 날짜");

            foreach (DataRow row in stuTable.Rows)
            {
                DataRow newRow = mStuListTable.NewRow();

                newRow["체크박스"] = new CheckBox();
                newRow["학년"] = row["학년"].ToString();
                newRow["반"] = row["반"].ToString();
                newRow["번호"] = row["번호"].ToString();
                newRow["성명"] = row["성명"].ToString();

                mStuListTable.Rows.Add(newRow);
            }

            StuDataGrid.DataContext = mStuListTable;
        }

        private void ShowFaultsList(DataTable attTable)
        {
            mFaultsTable = new DataTable();

            mFaultsTable.Columns.Add(new DataColumn("구분", typeof(string)));
            mFaultsTable.Columns.Add(new DataColumn("일자", typeof(string)));
            mFaultsTable.Columns.Add(new DataColumn("해당사항", typeof(string)));
            mFaultsTable.Columns.Add(new DataColumn("벌점", typeof(int)));
            int penaltySum = 0;

            foreach (DataRow row in attTable.Rows)
            {
                AttOfDay attOfDay = new AttOfDay((DateTime)row["일자"]);

                for (int period = 0; period <= 8; ++period)
                {
                    AttEnum attEnum = (AttEnum)row[$"{period}교시"];
                    attOfDay.Add(attEnum);
                }

                if (attOfDay.HasFaults)
                {
                    DataRow newRow = mFaultsTable.NewRow();
                    int penalty = attOfDay.PenaltyPoints;

                    newRow["구분"] = "벌점";
                    newRow["일자"] = ((DateTime)row["일자"]).ToShortDateString();
                    newRow["해당사항"] = attOfDay.Description;
                    newRow["벌점"] = penalty;

                    mFaultsTable.Rows.Add(newRow);
                    penaltySum += penalty;
                }
            }

            if (penaltySum > 0)
            {
                DataRow sumRow = mFaultsTable.NewRow();
                sumRow["구분"] = "합계";
                sumRow["벌점"] = penaltySum;
                mFaultsTable.Rows.Add(sumRow);
            }
            else
            {
                DataRow row = mFaultsTable.NewRow();
                row["해당사항"] = "근태 불량 사항 없음";
                mFaultsTable.Rows.Add(row);
            }

            FaultsDataGrid.DataContext = mFaultsTable;
        }

        private void ShowEmptyFaultsList()
        {
            mFaultsTable = new DataTable();

            mFaultsTable.Columns.Add(new DataColumn("구분", typeof(string)));
            mFaultsTable.Columns.Add(new DataColumn("일자", typeof(string)));
            mFaultsTable.Columns.Add(new DataColumn("해당사항", typeof(string)));
            mFaultsTable.Columns.Add(new DataColumn("벌점", typeof(int)));

            DataRow row = mFaultsTable.NewRow();
            row["해당사항"] = "근태 불량 사항 없음";
            mFaultsTable.Rows.Add(row);

            FaultsDataGrid.DataContext = mFaultsTable;
        }

        public void Clear()
        {
            mStuListTable?.Clear();
            mFaultsTable?.Clear();
            mCurrentClass = null;
            mCurrentGrade = null;
            mCurrentInquiredRow = null;
            RecentAttReq = new TcpDataSetRequirement();
            RecentStuListReq = new TcpDataSetRequirement();
            FirstDateControl.SelectedDate = DateTime.Now;
            SecondDateControl.SelectedDate = DateTime.Now;

            ToExcel.IsEnabled = false;
            TopCheckBox.IsChecked = false;
            InquiredText.Text = "- ~ -";
            SelectedStudentText.Text = "";
        }

        private void TopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (mStuListTable != null)
            {
                for (int i = 0; i < mStuListTable.Rows.Count; ++i)
                {
                    (mStuListTable.Rows[i]["체크박스"] as CheckBox).IsChecked = true;
                }
            }
        }

        private void TopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (mStuListTable != null)
            {
                for (int i = 0; i < mStuListTable.Rows.Count; ++i)
                {
                    (mStuListTable.Rows[i]["체크박스"] as CheckBox).IsChecked = false;
                }
            }
        }

        private void ToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string stuInfo = $"{mCurrentGrade}학년 {mCurrentClass}반 {mCurrentInquiredRow["번호"].ToString()}번 {mCurrentInquiredRow["성명"]}";
                mFaultsTable.ToExcel($"{mCurrentFirst.ToShortDateString()} ~ {mCurrentSecond.ToShortDateString()} {stuInfo} 벌점 조회.xlsx", stuInfo);
            }
            catch (Exception)
            {
                MessageBox.Show("학생을 선택(더블클릭)한 뒤 다시 시도해주십시오.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataRow selectedRow;

            try
            {
                selectedRow = mStuListTable.Rows[StuDataGrid.SelectedIndex];
                SelectedStudentText.Text = $"현재 선택된 학생: {mCurrentGrade}학년 {mCurrentClass}반 {(string)selectedRow["번호"]}번 {(string)selectedRow["성명"]}";
            }
            catch (Exception)
            {
                ParentWindow.Dispatcher.Invoke(() => MessageBox.Show("조회하고자 하는 학생을 정확히 선택해주십시오.", "학생 선택 실패", MessageBoxButton.OK, MessageBoxImage.Warning));
                return;
            }

            // 선택된 학생의 정해진 기간의 출석부 조회. (무단 기록이 있는 것만)
            ParentWindow.ChildManager.TryToShow<LoadingWindow>("서버와 통신 중...", (WaitCallback)((o) =>
            {
                ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    TcpDataSetRequirement dataReq = new TcpDataSetRequirement(new string[] { "*" }, "[출석부]",
                    $"[일자] BETWEEN {mCurrentFirst.ToSQLString()} AND {mCurrentSecond.ToSQLString()} AND [학년]={mCurrentGrade.ToSQLString()} AND [반]={mCurrentClass.ToSQLString()} AND [번호]={((string)selectedRow["번호"]).ToSQLString()} AND ({ONLY_FAULTS_WHERE})", null);
                    ParentWindow.Client.RequestDataSet(dataReq);

                    RecentAttReq = dataReq;
                })).Wait();
            }));

            mCurrentInquiredRow = selectedRow;
            this.IsEnabled = false;
        }

        private void Inquire_Click(object sender, RoutedEventArgs e)
        {
            if (FirstDateControl.SelectedDate == null || SecondDateControl.SelectedDate == null)
            {
                ParentWindow.Dispatcher.Invoke(() => MessageBox.Show("기간을 설정해주십시오.", "기간 입력 없음", MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }

            if (FirstDateControl.SelectedDate.Value > SecondDateControl.SelectedDate.Value)
            {
                ParentWindow.Dispatcher.Invoke(() => MessageBox.Show("기간이 올바르게 입력되지 않았습니다. 첫번째날이 두번째날보다 나중이여선 안됩니다.", "기간 입력 오류", MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }

            string gradeStr = null, classStr = null;
            ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                gradeStr = ParentWindow.GradeCombo.Text;
                classStr = ParentWindow.ClassCombo.Text;
            })).Wait();

            //TODO : 계정 권한에 따라 접근을 거부하는 코드 필요.
            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, gradeStr, classStr, AccessType.Inquire, ObjectToAccess.StudentList))
            {
                MessageBox.Show("데이터에 접근할 권한이 없습니다.\n\n특정 학급의 출석부와 학생 명단을 조회하려면 그 학급의 담임이거나, 전 학년 총괄 관리자여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            ToExcel.IsEnabled = false;

            mCurrentGrade = gradeStr;
            mCurrentClass = classStr;
            mCurrentFirst = FirstDateControl.SelectedDate.Value;
            mCurrentSecond = SecondDateControl.SelectedDate.Value;

            InquiredText.Text = $"{mCurrentFirst.ToShortDateString()} ~ {mCurrentSecond.ToShortDateString()}";

            ParentWindow.ChildManager.TryToShow<LoadingWindow>("서버와 통신 중...", (WaitCallback)((o) =>
            {
                // 해당 기간에 맞는 학생명단 요청
                ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    TcpDataSetRequirement dataReq = new TcpDataSetRequirement(new string[] { "*" }, "[학생 명단]",
                    $"[등록 날짜]<{SecondDateControl.SelectedDate.Value.ToSQLString()} AND [학년]={mCurrentGrade.ToSQLString()} AND [반]={mCurrentClass.ToSQLString()}", "Val([번호])");
                    ParentWindow.Client.RequestDataSet(dataReq);

                    RecentStuListReq = dataReq;
                })).Wait();
            }));

            this.IsEnabled = false;
        }
    }
}

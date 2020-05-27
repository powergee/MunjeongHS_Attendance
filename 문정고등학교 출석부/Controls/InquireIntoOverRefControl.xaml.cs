using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
    /// InquireIntoOverRefControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InquireIntoOverRefControl : UserControl, IClearable
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

        public TcpDataSetRequirement RecentAttReq { get; private set; } = new TcpDataSetRequirement();

        private int mCurrentPenalty = 0;
        private string mCurrentGrade;
        private string mCurrentClass;
        private DateTime mCurrentFirst;
        private DateTime mCurrentSecond;

        private List<StuAndPenalty> mStuPenaltyList;

        public WorkWindow ParentWindow { get; private set; }

        private static bool IsTextAllowed(string text)
        {
            int temp;
            return int.TryParse(text, out temp);
        }

        public InquireIntoOverRefControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as WorkWindow;
            FirstDateControl.SelectedDate = DateTime.Now;
            SecondDateControl.SelectedDate = DateTime.Now;
            ParentWindow.Client.ReceivedDataSetResult += Client_ReceivedDataSetResult;

            DataObject.AddPastingHandler(RefText, OnTextPaste);
        }

        private void OnTextPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void Client_ReceivedDataSetResult(object sender, ReceivedDataSetResultEventArgs e)
        {
            if (!e.Result.Success)
                Debug.Error("서버로부터 데이터를 수신하는데 실패하였습니다.");

            if (RecentAttReq.Equals(e.Result.Requirement))
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (e.Result.Data.Tables[0].Rows.Count > 0)
                    {
                        ProcessData(e.Result.Data.Tables[0]);
                        ShowData();
                    }

                    else
                    {
                        ShowEmpty();
                    }

                    this.IsEnabled = true;
                    this.ToExcel.IsEnabled = true;
                });

                return;
            }
        }

        private void ProcessData(DataTable dataToProcess)
        {
            mStuPenaltyList = new List<StuAndPenalty>();

            foreach (DataRow row in dataToProcess.Rows)
            {
                AttOfDay attOfDay = new AttOfDay((DateTime)row["일자"]);

                for (int period = 0; period <= 8; ++period)
                {
                    AttEnum attEnum = (AttEnum)row[$"{period}교시"];
                    attOfDay.Add(attEnum);
                }

                if (attOfDay.HasFaults)
                {
                    StuAndPenalty sap = null;

                    for (int i = 0; i < mStuPenaltyList.Count; ++i)
                    {
                        if (mStuPenaltyList[i].Name == row["성명"].ToString())
                        {
                            sap = mStuPenaltyList[i];
                            break;
                        }
                    }

                    if (sap == null)
                    {
                        sap = new StuAndPenalty(row["학년"].ToString(), row["반"].ToString(), row["번호"].ToString(), row["성명"].ToString(), attOfDay.PenaltyPoints);
                        mStuPenaltyList.Add(sap);
                    }
                    else
                    {
                        sap.Penalty += attOfDay.PenaltyPoints;
                    }
                }
            }
        }

        private void ShowData()
        {
            try
            {
                if (mStuPenaltyList != null)
                {
                    if (mStuPenaltyList.Count > 0)
                    {
                        mCurrentPenalty = int.Parse(RefText.Text);

                        var selected = from sap in mStuPenaltyList
                                       where sap.Penalty >= mCurrentPenalty
                                       orderby sap.Penalty descending, sap.Number ascending
                                       select sap;

                        if (selected.Count() > 0)
                            MainDataGrid.DataContext = selected;
                        else
                            ShowEmpty();
                    }
                    else
                    {
                        ShowEmpty();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void ShowEmpty()
        {
            List<StuAndPenalty> stus = new List<StuAndPenalty>();
            StuAndPenalty sap = new StuAndPenalty();
            sap.Name = "해당 사항 없음";
            stus.Add(sap);
            MainDataGrid.DataContext = stus;
            ToExcel.IsEnabled = false;
        }

        public void Clear()
        {
            mCurrentGrade = null;
            mCurrentClass = null;
            RecentAttReq = new TcpDataSetRequirement();
            mStuPenaltyList?.Clear();

            ToExcel.IsEnabled = false;
            FirstDateControl.SelectedDate = DateTime.Now;
            SecondDateControl.SelectedDate = DateTime.Now;
            MainDataGrid.DataContext = null;
            InquiredText.Text = "- ~ -";
            RefText.Text = "0";
        }

        private void RefText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
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

            if (!AuthorityChecker.CheckAuthority(accType, gradeStr, classStr, AccessType.Inquire, ObjectToAccess.Attendance))
            {
                MessageBox.Show("데이터에 접근할 권한이 없습니다.\n\n특정 학급의 출석부를 조회하려면 그 학급의 담임이거나, 전 학년 총괄 관리자여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            ToExcel.IsEnabled = false;

            mCurrentGrade = gradeStr;
            mCurrentClass = classStr;
            mCurrentFirst = FirstDateControl.SelectedDate.Value;
            mCurrentSecond = SecondDateControl.SelectedDate.Value;

            InquiredText.Text = $"{mCurrentFirst.ToShortDateString()} ~ {mCurrentSecond.ToShortDateString()}";

            // 정해진 기간중 근태 불량 상태가 있는 출석부만 조회.
            ParentWindow.ChildManager.TryToShow<LoadingWindow>("서버와 통신 중...", (WaitCallback)((o) =>
            {
                ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    TcpDataSetRequirement dataReq = new TcpDataSetRequirement(new string[] { "*" }, "[출석부]",
                    $"[일자] BETWEEN {mCurrentFirst.ToSQLString()} AND {mCurrentSecond.ToSQLString()} AND [학년]={mCurrentGrade.ToSQLString()} AND [반]={mCurrentClass.ToSQLString()} AND ({ONLY_FAULTS_WHERE})", "Val([번호])");
                    ParentWindow.Client.RequestDataSet(dataReq);

                    RecentAttReq = dataReq;
                })).Wait();
            }));

            this.IsEnabled = false;
        }

        private void RefText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowData();
        }

        private void ToExcel_Click(object sender, RoutedEventArgs e)
        {
            DataTable table = new DataTable();

            table.Columns.Add(new DataColumn("학년", typeof(int)));
            table.Columns.Add(new DataColumn("반", typeof(int)));
            table.Columns.Add(new DataColumn("번호", typeof(int)));
            table.Columns.Add(new DataColumn("성명", typeof(string)));
            table.Columns.Add(new DataColumn("벌점", typeof(int)));

            foreach (StuAndPenalty sap in mStuPenaltyList)
            {
                if (sap.Penalty >= mCurrentPenalty)
                {
                    DataRow newRow = table.NewRow();
                    sap.AutoFill(newRow);
                    table.Rows.Add(newRow);
                }
            }

            if (table.Rows.Count > 0)
            {
                string sheetName = $"{mCurrentGrade}학년 {mCurrentClass}반 출결 벌점 {mCurrentPenalty}점 이상자";
                table.ToExcel(sheetName, sheetName);
            }
        }
    }

    internal class StuAndPenalty : INotifyPropertyChanged
    {
        private string mGrade;
        private string mClass;
        private string mNumber;
        private string mName;
        private int mPenalty;

        public StuAndPenalty(string grade, string @class, string number, string name, int penalty)
        {
            Grade = grade ?? throw new ArgumentNullException(nameof(grade));
            Class = @class ?? throw new ArgumentNullException(nameof(@class));
            Number = number ?? throw new ArgumentNullException(nameof(number));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Penalty = penalty;
        }

        public StuAndPenalty()
        {
            Grade = string.Empty;
            Class = string.Empty;
            Number = string.Empty;
            Name = string.Empty;
            Penalty = 0;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Grade { get => mGrade;
            set
            {
                mGrade = value;
                NotifyPropertyChanged();
            }
        }

        public string Class { get => mClass;
            set
            {
                mClass = value;
                NotifyPropertyChanged();
            }
        }

        public string Number { get => mNumber;
            set
            {
                mNumber = value;
                NotifyPropertyChanged();
            }
        }

        public string Name { get => mName;
            set
            {
                mName = value;
                NotifyPropertyChanged();
            }
        }

        public int Penalty { get => mPenalty;
            set
            {
                mPenalty = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AutoFill(DataRow row)
        {
            try
            {
                row["학년"] = StrToInt(this.Grade);
                row["반"] = StrToInt(this.Class);
                row["번호"] = StrToInt(this.Number);
                row["성명"] = this.Name;
                row["벌점"] = this.Penalty;
            }
            catch
            {
                return false;
            }

            return true;
        }

        private int StrToInt(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return 0;

            int temp;
            if (int.TryParse(s, out temp))
                return temp;
            else
                return 0;

        }
    }
}

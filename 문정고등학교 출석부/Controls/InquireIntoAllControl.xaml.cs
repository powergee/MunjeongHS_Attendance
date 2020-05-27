using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
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
using Microsoft.Win32;
using TcpCore;

using static 문정고등학교_출석부.Attendance;

namespace 문정고등학교_출석부
{
    /// <summary>
    /// InquireIntoAllControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InquireIntoAllControl : UserControl, IClearable
    {
        class StudentInfo : IComparable<StudentInfo>
        {
            public string Grade { get; set; }
            public string Class { get; set; }
            public string Number { get; set; }
            public string Name { get; set; }

            public StudentInfo(string grade, string @class, string number, string name)
            {
                Grade = grade ?? throw new ArgumentNullException(nameof(grade));
                Class = @class ?? throw new ArgumentNullException(nameof(@class));
                Number = number ?? throw new ArgumentNullException(nameof(number));
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public override bool Equals(object obj)
            {
                var info = obj as StudentInfo;
                return info != null &&
                       Grade == info.Grade &&
                       Class == info.Class &&
                       Number == info.Number &&
                       Name == info.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -708036343;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Grade);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Class);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Number);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }

            public int CompareTo(StudentInfo other)
            {
                int result;
                if ((result = this.Grade.CompareTo(other.Grade)) == 0)
                {
                    if ((result = this.Class.CompareTo(other.Class)) == 0)
                    {
                        return this.Number.CompareTo(other.Number);
                    }
                    else return result;
                }
                else return result;
            }
        }

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

        private ObservableCollection<StudentInfo> mStudents;
        private Dictionary<StudentInfo, List<AttOfDay>> mFaultsDict;
        private LoadingWindow mLoading;
        private DataTable mFaultsTable;
        private DateTime mCurrentFirst, mCurrentSecond;

        public InquireIntoAllControl()
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
                        if (mLoading != null)
                            mLoading.SetMainText("벌점 정보 추출 중...");

                        ImportData(e.Result.Data.Tables[0]);

                        StuDataGrid.DataContext = mStudents;
                        if (mLoading != null)
                            mLoading.Dispatcher.Invoke(() => mLoading.Close());

                        this.IsEnabled = true;
                        ToExcel.IsEnabled = true;
                    }
                    else
                    {
                        ParentWindow.Dispatcher.Invoke(() => MessageBox.Show(ParentWindow, "벌점이 있는 학생이 없습니다.", "근태 불량자 없음", MessageBoxButton.OK, MessageBoxImage.Information));

                        if (mLoading != null)
                            mLoading.Dispatcher.Invoke(() => mLoading.Close());
                        ;
                        this.IsEnabled = true;
                    }
                });

                return;
            }
        }

        private void ImportData(DataTable attdata)
        {
            if (mStudents == null)
                mStudents = new ObservableCollection<StudentInfo>();
            else mStudents.Clear();

            if (mFaultsDict == null)
                mFaultsDict = new Dictionary<StudentInfo, List<AttOfDay>>();
            else mFaultsDict.Clear();

            foreach (DataRow row in attdata.Rows)
            {
                AttOfDay attOfDay = new AttOfDay((DateTime)row["일자"]);
                StudentInfo si = new StudentInfo(row["학년"].ToString(), row["반"].ToString(), row["번호"].ToString(), row["성명"].ToString());

                for (int period = 0; period <= 8; ++period)
                {
                    AttEnum attEnum = (AttEnum)row[$"{period}교시"];
                    attOfDay.Add(attEnum);
                }

                if (mStudents.Contains(si))
                {
                    mFaultsDict[si].Add(attOfDay);
                }
                else
                {
                    mStudents.Add(si);
                    mFaultsDict.Add(si, new List<AttOfDay>() { attOfDay });
                }
            }

            mStudents = new ObservableCollection<StudentInfo>(mStudents.OrderBy(k => k));
        }

        private void ShowFaultsList(List<AttOfDay> attList)
        {
            if (mFaultsTable != null)
            {
                mFaultsTable.Clear();
            }

            mFaultsTable = FaultsToTable(attList);

            FaultsDataGrid.DataContext = mFaultsTable;
        }

        private DataTable FaultsToTable(List<AttOfDay> attList)
        {
            DataTable resultTable = new DataTable();

            resultTable.Columns.Add(new DataColumn("구분", typeof(string)));
            resultTable.Columns.Add(new DataColumn("일자", typeof(string)));
            resultTable.Columns.Add(new DataColumn("해당사항", typeof(string)));
            resultTable.Columns.Add(new DataColumn("벌점", typeof(int)));

            int penaltySum = 0;

            foreach (AttOfDay attOfDay in attList)
            {
                if (attOfDay.HasFaults)
                {
                    DataRow newRow = resultTable.NewRow();
                    int penalty = attOfDay.PenaltyPoints;

                    newRow["구분"] = "벌점";
                    newRow["일자"] = attOfDay.Date.ToShortDateString();
                    newRow["해당사항"] = attOfDay.Description;
                    newRow["벌점"] = penalty;

                    resultTable.Rows.Add(newRow);
                    penaltySum += penalty;
                }
            }

            if (penaltySum > 0)
            {
                DataRow sumRow = resultTable.NewRow();
                sumRow["구분"] = "합계";
                sumRow["벌점"] = penaltySum;
                resultTable.Rows.Add(sumRow);
            }
            else
            {
                DataRow row = resultTable.NewRow();
                row["해당사항"] = "근태 불량 사항 없음";
                resultTable.Rows.Add(row);
            }

            return resultTable;
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
            if (mLoading != null)
                mLoading.Close();

            mStudents?.Clear();
            mFaultsTable?.Clear();
            RecentAttReq = new TcpDataSetRequirement();
            FirstDateControl.SelectedDate = DateTime.Now;
            SecondDateControl.SelectedDate = DateTime.Now;

            InquiredText.Text = "- ~ -";
            SelectedStudentText.Text = "";

            ToExcel.IsEnabled = false;
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            StudentInfo si = mStudents[StuDataGrid.SelectedIndex];
            SelectedStudentText.Text = $"현재 선택된 학생: {si.Grade}학년 {si.Class}반 {si.Number}번 {si.Name}";
            ShowFaultsList(mFaultsDict[si]);
        }

        private void ToExcel_Click(object sender, RoutedEventArgs e)
        {
            StudentInfo[] studentInfos = mStudents.ToArray();

            SaveFileDialog saveD = new SaveFileDialog();
            saveD.Filter = $"Excel 통합문서 (.xlsx)|*.xlsx";
            saveD.Title = "저장 경로 선택";

            string fileName;
            try
            {
                string first = FirstDateControl.SelectedDate.Value.ToShortDateString();
                string second = SecondDateControl.SelectedDate.Value.ToShortDateString();
                if (first == second)
                    fileName = $"{first} 근태 불량자 명단.xlsx";
                else
                    fileName = $"{first} ~ {second} 근태 불량자 명단.xlsx";
            }
            catch (Exception)
            {
                fileName = "근태 불량자 명단.xlsx";
            }
            saveD.FileName = fileName;

            FileStream fs = null;

            try
            {
                if (saveD.ShowDialog() == true)
                {
                    fs = saveD.OpenFile() as FileStream;

                    DataTable[] tables = new DataTable[studentInfos.Length];
                    string[] heads = new string[studentInfos.Length];

                    for (int i = 0; i < studentInfos.Length; ++i)
                    {
                        tables[i] = FaultsToTable(mFaultsDict[studentInfos[i]]);
                        heads[i] = $"{studentInfos[i].Grade}학년 {studentInfos[i].Class}반 {studentInfos[i].Number}번 {studentInfos[i].Name}";
                    }

                    ExcelCreater.FromDataTableArray(tables, heads, fileName.Substring(0, fileName.IndexOf('.')), fs);
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

            if (MessageBox.Show("전교의 벌점을 조회하는 과정은 오래 걸릴 수 있습니다.\n계속하시겠습니까?", "전교 조회 전 경고", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            //TODO : 계정 권한에 따라 접근을 거부하는 코드 필요.
            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, null, null, AccessType.Inquire, ObjectToAccess.Attendance))
            {
                MessageBox.Show("데이터에 접근할 권한이 없습니다.\n\n전교의 출석부를 조회하려면 전 학년 총괄 관리자여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            mCurrentFirst = FirstDateControl.SelectedDate.Value;
            mCurrentSecond = SecondDateControl.SelectedDate.Value;

            InquiredText.Text = $"{mCurrentFirst.ToShortDateString()} ~ {mCurrentSecond.ToShortDateString()}";

            mLoading = new LoadingWindow("출석부를 가져오는 중...");
            double top = 0, left = 0;

            ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                top = ParentWindow.Top + ParentWindow.Height / 2 - mLoading.Height / 2;
                left = ParentWindow.Left + ParentWindow.Width / 2 - mLoading.Width / 2;
            })).Wait();

            mLoading.Top = top;
            mLoading.Left = left;
            mLoading.Owner = ParentWindow;

            mLoading.Show();

            // 해당 기간에 맞는 불량사항 있는 출석부 모두 요청
            ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                TcpDataSetRequirement dataReq = new TcpDataSetRequirement(new string[] { "*" }, "[출석부]",
                    $"[일자] BETWEEN {mCurrentFirst.ToSQLString()} AND {mCurrentSecond.ToSQLString()} AND ({ONLY_FAULTS_WHERE})", null);
                ParentWindow.Client.RequestDataSet(dataReq);

                RecentAttReq = dataReq;
            }));
            
            this.IsEnabled = false;
        }
    }
}

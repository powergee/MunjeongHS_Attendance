using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using TcpCore;
using static 문정고등학교_출석부.Attendance;

namespace 문정고등학교_출석부
{
    /// <summary>
    /// AttendanceByDayControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AttendanceByDayControl : UserControl, ISaveable, IClearable
    {
        public WorkWindow ParentWindow { get; private set; }
        public TcpDataSetRequirement RecentAttReq { get; private set; } = new TcpDataSetRequirement();
        public TcpDataSetRequirement RecentStuListReq { get; private set; } = new TcpDataSetRequirement();

        private DataTable mTable;
        private DataTable mOriginal;
        private string mCurrentGrade, mCurrentClass;
        private HashSet<int> mEdited;

        public AttendanceByDayControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as WorkWindow;
            DateControl.SelectedDate = DateTime.Now;
            ParentWindow.Client.ReceivedDataSetResult += Client_ReceivedDataSetResult;
        }

        private void Client_ReceivedDataSetResult(object sender, ReceivedDataSetResultEventArgs e)
        {
            if (!e.Result.Success)
                Debug.Error("서버로부터 데이터를 수신하는데 실패하였습니다.");

            if (RecentAttReq.Equals(e.Result.Requirement))
            {
                this.Dispatcher.Invoke(() =>
                {
                    // 출석부가 이미 존재함. 준비 완료.
                    if (e.Result.Data.Tables[0].Rows.Count > 0)
                    {
                        ShowData(e.Result.Data.Tables[0]);

                        this.IsEnabled = true;

                        Set1.IsEnabled = true;
                        Set2.IsEnabled = true;
                        Set3.IsEnabled = true;
                        Set4.IsEnabled = true;
                        Set5.IsEnabled = true;
                        Set6.IsEnabled = true;
                        Save.IsEnabled = false;
                        Cancel.IsEnabled = false;
                        ToExcel.IsEnabled = true;

                        mEdited = new HashSet<int>();
                    }
                    // 출석부가 존재하지 않음. 학생 목록을 요청해서 새롭게 생성해야 함. (계정 권한에 관계없이 항상 추가)
                    else
                    {
                        TcpDataSetRequirement stuListReq = new TcpDataSetRequirement(new string[] { "*" }, "[학생 명단]", $"[학년]={mCurrentGrade.ToSQLString()} AND [반]={mCurrentClass.ToSQLString()}", null);
                        ParentWindow.Client.RequestDataSet(stuListReq);

                        RecentStuListReq = stuListReq;
                    }
                });

                return;
            }

            if (RecentStuListReq.Equals(e.Result.Requirement))
            {
                this.Dispatcher.Invoke(() =>
                {
                    // 학생 목록을 가져왔음. 이제 이를 이용하여 default 출석부 입력.
                    if (e.Result.Data.Tables[0].Rows.Count > 0)
                    {
                        ParentWindow.Client.InsertedData += Client_InsertedData;
                        InsertDefaultAttToServer(e.Result.Data.Tables[0]);
                    }
                    // 학생 목록 없음. 출석부 조회 불가.
                    else
                    {
                        ParentWindow.Dispatcher.Invoke(() => MessageBox.Show("서버에 해당 학급 학생명단이 입력되지 않았습니다. 서버에 학생명단을 입력한 뒤 다시 시도하십시오.", "학생 명단 없음", MessageBoxButton.OK, MessageBoxImage.Warning));
                        this.IsEnabled = true;

                        Set1.IsEnabled = false;
                        Set2.IsEnabled = false;
                        Set3.IsEnabled = false;
                        Set4.IsEnabled = false;
                        Set5.IsEnabled = false;
                        Set6.IsEnabled = false;
                        Save.IsEnabled = false;
                        Cancel.IsEnabled = false;
                        ToExcel.IsEnabled = false;

                        mTable?.Clear();
                        mOriginal?.Clear();
                    }
                });

                return;
            }
        }

        private void ShowData(DataTable dataTable)
        {
            mTable = new DataTable();

            mTable.Columns.Add(new DataColumn("일자", typeof(string)));
            mTable.Columns.Add(new DataColumn("체크박스", typeof(CheckBox)));
            mTable.Columns.Add(new DataColumn("학년", typeof(string)));
            mTable.Columns.Add(new DataColumn("반", typeof(string)));
            mTable.Columns.Add(new DataColumn("번호", typeof(string)));
            mTable.Columns.Add(new DataColumn("성명", typeof(string)));
            mTable.Columns.Add(new DataColumn("0교시", typeof(string)));
            mTable.Columns.Add(new DataColumn("1교시", typeof(string)));
            mTable.Columns.Add(new DataColumn("2교시", typeof(string)));
            mTable.Columns.Add(new DataColumn("3교시", typeof(string)));
            mTable.Columns.Add(new DataColumn("4교시", typeof(string)));
            mTable.Columns.Add(new DataColumn("5교시", typeof(string)));
            mTable.Columns.Add(new DataColumn("6교시", typeof(string)));
            mTable.Columns.Add(new DataColumn("7교시", typeof(string)));
            mTable.Columns.Add(new DataColumn("8교시", typeof(string)));

            foreach (DataRow row in dataTable.Rows)
            {
                DataRow newRow = mTable.NewRow();

                newRow["체크박스"] = new CheckBox();
                newRow["학년"] = row["학년"].ToString();
                newRow["반"] = row["반"].ToString();
                newRow["번호"] = row["번호"].ToString();
                newRow["성명"] = row["성명"].ToString();

                for (int period = 0; period < 9; period++)
                {
                    AttEnum attendance = (AttEnum)row[$"{period}교시"];
                    newRow[$"{period}교시"] = AttToStrDict[attendance]; 
                }

                newRow["일자"] = ((DateTime)row["일자"]).ToShortDateString();

                mTable.Rows.Add(newRow);
            }

            MainDataGrid.DataContext = mTable;
            mOriginal = mTable.Copy();
        }

        private void Client_InsertedData(object sender, InsertedDataEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                Inquire.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                ParentWindow.Client.InsertedData -= Client_InsertedData;
            });
        }

        private void InsertDefaultAttToServer(DataTable stuList)
        {
            TcpDataInsert insert = new TcpDataInsert();
            insert.TableName = "출석부";
            insert.ColumnsText = null;

            List<DatabaseValues> values = new List<DatabaseValues>();

            foreach (DataRow row in stuList.Rows)
            {
                values.Add(new DatabaseValues(DateControl.SelectedDate.Value.ToSQLString(),
                    row["학년"].ToString().ToSQLString(), row["반"].ToString().ToSQLString(), row["번호"].ToString().ToSQLString(), row["성명"].ToString().ToSQLString(),
                    "1", "1", "1", "1", "1", "1", "1", "1", "1"));
            }

            insert.Values = values;

            ParentWindow.Client.InsertData(insert);
        }

        private void TopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (mTable != null)
            {
                for (int i = 0; i < mTable.Rows.Count; ++i)
                {
                    (mTable.Rows[i]["체크박스"] as CheckBox).IsChecked = true;
                } 
            }
        }

        private void TopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (mTable != null)
            {
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

        private void SetAttState(List<int> rows, string state)
        {
            if (rows == null || rows?.Count == 0)
                return;

            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, mCurrentGrade, mCurrentClass, AccessType.Edit, ObjectToAccess.Attendance))
            {
                MessageBox.Show("데이터를 수정할 권한이 없습니다.\n\n특정 학급의 출석부를 수정하려면 그 학급의 담임이여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (EditMultipleRadio.IsChecked == true)
            {
                int periodCount = int.Parse(PeriodCount.Text);

                foreach (int index in rows)
                {
                    for (int period = 0; period <= periodCount; ++period)
                    {
                        mTable.Rows[index][$"{period}교시"] = state;
                    }

                    mEdited.Add(index);
                }
            }
            else
            {
                // 무단 결석 : 뒤에 있는 교시도 수정.
                // 무단 지각 : 앞에 있는 교시도 수정.
                // 무단 조퇴 : 뒤에 있는 교시도 수정.

                int period = int.Parse(EditOne_Period.Text);
                int endOfPeriods = int.Parse(PeriodCount.Text);

                if (endOfPeriods < period)
                {
                    ParentWindow.Dispatcher.Invoke(() => MessageBox.Show(ParentWindow, "수정하려는 교시가 이 날의 교시수보다 큽니다.", "교시가 범위를 벗어남", MessageBoxButton.OK, MessageBoxImage.Warning));
                    
                    Save.IsEnabled = true;
                    Cancel.IsEnabled = true;
                    return;
                }

                foreach (int index in rows)
                {
                    switch (StrToAttDict[state])
                    {
                        case AttEnum.Absent:
                        case AttEnum.EarlyLeft:
                            for (int i = period; i <= endOfPeriods; ++i)
                                mTable.Rows[index][$"{i}교시"] = state;
                            break;

                        case AttEnum.Late:
                            for (int i = 0; i <= period; ++i)
                                mTable.Rows[index][$"{i}교시"] = state;
                            break;

                        default:
                            mTable.Rows[index][$"{period}교시"] = state;
                            break;
                    }

                    mEdited.Add(index);
                }
            }

            Save.IsEnabled = true;
            Cancel.IsEnabled = true;
        }

        private void Set1_Click(object sender, RoutedEventArgs e)
        {
            SetAttState(GetCheckedRows(), Set1.Content.ToString());
        }

        private void Set2_Click(object sender, RoutedEventArgs e)
        {
            SetAttState(GetCheckedRows(), Set2.Content.ToString());
        }

        private void Set3_Click(object sender, RoutedEventArgs e)
        {
            SetAttState(GetCheckedRows(), Set3.Content.ToString());
        }

        private void Set4_Click(object sender, RoutedEventArgs e)
        {
            SetAttState(GetCheckedRows(), Set4.Content.ToString());
        }

        private void Set5_Click(object sender, RoutedEventArgs e)
        {
            SetAttState(GetCheckedRows(), Set5.Content.ToString());
        }

        private void Set6_Click(object sender, RoutedEventArgs e)
        {
            SetAttState(GetCheckedRows(), Set6.Content.ToString());
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            UpdateServerData();
        }

        private void UpdateServerData()
        {
            if (mEdited.Count == 0)
            {
                MessageBox.Show("변경사항이 없으므로 저장할 데이터가 없습니다.", "저장하지 않음", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, mCurrentGrade, mCurrentClass, AccessType.Edit, ObjectToAccess.Attendance))
            {
                MessageBox.Show("데이터를 수정할 권한이 없습니다.\n\n특정 학급의 출석부를 수정하려면 그 학급의 담임이여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            foreach (int index in mEdited)
            {
                TcpDataUpdate update = new TcpDataUpdate();
                update.TableName = "출석부";

                for (int period = 0; period <= 8; ++period)
                    update.Setter.Add($"[{period}교시]", ((int)StrToAttDict[mTable.Rows[index][$"{period}교시"].ToString()]).ToString());

                update.Where = $"[일자]=#{mOriginal.Rows[index]["일자"]}# AND [학년]={mOriginal.Rows[index]["학년"].ToString().ToSQLString()} AND [반]={mOriginal.Rows[index]["반"].ToString().ToSQLString()} AND [번호]={mOriginal.Rows[index]["번호"].ToString().ToSQLString()}";

                ParentWindow.Client.UpdateData(update);
            }

            mEdited.Clear();
            Save.IsEnabled = false;
            Cancel.IsEnabled = false;
            mOriginal = mTable.Copy();
        }

        /// <summary>
        /// 반환이 Yes와 No와 null이 아닐경우에 하려던 작업을 중단하면 됨.
        /// </summary>
        /// <returns></returns>
        public MessageBoxResult? CheckSave(bool wait)
        {
            MessageBoxResult result;

            if (mEdited == null ? false : mEdited.Count > 0)
            {
                result = MessageBox.Show("수정된 자료가 저장되지 않았습니다.\n저장하시겠습니까?", "변경 사항 저장", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        if (wait)
                            this.Dispatcher.BeginInvoke((Action)(() => UpdateServerData())).Wait();
                        else
                            this.Dispatcher.Invoke(() => UpdateServerData());
                        break;

                    case MessageBoxResult.No:
                        break;

                    default:
                        break;
                }
            }
            else return null;

            return result;
        }

        private void ToExcel_Click(object sender, RoutedEventArgs e)
        {
            DataTable dataToProcess = mTable.Clone();
            dataToProcess.Columns.Remove("체크박스");
            dataToProcess.Columns["학년"].DataType = typeof(int);
            dataToProcess.Columns["반"].DataType = typeof(int);
            dataToProcess.Columns["번호"].DataType = typeof(int);

            foreach (DataRow row in mTable.Rows)
            {
                dataToProcess.ImportRow(row);
            }

            dataToProcess.ToExcel($"{mTable.Rows[0]["일자"].ToString()} {mCurrentGrade}학년 {mCurrentClass}반 출석부.xlsx", "출석부");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            mEdited.Clear();
            Save.IsEnabled = false;
            Cancel.IsEnabled = false;
            mTable = mOriginal.Copy();

            MainDataGrid.DataContext = mTable;
        }

        private void Save_Click_1(object sender, RoutedEventArgs e)
        {
            UpdateServerData();
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

            MessageBoxResult? checkResult = CheckSave(true);
            if (checkResult != null)
            {
                if (checkResult.Value != MessageBoxResult.Yes && checkResult.Value != MessageBoxResult.No)
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
                    $"[일자]={DateControl.SelectedDate.Value.ToSQLString()} AND [학년]={mCurrentGrade.ToSQLString()} AND [반]={mCurrentClass.ToSQLString()}", "Val([번호])");
                    ParentWindow.Client.RequestDataSet(dataReq);

                    RecentAttReq = dataReq;
                })).Wait();
            }));

            this.IsEnabled = false;
        }

        public void Clear()
        {
            mTable?.Clear();
            mOriginal?.Clear();
            mEdited?.Clear();
            mCurrentClass = null;
            mCurrentGrade = null;
            RecentAttReq = new TcpDataSetRequirement();
            RecentStuListReq = new TcpDataSetRequirement();
            DateControl.SelectedDate = DateTime.Now;

            ToExcel.IsEnabled = false;
            Set1.IsEnabled = false;
            Set2.IsEnabled = false;
            Set3.IsEnabled = false;
            Set4.IsEnabled = false;
            Set5.IsEnabled = false;
            Set6.IsEnabled = false;
            Save.IsEnabled = false;
            Cancel.IsEnabled = false;
            TopCheckBox.IsChecked = false;
        }
    }
}

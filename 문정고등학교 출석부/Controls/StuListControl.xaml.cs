using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace 문정고등학교_출석부
{
    /// <summary>
    /// StuListControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class StuListControl : UserControl, IClearable
    {
        private string mCurrentGrade;
        private string mCurrentClass;
        private DataTable mTable;
        private DataTable mOriginal;
        private ChildWindowManager mChildManager;

        private bool IsTableEmpty => mTable == null ? true : "작성된 학생 없음" == (mTable.Rows[0]["성명"].ToString());

        public TcpDataSetRequirement RecentStuListReq { get; private set; } = new TcpDataSetRequirement();

        public WorkWindow ParentWindow { get; private set; }

        public StuListControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as WorkWindow;
            ParentWindow.Client.ReceivedDataSetResult += Client_ReceivedDataSetResult;
            mChildManager = new ChildWindowManager(ParentWindow);
        }

        private void Client_ReceivedDataSetResult(object sender, ReceivedDataSetResultEventArgs e)
        {
            if (!e.Result.Success)
                Debug.Error("서버로부터 데이터를 수신하는데 실패하였습니다.");

            if (RecentStuListReq.Equals(e.Result.Requirement))
            {
                this.Dispatcher.Invoke(() =>
                {
                    ProcessAndShowData(e.Result.Data.Tables[0]);

                    this.IsEnabled = true;
                    this.ToExcel.IsEnabled = true;
                    this.AddStu.IsEnabled = true;
                });

                return;
            }
        }

        private void ProcessAndShowData(DataTable stuList)
        {
            mTable = new DataTable();

            mTable.Columns.Add(new DataColumn("학년", typeof(string)));
            mTable.Columns.Add(new DataColumn("반", typeof(string)));
            mTable.Columns.Add(new DataColumn("번호", typeof(string)));
            mTable.Columns.Add(new DataColumn("성명", typeof(string)));
            mTable.Columns.Add(new DataColumn("등록날짜", typeof(string)));

            if (stuList.Rows.Count == 0)
            {
                DataRow row = mTable.NewRow();
                row["성명"] = "작성된 학생 없음";
                mTable.Rows.Add(row);
            }
            else
            {
                foreach (DataRow row in stuList.Rows)
                {
                    DataRow newRow = mTable.NewRow();

                    newRow["학년"] = row["학년"];
                    newRow["반"] = row["반"];
                    newRow["번호"] = row["번호"];
                    newRow["성명"] = row["성명"];
                    newRow["등록날짜"] = ((DateTime)row["등록 날짜"]).ToShortDateString();

                    mTable.Rows.Add(newRow);
                }
            }

            mOriginal = mTable.Copy();
            MainDataGrid.DataContext = mTable;
        }

        private void ToExcel_Click(object sender, RoutedEventArgs e)
        {
            mTable.ToExcel($"{mCurrentGrade}학년 {mCurrentClass}반 학생 명단", "학생 명단");
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

            if (!AuthorityChecker.CheckAuthority(accType, gradeStr, classStr, AccessType.Inquire, ObjectToAccess.StudentList))
            {
                MessageBox.Show("데이터에 접근할 권한이 없습니다.\n\n특정 학급의 학생 명단을 조회하려면 그 학급의 담임이거나, 전 학년 총괄 관리자여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            ToExcel.IsEnabled = false;

            mCurrentGrade = gradeStr;
            mCurrentClass = classStr;

            // 정해진 기간중 근태 불량 상태가 있는 출석부만 조회.
            ParentWindow.ChildManager.TryToShow<LoadingWindow>("서버와 통신 중...", (WaitCallback)((o) =>
            {
                ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    TcpDataSetRequirement dataReq = new TcpDataSetRequirement(new string[] { "*" }, "[학생 명단]",
                    $"[학년]={mCurrentGrade.ToSQLString()} AND [반]={mCurrentClass.ToSQLString()}", "Val([번호])");
                    ParentWindow.Client.RequestDataSet(dataReq);

                    RecentStuListReq = dataReq;
                })).Wait();
            }));

            this.IsEnabled = false;
        }

        public void Clear()
        {
            mCurrentClass = null;
            mCurrentGrade = null;
            mTable?.Clear();
            mOriginal?.Clear();
            RecentStuListReq = new TcpDataSetRequirement();

            AddStu.IsEnabled = false;
            ToExcel.IsEnabled = false;
        }

        private void AddStu_Click(object sender, RoutedEventArgs e)
        {
            //TODO : 계정 권한에 따라 접근을 거부하는 코드 필요.
            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, mCurrentGrade, mCurrentClass, AccessType.Edit, ObjectToAccess.StudentList))
            {
                MessageBox.Show("데이터를 수정할 권한이 없습니다.\n\n특정 학급의 학생 명단을 수정하려면 그 학급의 담임이여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            string[] existNumbers;
            if (IsTableEmpty)
            {
                existNumbers = new string[] { };
            }
            else
            {
                List<string> numsList = new List<string>();
                foreach (DataRow row in mTable.Rows)
                {
                    numsList.Add(row["번호"].ToString());
                }

                existNumbers = numsList.ToArray();
            }

            ParentWindow.Dispatcher.Invoke(() => mChildManager.TryToShow<AddStudentsWindow>((EventHandler)((o, ev) => 
            {
                this.Dispatcher.Invoke(() => this.IsEnabled = true);
                ParentWindow.Dispatcher.Invoke(() => ParentWindow.IsEnabled = true);
            }), this, existNumbers,mCurrentGrade, mCurrentClass));

            this.IsEnabled = false;
            ParentWindow.Dispatcher.Invoke(() => ParentWindow.IsEnabled = false);
        }

        public bool Import(DataTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (mTable == null)
                return false;

            bool success = false;
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (IsTableEmpty)
                    mTable.Rows.Clear();

                try
                {
                    TcpDataInsert insert = new TcpDataInsert();
                    insert.TableName = "[학생 명단]";
                    insert.ColumnsText = null;

                    List<DatabaseValues> values = new List<DatabaseValues>();

                    foreach (DataRow row in table.Rows)
                    {
                        DataRow newRow = mTable.NewRow();

                        newRow["학년"] = mCurrentGrade;
                        newRow["반"] = mCurrentClass;
                        newRow["번호"] = row["번호"];
                        newRow["성명"] = row["성명"];
                        newRow["등록날짜"] = (DateTime.Now).ToShortDateString();

                        values.Add(new DatabaseValues(newRow["학년"].ToString().ToSQLString(),
                            newRow["반"].ToString().ToSQLString(),
                            newRow["번호"].ToString().ToSQLString(),
                            newRow["성명"].ToString().ToSQLString(),
                            DateTime.Now.ToSQLString()));

                        mTable.Rows.Add(newRow);
                    }

                    insert.Values = values;

                    ParentWindow.Client.InsertData(insert);

                    success = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"학생 명단에 데이터를 추가하는데 실패하였습니다...\n\n디버그용 메세지 : {e.Message}\n{e.StackTrace}", "추가 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                    success = false;
                }
            })).Wait();

            return success;
        }

        public bool Update(DataRow oldRow, string newNum, string newName)
        {
            if (oldRow == null)
            {
                throw new ArgumentNullException(nameof(oldRow));
            }

            if (mTable == null)
                return false;

            bool success = false;
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    TcpDataUpdate update = new TcpDataUpdate();
                    update.TableName = "[출석부]";
                    update.Setter.Add("[번호]", newNum.ToSQLString());
                    update.Setter.Add("[성명]", newName.ToSQLString());
                    update.Where = $"[학년]={oldRow["학년"].ToString().ToSQLString()} AND [반]={oldRow["반"].ToString().ToSQLString()} AND [번호]={oldRow["번호"].ToString().ToSQLString()} AND [성명]={oldRow["성명"].ToString().ToSQLString()}";

                    ParentWindow.Client.UpdateData(update);

                    success = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"출석부의 데이터를 새로운 번호와 성명으로 수정하는데 실패하였습니다...\n\n디버그용 메세지 : {e.Message}\n{e.StackTrace}", "수정 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                    success = false;
                }

                if (!success)
                    return;

                try
                {
                    TcpDataUpdate update = new TcpDataUpdate();
                    update.TableName = "[학생 명단]";
                    update.Setter.Add("[번호]", newNum.ToSQLString());
                    update.Setter.Add("[성명]", newName.ToSQLString());
                    update.Where = $"[학년]={oldRow["학년"].ToString().ToSQLString()} AND [반]={oldRow["반"].ToString().ToSQLString()} AND [번호]={oldRow["번호"].ToString().ToSQLString()} AND [성명]={oldRow["성명"].ToString().ToSQLString()}";

                    oldRow["번호"] = newNum;
                    oldRow["성명"] = newName;

                    ParentWindow.Client.UpdateData(update);

                    success = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"학생 명단의 데이터를 수정하는데 실패하였습니다...\n\n디버그용 메세지 : {e.Message}\n{e.StackTrace}", "수정 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                    success = false;
                }
            })).Wait();

            return success;
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //TODO : 계정 권한에 따라 접근을 거부하는 코드 필요.
            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, mCurrentGrade, mCurrentClass, AccessType.Edit, ObjectToAccess.StudentList))
            {
                MessageBox.Show("데이터를 수정할 권한이 없습니다.\n\n특정 학급의 학생 명단을 수정하려면 그 학급의 담임이여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (!IsTableEmpty)
            {
                DataRow selectedRow;

                try
                {
                    selectedRow = mTable.Rows[MainDataGrid.SelectedIndex];
                }
                catch (Exception)
                {
                    ParentWindow.Dispatcher.Invoke(() => MessageBox.Show("수정하고자 하는 학생을 정확히 선택해주십시오.", "학생 선택 실패", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                List<string> numsList = new List<string>();
                foreach (DataRow row in mTable.Rows)
                {
                    numsList.Add(row["번호"].ToString());
                }

                string[] existNumbers = numsList.ToArray();

                ParentWindow.Dispatcher.Invoke(() => mChildManager.TryToShow<EditStudentsWindow>((EventHandler)((o, ev) =>
                {
                    this.Dispatcher.Invoke(() => this.IsEnabled = true);
                    ParentWindow.Dispatcher.Invoke(() => ParentWindow.IsEnabled = true);
                }), this, existNumbers, mCurrentGrade, mCurrentClass, selectedRow, MainDataGrid.SelectedIndex));

                this.IsEnabled = false;
                ParentWindow.Dispatcher.Invoke(() => ParentWindow.IsEnabled = false);
            }
        }
    }
}

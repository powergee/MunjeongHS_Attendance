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

namespace 문정고등학교_출석부
{
    /// <summary>
    /// UnacceptedAccountsControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UnacceptedAccountsControl : UserControl, IClearable
    {
        public WorkWindow ParentWindow { get; private set; }

        public TcpDataSetRequirement RecentTeachersReq { get; private set; } = new TcpDataSetRequirement();

        private DataTable mTable;

        public UnacceptedAccountsControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as WorkWindow;
            ParentWindow.Client.ReceivedDataSetResult += Client_ReceivedDataSetResult; ;
        }

        private void Client_ReceivedDataSetResult(object sender, ReceivedDataSetResultEventArgs e)
        {
            if (!e.Result.Success)
                Debug.Error("서버로부터 데이터를 수신하는데 실패하였습니다.");

            if (RecentTeachersReq.Equals(e.Result.Requirement))
            {
                this.Dispatcher.Invoke(() =>
                {
                    ProcessAndShowData(e.Result.Data.Tables[0]);

                    this.IsEnabled = true;
                });

                return;
            }
        }

        private void ProcessAndShowData(DataTable dataTable)
        {
            mTable = new DataTable();

            mTable.Columns.Add(new DataColumn("교사ID", typeof(string)));
            mTable.Columns.Add(new DataColumn("성명", typeof(string)));
            mTable.Columns.Add(new DataColumn("계정구분", typeof(string)));
            mTable.Columns.Add(new DataColumn("체크박스", typeof(CheckBox)));

            foreach (DataRow row in dataTable.Rows)
            {
                DataRow newRow = mTable.NewRow();

                newRow["교사ID"] = row["교사 ID"];
                newRow["성명"] = row["성명"];
                newRow["계정구분"] = AuthorityChecker.ToDescription(row["계정 구분"].ToString());
                newRow["체크박스"] = new CheckBox();

                mTable.Rows.Add(newRow);
            }

            MainDataGrid.DataContext = mTable;
        }

        private void Inquire_Click(object sender, RoutedEventArgs e)
        {
            //TODO : 계정 권한에 따라 접근을 거부하는 코드 필요.
            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, null, null, AccessType.Inquire, ObjectToAccess.Accounts))
            {
                MessageBox.Show("데이터에 접근할 권한이 없습니다.\n\n대기중인 계정 목록을 조회하려면 전 학년 총괄 관리자여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            ParentWindow.ChildManager.TryToShow<LoadingWindow>("서버와 통신 중...", (WaitCallback)((o) =>
            {
                ParentWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    TcpDataSetRequirement dataReq = new TcpDataSetRequirement(new string[] { "[교사 ID]", "[성명]", "[계정 구분]" }, "[교사 목록]",
                    $"[가입 허가 여부]=False", "[성명]");
                    ParentWindow.Client.RequestDataSet(dataReq);

                    RecentTeachersReq = dataReq;
                })).Wait();
            }));

            this.IsEnabled = false;
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
            if (mTable == null ? true : mTable.Rows.Count == 0)
                return null;

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

        private void AcceptChecked_Click(object sender, RoutedEventArgs e)
        {
            List<int> rowIndexes = GetCheckedRows();

            if (rowIndexes == null ? true : rowIndexes.Count == 0)
            {
                MessageBox.Show("체크된 항목이 없습니다.", "항목 없음", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                return;
            }

            string accType = ParentWindow.Client.AccountType;

            if (!AuthorityChecker.CheckAuthority(accType, null, null, AccessType.Edit, ObjectToAccess.Accounts))
            {
                MessageBox.Show("데이터를 수정할 권한이 없습니다.\n\n대기중인 계정 목록을 수정하려면 전 학년 총괄 관리자여야 합니다.",
                    "권한 없음", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            MessageBoxResult qResult = MessageBox.Show($"총 {rowIndexes.Count} 개의 계정이 수정됩니다.\n계속하시겠습니까?", "준비 완료", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (qResult == MessageBoxResult.Yes)
            {
                foreach (int index in rowIndexes)
                {
                    TcpDataUpdate update = new TcpDataUpdate();
                    update.TableName = "[교사 목록]";
                    update.Setter.Add("[가입 허가 여부]", "True");
                    update.Where = $"[교사 ID]={mTable.Rows[index]["교사ID"].ToString().ToSQLString()}";

                    ParentWindow.Client.UpdateData(update);
                }

                Clear();
                this.Dispatcher.Invoke(() =>
                {
                    Inquire.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                });
            }
            else
            {
                return;
            }
        }

        public void Clear()
        {
            mTable?.Clear();
            TopCheckBox.IsChecked = false;
            RecentTeachersReq = new TcpDataSetRequirement();
        }
    }
}

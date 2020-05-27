using System;
using System.Collections.Generic;
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
using TcpCore;
using System.Reflection;

namespace 문정고등학교_출석부
{
    /// <summary>
    /// WorkWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkWindow : Window
    {
        public ProgramSetting Setting { get; private set; }
        public EventClient Client { get; private set; }
        public ChildWindowManager ChildManager { get; private set; }

        UserControl[] mControls;
        UserControl mCurrent;

        public WorkWindow()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
        }

        public WorkWindow(EventClient client, ProgramSetting setting)
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            Setting = setting;
            Client = client;
            mControls = new UserControl[] { AttByClass, AttByDay, InqAbsentees, InqIndividual, InqOver, StuList, Accounts, AcceptedAccounts, InqAll };
            ChildManager = new ChildWindowManager(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            string version = null;
            try
            {
                version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch (Exception)
            {
                version = "버전 조회 불가";
            }

            VersionText.Text = version;

            Debug.MessageWritten += ((o, messageEv) =>
            {
                if (messageEv.Type == DebugMessageType.Error)
                {
                    MessageBox.Show($"프로그램 작동 도중 오류가 발생하였습니다!\n{messageEv.Message}\n프로그램을 종료합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);

                    this.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
                }
            });

            if (Client == null)
                return;

            if (!Client.LoginAccepted)
            {
                MessageBox.Show("로그인되지 않은 상태에서 프로그램에 접근할 수 없습니다.", "로그인되지 않았음", MessageBoxButton.OK, MessageBoxImage.Error);
                Client.Disconnect();
                e.Handled = true;
                System.Windows.Application.Current.Shutdown();
                return;
            }

            NameText.Text = Client.AccountName;

            if (Client.AccountType == "Super")
            {
                TypeText.Text = "전 학년 총괄";

                SetClassCombo(1, 1);
            }
            else
            {
                string[] parts = Client.AccountType.Split(' ');

                if (parts[0] == "Head")
                {
                    TypeText.Text = $"{parts[1]}학년 부장";

                    SetClassCombo(int.Parse(parts[1]), 1);
                }
                else
                {
                    TypeText.Text = $"{parts[0]}-{parts[1]} 담임";
                    
                    UnacceptedAccountsText.Visibility = Visibility.Collapsed;
                    AcceptedAccountsText.Visibility = Visibility.Collapsed;
                    InquireIntoAllText.Visibility = Visibility.Collapsed;
                    MenuSeparater.Visibility = Visibility.Collapsed;

                    SetClassCombo(int.Parse(parts[0]), int.Parse(parts[1]));
                }
            }

            this.Width = (double)Setting["Width"];
            this.Height = (double)Setting["Height"];

            ViewControl(AttByClass);
        }

        private void SetClassCombo(int gradeNum, int classNum)
        {
            if (gradeNum < 1 || gradeNum > 3)
                throw new ArgumentOutOfRangeException("gradeNum");
            if (classNum < 1 || classNum > 12)
                throw new ArgumentOutOfRangeException("classNum");

            GradeCombo.SelectedIndex = gradeNum - 1;
            ClassCombo.SelectedIndex = classNum - 1;
        }

        private void AttendanceByClassText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(AttByClass);
        }

        private void AttendanceByDayText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(AttByDay);
        }

        private void InquireIntoAbsenteesText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(InqAbsentees);
        }

        private void InquireIntoIndividualText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(InqIndividual);
        }

        private void InquireIntoOverRefText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(InqOver);
        }

        private void StuListText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(StuList);
        }

        private void UnacceptedAccountsText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(Accounts);
        }

        private void AcceptedAccountsText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(AcceptedAccounts);
        }

        private void InquireIntoAllText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(InqAll);
        }

        private void ClearDatabaseText_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void ViewControl(UserControl control)
        {
            if (mCurrent != null && mCurrent is ISaveable)
            {
                MessageBoxResult? checkResult = (mCurrent as ISaveable).CheckSave(false);
                if (checkResult != null)
                {
                    if (checkResult.Value != MessageBoxResult.Yes && checkResult.Value != MessageBoxResult.No)
                        return;
                }
            }

            if (mCurrent != null && mCurrent is IClearable)
            {
                (mCurrent as IClearable).Clear();
            }

            control.Visibility = Visibility.Visible;
            control.IsEnabled = true;

            foreach (UserControl element in mControls)
            {
                if (element == control)
                    continue;

                element.IsEnabled = false;
                element.Visibility = Visibility.Collapsed;
            }

            mCurrent = control;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mCurrent != null && mCurrent is ISaveable)
            {
                MessageBoxResult? checkResult = (mCurrent as ISaveable).CheckSave(true);
                if (checkResult != null)
                {
                    if (checkResult.Value != MessageBoxResult.Yes && checkResult.Value != MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            Setting["Width"] = this.Width;
            Setting["Height"] = this.Height;
            Setting.Save();

            Client.Disconnect();
        }
    }
}

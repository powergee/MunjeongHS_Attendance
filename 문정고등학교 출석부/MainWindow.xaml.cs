using System;
using System.Collections.Generic;
using System.Data.OleDb;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.IO;
using TcpCore;
using System.Threading;

namespace 문정고등학교_출석부
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly BitmapImage SUCCESS_IMG = new BitmapImage(new Uri(@"/Resources/Success.png", UriKind.Relative));
        public static readonly BitmapImage FAILURE_IMG = new BitmapImage(new Uri(@"/Resources/Failure.png", UriKind.Relative));

        ChildWindowManager mChildManager;
        ProgramSetting mSetting;
        EventClient mClient;
        bool mClientLoggedIn = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            mClient.RequestLogin(IDText.Text, PWText.Password);
            LogInButton.IsEnabled = false;
        }

        private void MClient_ReceivedLoginResult(object sender, ReceivedLoginResultEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (e.Result.Success)
                {
                    WorkWindow workWindow = new WorkWindow(mClient, mSetting);

                    if ((bool)mSetting["RememberID"])
                        mSetting["ID"] = IDText.Text;
                    mSetting.Save();

                    workWindow.Show();
                    mClientLoggedIn = true;
                    this.Close();

                    mClient.ReceivedLoginResult -= MClient_ReceivedLoginResult;
                }
                else
                {
                    this.Dispatcher.Invoke(() => MessageBox.Show(this, $"로그인하지 못했습니다.\n{e.Result.ToString()}", "로그인 실패", MessageBoxButton.OK, MessageBoxImage.Error));

                    LogInButton.IsEnabled = true;
                }
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                mSetting = ProgramSetting.Load();
                mChildManager = new ChildWindowManager(this);

                if ((bool)mSetting["RememberID"])
                {
                    SaveIDCheck.IsChecked = true;
                    IDText.Text = (mSetting["ID"] as string) ?? "";
                }

                mChildManager.TryToShow<LoadingWindow>(new EventHandler((o, ev) =>
                {
                    this.Dispatcher.Invoke(() => { SetState(); });
                }), "서버에 연결 중입니다...", (WaitCallback)Connect);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그인 준비를 하는 과정에서 오류가 발생하였습니다.\n{ex.Message}");
            }
        }

        private void SetState()
        {
            if (mClient == null)
            {
                StateImage.Source = FAILURE_IMG;
                StateDescribeText.Text = "서버와 연결하지 못했습니다...";
                IPEndPointText.Text = "-";

                LogInButton.IsEnabled = false;
                Register.IsEnabled = false;
            }
            else
            {
                StateImage.Source = SUCCESS_IMG;
                StateDescribeText.Text = "서버와 연결되었습니다!";
                IPEndPointText.Text = mClient.IP.ToString();
            }
        }

        private void Connect(object o)
        {
            try
            {
                TcpClient client = new TcpClient(mSetting["ServerIP"] as string, (int)mSetting["ServerPort"]);
                mClient = new EventClient(client, false);

                mClient.ReceivedLoginResult += MClient_ReceivedLoginResult;
            }
            catch (Exception e)
            {
                this.Dispatcher.Invoke(() => MessageBox.Show(this, "서버에 연결할 수 없습니다. 인터넷 연결상태를 확인해주세요.\n\n자세한 오류 메세지 : " + e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void SaveIDCheck_Checked(object sender, RoutedEventArgs e)
        {
            mSetting["RememberID"] = true;
        }

        private void SaveIDCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            mSetting["RememberID"] = false;
        }

        private void EnterKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && LogInButton.IsEnabled)
            {
                LogInButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;

            mChildManager.TryToShow<RegisterWindow>(new EventHandler((o, ev) =>
            {
                this.Dispatcher.Invoke(() => { this.IsEnabled = true; });
            }), mClient);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mClient != null && !mClientLoggedIn)
                mClient.Disconnect();
        }
    }
}

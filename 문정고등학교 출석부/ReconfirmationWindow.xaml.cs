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


// 데이터베이스를 초기화하는 작업은 개발이 잠정 중단되었음.
// 나중에 다시 개발할 때는
// 비밀번호를 2차 확인하는 기능을 다시 확인하고,
// 서버에 데이터베이스 초기화 명령을 내리는 기능을 구현하여야 함.

namespace 문정고등학교_출석부
{
    /// <summary>
    /// ReconfirmationWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReconfirmationWindow : Window
    {
        private string InfoStr = null;
        private string IDStr = null;
        private EventClient Client = null;

        public bool? Result = null;

        public ReconfirmationWindow(string info, string id, EventClient client)
        {
            InitializeComponent();

            InfoStr = info;
            IDStr = id;
            Client = client;
        }

        private void EnterKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && LogInButton.IsEnabled)
            {
                LogInButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Info.Text = InfoStr;
            IDText.Text = IDStr;

            Client.ReceivedLoginResult += Client_ReceivedLoginResult;
        }

        private void Client_ReceivedLoginResult(object sender, ReceivedLoginResultEventArgs e)
        {
            if (e.Result.IsAccepted)
            {
                MessageBox.Show(this, "패스워드 재확인이 성공하였습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                Result = true;
                this.Close();
                Client.ReceivedLoginResult -= Client_ReceivedLoginResult;
            }
            else
            {
                MessageBox.Show(this, $"패스워드 재확인이 실패하였습니다.\n{e.Result.ToString()}", "실패", MessageBoxButton.OK, MessageBoxImage.Error);
                LogInButton.IsEnabled = true;
            }
        }

        private void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            Client.RequestLogin(IDStr, PWText.Password);
            LogInButton.IsEnabled = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Result != true)
                Result = false;
        }
    }
}

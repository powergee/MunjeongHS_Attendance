using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using TcpCore;

namespace 문정고등학교_출석부
{
    /// <summary>
    /// RegisterWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private EventClient mClient;
        ChildWindowManager mChildManager;
        TcpAccountInfo AccountInfo;

        public RegisterWindow(EventClient client)
        {
            InitializeComponent();
            mClient = client;
        }

        private void PasswordText2_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordText2.Password) || PasswordText1.Password == PasswordText2.Password)
                PasswordText2.BorderBrush = Brushes.Green;
            else PasswordText2.BorderBrush = Brushes.Red;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordText2.Password) || PasswordText1.Password != PasswordText2.Password)
            {
                MessageBox.Show(this, "비밀번호 항목과 비밀번호 확인 항목이 일치하지 않거나, 사용할 수 없는 비밀번호를 사용했습니다.", "가입 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(IDText.Text) || string.IsNullOrWhiteSpace(PasswordText2.Password)
                || string.IsNullOrWhiteSpace(NameText.Text))
            {
                MessageBox.Show(this, "각 항목을 제대로 입력해주십시오.\n모든 항목은 비어있거나 공백만으로 구성되어선 안됩니다.", "가입 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string type;

                if (SuperRadio.IsChecked == true)
                    type = "Super";
                else if (HomeRadio.IsChecked == true)
                    type = $"{HomeGrade.Text} {HomeClass.Text}";
                else
                {
                    MessageBox.Show(this, "회원 구분을 체크해주십시오.", "가입 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                mClient.ReceivedRegisterResult += MClient_ReceivedRegisterResult;
                AccountInfo = new TcpAccountInfo(IDText.Text, PasswordText2.Password, NameText.Text, type);
                mClient.Register(AccountInfo);

                this.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"가입을 시도하던 중 예기치 못한 오류가 발생하였습니다.\n{ex.Message}", "가입 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void MClient_ReceivedRegisterResult(object sender, ReceivedRegisterResultEventArgs e)
        {
            if (e.Result.Success)
            {
                this.Dispatcher.Invoke(() => 
                {
                    MessageBox.Show(this, "성공적으로 회원가입 요청을 마쳤습니다.", "가입 요청 성공", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(this, "ID가 이미 존재합니다.", "가입 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.IsEnabled = true;
                });
            }

            mClient.ReceivedRegisterResult -= MClient_ReceivedRegisterResult;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mChildManager = new ChildWindowManager(this);
        }
    }
}

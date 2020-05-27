using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
using System.Reflection;
using System.Diagnostics;

namespace 문정고등학교_출석부_Server
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly BitmapImage SUCCESS_IMG = new BitmapImage(new Uri(@"/Resources/Success.png", UriKind.Relative));
        public static readonly BitmapImage FAILURE_IMG = new BitmapImage(new Uri(@"/Resources/Failure.png", UriKind.Relative));

        public static readonly string DIRECTORY = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public ChildWindowManager ChildManager { get; private set; }
        public EventServer Server { get; private set; }

        UserControl[] mControls;
        private DateTime? mStartTime = null;
        private bool mKillProcess = false;
        private System.Windows.Forms.NotifyIcon mNotify;

        public MainWindow()
        {
            InitializeComponent();

            mControls = new UserControl[] { Clients, Logs };

            mNotify = new System.Windows.Forms.NotifyIcon();
            var iconHandle = 문정고등학교_출석부_Server.Properties.Resources.Book.GetHicon();
            mNotify.Icon = System.Drawing.Icon.FromHandle(iconHandle);
            mNotify.MouseDoubleClick += MNotify_MouseDoubleClick;
            mNotify.Visible = true;


        }

        private void MNotify_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Show();
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DatabaseManager.Initialize();
            ChildManager = new ChildWindowManager(this);
            ChildManager.TryToShow<LoadingWindow>("서버를 여는 중 입니다...", (WaitCallback)OpenServer);

            //MessageBox.Show("서버가 실행되었습니다.\n곧 트레이에 숨겨집니다..", "시작됨", MessageBoxButton.OK, MessageBoxImage.Information);

            this.HideToTray();
        }

        private void OpenServer(object o)
        {
            try
            {
                Server = new EventServer(31006);
                Server.Start();

                this.Dispatcher.Invoke(() =>
                {
                    SetState(true);
                    Clients.BindCollection();
                });
            }
            catch (Exception e)
            {
                this.Dispatcher.Invoke(() =>
                {
                    SetState(false);
                    MessageBox.Show(this, "서버를 시작할 수 없습니다. 인터넷 연결상태를 확인해주세요.\n\n자세한 오류 메세지 : " + e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void SetState(bool serverSuccess)
        {
            if (serverSuccess)
            {
                PortText.Text = $"{Server.Port}번 포트";
                StateText.Text = "Listening";
                StateImage.Source = SUCCESS_IMG;

                mStartTime = DateTime.Now;
            }
            else
            {
                PortText.Text = $"-";
                StateText.Text = "서버 생성 실패";
                StateImage.Source = FAILURE_IMG;
            }
        }

        private void ClientsText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(Clients);
        }

        private void LogsText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewControl(Logs);
        }

        private void ViewControl(UserControl control)
        {
            control.Visibility = Visibility.Visible;
            control.IsEnabled = true;

            foreach (UserControl element in mControls)
            {
                if (element == control)
                    continue;

                element.IsEnabled = false;
                element.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            {
                if (mKillProcess)
                {
                    if (mStartTime != null)
                    {
                        if (!Directory.Exists($"{DIRECTORY}\\Logs"))
                            Directory.CreateDirectory($"{DIRECTORY}\\Logs");

                        using (FileStream fs = new FileStream($"{DIRECTORY}\\Logs\\{mStartTime.Value.ToShortDateString()} ~ {DateTime.Now.ToShortDateString()} Server Log.txt", FileMode.Create))
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.Write(Logs.LogsText.Text);
                            sw.Flush();
                        }
                    }
                }

                else
                {
                    HideToTray();

                    e.Cancel = true;
                }
            });
        }

        private void HideToTray()
        {
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(DIRECTORY);
        }

        private void KillServer_Click(object sender, RoutedEventArgs e)
        {
            mKillProcess = true;
            this.Close();
        }
    }
}

using System.Windows;
using System.Windows.Input;
using System.Threading;

namespace 문정고등학교_출석부
{
    /// <summary>
    /// LoadingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoadingWindow : Window
    {
        WaitCallback mWork;

        public LoadingWindow(string mainText, WaitCallback work)
        {
            InitializeComponent();

            MainText.Text = mainText;
            mWork = work;
        }

        public LoadingWindow(string mainText)
        {
            InitializeComponent();

            MainText.Text = mainText;
        }

        public void SetMainText(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                MainText.Text = text;
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (mWork != null)
            {
                mWork += WorkEnd;
                ThreadPool.QueueUserWorkItem(mWork);
            }
        }

        private void WorkEnd(object o)
        {
            this.Dispatcher.Invoke(() => { this.Close(); });
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}

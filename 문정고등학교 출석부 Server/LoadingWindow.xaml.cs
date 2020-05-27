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

namespace 문정고등학교_출석부_Server
{
    /// <summary>
    /// LoadingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoadingWindow : Window
    {
        WaitCallback mCallback;

        public LoadingWindow(string mainText, WaitCallback callback)
        {
            InitializeComponent();

            MainText.Text = mainText;
            mCallback = callback;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mCallback += WorkEnd;
            ThreadPool.QueueUserWorkItem(mCallback);
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

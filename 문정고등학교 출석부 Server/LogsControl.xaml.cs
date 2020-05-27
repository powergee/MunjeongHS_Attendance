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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TcpCore;

namespace 문정고등학교_출석부_Server
{
    /// <summary>
    /// LogsControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LogsControl : UserControl
    {
        public MainWindow ParentWindow { get; private set; }

        public LogsControl()
        {
            InitializeComponent();
            Debug.MessageWritten += Debug_MessageWritten;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as MainWindow;
        }

        private void Debug_MessageWritten(object sender, MessageWrittenEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                switch (e.Type)
                {
                    case DebugMessageType.Error:
                        LogsText.AppendText($"[{DateTime.Now.ToString()}] ERROR : {e.Message}" + Environment.NewLine);
                        break;
                    case DebugMessageType.General:
                        LogsText.AppendText($"[{DateTime.Now.ToString()}] GENERAL : {e.Message}" + Environment.NewLine);
                        break;
                }

                if (LogsText.LineCount == int.MaxValue - 1)
                    LogsText.Clear();

                LogsText.ScrollToEnd();
            });
        }
    }
}

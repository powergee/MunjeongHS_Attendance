using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// ClientsControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ClientsControl : UserControl
    {
        public MainWindow ParentWindow { get; private set; }

        public ClientsControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as MainWindow;
        }

        public void BindCollection()
        {
            this.Dispatcher.Invoke(() => ClientsGrid.ItemsSource = ParentWindow.Server.Clients);
        }
    }
}

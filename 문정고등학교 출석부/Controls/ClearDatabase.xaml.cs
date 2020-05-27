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


// 데이터베이스를 초기화하는 작업은 개발이 잠정 중단되었음.
// 나중에 다시 개발할 때는
// 비밀번호를 2차 확인하는 기능을 다시 확인하고,
// 서버에 데이터베이스 초기화 명령을 내리는 기능을 구현하여야 함.


namespace 문정고등학교_출석부.Controls
{
    /// <summary>
    /// ClearDatabase.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ClearDatabase : UserControl
    {
        public WorkWindow ParentWindow { get; private set; }

        public ClearDatabase()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentWindow = Window.GetWindow(this) as WorkWindow;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageResult = MessageBox.Show("모든 출석부 데이터와 학생 명단 데이터를 초기화합니다.\n정말로 계속하시겠습니까?", "데이터베이스 초기화 경고", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (messageResult == MessageBoxResult.Yes)
            {
                ParentWindow.Dispatcher.Invoke(() =>
                ParentWindow.ChildManager.TryToShow<ReconfirmationWindow>((EventHandler)((o, ev) =>
                    this.Dispatcher.Invoke(() => HandleReconfirmationResult((o as ReconfirmationWindow).Result == true))
                    ), "데이터베이스를 초기화하는 작업은 악용될 경우 매우 위험하므로, 다시 한번 패스워드를 확인해야합니다.", ParentWindow.Client.AccountID, ParentWindow.Client));
            }
            else
            {
                
            }
        }

        private void HandleReconfirmationResult(bool result)
        {
            // 데이터베이스 초기화 작업 구현 필요...
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 문정고등학교_출석부
{
    public class ChildWindowManager
    {
        private List<Type> mOpenedWindowList = new List<Type>();
        private Window mRefParent;

        public ChildWindowManager(Window parent)
        {
            mRefParent = parent;
        }

        public bool TryToShow<WindowT>(params object[] args) where WindowT : Window
        {
            if (mOpenedWindowList.Contains(typeof(WindowT))) return false;
            try
            {
                Window childIns = Activator.CreateInstance(typeof(WindowT), args) as Window;
                childIns.Closed += ChildWindowClosed;

                childIns.Top = mRefParent.Top + mRefParent.Height / 2 - childIns.Height / 2;
                childIns.Left = mRefParent.Left + mRefParent.Width / 2 - childIns.Width / 2;
                childIns.Owner = mRefParent;

                childIns.Show();
                mOpenedWindowList.Add(typeof(WindowT));
                mRefParent.Focusable = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"윈도우 창을 출력하는데 실패하였습니다. ({typeof(WindowT).ToString()})\n{ex.Message}\n{ex.StackTrace}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        public bool TryToShow<WindowT>(EventHandler windowClosed, params object[] args) where WindowT : Window
        {
            if (mOpenedWindowList.Contains(typeof(WindowT))) return false;
            try
            {
                Window childIns = Activator.CreateInstance(typeof(WindowT), args) as Window;
                childIns.Closed += ChildWindowClosed;
                if (windowClosed != null)
                    childIns.Closed += windowClosed;

                childIns.Top = mRefParent.Top + mRefParent.Height / 2 - childIns.Height / 2;
                childIns.Left = mRefParent.Left + mRefParent.Width / 2 - childIns.Width / 2;
                childIns.Owner = mRefParent;

                childIns.Show();
                mOpenedWindowList.Add(typeof(WindowT));
                mRefParent.Focusable = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"윈도우 창을 출력하는데 실패하였습니다. ({typeof(WindowT).ToString()})\n{ex.Message}\n{ex.StackTrace}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void ChildWindowClosed(object sender, EventArgs e)
        {
            mOpenedWindowList.Remove(sender.GetType());
            mRefParent?.Dispatcher?.Invoke(() =>
            {
                mRefParent.Focusable = true;
                mRefParent.Activate();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 문정고등학교_출석부
{
    public interface ISaveable
    {
        /// <summary>
        /// 반환이 Yes와 No와 null이 아닐경우에 하려던 작업을 중단하면 됨.
        /// </summary>
        MessageBoxResult? CheckSave(bool wait);
    }
}

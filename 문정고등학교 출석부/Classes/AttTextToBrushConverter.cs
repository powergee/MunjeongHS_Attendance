using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using static 문정고등학교_출석부.Attendance;

namespace 문정고등학교_출석부
{
    public class AttTextToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string text = value.ToString();

                AttEnum att;

                if (StrToAttDict.Keys.Contains(text))
                    att = StrToAttDict[text];
                else return DependencyProperty.UnsetValue;

                switch (att)
                {
                    case AttEnum.NoData:
                        return "Black";
                    case AttEnum.Attended:
                        return "Gray";
                    case AttEnum.Absent:
                        return "Red";
                    case AttEnum.EarlyLeft:
                        return "Navy";
                    case AttEnum.Late:
                        return "Purple";
                    case AttEnum.NotAppeared:
                        return "Orange";

                    default:
                        return DependencyProperty.UnsetValue;
                }
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

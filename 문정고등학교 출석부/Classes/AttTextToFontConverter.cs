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
    public class AttTextToFontConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string text = value.ToString();

                AttEnum att = AttEnum.NoData;

                if (StrToAttDict.Keys.Contains(text))
                    att = StrToAttDict[text];

                if (att != AttEnum.NoData && att != AttEnum.Attended)
                    return "Bold";
                else
                    return "Regular";
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

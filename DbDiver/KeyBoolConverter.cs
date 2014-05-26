using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace DbDiver
{
    public class KeyBoolConverter : IValueConverter
    {
        private static KeyBoolConverter instance;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            KeyType has = (KeyType)parameter;
            KeyType key = (KeyType)value;

            return (key & has) == has;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static KeyBoolConverter Instance
        {
            get
            {
                if (instance == null)
                    instance = new KeyBoolConverter();
                return instance;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DbDiver
{
    public class DataDisplaySelector : DataTemplateSelector
    {
        static DataDisplaySelector instance;

        public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            ColumnData data = item as ColumnData;

            if (item == null)
                return null;
            if(data.Key != KeyType.None)
                return App.Current.Resources["cellDisplayKey"] as DataTemplate;
            if (data.Data is String)
                return App.Current.Resources["cellDisplayString"] as DataTemplate;
            if (data.Data is Boolean)
                return App.Current.Resources["cellDisplayBoolean"] as DataTemplate;

            return App.Current.Resources["cellDisplayNumber"] as DataTemplate;
        }

        public static DataDisplaySelector Instance
        {
            get
            {
                if (instance == null)
                    instance = new DataDisplaySelector();

                return instance;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DbDiver
{
    public class DataEditSelector : DataTemplateSelector
    {
        static DataEditSelector instance;

        public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            ColumnData data = item as ColumnData;

            if (item == null)
                return null;
            else if (data.Is(KeyType.Foreign))
                return App.Current.Resources["cellEditForeignKey"] as DataTemplate;
            else if (data.Is(KeyType.Primary))
                return App.Current.Resources["cellEditPrimaryKey"] as DataTemplate;
            else if (data.Data is String)
                return App.Current.Resources["cellEditString"] as DataTemplate;
            else if (data.Data is Boolean)
                return App.Current.Resources["cellEditBoolean"] as DataTemplate;

            return App.Current.Resources["cellEditNumber"] as DataTemplate;
        }

        public static DataEditSelector Instance
        {
            get
            {
                if (instance == null)
                    instance = new DataEditSelector();

                return instance;
            }
        }
    }
}

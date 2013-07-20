using System;
using System.Data;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace DataGridSearch
{
    public class SearchTermConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var cell = values[0] as DataGridCell;
            var rowView = cell.DataContext as DataRowView;
            var column = values[1] as DataGridColumn;
            var searchTerm = values[2] as string;

            if (!String.IsNullOrWhiteSpace(searchTerm) && rowView != null && column != null)
            {
                var item = rowView.Row[column.SortMemberPath];// column.GetCellContent(rowView.Row);

                if (item != null && item.ToString().ToLower().Contains(searchTerm.ToLower()))
                {
                    cell.RaiseEvent(new MatchFoundEventArgs(SearchOperations.MatchFoundEvent)
                    {
                        Source = this,
                        Row = rowView,
                    });

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

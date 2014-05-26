using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace DbDiver
{
    public class GridDataControl : UserControl
    {
        private int currentFound = 0;

        protected virtual DataGrid Data {
            // This *ought* to be abstract, but then we can't name child controls that derive from it (?!).
            get { throw new NotImplementedException(); }
        }

        public GridDataControl()
        {
            CommandBindings.Add(new CommandBinding(Commands.FindNext, ExecuteFind));
        }

        private void ExecuteFind(object sender, ExecutedRoutedEventArgs args)
        {
            string search = args.Parameter as string;
            var data = Data.ItemsSource as DataView;

            if (search == null)
            {
                currentFound = 0;
            }
            else
            {
                object found = null;

                if (currentFound >= data.Count)
                    currentFound = 0;

                while (found == null && currentFound < data.Count)
                {
                    var row = Data.Items.GetItemAt(currentFound) as DataRowView;

                    if (row.Row.ItemArray.Any(item =>
                            item.ToString().ToLower().Contains(search)))
                        found = row;

                    currentFound += 1;
                }

                if (found != null)
                {
                    Data.ScrollIntoView(found);
                    Data.SelectedItem = found;
                }
            }

            Data.Focus();
        }
    }
}

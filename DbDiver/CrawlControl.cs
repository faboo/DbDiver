using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace DbDiver
{
    public class CrawlControl : UserControl
    {
        int currentFoundColumn;
        int currentFoundTable;

        public virtual ObservableCollection<Table> OpenTables { get; set; }
        protected virtual ItemsControl TablesList
        {
            get { throw new NotImplementedException(); }
        }

        public CrawlControl()
        {
            CommandBindings.Add(new CommandBinding(Commands.FindNext, ExecuteFind));
        }

        private void ExecuteFind(object sender, ExecutedRoutedEventArgs args)
        {
            string search = args.Parameter as string;

            if (search == null)
            {
                currentFoundTable = 0;
                currentFoundColumn = 0;
            }
            else
            {
                object foundTable = null;
                object foundColumn = null;

                if (currentFoundTable >= OpenTables.Count)
                {
                    currentFoundTable = 0;
                    currentFoundColumn = 0;
                }

                while (foundTable == null && currentFoundTable < OpenTables.Count && currentFoundColumn < OpenTables[currentFoundTable].Columns.Count)
                {
                    var table = OpenTables[currentFoundTable];

                    while (foundColumn == null && currentFoundColumn < OpenTables[currentFoundTable].Columns.Count)
                    {
                        var column = table.Columns[currentFoundColumn];

                        if (column.Name.ToLower().Contains(search))
                        {
                            foundTable = table;
                            foundColumn = column;
                        }

                        currentFoundColumn += 1;
                    }

                    if (foundColumn == null)
                    {
                        currentFoundTable += 1;
                        currentFoundColumn = 0;
                    }
                }

                if (foundTable != null)
                {
                    var grid = TablesList.ItemContainerGenerator.ContainerFromIndex(currentFoundTable)
                        .FindVisualChild<DataGrid>();

                    grid.BringIntoView();
                    grid.SelectedIndex = currentFoundColumn - 1;
                    grid.ScrollIntoView(foundColumn);
                    grid.Focus();
                }
            }
        }
    }
}

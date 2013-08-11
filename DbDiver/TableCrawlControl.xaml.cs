using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DbDiver {
    /// <summary>
    /// Interaction logic for TableCrawl.xaml
    /// </summary>
    public partial class TableCrawlControl: UserControl {
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register("Connection", typeof(Connection), typeof(TableCrawlControl));
		public static readonly DependencyProperty FirstTableProperty = DependencyProperty.Register("FirstTable", typeof(string), typeof(TableCrawlControl));
        public static readonly RoutedEvent OnQueryEvent = EventManager.RegisterRoutedEvent("OnQuery", RoutingStrategy.Bubble, typeof(QueryEventHandler), typeof(TableCrawlControl));

        private List<Column> ConnectedColumns = new List<Column>();

        public Connection Connection
		{
            get { return (Connection)GetValue(ConnectionProperty); }
			set { SetValue(ConnectionProperty, value); }
		}
        public ObservableCollection<Table> OpenTables { get; set; }
        public ObservableCollection<string> FirstTables { get; set; }

        public event QueryEventHandler OnQuery
		{
			add { AddHandler(OnQueryEvent, value); }
			remove { RemoveHandler(OnQueryEvent, value); }
		}

        int currentFoundColumn;
        int currentFoundTable;

        public TableCrawlControl() {
            OpenTables = new ObservableCollection<Table>();
            FirstTables = new ObservableCollection<string>();
            InitializeComponent();
        }

        private void ExecuteOpenTable(object sender, ExecutedRoutedEventArgs args) {
            var connection = Connection;
            Column column = args.Parameter as Column;
            string name = column != null? column.ForeignTable : args.Parameter as string;
            if (args.Command == NavigationCommands.BrowseHome)
            {
                OpenTables.Clear();
            }
            else
            {
                int parent = OpenTables.IndexOf(column.Table);

                while (OpenTables.Count > parent+1)
                    OpenTables.RemoveAt(parent + 1);
                while(ConnectedColumns.Count > parent)
                    ConnectedColumns.RemoveAt(parent);
                ConnectedColumns.Add(column);
            }

            FirstTables.Remove(name);
            FirstTables.Insert(0, name);
            ThreadPool.QueueUserWorkItem(obj => getTable(connection, name));
        }

        private void CanExecuteOpenTable(object sender, CanExecuteRoutedEventArgs args) {
            args.CanExecute = args.Parameter != null && (!(args.Parameter is string) || !String.IsNullOrWhiteSpace(args.Parameter as string));
        }

        private void AddTable(Table table) {
            OpenTables.Add(table);
        }

        private void getTable(Connection connection, string name)
        {
            try{
                Table table = connection.GetTable(name);
                Dispatcher.BeginInvoke(new Action<Table>(AddTable), table);
            }
            catch(Exception ex){
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
                    () => MessageBox.Show(ex.Message, "DbDiver")));
            }
        }

        private void ExecuteCreateQuery(object sender, ExecutedRoutedEventArgs args) {
            StringBuilder query = new StringBuilder();

            query.AppendFormat("select * from [{0}]\n", OpenTables[0].Name);

            for(int idx = 0; idx < ConnectedColumns.Count; ++idx)
            {
                Column key = ConnectedColumns[idx];
                Table foreign = OpenTables[idx+1];
                query.AppendFormat("join [{0}] on [{0}].[{1}] = [{2}].[{3}]\n", foreign.Name, key.ForeignColumn, key.Table.Name, key.Name);
            }

            if(OpenTables[0].Primary.Count > 0)
                query.AppendFormat("where [{0}].[{1}] = ", OpenTables[0].Name, OpenTables[0].Primary[0].Name);

            RaiseEvent(new QueryEventArgs { Query = query.ToString(), RoutedEvent = OnQueryEvent});
        }

        private void CanExecuteNew(object sender, CanExecuteRoutedEventArgs args) {
            args.CanExecute = OpenTables.Count > 0;
        }

        private void ExecuteFind(object sender, ExecutedRoutedEventArgs args)
        {
            //OpenTables
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

        private void OnEnterKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == System.Windows.Input.Key.Enter)
            {
                ApplicationCommands.Open.Execute(null, this);
            }
        }
    }
}

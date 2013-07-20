using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace DbDiver {
    /// <summary>
    /// Interaction logic for QueryControl.xaml
    /// </summary>
    public partial class QueryControl: UserControl {
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register("Connection", typeof(Connection), typeof(QueryControl));
		public static readonly DependencyProperty QueryProperty = DependencyProperty.Register("Query", typeof(string), typeof(QueryControl));
		public static readonly DependencyProperty ResultsProperty = DependencyProperty.Register("Results", typeof(ObservableCollection<DataTable>), typeof(QueryControl));
		public static readonly DependencyProperty ViewProperty = DependencyProperty.Register("View", typeof(GridView), typeof(QueryControl));

        public Connection Connection
		{
            get { return (Connection)GetValue(ConnectionProperty); }
			set { SetValue(ConnectionProperty, value); }
		}
		public string Query
		{
			get { return (string)GetValue(QueryProperty); }
			set { SetValue(QueryProperty, value); }
		}
        public ObservableCollection<DataTable> Results
		{
            get { return (ObservableCollection<DataTable>)GetValue(ResultsProperty); }
			set { SetValue(ResultsProperty, value); }
		}
/*		public GridView View
		{
			get { return (GridView)GetValue(ViewProperty); }
			set { SetValue(ViewProperty, value); }
		}*/

        int currentFoundRow;
        int currentFoundTable;

        public QueryControl() {
            Results = new ObservableCollection<DataTable>();
            InitializeComponent();
        }

        private void RunQueryExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Connection connection = Connection;
            string query = Query;
            Cursor = Cursors.AppStarting;
            Results.Clear();
            ThreadPool.QueueUserWorkItem(obj => executeQuery(connection, query));
        }

        private void CanRunQuery(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = !String.IsNullOrWhiteSpace(Query);
        }

        private void executeQuery(Connection connection, string query)
        {
            try
            {
                using (var conn = connection.Get())
                {
                    using (var command = conn.CreateCommand())
                    {
                        DataSet results = new DataSet();
                        DbDataAdapter adapter = connection.CreateDataAdapter();

                        command.CommandText = query;
                        adapter.SelectCommand = command;
                        adapter.Fill(results);

                        foreach (var result in results.Tables)
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<DataTable>(SetResults), result);
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
                    () => MessageBox.Show(ex.Message, "DbDiver")));
            }
            finally
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    Cursor = Cursors.Arrow));

            }
        }

        private void SetResults(DataTable results)
        {
            Results.Add(results);
        }

        private void ExecuteFind(object sender, ExecutedRoutedEventArgs args)
        {
            string search = args.Parameter as string;

            if (search == null)
            {
                currentFoundTable = 0;
                currentFoundRow = 0;
            }
            else
            {
                object foundTable = null;
                object foundRow = null;

                if (currentFoundTable >= Results.Count)
                {
                    currentFoundTable = 0;
                    currentFoundRow = 0;
                }

                while (foundTable == null && foundRow == null && currentFoundTable < Results.Count && currentFoundRow < Results[currentFoundTable].Rows.Count)
                {
                    var table = Results[currentFoundTable];

                    while (foundRow == null && currentFoundRow < Results[currentFoundTable].Rows.Count)
                    {
                        var row = table.Rows[currentFoundRow];

                        foreach (var item in row.ItemArray)
                            if (item.ToString().ToLower().Contains(search))
                            {
                                foundTable = table;
                                foundRow = row;
                                break;
                            }

                        currentFoundRow += 1;
                    }

                    if (foundRow == null)
                    {
                        currentFoundTable += 1;
                        currentFoundRow = 0;
                    }
                }

                if (foundTable != null)
                {
                    var grid = ResultsList.ItemContainerGenerator.ContainerFromIndex(currentFoundTable)
                        .FindVisualChild<DataGrid>();

                    grid.BringIntoView();
                    grid.SelectedIndex = currentFoundRow - 1;
                    grid.ScrollIntoView(foundRow);
                    grid.Focus();
                }
            }
        }
    }
}

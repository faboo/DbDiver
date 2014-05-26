using System;
using System.Data;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DbDiver
{
    /// <summary>
    /// Interaction logic for TablesControl.xaml
    /// </summary>
    public partial class TablesControl : GridDataControl
    {
		public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register("Connection", typeof(Connection), typeof(TablesControl));
		public static readonly DependencyProperty TablesProperty = DependencyProperty.Register("Tables", typeof(DataTable), typeof(TablesControl));
		public static readonly DependencyProperty SearchProperty = DependencyProperty.Register("Search", typeof(string), typeof(TablesControl));
        public static readonly RoutedEvent OnCrawlEvent = EventManager.RegisterRoutedEvent("OnCrawl", RoutingStrategy.Bubble, typeof(CrawlEventHandler), typeof(TablesControl));
        public static readonly RoutedEvent OnQueryEvent = EventManager.RegisterRoutedEvent("OnQuery", RoutingStrategy.Bubble, typeof(QueryEventHandler), typeof(TablesControl));
        public static readonly RoutedEvent OnEditDataEvent = EventManager.RegisterRoutedEvent("OnEditData", RoutingStrategy.Bubble, typeof(EditDataEventHandler), typeof(TablesControl));

        bool tablesLoaded = false;

        public Connection Connection
		{
            get { return (Connection)GetValue(ConnectionProperty); }
			set { SetValue(ConnectionProperty, value); }
		}
		public DataTable Tables
		{
			get { return (DataTable)GetValue(TablesProperty); }
			set { SetValue(TablesProperty, value); }
		}
		public string Search
		{
			get { return (string)GetValue(SearchProperty); }
			set { SetValue(SearchProperty, value); }
		}
        protected override DataGrid Data
        {
            get { return RowGrid; }
        }

        public event CrawlEventHandler OnCrawl
		{
			add { AddHandler(OnCrawlEvent, value); }
			remove { RemoveHandler(OnCrawlEvent, value); }
		}
        public event QueryEventHandler OnQuery
		{
			add { AddHandler(OnQueryEvent, value); }
			remove { RemoveHandler(OnQueryEvent, value); }
		}
        public event EditDataEventHandler OnEditData
		{
			add { AddHandler(OnEditDataEvent, value); }
			remove { RemoveHandler(OnEditDataEvent, value); }
		}

        public TablesControl()
        {
            IsVisibleChanged += (s, e) => UpdateTables();
            InitializeComponent();
        }

        private void getTables(Connection connection)
        {
            try
            {
                DataTable results = connection.GetTables();

                Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action<DataTable>(AddTables),
                    results);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
                    () => MessageBox.Show(ex.Message, "DbDiver")));
            }
        }

        private void AddTables(DataTable tables)
        {
            Tables = tables;
        }

        public void ResetTables()
        {
            tablesLoaded = false;
            UpdateTables();
        }

        private void UpdateTables()
        {
            if (!tablesLoaded)
            {
                Connection connection = Connection;

                tablesLoaded = true;
                ThreadPool.QueueUserWorkItem(obj => getTables(connection));
            }
        }

        private void CanExecuteOnRow(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = args.Parameter is DataRow || Data.SelectedItem != null;
            args.Handled = true;
        }

        private void ExecuteDescribe(object sender, ExecutedRoutedEventArgs args)
        {
            Crawl(args.Parameter as DataRow ?? (Data.SelectedItem as DataRowView).Row);
            args.Handled = true;
        }

        private void OnRowDoubleClick(object sender, MouseButtonEventArgs args)
        {
            var row = ((args.OriginalSource as FrameworkElement).DataContext as DataRowView).Row;

            Crawl(row);
            args.Handled = true;
        }

        private void Crawl(DataRow row)
        {
            RaiseEvent(new CrawlEventArgs { Table = row[0] as string, RoutedEvent = OnCrawlEvent });
        }

        private void ExecuteQueryTable(object sender, ExecutedRoutedEventArgs args)
        {
			string table = ((DataRow)args.Parameter)["Table"].ToString();
            string query = String.Format("select * from [{0}]", table);

            RaiseEvent(new QueryEventArgs { Query = query, RoutedEvent = OnQueryEvent });
        }

        private void ExecuteEditData(object sender, ExecutedRoutedEventArgs args)
        {
            DataRow row = ((DataRow)args.Parameter);
            string table = row["Table"].ToString();

            RaiseEvent(new EditDataEventArgs { 
                Table = table,
                Size = int.Parse(row["Rows"].ToString()),
                RoutedEvent = OnEditDataEvent
            });
        }
    }
}

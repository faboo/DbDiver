using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;

namespace DbDiver {
    /// <summary>
    /// Interaction logic for DbTools.xaml
    /// </summary>
    public partial class DbTools: Window {
        private static readonly DependencyPropertyKey ConnectionPropertyKey = DependencyProperty.RegisterReadOnly("Connection", typeof(Connection), typeof(DbTools), new FrameworkPropertyMetadata(null));
		public static readonly DependencyProperty ConnectionProperty = ConnectionPropertyKey.DependencyProperty;
		public static readonly DependencyProperty DatabasesProperty = DependencyProperty.Register("Databases", typeof(IEnumerable<String>), typeof(DbTools));
		public static readonly DependencyProperty SelectedDatabaseProperty = DependencyProperty.Register("SelectedDatabase", typeof(int), typeof(DbTools), new FrameworkPropertyMetadata(OnSelectedDatabaseChanged));
		public static readonly DependencyProperty SearchProperty = DependencyProperty.Register("Search", typeof(string), typeof(DbTools));

        public DbTools(Connection connection)
        {
            Connection = connection;
            ThreadPool.QueueUserWorkItem(obj => loadDatabases(connection));
            InitializeComponent();
            PreviewKeyDown += (s, a) =>
                           {
                               if (a.Key == System.Windows.Input.Key.F3)
                               {
                                   Commands.FindNext.Execute(Search.ToLower(), tabs.SelectedContent as IInputElement);
                                   a.Handled = true;
                               }
                           };
        }

        public Connection Connection
		{
            get { return (Connection)GetValue(ConnectionProperty); }
			set { SetValue(ConnectionPropertyKey, value); }
		}
		public IEnumerable<String> Databases
		{
			get { return (IEnumerable<String>)GetValue(DatabasesProperty); }
			set { SetValue(DatabasesProperty, value); }
		}
		public int SelectedDatabase
		{
			get { return (int)GetValue(SelectedDatabaseProperty); }
			set { SetValue(SelectedDatabaseProperty, value); }
		}
		public string Search
		{
			get { return (string)GetValue(SearchProperty); }
			set { SetValue(SearchProperty, value); }
		}

        private void loadDatabases(Connection connection)
        {
            try{
                List<string> databases = connection.GetDatabases();

                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LoadDatabases(databases)));
            }
            catch (Exception ex){
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
                    () => MessageBox.Show(ex.Message, "DbDiver")));
            }
		}

        private void LoadDatabases(List<String> databases) {
            Databases = databases;
            SelectedDatabase = 0;
            if(Databases != null)
                Connection.Database = Databases.First();
        }

        private void OnSelectedDatabaseChanged(DependencyPropertyChangedEventArgs args){
            Connection.Database = Databases.ElementAt(SelectedDatabase);
            tables.ResetTables();
        }

        private static void OnSelectedDatabaseChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args){
            (sender as DbTools).OnSelectedDatabaseChanged(args);
        }

        private void OnQuery(object sender, QueryEventArgs args)
        {
            query.Query = args.Query;
            tabs.SelectedItem = queryTab;
        }

        private void OnCrawl(object sender, CrawlEventArgs args)
        {
            tabs.SelectedItem = crawlTab;
            NavigationCommands.BrowseHome.Execute(args.Table, crawl);
        }

        private void OnLookup(object sender, LookupEventArgs args)
        {
            tabs.SelectedItem = lookupTab;
            NavigationCommands.GoToPage.Execute(args.Procedure, lookup);
        }

        private void OnSearchKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == System.Windows.Input.Key.Enter)
            {
                (tabs.SelectedItem as Control).Focus();
                (tabs.SelectedItem as Control).MoveFocus(
                    new TraversalRequest(FocusNavigationDirection.Next));
                Commands.FindNext.Execute(null, tabs.SelectedContent as IInputElement);
                Commands.FindNext.Execute(Search.ToLower(), tabs.SelectedContent as IInputElement);
                args.Handled = true;
            }
        }

        private void ExecuteGotoFind(object sender, ExecutedRoutedEventArgs e)
        {
            search.Focus();
        }

        private void OnEditTableData(object sender, EditDataEventArgs args)
        {
            tabs.SelectedItem = dataTab;
            editData.TableName = args.Table;
            if(args.Size < 100)
                ApplicationCommands.Open.Execute(args.Table, editData);
        }
    }
}

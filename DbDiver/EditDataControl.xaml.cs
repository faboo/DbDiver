using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;

namespace DbDiver
{
    /// <summary>
    /// Interaction logic for EditData.xaml
    /// </summary>
    public partial class EditDataControl : UserControl
    {
		public static readonly DependencyProperty TableNameProperty = DependencyProperty.Register("TableName", typeof(string), typeof(EditDataControl));
        public static readonly DependencyProperty TablesProperty = DependencyProperty.Register("Tables", typeof(ObservableCollection<string>), typeof(EditDataControl));
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register("Connection", typeof(Connection), typeof(EditDataControl));
		public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(DataTable), typeof(EditDataControl));

        public EditDataControl()
        {
            Tables = new ObservableCollection<string>();
            InitializeComponent();
        }

		public string TableName
		{
			get { return (string)GetValue(TableNameProperty); }
			set { SetValue(TableNameProperty, value); }
		}
		public ObservableCollection<string> Tables
		{
            get { return (ObservableCollection<string>)GetValue(TablesProperty); }
			set { SetValue(TablesProperty, value); }
		}
        public Connection Connection
        {
            get { return (Connection)GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }
		public DataTable Rows
		{
			get { return (DataTable)GetValue(RowsProperty); }
			set { SetValue(RowsProperty, value); }
		}

        private Table table;

        private void ExecuteCrawl(object sender, ExecutedRoutedEventArgs args)
        {
            DataRow row = ((DataRowView)RowGrid.SelectedItem).Row;
            TableData data = new TableData(table);
            DataCrawl crawl = new DataCrawl();

            data.AddData(row);
            crawl.Connection = Connection;
            crawl.OpenData.Add(data);
            crawl.Show();
        }

        private void ExecuteOpenTable(object sender, ExecutedRoutedEventArgs args)
        {
            Cursor = Cursors.AppStarting;
            this.Weave<DataTable>(
                new Func<Connection, string, string, DataTable>(getTable),
                AddRows,
                () => Cursor = Cursors.Arrow,

                Connection,
                TableName,
                WhereBox.Text);
        }

        private void CanOpenTable(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = !String.IsNullOrWhiteSpace(TablesBox.Text);
        }

        private DataTable getTable(Connection conn, String name, String where)
        {
            table = conn.GetTable(name);

            return conn.GetTableData(name, null, where);
        }

        private void AddRows(DataTable rows)
        {
            string tableName = TableName;
            Rows = rows;
            Rows.RowChanged += OnRowChanged;
            Rows.RowDeleted += OnRowDeleted;
            Tables.Remove(tableName);
            Tables.Insert(0, tableName);
            Cursor = Cursors.Arrow;
        }

        void OnRowDeleted(object sender, DataRowChangeEventArgs args)
        {
            Cursor = Cursors.AppStarting;
            this.WeaveWithError<bool>(
                new Func<Connection, DataRow, bool>(
                    (conn, row) =>
                        {
                            conn.DeleteRow(
                                row,
                                table,
                                !conn.HasDependents(table) || FollowDependents());
                            return false;
                        }),
                null,
                () => Cursor = Cursors.Arrow,
                ex => {
                    args.Row.CancelEdit();
                    MessageBox.Show(Application.Current.MainWindow, ex.Message, "DbDiver");
                },
                Connection,
                args.Row);
        }

        bool FollowDependents()
        {
            return (bool)Dispatcher.Invoke(new Func<bool>(() =>
                MessageBox.Show(
                    "This row has dependent rows. Should we delete those (and their dependents) first?",
                    "DbDiver",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes),
                DispatcherPriority.Background);
        }

        void OnRowChanged(object sender, DataRowChangeEventArgs args)
        {
            if(args.Row.RowState == DataRowState.Modified)
                this.Background(
                    new Action<Connection, DataRow>((conn, row) =>
                        conn.UpdateRow(row, table)),
                    Connection,
                    args.Row);
            else
                this.Background(
                    new Action<Connection, DataRow>((conn, row) =>
                        conn.InsertRow(row, table)),
                    Connection,
                    args.Row);
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs args)
        {
            args.Cancel = (args.Column.Header as string).Equals(table.Primary);
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

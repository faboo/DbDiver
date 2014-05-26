using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Data;

namespace DbDiver
{
    /// <summary>
    /// Interaction logic for EditData.xaml
    /// </summary>
    public partial class EditDataControl : GridDataControl
    {
		public static readonly DependencyProperty TableNameProperty = DependencyProperty.Register("TableName", typeof(string), typeof(EditDataControl));
        public static readonly DependencyProperty TablesProperty = DependencyProperty.Register("Tables", typeof(ObservableCollection<string>), typeof(EditDataControl));
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register("Connection", typeof(Connection), typeof(EditDataControl));
        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(TableData), typeof(EditDataControl));

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
        public TableData Rows
		{
            get { return (TableData)GetValue(RowsProperty); }
			set { SetValue(RowsProperty, value); }
		}
        protected override DataGrid Data
        {
            get { return RowGrid; }
        }

        private Table table;

        private void ExecuteCrawl(object sender, ExecutedRoutedEventArgs args)
        {
            RowData row = ((RowData)RowGrid.CurrentCell.Item);
            TableData data = new TableData(table, Connection);
            DataCrawl crawl = new DataCrawl();

            data.AddData(row);
            crawl.Connection = Connection;
            crawl.OpenData.Add(data);
            crawl.Show();
        }

        private void ExecuteOpenTable(object sender, ExecutedRoutedEventArgs args)
        {
            Cursor = Cursors.AppStarting;
            this.Weave<TableData>(
                new Func<Connection, string, string, TableData>(getTable),
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

        private TableData getTable(Connection conn, String name, String where)
        {
            table = conn.GetTable(name);

            return conn.GetTableData(name, null, where);
        }

        private void AddRows(TableData rows)
        {
            string tableName = TableName;
            Rows = rows;

            RowGrid.Columns.Clear();
            for(int column = 0; column < rows.Table.Columns.Count; column += 1){
                RowGrid.Columns.Add(new DataGridColumn
                    {
                        Header = rows.Table.Columns[column].Name.Replace('_', ' '),
                        ColumnName = rows.Table.Columns[column].Name,
                    });
            }

            // TODO: Add these or similar
            //Rows.RowChanged += OnRowChanged;
            //Rows.RowDeleted += OnRowDeleted;
            Tables.Remove(tableName);
            Tables.Insert(0, tableName);
            TableName = tableName;
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

        void OnRowChanged(TableData data, RowChangedEventArgs args)
        {
            if(!args.Row.New)
                this.Background(
                    new Action<Connection, RowData, Table>((conn, row, table) =>
                        conn.UpdateRow(row, table)),
                    Connection,
                    args.Row,
                    data.Table);
            else
                this.Background(
                    new Action<Connection, RowData>((conn, row) =>
                        conn.InsertRow(row, table)),
                    Connection,
                    args.Row);
        }

        private void OnInitializingNewItem(object sender, InitializingNewItemEventArgs args)
        {
            //args.NewItem = Rows.NewRow();
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs args)
        {
            args.Cancel = (args.Column.Header as string).Equals(table.Primary);
        }

        private void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs args)
        {
            var rowData = args.Row.DataContext as RowData;

            if (!rowData.New)
                this.Background(
                    new Action<Connection, RowData, Table>((conn, row, table) =>
                        conn.UpdateRow(row, table)),
                    Connection,
                    rowData,
                    Rows);
            else
                this.Background(
                    new Action<Connection, RowData>((conn, row) =>
                        conn.InsertRow(row, table)),
                    Connection,
                    rowData);
        }

        private void OnEnterKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == System.Windows.Input.Key.Enter || args.Key == System.Windows.Input.Key.Return)
            {
                ApplicationCommands.Open.Execute(null, this);
            }
        }

        private void OnRowGridAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs args)
        {
            var column = new DataGridColumn
                {
                    Header = args.Column.Header,
                    ColumnName = args.PropertyName,
                    CellTemplate = (DataTemplate)Resources["cellDisplay"],
                };

            args.Column = column;
        }
    }
}

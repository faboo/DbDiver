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
    public partial class EditData : UserControl
    {
		public static readonly DependencyProperty TableNameProperty = DependencyProperty.Register("TableName", typeof(string), typeof(EditData));
        public static readonly DependencyProperty TablesProperty = DependencyProperty.Register("Tables", typeof(ObservableCollection<string>), typeof(EditData));
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register("Connection", typeof(Connection), typeof(EditData));
		public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(DataTable), typeof(EditData));

        public EditData()
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
            Rows = rows;
            Rows.RowChanged += OnRowChanged;
            Rows.RowDeleted += OnRowDeleted;
            Tables.Remove(TableName);
            Tables.Insert(0, TableName);
            Cursor = Cursors.Arrow;
        }

        void OnRowDeleted(object sender, DataRowChangeEventArgs args)
        {
            this.Background(
                new Action<Connection, DataRow>((conn, row) =>
                    conn.DeleteRow(row, table)),
                Connection,
                args.Row);
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

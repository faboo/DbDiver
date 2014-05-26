using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DbDiver
{
    /// <summary>
    /// Interaction logic for DataCrawlControl.xaml
    /// </summary>
    public partial class DataCrawlControl : UserControl
    {
        public DataCrawlControl()
        {
            OpenData = new ObservableCollection<TableData>();
            InitializeComponent();
        }

        public Connection Connection { get; set; }
        public ObservableCollection<TableData> OpenData { get; set; }

        private void ExecuteOpenTable(object sender, ExecutedRoutedEventArgs args)
        {
            ColumnData column = args.Parameter as ColumnData;

            Cursor = Cursors.AppStarting;
            
            this.Weave<TableData>(
                new Func<Connection, ColumnData, TableData>(getRows),
                data => AddTable(column.Set, data),
                () => Cursor = Cursors.Arrow,
                Connection,
                column);
        }

        private TableData getRows(Connection connection, ColumnData column)
        {
            Table table = connection.GetTable(column.ForeignTable);
            TableData data = new TableData(table, connection);

            using (var conn = connection.Get())
            using (var command = conn.CreateCommand())
            using (var adapter = connection.CreateDataAdapter())
            {
                DataSet results = new DataSet();
   
                command.CommandText = String.Format(
                    "select * from {0} where {1} = @key",
                    table.Name,
                    column.ForeignColumn);
                command.AddWithValue("@key", column.Data);
                adapter.SelectCommand = command;
                adapter.Fill(results);

                foreach (DataRow row in results.Tables[0].Rows)
                    data.AddData(row);
            }

            return data;
        }

        private void AddTable(TableData parent, TableData child)
        {
            int parentIdx = OpenData.IndexOf(parent);

            while (OpenData.Count > parentIdx + 1)
                OpenData.RemoveAt(parentIdx + 1);

            child.RowChanged += OnRowChanged;
            OpenData.Add(child);
        }

        void OnRowChanged(TableData data, RowChangedEventArgs args)
        {
            if (!args.Row.New)
                this.Background(
                    new Action<Connection, RowData, Table>((conn, row, table) =>
                        conn.UpdateRow(row, table)),
                    Connection,
                    args.Row,
                    data.Table);
        }

        private void UnshiftTable(TableData parent, TableData child)
        {
            int at = OpenData.IndexOf(parent);

            while (at > 0)
            {
                OpenData.RemoveAt(0);
                at -= 1;
            }

            OpenData.Insert(0, child);
        }

        private void OnDependentKeySelected(object sender, SelectionChangedEventArgs args)
        {
            ColumnData column = (sender as ComboBox).DataContext as ColumnData;
            Key dependentKey = args.AddedItems[0] as Key;

            this.Weave<TableData>(
                new Func<Connection,Key,object,TableData>(loadTable),
                child => UnshiftTable(column.Set, child),
                () => Cursor = Cursors.Arrow,
                Connection,
                dependentKey,
                column.Data);
        }

        private TableData loadTable(Connection connection, Key dependentKey, object value)
        {
            Table table = connection.GetTable(dependentKey.ForeignTable);
            TableData data = new TableData(table, connection);

            using (var conn = connection.Get())
            using (var command = conn.CreateCommand())
            using (var adapter = connection.CreateDataAdapter())
            {
                DataSet results = new DataSet();

                command.CommandText = String.Format(
                    "select * from {0} where {1} = @key",
                    table.Name,
                    dependentKey.ForeignColumn);
                command.AddWithValue("@key", value);
                adapter.SelectCommand = command;
                adapter.Fill(results);

                foreach (DataRow row in results.Tables[0].Rows)
                    data.AddData(row);
            }

            return data;
        }
    }
}

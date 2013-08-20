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
using System.Windows.Shapes;

namespace DbDiver
{
    /// <summary>
    /// Interaction logic for DataCrawl.xaml
    /// </summary>
    public partial class DataCrawl : Window
    {
        public DataCrawl()
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
                new Func<Connection, ColumnData, TableData>(getDependents),
                data => AddTable(column.Set, data),
                () => Cursor = Cursors.Arrow,
                Connection,
                column);
        }

        private TableData getDependents(Connection connection, ColumnData column)
        {
            Table table = connection.GetTable(column.ForeignTable);
            TableData data = new TableData(table);

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

            OpenData.Add(child);
        }
    }
}

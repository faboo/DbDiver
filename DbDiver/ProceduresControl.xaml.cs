using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Text;

namespace DbDiver
{
    /// <summary>
    /// Interaction logic for ProceduresControl.xaml
    /// </summary>
    public partial class ProceduresControl : UserControl
    {
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register("Connection", typeof(Connection), typeof(ProceduresControl));
        public static readonly DependencyProperty ProceduresProperty = DependencyProperty.Register("Procedures", typeof(DataTable), typeof(ProceduresControl));
        public static readonly RoutedEvent OnLookupEvent = EventManager.RegisterRoutedEvent("OnLookup", RoutingStrategy.Bubble, typeof(LookupEventHandler), typeof(ProceduresControl));
        public static readonly RoutedEvent OnQueryEvent = EventManager.RegisterRoutedEvent("OnQuery", RoutingStrategy.Bubble, typeof(QueryEventHandler), typeof(ProceduresControl));

        bool proceduresLoaded = false;
        private int currentFound = 0;

        public Connection Connection
		{
            get { return (Connection)GetValue(ConnectionProperty); }
			set { SetValue(ConnectionProperty, value); }
		}
        public DataTable Procedures
		{
            get { return (DataTable)GetValue(ProceduresProperty); }
            set { SetValue(ProceduresProperty, value); }
		}

        public event LookupEventHandler OnLookup
		{
            add { AddHandler(OnLookupEvent, value); }
            remove { RemoveHandler(OnLookupEvent, value); }
		}
        public event QueryEventHandler OnQuery
        {
            add { AddHandler(OnQueryEvent, value); }
            remove { RemoveHandler(OnQueryEvent, value); }
        }

        public ProceduresControl()
        {
            IsVisibleChanged += (s, e) => UpdateProcedures();
            InitializeComponent();
        }

        private void getProcedures(Connection connection)
        {
            try
            {
                DataTable results = connection.GetProcedures();

                Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action<DataTable>(AddProcedures),
                    results);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
                    () => MessageBox.Show(ex.Message, "DbDiver")));
            }
        }

        private void AddProcedures(DataTable procedures)
        {
            Procedures = procedures;
            if (procedures == null)
                this.Visibility = Visibility.Collapsed;
        }

        private void UpdateProcedures()
        {
            if (!proceduresLoaded)
            {
                Connection connection = Connection;

                proceduresLoaded = true;
                ThreadPool.QueueUserWorkItem(obj => getProcedures(connection));
            }
        }

        private void CanExecuteDescribe(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = args.Parameter is DataRow || Data.SelectedItem != null;
            args.Handled = true;
        }

        private void ExecuteDescribe(object sender, ExecutedRoutedEventArgs args)
        {
            Describe(args.Parameter as DataRow ?? (Data.SelectedItem as DataRowView).Row);
            args.Handled = true;
        }

        private void OnRowDoubleClick(object sender, MouseButtonEventArgs args)
        {
            var row = ((args.OriginalSource as FrameworkElement).DataContext as DataRowView).Row;

            Describe(row);
            args.Handled = true;
        }

        private void Describe(DataRow row)
        {
            RaiseEvent(new LookupEventArgs { Procedure = row[0] as string, RoutedEvent = OnLookupEvent });
        }

        private void ExecuteFind(object sender, ExecutedRoutedEventArgs args)
        {
            string search = args.Parameter as string;

            if (search == null)
            {
                currentFound = 0;
            }
            else{
                object found = null;

                if (currentFound >= Procedures.Rows.Count)
                    currentFound = 0;

                while (found == null && currentFound < Procedures.Rows.Count)
                {
                    var row = Data.Items.GetItemAt(currentFound) as DataRowView;

                    foreach(var item in row.Row.ItemArray)
                        if (item.ToString().ToLower().Contains(search))
                        {
                            found = row;
                            break;
                        }

                    currentFound += 1;
                }

                if (found != null)
                {
                    Data.ScrollIntoView(found);
                    Data.SelectedItem = found;
                }
            }
        }

        private void ExecuteQueryModule(object sender, ExecutedRoutedEventArgs args)
        {
            string procedure = (args.Parameter as DataRow)["Procedure"] as string;
            List<string> parameters = GetParameters(procedure);
            StringBuilder query = new StringBuilder();

            query.AppendFormat("exec {0} ", procedure);

            foreach (string param in parameters)
                query.AppendFormat("{0}, ", param);

            query.Remove(query.Length - 2, 2);

            RaiseEvent(new QueryEventArgs { Query = query.ToString(), RoutedEvent = OnQueryEvent });
        }

		private List<string> GetParameters(string procedure){
            List<string> parameters = new List<string>();

            using (var conn = Connection.Get())
            {
                using (var command = conn.CreateCommand())
                {
                    DbDataReader reader;

                    command.CommandText = "select parameter_name from information_schema.parameters "
                        + "where specific_name='" + procedure + "' order by ordinal_position";
                    reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        parameters.Add(reader["parameter_name"] as string);
                    }
                }
            }

            return parameters;
		}
    }
}

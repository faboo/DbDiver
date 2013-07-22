using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Sql;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DbDiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ServerSelector : Window
    {
		public static readonly DependencyProperty InstancesProperty = DependencyProperty.Register("Instances", typeof(ObservableCollection<DbInstance>), typeof(ServerSelector));
		public static readonly DependencyProperty SelectedServerProperty = DependencyProperty.Register("SelectedServer", typeof(int), typeof(ServerSelector));
		public static readonly DependencyProperty CanLoginProperty = DependencyProperty.Register("CanLogin", typeof(bool), typeof(ServerSelector));
		public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register("Username", typeof(string), typeof(ServerSelector));

        public ServerSelector()
        {
            Instances = new ObservableCollection<DbInstance> {
                new SqlInstance { Server = "localhost" },
                new MySqlInstance { Server = "localhost" },
            };
            SelectedServer = 0;
            CanLogin = true;
            Cursor = Cursors.AppStarting;
            ThreadPool.QueueUserWorkItem(discoverSqlInstances);
            ThreadPool.QueueUserWorkItem(discoverMySqlInstances);
            InitializeComponent();
        }

        public ObservableCollection<DbInstance> Instances
		{
            get { return (ObservableCollection<DbInstance>)GetValue(InstancesProperty); }
			set { SetValue(InstancesProperty, value); }
		}
		public int SelectedServer
		{
			get { return (int)GetValue(SelectedServerProperty); }
			set { SetValue(SelectedServerProperty, value); }
		}
		public bool CanLogin
		{
			get { return (bool)GetValue(CanLoginProperty); }
			set { SetValue(CanLoginProperty, value); }
		}
		public string Username
		{
			get { return (string)GetValue(UsernameProperty); }
			set { SetValue(UsernameProperty, value); }
		}

        private void discoverSqlInstances(object obj)
        {
            SqlDataSourceEnumerator enumerator = SqlDataSourceEnumerator.Instance;
            DataTable sources = enumerator.GetDataSources();
            List<DbInstance> instances = new List<DbInstance>();

            foreach (DataRow source in sources.Rows)
                instances.Add(new SqlInstance
                    {
                        Server = source.Field<string>("ServerName"),
                        Instance = source.Field<string>("InstanceName"),
                    });

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LoadInstances(instances)));
        }

        private void discoverMySqlInstances(object obj)
        {
            /* MySQL apparently doesn't implement CreateDataSourceEnumerator
            MySqlClientFactory factory = new MySqlClientFactory();
            DbDataSourceEnumerator enumerator = factory.CreateDataSourceEnumerator();
            List<DbInstance> instances = new List<DbInstance>();

            foreach (DataRow source in enumerator.GetDataSources().Rows)
                instances.Add(new MySqlInstance
                {
                    Server = source.Field<string>("ServerName"),
                    Instance = source.Field<string>("InstanceName"),
                });

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LoadInstances(instances)));
             */
        }

        private void LoadInstances(List<DbInstance> instances)
        {

            instances.ForEach(i => Instances.Add(i));
			CanLogin = true;
            //SelectedServer = 0;
            Cursor = Cursors.Arrow;
        }

        private void ExecuteLogin(object sender, ExecutedRoutedEventArgs args)
        {
            DbInstance instance = Instances[SelectedServer];
            Connection connection = instance.CreateConnection(Username, Password.Password);

            IsEnabled = false;
			OpenTools(connection);
        }

        private void CanExecuteLogin(object sender, CanExecuteRoutedEventArgs args) {
            args.CanExecute = CanLogin;
        }

		private void OpenTools(Connection connection){
            try {
                DbTools dialog;

                dialog = new DbTools(connection);
                dialog.ShowActivated = true;
                dialog.Show();
                Close();
            }
            catch(Exception ex) {
                MessageBox.Show(ex.GetType().Name + ":\n " + ex.Message, Title + " Error");
                IsEnabled = true;
            }
		}

        private void ExecuteQuit(object sender, ExecutedRoutedEventArgs args)
        {
            Application.Current.Shutdown();
        }

        private void ExecuteRefresh(object sender, ExecutedRoutedEventArgs args) {
            CanLogin = true;
            Instances.Clear();
            Instances.Add(new SqlInstance { Server = "localhost" });
            SelectedServer = 0;
            ThreadPool.QueueUserWorkItem(discoverSqlInstances);
        }

        private void CanExecuteRefresh(object sender, CanExecuteRoutedEventArgs args) {
            args.CanExecute = CanLogin;
        }

        private void ExecuteOpenFile(object sender, ExecutedRoutedEventArgs args)
        {
            OpenFileDialog dialog = new OpenFileDialog {
                DefaultExt = ".sdf",
                CheckFileExists = true,
                DereferenceLinks = true,
                Filter = "MS SQL Ce|*.sdf",
                Title = "Open Embedded Database file"
            };

            IsEnabled = false;

            if (dialog.ShowDialog() == true)
            {
                Connection connection = new CeConnection()
                    {
                        ConnectionString = String.Format(
                            "Data Source={0}",
                            dialog.FileName)
                    };

                OpenTools(connection);
            }
            else
            {
                IsEnabled = true;
            }
        }

        private void ExecuteManual(object sender, ExecutedRoutedEventArgs args) {
            var dialog = new ManualServer();

            if(dialog.ShowDialog() == true) {
                Instances.Insert(0, dialog.DbInstance);
                SelectedServer = 0;
            }
        }
    }
}

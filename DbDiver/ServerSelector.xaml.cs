using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
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
		public static readonly DependencyProperty InstancesProperty = DependencyProperty.Register("Instances", typeof(List<DbInstance>), typeof(ServerSelector));
		public static readonly DependencyProperty SelectedServerProperty = DependencyProperty.Register("SelectedServer", typeof(int), typeof(ServerSelector));
		public static readonly DependencyProperty CanLoginProperty = DependencyProperty.Register("CanLogin", typeof(bool), typeof(ServerSelector));
		public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register("Username", typeof(string), typeof(ServerSelector));

        public ServerSelector()
        {
            Instances = new List<DbInstance> {
                new DbInstance { Server = "localhost" }
            };
            SelectedServer = 0;
            CanLogin = true;
            Cursor = Cursors.AppStarting;
            ThreadPool.QueueUserWorkItem(discoverInstances);
            InitializeComponent();
        }

		public List<DbInstance> Instances
		{
			get { return (List<DbInstance>)GetValue(InstancesProperty); }
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

        private void discoverInstances(object obj)
        {
            SqlDataSourceEnumerator enumerator = SqlDataSourceEnumerator.Instance;
            DataTable sources = enumerator.GetDataSources();
            List<DbInstance> instances = new List<DbInstance>
                {
                    new DbInstance {
                        Server = "localhost"
                    }
                };

            foreach (DataRow source in sources.Rows)
                instances.Add(new DbInstance
                    {
                        Server = source.Field<string>("ServerName"),
                        Instance = source.Field<string>("InstanceName"),
                    });

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LoadInstances(instances)));
        }

        private void LoadInstances(List<DbInstance> instances)
        {
            Instances = instances;
			CanLogin = true;
            SelectedServer = 0;
            Cursor = Cursors.Arrow;
        }

        private void ExecuteLogin(object sender, ExecutedRoutedEventArgs args)
        {
            DbInstance instance = Instances[SelectedServer];
            Connection connection = new Connection();

            IsEnabled = false;

            if(String.IsNullOrWhiteSpace(Username)){
                if(String.IsNullOrWhiteSpace(instance.Instance))
                    connection.ConnectionString = String.Format(
                        "Server={0};Trusted_Connection=True;",
                        instance.Server);
                else
                    connection.ConnectionString = String.Format(
                        "Server={0}\\{1};Trusted_Connection=True;",
                        instance.Server,
                        instance.Instance);
            }
            else{
                if(String.IsNullOrWhiteSpace(instance.Instance))
                    connection.ConnectionString = String.Format(
                        "Server={0};User Id={1};Password={2};",
                        instance.Server,
                        Username,
                        Password.Password);
                else
                    connection.ConnectionString = String.Format(
                        "Server={0}\\{1};User Id={2};Password={3};",
                        instance.Server,
                        instance.Instance,
                        Username,
                        Password.Password);
            }

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
                this.Close();
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
            Instances = new List<DbInstance> {
                new DbInstance { Server = "localhost" }
            };
            SelectedServer = 0;
            ThreadPool.QueueUserWorkItem(discoverInstances);
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

        public class DbInstance
        {
            public string Server { get; set; }
            public string Instance { get; set; }
            public string Name
            {
                get { return String.Format("{0} ({1})", Server, Instance); }
            }
        }
    }
}

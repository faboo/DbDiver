using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.Sql;
using System.IO;
using System.Linq;
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
        public static readonly DependencyProperty InstancesProperty = DependencyProperty.Register("Instances", typeof(ObservableCollection<NetworkInstance>), typeof(ServerSelector));
		public static readonly DependencyProperty SelectedServerProperty = DependencyProperty.Register("SelectedServer", typeof(int), typeof(ServerSelector));
		public static readonly DependencyProperty CanLoginProperty = DependencyProperty.Register("CanLogin", typeof(bool), typeof(ServerSelector));
		public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register("Username", typeof(string), typeof(ServerSelector));

        public ServerSelector()
        {
            Instances = new ObservableCollection<NetworkInstance> {
                new SqlInstance { Server = "localhost" },
                new MySqlInstance { Server = "localhost" },
            };
            SelectedServer = 0;
            CanLogin = true;
            Cursor = Cursors.AppStarting;
            ThreadPool.QueueUserWorkItem(discoverSqlInstances);
            InitializeComponent();
        }

        public ObservableCollection<NetworkInstance> Instances
		{
            get { return (ObservableCollection<NetworkInstance>)GetValue(InstancesProperty); }
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
            var instances = DbProvider.GetPublicInstances();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LoadInstances(instances)));
        }

        private void LoadInstances(List<NetworkInstance> instances)
        {

            instances.ForEach(i => Instances.Add(i));
			CanLogin = true;
            Cursor = Cursors.Arrow;
        }

        private void ExecuteLogin(object sender, ExecutedRoutedEventArgs args)
        {
            NetworkInstance instance = Instances[SelectedServer];
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

                // Test the login credentials:
                using (var conn = connection.Get())
                    conn.Close();

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
            List<FileInstance> instances = DbProvider.FileProviders;
            OpenFileDialog dialog = new OpenFileDialog {
                DefaultExt = instances.First().FileType,
                CheckFileExists = true,
                DereferenceLinks = true,
                Filter = instances.Select(i => String.Format("{0}|*{1}", i.Name, i.FileType)).Join(';'),
                Title = "Open Embedded Database file"
            };

            IsEnabled = false;

            if (dialog.ShowDialog() == true)
            {
                FileInstance instance = instances.First(i =>
                    i.FileType.Equals(Path.GetExtension(dialog.FileName)));
                Connection connection = instance.CreateConnection(dialog.FileName);

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

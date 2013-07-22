using System;
using System.Collections.Generic;
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

namespace DbDiver {
    /// <summary>
    /// Interaction logic for ManualServer.xaml
    /// </summary>
    public partial class ManualServer: Window {
		public static readonly DependencyProperty TypeProperty = DependencyProperty.Register("Type", typeof(string), typeof(ManualServer));
		public static readonly DependencyProperty AddressProperty = DependencyProperty.Register("Address", typeof(string), typeof(ManualServer));
		public static readonly DependencyProperty InstanceProperty = DependencyProperty.Register("Instance", typeof(string), typeof(ManualServer));
		public static readonly DependencyProperty PortProperty = DependencyProperty.Register("Port", typeof(string), typeof(ManualServer));

		public string Type
		{
			get { return (string)GetValue(TypeProperty); }
			set { SetValue(TypeProperty, value); }
		}
		public string Address
		{
			get { return (string)GetValue(AddressProperty); }
			set { SetValue(AddressProperty, value); }
		}
		public string Instance
		{
			get { return (string)GetValue(InstanceProperty); }
			set { SetValue(InstanceProperty, value); }
		}
		public string Port
		{
			get { return (string)GetValue(PortProperty); }
			set { SetValue(PortProperty, value); }
		}

        public DbInstance DbInstance { get; private set; }

        public ManualServer() {
            InitializeComponent();
            typeBox.SelectedIndex = 0;
        }

        private void ExecuteOk(object sender, ExecutedRoutedEventArgs args) {
            if(Type.Equals("MS SQL"))
                DbInstance = new SqlInstance {
                    Server = Address,
                    Instance = Instance,
                    Port = Port,
                };
            else if(Type.Equals("MySQL"))
                DbInstance = new MySqlInstance {
                    Server = Address,
                    Port = Port,
                };

            DialogResult = true;
            args.Handled = true;
        }

        private void CanExecuteOk(object sender, CanExecuteRoutedEventArgs args) {
            args.CanExecute = !String.IsNullOrWhiteSpace(Address);
        }

        private void ExecuteCancel(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
        }
    }
}

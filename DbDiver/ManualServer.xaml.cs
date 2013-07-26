using System;
using System.Data.Common;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace DbDiver {
    using Provider = Tuple<string, DbProviderFactory, Func<NetworkInstance>>;


    /// <summary>
    /// Interaction logic for ManualServer.xaml
    /// </summary>
    public partial class ManualServer: Window {
        public static readonly DependencyProperty TypesProperty = DependencyProperty.Register("Types", typeof(IEnumerable<Provider>), typeof(ManualServer));
        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register("Type", typeof(Provider), typeof(ManualServer));
		public static readonly DependencyProperty AddressProperty = DependencyProperty.Register("Address", typeof(string), typeof(ManualServer));
		public static readonly DependencyProperty InstanceProperty = DependencyProperty.Register("Instance", typeof(string), typeof(ManualServer));
		public static readonly DependencyProperty PortProperty = DependencyProperty.Register("Port", typeof(string), typeof(ManualServer));

        public IEnumerable<Provider> Types
		{
            get { return (IEnumerable<Provider>)GetValue(TypesProperty); }
			set { SetValue(TypesProperty, value); }
		}
        public Provider Type
		{
            get { return (Provider)GetValue(TypeProperty); }
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

        public NetworkInstance DbInstance { get; private set; }

        public ManualServer() {
            Types = DbProvider.NetworkProviders;
            Type = Types.First();
            InitializeComponent();
        }

        private void ExecuteOk(object sender, ExecutedRoutedEventArgs args) {
            DbInstance = Type.Item3();

            DbInstance.Server = Address;
            DbInstance.Instance = Instance;
            DbInstance.Port = Port;

            DialogResult = true;
            args.Handled = true;
        }

        private void CanExecuteOk(object sender, CanExecuteRoutedEventArgs args) {
            args.CanExecute = !String.IsNullOrWhiteSpace(Address);
        }

        private void ExecuteCancel(object sender, ExecutedRoutedEventArgs args) {
            Close();
            args.Handled = true;
        }
    }
}

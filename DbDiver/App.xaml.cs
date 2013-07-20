using System;
using System.Windows;
using System.Windows.Threading;

namespace DbDiver
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            DispatcherUnhandledException += OnDispatcherUnhandledException;
#endif
            new ServerSelector
            {
                ShowActivated = true
            }.Show();
            base.OnStartup(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            if (!(args.Exception is ArgumentOutOfRangeException))
                MessageBox.Show("An error occurred: " + args.Exception + "\n", "DbDiver Error");
            args.Handled = true;
        }
    }
}

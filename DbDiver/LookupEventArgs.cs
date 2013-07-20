using System.Windows;

namespace DbDiver {
    public delegate void LookupEventHandler(object sender, LookupEventArgs args);

    public class LookupEventArgs: RoutedEventArgs {
        public string Procedure { get; set; }
    }
}

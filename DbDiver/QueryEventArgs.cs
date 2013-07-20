using System.Windows;

namespace DbDiver
{
    public delegate void QueryEventHandler(object sender, QueryEventArgs args);

    public class QueryEventArgs : RoutedEventArgs
    {
        public string Query { get; set; }
    }
}

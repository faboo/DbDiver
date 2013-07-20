using System.Windows;

namespace DbDiver
{
    public delegate void CrawlEventHandler(object sender, CrawlEventArgs args);

    public class CrawlEventArgs : RoutedEventArgs
    {
        public string Table { get; set; }
    }
}

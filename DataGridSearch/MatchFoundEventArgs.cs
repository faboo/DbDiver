using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;

namespace DataGridSearch
{
    public class MatchFoundEventArgs : RoutedEventArgs
    {
        public MatchFoundEventArgs(RoutedEvent routedEvent)
            : base(routedEvent)
        {
        }

        public DataRowView Row { get; set; }
    }

    public delegate void MatchFoundHandler(object sender, MatchFoundEventArgs args);
}

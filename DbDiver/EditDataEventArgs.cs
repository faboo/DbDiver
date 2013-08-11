using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace DbDiver
{
    public delegate void EditDataEventHandler(object sender, EditDataEventArgs args);

    public class EditDataEventArgs : RoutedEventArgs
    {
        public string Table { get; set; }
        public int Size { get; set; }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;

namespace DbDiver {
    /// <summary>
    /// Interaction logic for DescribeControl.xaml
    /// </summary>
    public partial class DescribeControl: UserControl {
		public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register("Connection", typeof(Connection), typeof(DescribeControl));
        public static readonly DependencyProperty ObjNameProperty = DependencyProperty.Register("ObjName", typeof(String), typeof(DescribeControl));
		//public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(String), typeof(DescribeControl));
        public static readonly RoutedEvent OnCrawlEvent = EventManager.RegisterRoutedEvent("OnCrawl", RoutingStrategy.Bubble, typeof(CrawlEventHandler), typeof(DescribeControl));

        private string plainDescription;
        private int currentFound = 0;

        public DescribeControl() {
            ObjNames = new ObservableCollection<string>();
            InitializeComponent();
        }

        public Connection Connection
		{
            get { return (Connection)GetValue(ConnectionProperty); }
			set { SetValue(ConnectionProperty, value); }
		}
		public String ObjName
		{
            get { return (String)GetValue(ObjNameProperty); }
            set { SetValue(ObjNameProperty, value); }
		}
        public ObservableCollection<string> ObjNames { get; set; }

        public event CrawlEventHandler OnCrawl
        {
            add { AddHandler(OnCrawlEvent, value); }
            remove { RemoveHandler(OnCrawlEvent, value); }
        }

        private void ExecuteDescribe(object sender, ExecutedRoutedEventArgs args){
            string name = args.Parameter as string;
            var connection = Connection;
            Cursor = Cursors.AppStarting;
            ObjNames.Remove(name);
            ObjNames.Insert(0, name);
            ThreadPool.QueueUserWorkItem(obj => describe(connection, name));
		}

		private void CanExecuteDescribe(object sender, CanExecuteRoutedEventArgs args){
			args.CanExecute = args.Parameter is string && !String.IsNullOrWhiteSpace(args.Parameter as string);
		}

        private void ExecuteDescribeSelected(object sender, ExecutedRoutedEventArgs args)
        {
            NavigationCommands.GoToPage.Execute(Description.Selection.Text.Trim(), this);
        }

        private void ExecuteCrawl(object sender, ExecutedRoutedEventArgs args)
        {
            RaiseEvent(new CrawlEventArgs { Table = Description.Selection.Text.Trim(), RoutedEvent = OnCrawlEvent });
        }

        private void describe(Connection connection, string name)
        {
            try {
                string description = connection.DescribeProcedure(name);

                updateDescription(Highlight.Sql, description);
            }
            catch (Exception ex){
                updateDescription(Highlight.Sql,
                    "Exception examining object:\n"+
                    ex.GetType().Name+": \n"+
                    ex.Message);
            }

        }

        private void updateDescription(Highlighter highlighter, string description)
        {
            Dispatcher.Invoke(
                DispatcherPriority.Background,
                new Action<String, String>(Describe),
                description,
                serializeDocument(Highlight.Plain.Highlight(description)));
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action<String,String>(Describe),
                description,
                serializeDocument(highlighter.Highlight(description)));
        }

        private string serializeDocument(FlowDocument document)
        {
            string serialized = null;

            using (MemoryStream stream = new MemoryStream())
            {
                XamlWriter.Save(document, stream);
                TextReader read;

                stream.Position = 0;
                read = new StreamReader(stream);
                serialized = read.ReadToEnd();
                stream.Close();
            }

            return serialized;
        }

        private void Describe(string plainDescription, string description) {
            this.plainDescription = plainDescription;
            Description.Document = (FlowDocument)XamlReader.Parse(description);
            Cursor = Cursors.Arrow;
        }

        private void ExecuteFind(object sender, ExecutedRoutedEventArgs args)
        {
            if (currentFound > plainDescription.Length || currentFound < 0)
                currentFound = 0;

            /*currentFound = plainDescription.IndexOf(
                SearchOperations.GetSearchTerm(this),
                currentFound,
                StringComparison.InvariantCultureIgnoreCase);*/

            if (currentFound >= 0)
            {
                var position = Description.CaretPosition.DocumentStart.GetPositionAtOffset(currentFound, LogicalDirection.Forward);
                Description.CaretPosition = position;
                Description.ScrollToVerticalOffset(Description.CaretPosition.GetCharacterRect(LogicalDirection.Forward).Top);
            }
        }


        private void OnEnterKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == System.Windows.Input.Key.Enter)
            {
                NavigationCommands.GoToPage.Execute(null, this);
            }
        }
    }
}

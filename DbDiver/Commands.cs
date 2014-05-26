using System.Windows.Input;

namespace DbDiver
{
    public static class Commands
    {
        public static RoutedCommand FindNext = new RoutedUICommand("Find Next", "FindNext", typeof(Commands),
            new InputGestureCollection {
                new KeyGesture(System.Windows.Input.Key.F3)
            });
        public static RoutedCommand Describe = new RoutedUICommand("Describe", "Describe", typeof(Commands),
            new InputGestureCollection {
                new KeyGesture(System.Windows.Input.Key.D, ModifierKeys.Control)
            });
    }
}

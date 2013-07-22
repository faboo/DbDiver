using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DbDiver {
    // This is based on a post here:
    // http://stackoverflow.com/questions/5175629/how-to-style-grid-columndefinitions-in-wpf
    class GridExtension {
        public static readonly DependencyProperty ColumnDefinitionsProperty =
            DependencyProperty.RegisterAttached(
                "ColumnDefinitions",
                typeof(ColumnDefinitionCollection),
                typeof(GridExtension),
                new PropertyMetadata(
                    default(ColumnDefinitionCollection),
                    OnColumnDefinitionsChanged));

        private static void OnColumnDefinitionsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
            var grid = (Grid)obj;
            var oldValue = (ColumnDefinitionCollection)args.OldValue;
            var newValue = (ColumnDefinitionCollection)args.NewValue;

            grid.ColumnDefinitions.Clear();
            if(newValue != null)
                foreach(var column in newValue)
                    grid.ColumnDefinitions.Add(new ColumnDefinition {
                        MaxWidth = column.MaxWidth,
                        MinWidth = column.MinWidth,
                        SharedSizeGroup = column.SharedSizeGroup,
                        Width = column.Width,
                    });
        }

        public static void SetColumnDefinitions(Grid control, ColumnDefinitionCollection value) {
            control.SetValue(ColumnDefinitionsProperty, value);
        }

        public static ColumnDefinitionCollection GetColumnDefinitions(Grid control) {
            return (ColumnDefinitionCollection)control.GetValue(ColumnDefinitionsProperty);
        }       
    }

    public class ColumnDefinitionCollection: List<ColumnDefinition> { }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections;

namespace DataGridSearch
{
    public static class SearchOperations
    {
        public static string GetSearchTerm(DependencyObject obj)
        {
            return (string)obj.GetValue(SearchTermProperty);
        }

        public static void SetSearchTerm(DependencyObject obj, string value)
        {
            obj.SetValue(SearchTermProperty, value);
        }

        public static readonly DependencyProperty SearchTermProperty =
            DependencyProperty.RegisterAttached(
                "SearchTerm",
                typeof(string),
                typeof(SearchOperations),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.Inherits));




        public static bool GetIsMatch(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMatchProperty);
        }

        public static void SetIsMatch(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMatchProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsMatch.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMatchProperty =
            DependencyProperty.RegisterAttached("IsMatch", typeof(bool), typeof(SearchOperations), new UIPropertyMetadata(false));

        public static void AddMatchFoundHandler(DependencyObject receiver, MatchFoundHandler handler)
        {
            UIElement element = receiver as UIElement;
         
            if (element != null)
                element.AddHandler(MatchFoundEvent, handler);
        }
        public static void RemoveNeedsCleaningHandler(DependencyObject receiver, MatchFoundHandler handler)
        {
            UIElement element = receiver as UIElement;
            if (element != null)
                element.RemoveHandler(MatchFoundEvent, handler);
        }

        public static readonly RoutedEvent MatchFoundEvent = EventManager.RegisterRoutedEvent(
            "MatchFound", RoutingStrategy.Bubble, typeof(MatchFoundHandler), typeof(SearchOperations));
    }
}

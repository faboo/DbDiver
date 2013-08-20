using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DbDiver
{
    public static class FrameworkElementExtension
    {
        public static T FindVisualChild<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        return (T)child;
                    }

                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }

        public static void Weave<T, R>(this FrameworkElement element, Func<T, R> background, Action<R> foreground, Action after, T val)
        {
            Weave<R>(element, background, foreground, after, val);
        }

        public static void Weave<R>(this FrameworkElement element, Delegate background, Action<R> foreground, Action after, params object[] vals)
        {
            WeaveWithError<R>(element, background, foreground, after, null, vals);
        }


        public static void WeaveWithError<R>(
            this FrameworkElement element,
            Delegate background,
            Action<R> foreground,
            Action after,
            Action<Exception> error,
            params object[] vals)
        {
            ThreadPool.QueueUserWorkItem(
                obj =>
                {
                    try
                    {
                        R result = (R)background.DynamicInvoke(vals);
                        element.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(
                                () =>
                                {
                                    if(foreground != null)
                                        foreground(result);
                                    after();
                                }));
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                            ex = ex.InnerException;

                        element.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(
                                () =>
                                {
                                    after();
                                    if (error != null)
                                        error(ex);
                                    else
                                        MessageBox.Show(Application.Current.MainWindow, ex.Message, "DbDiver");
                                }));
                    }

                });
        }

        public static void Background(this FrameworkElement element, Action background)
        {
            Background(element, (Delegate)background);
        }

        public static void Background(this FrameworkElement element, Delegate background, params object[] vals)
        {
            element.Cursor = Cursors.AppStarting;
            ThreadPool.QueueUserWorkItem(
                obj =>
                {
                    try
                    {
                        background.DynamicInvoke(vals);
                        element.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() => element.Cursor = Cursors.Arrow));
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                            ex = ex.InnerException;

                        element.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(
                                () =>
                                {
                                    element.Cursor = Cursors.Arrow;
                                    MessageBox.Show(Application.Current.MainWindow, ex.Message, "DbDiver");
                                }));
                    }
                });
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using installEAS;
namespace installEAS.Controls;

// public static class ProgressBarExtensions
// {
//     public static TimeSpan duration = TimeSpan.FromMilliseconds(1000);
//
//     public static void SetPercent(this ProgressBar progressBar, double percentage, TimeSpan span)
//     {
//         DoubleAnimation animation = new(percentage, span);
//         progressBar.Dispatcher.InvokeOrExecute(() =>
//         {
//             progressBar.BeginAnimation(RangeBase.ValueProperty, animation);
//         });
//        
//     }
//     public static void ProgressBarSet(double percentage, int timespan)
//     {
//         var span = TimeSpan.FromMilliseconds(timespan);
//         MainWindow.MainFrame.Dispatcher.InvokeOrExecute(() =>
//         {
//             MainWindow.MainFrame.pb.SetPercent(percentage, span);
//         });
//     }
//
//     public static void SetPercentDuration(this ProgressBar progressBar, double percentage, int timespan)
//     {
//         var span = TimeSpan.FromMilliseconds(timespan);
//         DoubleAnimation animation = new(percentage, span);
//         progressBar.Dispatcher.InvokeOrExecute(() =>
//         {
//             progressBar.BeginAnimation(RangeBase.ValueProperty, animation);
//             
//         });
//     }
// }
//
// public static class ProgressBarSmoother
// {
//     public static readonly DependencyProperty SmoothValueProperty =
//         DependencyProperty.RegisterAttached("SmoothValue", typeof(double), typeof(ProgressBarSmoother), new PropertyMetadata(0.0, changing));
//
//     public static void changing(DependencyObject d, DependencyPropertyChangedEventArgs e)
//     {
//         var anim = new DoubleAnimation((double)e.OldValue, (double)e.NewValue, new TimeSpan(0, 0, 0, 0, 10000));
//         (d as ProgressBar)?.BeginAnimation(RangeBase.ValueProperty, anim, HandoffBehavior.Compose);
//     }
//
//     public static double GetSmoothValue(DependencyObject obj)
//     {
//         return (double)obj.GetValue(SmoothValueProperty);
//     }
//
//     public static void SetSmoothValue(DependencyObject obj, double value)
//     {
//         obj.SetValue(SmoothValueProperty, value);
//     }
// }
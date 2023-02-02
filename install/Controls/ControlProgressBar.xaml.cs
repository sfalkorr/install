using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using installEAS.Helpers;

namespace installEAS.Controls;

public partial class UIControlProgressBar
{
    public UIControlProgressBar()
    {
        InitializeComponent();
        pbLabel.Visibility = Visibility.Hidden;
    }
}

public static class ProgressBarExtensions
{
    private static UIControlProgressBar _uiControlProgress = new();
    public static  DoubleAnimation      animStop           = new(0, TimeSpan.FromMilliseconds(1));

    public static void SetPercent(this ProgressBar progressBar, double percentage, TimeSpan span)
    {
        var anim = new DoubleAnimation(percentage, span);
        progressBar.Dispatcher.InvokeOrExecute(() => { progressBar.BeginAnimation(RangeBase.ValueProperty, anim); });
    }

    public static void ProgressBarSet(double percentage, int timespan)
    {
        var span = TimeSpan.FromMilliseconds(timespan);
        _uiControlProgress.progressBar.SetPercent(percentage, span);
    }

    [STAThread]
    public static void SetPercentDuration(this ProgressBar progressBar, double percentage, int timespan)
    {
        MainWindow.MainFrame.pb.pbLabel.Visibility = Visibility.Visible;
        var span = TimeSpan.FromMilliseconds(timespan);
        var anim = new DoubleAnimation(percentage, span);
        anim.Completed += async (_, _) =>
        {
            await Task.Delay(250).ConfigureAwait(false);
            MainWindow.MainFrame.Dispatcher.InvokeOrExecute(() => { MainWindow.MainFrame.pb.pbLabel.Visibility = Visibility.Hidden; });
            progressBar.Dispatcher.InvokeOrExecute(() => { progressBar.BeginAnimation(RangeBase.ValueProperty, animStop); });
        };
        progressBar.Dispatcher.InvokeOrExecute(() => { progressBar.BeginAnimation(RangeBase.ValueProperty, anim); });
    }
}

public static class ProgressBarSmoother
{
    public static readonly DependencyProperty SmoothValueProperty = DependencyProperty.RegisterAttached("SmoothValue", typeof(double), typeof(ProgressBarSmoother), new PropertyMetadata(0.0, changing));

    public static void changing(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var anim = new DoubleAnimation((double)e.OldValue, (double)e.NewValue, new TimeSpan(0, 0, 0, 0, 10000));
        (d as ProgressBar)?.BeginAnimation(RangeBase.ValueProperty, anim, HandoffBehavior.Compose);
    }

    public static double GetSmoothValue(DependencyObject obj)
    {
        return (double)obj.GetValue(SmoothValueProperty);
    }

    public static void SetSmoothValue(DependencyObject obj, double value)
    {
        obj.SetValue(SmoothValueProperty, value);
    }
}
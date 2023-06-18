namespace installEAS.Controls;

public partial class ProgressBarControl
{
    public ProgressBarControl()
    {
        InitializeComponent();
        pbLabel.Visibility = Visibility.Hidden;
    }
}

public static class ProgressBarExtensions
{
    private static ProgressBarControl _progressBarControlProgress = new();
    public static  DoubleAnimation    animStop                    = new(0, TimeSpan.FromMilliseconds(1));

    public static void SetPercent(this ProgressBar progressBar, double percentage, TimeSpan span)
    {
        var anim = new DoubleAnimation(percentage, span);
        progressBar.Dispatcher.InvokeOrExecute(() => { progressBar.BeginAnimation(RangeBase.ValueProperty, anim); });
    }

    public static void ProgressBarSet(double percentage, int timespan)
    {
        var span = TimeSpan.FromMilliseconds(timespan);
        _progressBarControlProgress.progressBar.SetPercent(percentage, span);
    }

    [STAThread]
    public static void SetPercentDuration(this ProgressBar progressBar, double percentage, int timespan)
    {
        //MainFrame.pb.pbLabel.Foreground = Brushes.White;
        //MainFrame.pb.pbLabel.Visibility = Visibility.Visible;
        var span = TimeSpan.FromMilliseconds(timespan);
        var anim = new DoubleAnimation(percentage, span) { IsCumulative = false, FillBehavior = FillBehavior.Stop, IsAdditive = false };
        progressBar.Dispatcher.InvokeOrExecute(() =>
        {
            //anim.Completed += (_, _) => { MainFrame.pb.pbLabel.Visibility = Visibility.Hidden; };
            progressBar.BeginAnimation(RangeBase.ValueProperty, anim);
        });
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
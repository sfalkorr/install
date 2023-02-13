using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace installEAS.Helpers;

public class InClassName
{
    public InClassName(FrameworkElement Obj, string From, string To, int msecond)
    {
        this.Obj  = Obj;
        this.From = From;
        this.To   = To;
        Msecond   = msecond;
    }

    public FrameworkElement Obj     { get; }
    public string           From    { get; }
    public string           To      { get; }
    public int              Msecond { get; }
}

public abstract class Animate
{
    private static Func<string, object> _convertFromString;
    public static  string               controlFrom, controlTo, closeFrom, closeTo;

    public static void ColorAnimation(InClassName inClassName)
    {
        _convertFromString = ColorConverter.ConvertFromString;
        var from = (Color)_convertFromString(inClassName.From);
        var to   = (Color)_convertFromString(inClassName.To);
        {
            ColorAnimation animation = new() { From = from, To = to, Duration = new Duration(TimeSpan.FromMilliseconds(inClassName.Msecond)) };
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Grid.Background).(SolidColorBrush.Color)", null!));
            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin(inClassName.Obj);
        }
    }


    public static Task AnimateFrameworkElement(FrameworkElement Target, int Duration)
    {
        var sb = new Storyboard();
        var ta = new ThicknessAnimation
        {
            BeginTime         = new TimeSpan(0),
            Duration          = new Duration(TimeSpan.FromMilliseconds(Duration)),
            DecelerationRatio = 0.9,
            AccelerationRatio = 0,
            SpeedRatio        = 0.8,
            IsCumulative      = false,
            IsAdditive        = true
        };
        switch (Target.IsEnabled)
        {
            case false:
                ta.To            = new Thickness(0, 0, 0, 0);
                Target.Opacity   = 1;
                Target.IsEnabled = true;
                break;
            case true:
                ta.To            = new Thickness(0, -Target.ActualHeight - 30, 0, 0);
                Target.Opacity   = 1;
                Target.IsEnabled = false;
                break;
        }

        Timeline.SetDesiredFrameRate(ta, 60);
        Storyboard.SetTargetProperty(ta, new PropertyPath(FrameworkElement.MarginProperty));
        sb.FillBehavior = FillBehavior.HoldEnd;
        sb.Children.Add(ta);
        Target.Dispatcher.InvokeAsync(() => { sb.Begin(Target, HandoffBehavior.Compose); }, DispatcherPriority.Send);
        return Task.CompletedTask;
    }
}
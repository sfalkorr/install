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

    [STAThread]
    public static async Task AnimateFrameworkElement(FrameworkElement Target, int Duration)
    {
        //var rPoint = Convert.ToInt32( Target.TransformToAncestor( MainWindow.MainFrame ).Transform( new Point( 0, 0 ) ).Y );

        var sb = new Storyboard();
        var ta = new ThicknessAnimation
        {
            BeginTime         = new TimeSpan(1000),
            Duration          = new Duration(TimeSpan.FromMilliseconds(Duration)),
            DecelerationRatio = 0.5,
            AccelerationRatio = 0,
            SpeedRatio        = 1,
            IsCumulative      = false
        };
        Timeline.SetDesiredFrameRate(ta, 5);
        switch (Target.IsEnabled)
        {
            case false:
                ta.From          = new Thickness(0, -Target.ActualHeight - 30, 0, 0);
                ta.To            = new Thickness(0, 0, 0, 0);
                Target.Opacity   = 1;
                Target.IsEnabled = true;
                break;
            case true:
                ta.From          = new Thickness(0, 0, 0, 0);
                ta.To            = new Thickness(0, -Target.ActualHeight - 30, 0, 0);
                Target.Opacity   = 1;
                Target.IsEnabled = false;
                break;
        }

        Storyboard.SetTargetProperty(ta, new PropertyPath(FrameworkElement.MarginProperty));
        sb.FillBehavior = FillBehavior.HoldEnd;
        sb.Children.Add(ta);
        await Target.Dispatcher.InvokeAsync(() =>
        {
            sb.Begin(Target, HandoffBehavior.SnapshotAndReplace);
        }, DispatcherPriority.Send);
    }
}
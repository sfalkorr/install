using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;


namespace installEAS
{
    public class InClassName
    {
        public InClassName( FrameworkElement Obj, string From, string To, int Milliseconds )
        {
            this.Obj = Obj;
            this.From = From;
            this.To = To;
            this.Milliseconds = Milliseconds;
        }

        public FrameworkElement Obj { get; private set; }
        public string From { get; private set; }
        public string To { get; private set; }
        public int Milliseconds { get; private set; }
    }

    public static class Animate
    {
        private static Func<string, object> _convertFromString;
        public static string controlFrom, controlTo, closeFrom, closeTo;
        public static void ColorAnimation( InClassName inClassName )
        {
            _convertFromString = ColorConverter.ConvertFromString;
            var from = (Color)_convertFromString( inClassName.From );
            var to = (Color)_convertFromString( inClassName.To );
            {
                ColorAnimation animation = new()
                {
                    From = from,
                    To = to,
                    Duration = new Duration( TimeSpan.FromMilliseconds( inClassName.Milliseconds ) )
                };
                Storyboard.SetTargetProperty( animation, new PropertyPath( "(Grid.Background).(SolidColorBrush.Color)", null ) );
                var storyboard = new Storyboard();
                storyboard.Children.Add( animation );
                storyboard.Begin( inClassName.Obj );
            }
        }



        public class CustomSeventhPowerEasingFunction :EasingFunctionBase
        {
            public CustomSeventhPowerEasingFunction()
                : base()
            {
            }

            // Specify your own logic for the easing function by overriding
            // the EaseInCore method. Note that this logic applies to the "EaseIn"
            // mode of interpolation.
            protected override double EaseInCore( double normalizedTime )
            {
                // applies the formula of time to the seventh power.
                return Math.Pow( normalizedTime, 17 );
            }

            // Typical implementation of CreateInstanceCore
            protected override Freezable CreateInstanceCore()
            {

                return new CustomSeventhPowerEasingFunction();
            }
        }


        public static Task AnimateGridAsync( Grid Target, int Duration)
        {
            if (Target.Children != null)
            {
                var PanelTopPos = -((Target.Children.OfType<StackPanel>().FirstOrDefault()).Children.Count * 40);
                var relativePoint = Convert.ToInt32( Target.TransformToAncestor( MainWindow.MainFrame ).Transform( new Point( 0, 0 ) ).Y ) - 30;
                var sb = new Storyboard();
                
                var ta = new ThicknessAnimation
                {
                    BeginTime = new TimeSpan( 0 ),
                    Duration = new Duration( TimeSpan.FromMilliseconds( Duration ) ),
                    DecelerationRatio = 0,
                    AccelerationRatio = 0,
                    SpeedRatio = 0.5,
                    EasingFunction = new BounceEase()
                    //EasingFunction = new CustomSeventhPowerEasingFunction()

                    //EasingFunction = new CircleEase()
                    //EasingFunction  = new ElasticEase()

                };

                Storyboard.SetTargetProperty( ta, new PropertyPath( FrameworkElement.MarginProperty ) );

                sb.Children.Add( ta );
                if (Target.IsEnabled == false)
                {
                    ta.From = new Thickness( 0, PanelTopPos, 0, 0 );
                    ta.To = new Thickness( 0, 0, 0, 0 );
                    sb.Begin( Target );
                    Target.Opacity = 1;
                    Target.IsEnabled = true;
                }
                else
                {

                    ta.From = new Thickness( 0, relativePoint, 0, 0 );
                    ta.To = new Thickness( 0, PanelTopPos, 0, 0 );
                    sb.Begin( Target );
                    Target.Opacity = 0.8;
                    Target.IsEnabled = false;
                }
            }
            return  Task.CompletedTask;
        }
    }
}

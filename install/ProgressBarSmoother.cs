using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows;
using System.Windows.Media;

namespace installEAS
{
    public static class ProgressBarExtensions
    {
        public static TimeSpan duration = TimeSpan.FromMilliseconds( 1000 );

        public static void SetPercent( this ProgressBar progressBar, double percentage, TimeSpan duration )
        {
            DoubleAnimation animation = new( percentage, duration );
            progressBar.BeginAnimation( ProgressBar.ValueProperty, animation );
            
        }

        public static void ProgressBarSet( double percentage, TimeSpan duration  )
        {
            MainWindow.MainFrame.Dispatcher.InvokeOrExecute(() =>
            {
                MainWindow.MainFrame.pb.SetPercent( percentage, duration );
            });
        }

        public static void SetPercentDuration( this ProgressBar progressBar, double percentage, TimeSpan duration )
        {
            DoubleAnimation animation = new ( percentage, duration );
            progressBar.BeginAnimation( ProgressBar.ValueProperty, animation );

        }
    }
    public static class ProgressBarSmoother
    {
        public static readonly DependencyProperty SmoothValueProperty = DependencyProperty.RegisterAttached( "SmoothValue", typeof( double ), typeof( ProgressBarSmoother ), new PropertyMetadata( 0.0, changing ) );

        public static void changing( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            var anim = new DoubleAnimation( (double)e.OldValue, (double)e.NewValue, new TimeSpan( 0, 0, 0, 0, 10000 ) );
            (d as ProgressBar)?.BeginAnimation( ProgressBar.ValueProperty, anim, HandoffBehavior.Compose );
        }

        public static double GetSmoothValue( DependencyObject obj )
        {
            return (double)obj.GetValue( SmoothValueProperty );
        }

        public static void SetSmoothValue( DependencyObject obj, double value )
        {
            obj.SetValue( SmoothValueProperty, value );
        }
    }
}

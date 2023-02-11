using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace installEAS.Controls;

public partial class RounderProgressBarControl
{
    private delegate void VoidDelegete();
    public static Timer timer;

    public RounderProgressBarControl()
    {
        InitializeComponent();
        timer = new Timer( 100 );
    }
    public void Start()
    {

        Dispatcher.InvokeOrExecute( () =>
        {
            roundwindow.Visibility = Visibility.Visible;
            timer = new Timer( 100 );
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
        } );

    }

    public void Stop()
    {
        Dispatcher.InvokeOrExecute( () =>
        {
            roundwindow.Visibility = Visibility.Hidden;
            timer.Stop();
        } );
    }

    public void OnTimerElapsed( object sender, ElapsedEventArgs e )
    {
        rotationCanvas.Dispatcher.Invoke
        (
            new VoidDelegete(
                delegate
                {
                    SpinnerRotate.Angle += 30;
                    if (SpinnerRotate.Angle != 360) return;
                    SpinnerRotate.Angle = 0;
                    
                }
            ),
            null
        );

    }

}
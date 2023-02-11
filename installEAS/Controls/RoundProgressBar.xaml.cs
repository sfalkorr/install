using System;

namespace installEAS.Controls;

public partial class RoundedProgressBarControl
{
    public static RoundedProgressBarControl RoundedProgressBarControlRounded;

    public RoundedProgressBarControl()
    {
        RoundedProgressBarControlRounded = this;
        InitializeComponent();
    }

    [STAThread]
    public static void Start()
    {
        RoundedProgressBarControlRounded.sprocketControl.IsIndeterminate = true;
        RoundedProgressBarControlRounded.IsEnabled                       = true;
    }

    [STAThread]
    public static void Stop()
    {
        RoundedProgressBarControlRounded.sprocketControl.IsIndeterminate = false;
        RoundedProgressBarControlRounded.IsEnabled                       = false;
    }
}
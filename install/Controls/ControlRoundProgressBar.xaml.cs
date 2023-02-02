using System;

namespace installEAS.Controls;

public partial class UIControlRoundedProgressBar
{
    public static UIControlRoundedProgressBar UiControlRounded;

    public UIControlRoundedProgressBar()
    {
        UiControlRounded = this;
        InitializeComponent();
    }

    [STAThread]
    public static void Start()
    {
        UiControlRounded.sprocketControl.IsIndeterminate = true;
        UiControlRounded.IsEnabled                       = true;
    }

    [STAThread]
    public static void Stop()
    {
        UiControlRounded.sprocketControl.IsIndeterminate = false;
        UiControlRounded.IsEnabled                       = false;
    }
}
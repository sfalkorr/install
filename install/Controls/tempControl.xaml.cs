using System;
using System.Windows;
using System.Windows.Input;
using installEAS.Common;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;
using static installEAS.Variables;
using static installEAS.MainWindow;
using static installEAS.Helpers.Log;
using static installEAS.Controls.SlidePanelsControl;


namespace installEAS.Controls;

public partial class tempControl
{
    public static void CreatetempControlInstance()
    {
        var variablesInstance = CreatetempControlInstance;
        Console.WriteLine(variablesInstance.Method);
    }

    public tempControl()
    {
        InitializeComponent();
    }

    private void Btn1_OnClick(object sender, RoutedEventArgs e)
    {
        //Console.WriteLine(
        //Replica.ReplicaGetSqlPackage());
        Replica.ReplicaSqlPackageStartAsync();
    }


    private void Btn2_OnClick(object sender, RoutedEventArgs e)
    {
        //MainFrame.fluidProgressBar.StartFluidAnimation();
    }

    private void Btn3_OnClick(object sender, RoutedEventArgs e)
    {
        RoundedProgressBarControl.Stop();
    }


    private void Btn4_OnClick(object sender, RoutedEventArgs e)
    {
        MainFrame.pb.Dispatcher.InvokeOrExecute(() => { MainFrame.pb.progressBar.SetPercentDuration(100, 5000); });
    }


    private void Btn5_OnClick(object sender, RoutedEventArgs e)
    {
        CreateVariablesInstance();
        CreateSlidePanelsInstance();
    }

    private void Btn6_OnClick(object sender, RoutedEventArgs e)
    {
    }


    private void Btn7_OnClick(object sender, RoutedEventArgs e)
    {
        log(Sql.IsServerConnected().ToString());
    }

    private void Btn8_OnClick(object sender, RoutedEventArgs e)
    {
        log(Sql.IsPasswordOK("QWEasd123*").ToString());
        log(Sql.IsPasswordOK("QWEasd123*1").ToString());
    }
}
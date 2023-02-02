using System;
using System.Windows;
using installEAS.Common;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;
using static installEAS.Variables;
using static installEAS.MainWindow;
using static installEAS.Helpers.Log;
using static installEAS.Controls.UIControlSlidePanels;


namespace installEAS.Controls;

public partial class tempControl
{
    public tempControl()
    {
        InitializeComponent();
    }

    private void Btn1_OnClick(object sender, RoutedEventArgs e)
    {
        log(POSVer.ToString());
        log(SevenZgPath);
        log(POSPath);
        Replica.ReplicaGetSqlPackage();
    }


    private void Btn2_OnClick(object sender, RoutedEventArgs e)
    {
        //MainFrame.pb.progressBar.SetPercentDuration(0,1000);
        //UIControlRoundedProgressBar.Start();
        UIControlRoundedProgressBar.Start();
        //Progress.pbLabel.Visibility = Visibility.Visible;
    }

    private void Btn3_OnClick(object sender, RoutedEventArgs e)
    {
        UIControlRoundedProgressBar.Stop();
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
        CustomMessageBox.ShowOK("Не найден путь ", "Ошибка инициализации", "Жопа!", MessageBoxImage.Error);
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
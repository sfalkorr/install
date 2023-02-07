using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using installEAS.Common;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;
using static installEAS.Variables;
using static installEAS.MainWindow;
using static installEAS.Helpers.Log;
using static installEAS.Helpers.Functions;
using static installEAS.Controls.SlidePanelsControl;
using Timer = System.Timers.Timer;


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
        Password.SaveSqlPassToReg("QWEasd123*");
    }

    private void Btn3_OnClick(object sender, RoutedEventArgs e)
    {
        log(Password.ReadSqlPassFromReg());
    }


    private  void Btn4_OnClick(object sender, RoutedEventArgs e)
    {
        _Btn4_OnClick().ConfigureAwait(false);
    }

    private static async Task _Btn4_OnClick()
    {
        
        log("a");
        await Sleep(3000).ConfigureAwait(false);
        log("b");
    }

    private  void Btn5_OnClick(object sender, RoutedEventArgs e)
    {

    }


    private void Btn6_OnClick(object sender, RoutedEventArgs e)
    {
        log("a");

        mLogAsync("хуй");
        log("b");
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
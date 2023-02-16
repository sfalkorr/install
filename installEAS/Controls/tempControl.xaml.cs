using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using installEAS.Common;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;
using Microsoft.Win32;
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


    private void Btn4_OnClick(object sender, RoutedEventArgs e)
    {
        MainFrame.rtb.AppendText(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n\n");
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n\n", Brushes.Coral);
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n\n", Brushes.Bisque);
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n\n", Brushes.Crimson);
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n", Brushes.Chartreuse);
        MainFrame.rtb.ScrollToEnd();

        //log("Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)", Brushes.OrangeRed);
        //log("Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)");
        //log("Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)", Brushes.GreenYellow);
    }


    private void Btn5_OnClick(object sender, RoutedEventArgs e)
    {
        ToEventLog(sender.ToString(), $"Хуйня случилась", Level.Error);
        ToEventLog(sender.ToString(), $"Нехуйня случилась", Level.Warning);
        ToEventLog(sender.ToString(), $"случилась", Level.Information);

        //MainFrame.rtb.Document.Blocks.Clear();
    }


    private void Btn6_OnClick(object sender, RoutedEventArgs e)
    {
    }


    private void Btn7_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void Btn8_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessTools.StartElevated("notepad", null);
    }
}
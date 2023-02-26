using System;
using System.Diagnostics;
using System.Management;
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
        //Replica.ReplicaSqlPackageStartAsync();
        //Console.WriteLine(EnvCheck.NameCheck(1, "R12-123456-N"));

        //Console.WriteLine(SQLRegParameters);
        // Console.WriteLine(IsServer());
        // Console.WriteLine(IsComputernameCorrect());
        // Console.WriteLine(OPSNum);
        // Console.WriteLine(DBOPSName);

        //WaitInput("Введите новый пароль для пользователя sa в SQL или введите new для генерации случайного");
        //SQLNewPassword();
        inputOpen();
        //Console.WriteLine(SetMachineName("C01-160024-N"));

        //Console.WriteLine(Reg.TestFilePath(@"HKLM:\SOFTWARE\7-Zip"));
        //Console.WriteLine(Reg.TestFilePath(@"HKLM:\SOFTWARE\7-Zip2"));
        //Console.WriteLine(RegistryTools.KeyExists(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQLServer\Parameters"));
    }


    private void Btn2_OnClick(object sender, RoutedEventArgs e)
    {
        log(inputClose());
        //Password.SaveSqlPassToReg("QWEasd123*");
        //MainFrame.pb.Dispatcher.InvokeOrExecute(() => { MainFrame.pb.progressBar.SetPercentDuration(99, 3000); });
    }

    private void Btn3_OnClick(object sender, RoutedEventArgs e)
    {
        var line = MainFrame.rtb.Document.Lines.Count;
        //MainFrame.rtb.Document.Lines.Remove(line);
        //MainFrame.pb.Dispatcher.InvokeOrExecute(() => { MainFrame.pb.progressBar.SetPercentDuration(0, 3000); });
        //log(Password.ReadSqlPassFromReg());
    }


    private void Btn4_OnClick(object sender, RoutedEventArgs e)
    {
        MainFrame.rtb.AppendText("Инициализация... Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n\n");
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n\n", Brushes.Coral);
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n\n", Brushes.Bisque);
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n", Brushes.YellowGreen);
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n", Brushes.Tomato);
        MainFrame.rtb.ScrollToEnd();
        MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n", Brushes.Yellow);
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
        log(GetEmbeddedResource("installEAS", "CustomHighlighting.xshd"));
    }


    private void Btn7_OnClick(object sender, RoutedEventArgs e)
    {
        //var result = CustomMessageBox.Show("Действительно закрыть приложение?", "Подтверждение выхода", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        Replica.ReplicaSqlPackageStartAsync();
    }

    private void Btn8_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessTools.StartElevated("notepad", null);
    }
}
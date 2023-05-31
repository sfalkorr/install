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
        //if (MainFrame.textBox.Text == "") MainFrame.tlabel.Text = "Пароль не может быть пустым";
        //MainFrame.tlabel.Visibility = Visibility.Visible;
        //MainFrame.textBoxOpen.Begin( MainFrame.textBox );
        //MainFrame.textBox.IsEnabled = true;
        //MainFrame.textBox.Focus();

        var res = MainFrame.userInput(inputType.AskNewSqlPassword);
        Console.WriteLine(res);
        //Console.WriteLine(sqlpass);
        //Replica.ReplicaSqlPackageStartAsync();
        //Console.WriteLine(EnvCheck.NameCheck(1, "R12-123456-N"));

        //Console.WriteLine(SQLRegParameters);
        // Console.WriteLine(IsServer());
        // Console.WriteLine(IsComputernameCorrect());
        // Console.WriteLine(OPSNum);
        // Console.WriteLine(DBOPSName);

        //WaitInput("Введите новый пароль для пользователя sa в SQL или введите new для генерации случайного");
        //SQLNewPassword();
        //inputOpen();
        //Console.WriteLine(inputClose());
        //Console.WriteLine(SetMachineName("C01-160024-N"));

        //Console.WriteLine(Reg.TestFilePath(@"HKLM:\SOFTWARE\7-Zip"));
        //Console.WriteLine(Reg.TestFilePath(@"HKLM:\SOFTWARE\7-Zip2"));
        //Console.WriteLine(RegistryTools.KeyExists(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQLServer\Parameters"));
    }

    private void Btn2_OnClick(object sender, RoutedEventArgs e)
    {
        var res = MainFrame.userInput(inputType.AskCurrentSqlPassword);
        Console.WriteLine(res);
        //log(NewSqlPass);
        //Password.SaveSqlPassToReg("QWEasd123*");
        //MainFrame.pb.Dispatcher.InvokeOrExecute(() => { MainFrame.pb.progressBar.SetPercentDuration(99, 3000); });
    }

    //public static int temperror { get; set; }

    private void Btn3_OnClick(object sender, RoutedEventArgs e)
    {
        WindowHelper.MoveToCenterBase(MainFrame);
        //IsSqlConnectionAsync2();

        //await Task.Run(async () => temperror = await TestSqlConnectionAsync().ConfigureAwait(true));
        //Console.WriteLine(temperror);
        //var accountTask  = Task.Run(async () => Console.WriteLine(await TestSqlConnectionAsync("QWEasd123**").ConfigureAwait(false)));
        //Console.WriteLine(accountTask.GetAwaiter());
        //Console.WriteLine(TestSqlConnectionAsync("QWEasd123*"));
        //Task.WhenAll(accountTask);
    }

    private void Btn4_OnClick(object sender, RoutedEventArgs e)
    {
        // MainFrame.rtb.AppendText(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n");
        // MainFrame.rtb.ScrollToEnd();
        // MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n",
        //                               Brushes.Aqua);
        // MainFrame.rtb.ScrollToEnd();
        // MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n",
        //                               Brushes.Bisque);
        // MainFrame.rtb.ScrollToEnd();
        // MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n",
        //                               Brushes.GreenYellow);
        // MainFrame.rtb.ScrollToEnd();
        // MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n",
        //                               Brushes.Tomato);
        // MainFrame.rtb.ScrollToEnd();
        // MainFrame.rtb.AppendColorLine(" Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)\n",
        //                               Brushes.Yellow);
        // MainFrame.rtb.ScrollToEnd();

        MainFrame.rtb.AppendText(" Что касается сохранения денег от инфляции, есть единственная вещь, которая за всю историю являлась дефляционным активом - это редкоземельные металлы. Да, в слитках, у тебя под кроватью в сейфе или ячейках. Конечно, бывали и затяжные даунтренды, это нормально, но на дистанции в 5-10-15 лет и более а так же в будущем это единственное спасение от инфляции. Пузыри раздуваются и лопаются, компании взлетают и превращаются в пыль, но интерес на металлы в ближайшие несколько сот лет как минимум будет всегда.\n\n");
        MainFrame.rtb.ScrollToEnd();
        //log("Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)", Brushes.OrangeRed);
        //log("Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)");
        //log("Кстати грипп можно определить точно, не только симптоматически, но и с помощью ИФА методов, есть даже экспресс-тесты, как во время ковида. Сейчас такие системы должны быть распространены в поликлиниках (на момент написания статьи)", Brushes.GreenYellow);
    }

    private void Btn5_OnClick(object sender, RoutedEventArgs e)
    {
        //var color1 = (SolidColorBrush)Application.Current.Resources["Tittle.Border.Color"];
        //Console.WriteLine(color1);
        //ToEventLog(sender.ToString(), $"Хуйня случилась", Level.Error);
        //ToEventLog(sender.ToString(), $"Нехуйня случилась", Level.Warning);
        //ToEventLog(sender.ToString(), $"случилась", Level.Information);

        //MainFrame.rtb.Document.Blocks.Clear();

        MainFrame.pb.Dispatcher.InvokeOrExecute(() => { MainFrame.pb.progressBar.SetPercentDuration(100, 10000); });
    }

    private void Btn6_OnClick(object sender, RoutedEventArgs e)
    {
        //MainFrame.rtb.TextArea.TextView.CurrentLineBackground = Brushes.Crimson;
        RoundedProgressBarControl.RoundedProgressBarControlRounded.Dispatcher.InvokeOrExecute(RoundedProgressBarControl.Start);
    }

    private void Btn7_OnClick(object sender, RoutedEventArgs e)
    {
        RoundedProgressBarControl.RoundedProgressBarControlRounded.Dispatcher.InvokeOrExecute(RoundedProgressBarControl.Stop);
        //var result = CustomMessageBox.Show("Действительно закрыть приложение?", "Подтверждение выхода", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        //Replica.ReplicaSqlPackageStartAsync();
    }

    private void Btn8_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessTools.StartElevated("notepad", null);
    }
}
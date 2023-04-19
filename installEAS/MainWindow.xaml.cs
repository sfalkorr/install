namespace installEAS;

[SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
public partial class MainWindow
{
    public static   MainWindow      MainFrame;
    public static   DoubleAnimation MainOpen;
    public static   DoubleAnimation MainClos;
    public readonly Storyboard      textBoxClos;
    public readonly Storyboard      textBoxOpen;
    public static   string          obj = "";

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var source = PresentationSource.FromVisual(this) as HwndSource;
        source?.AddHook(WndProc);
    }

    private const int WM_ENTERSIZEMOVE = 0x0231;
    private const int WM_EXITSIZEMOVE  = 0x0232;

    private Rect _windowRect;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_ENTERSIZEMOVE:
                _windowRect = GetWindowRect(this);
                break;

            case WM_EXITSIZEMOVE:
                if (_windowRect.Size != GetWindowRect(this).Size)
                {
                    Console.WriteLine("RESIZED");
                    rtb.ScrollToEnd();
                }

                break;
        }

        return IntPtr.Zero;
    }

    private static Rect GetWindowRect(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        return GetWindowRect(handle, out var rect) ? rect : default;
    }

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(
        IntPtr   hWnd,
        out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        private readonly int left;
        private readonly int top;
        private readonly int right;
        private readonly int bottom;

        public static implicit operator Rect(RECT rect) =>
            new Rect(rect.left, rect.top, (rect.right - rect.left), (rect.bottom - rect.top));
    }

    public MainWindow()
    {
        var s = typeof(MainWindow).Assembly.GetManifestResourceStream("installEAS.CustomHighlighting.xshd");
        if (s == null) throw new InvalidOperationException("Could not find embedded resource");
        XmlReader reader             = new XmlTextReader(s);
        var       customHighlighting = HighlightingLoader.Load(reader, Instance);
        Instance.RegisterHighlighting("Custom Highlighting", new[] { ".cool" }, customHighlighting);

        InitializeComponent();

        MainFrame    =  this;
        Left         =  5;
        Top          =  5;
        StateChanged += MainWindowStateChangeRaised;
        SizeChanged  += MainWin_SizeChanged;
        CurrentTheme =  ThemeTypes.ColorGray;
        MainOpen     =  new DoubleAnimation { From = 0.1, To  = 0.97, Duration = new Duration(TimeSpan.FromMilliseconds(500)) };
        MainClos     =  new DoubleAnimation { From = 0.97, To = 0.1, Duration  = new Duration(TimeSpan.FromMilliseconds(700)) };
        textBoxOpen  =  Resources["OpenTextBox"] as Storyboard;
        textBoxClos  =  Resources["CloseTextBox"] as Storyboard;

        textBox.IsEnabled                            = false;
        waitProgress.sprocketControl.IsIndeterminate = false;
        waitProgress.IsEnabled                       = false;
        labelVer.Content                             = $"InstallEAS v{AppVersion}";

        var SelectionBorder = new Pen { Brush = new SolidColorBrush(Colors.Wheat) };
        SelectionBorder.Brush.Opacity            = 0.5;
        rtb.TextArea.Cursor                      = Cursors.Arrow;
        rtb.IsReadOnly                           = true;
        rtb.TextArea.MouseSelectionMode          = MouseSelectionMode.WholeWord;
        rtb.TextArea.Caret.CaretBrush            = Brushes.Transparent;
        rtb.TextArea.SelectionForeground         = Brushes.White;
        rtb.TextArea.SelectionCornerRadius       = 5;
        rtb.TextArea.SelectionBorder             = SelectionBorder;
        rtb.Options.InheritWordWrapIndentation   = false;
        rtb.TextArea.SelectionBrush              = new SolidColorBrush(Color.FromArgb(100, 100, 100, 150));
        rtb.Options.EnableTextDragDrop           = false;
        rtb.Options.AllowScrollBelowDocument     = false;
        rtb.Options.HighlightCurrentLine         = false;
        rtb.Options.EnableRectangularSelection   = true;
        rtb.Options.ShowBoxForControlCharacters  = false;
        rtb.TextArea.OverstrikeMode              = false;
        rtb.TextArea.Options.WordWrapIndentation = double.MaxValue;
        rtb.TextArea.Options.EnableImeSupport    = false;
        textBox.Background                       = Brushes.Red;
    }

    public enum inputType
    {
        Empty,
        AskNewSqlPassword,
        AskCurrentSqlPassword,
        AskMachinename,
        AskConfirmation
    }

    public inputType AskType = inputType.Empty;

    public bool userInput(inputType type)
    {
        AskType = type;

        switch (AskType)
        {
            case inputType.AskNewSqlPassword:
                log("\nЗадайте новый пароль для пользователя sa в SQL и подтвердите ввод клавишей Enter\nИли нажимайте клавишу F12 для генерации случайных паролей, подтвердив ввод клавишей Enter\n\n");
                MainFrame.textBoxOpen.Completed += (_, _) =>
                {
                    MainFrame.tlabel.Visibility = Visibility.Visible;
                    MainFrame.textBox.IsEnabled = true;
                    MainFrame.textBox.Focus();
                    if (MainFrame.textBox.Text == "") MainFrame.tlabel.Text = "Пароль не может быть пустым";
                };
                MainFrame.textBoxOpen.Begin(MainFrame.textBox);
                return true;

            case inputType.AskCurrentSqlPassword:
                log("\nВведите текущий пароль для пользователя sa в SQL и нажмите Enter\n\n");
                MainFrame.textBoxOpen.Completed += (_, _) =>
                {
                    MainFrame.tlabel.Visibility = Visibility.Visible;
                    MainFrame.textBox.IsEnabled = true;
                    MainFrame.textBox.Focus();
                    if (MainFrame.textBox.Text == "") MainFrame.tlabel.Text = "Пароль не может быть пустым";
                };
                MainFrame.textBoxOpen.Begin(MainFrame.textBox);
                return true;

            case inputType.AskMachinename:
                Console.WriteLine();
                return true;
            case inputType.AskConfirmation:
                Console.WriteLine();
                return true;
            case inputType.Empty:
            default:
                return false;
        }
    }

    private void TextBox_OnKeyDownKeyDown(object sender, KeyEventArgs e)
    {
        //if (e.Key != Enter || MainFrame.textBox.Text == "") return;
        //if (Regex.IsMatch(MainFrame.textBox.Text, "^[0-9A-Z!@#$%^&*()_+=?-]+$", RegexOptions.IgnoreCase))
        //{
        //    log(MainFrame.textBox.Text);
        //    tlabel.Text                 = "";
        //    MainFrame.textBox.IsEnabled = false;
        //    MainFrame.textBox.Clear();
        //    MainFrame.textBoxClos.Begin(MainFrame.textBox);
        //    sqlpass = textBox.Text;
        //}
        //if (e.Key != Enter || MainFrame.textBox.Text == "") tlabel.Text = "Пароль не может быть пустым";
        // if ((ValidatePass(textBox.Text) != "Пароль корректен" && textBox.Text != "new") || e.Key != Enter) return;
        // log(newpass);
        // tlabel.Visibility           = Visibility.Collapsed;
        // MainFrame.textBox.IsEnabled = false;
        // MainFrame.textBox.Clear();
        // MainFrame.textBoxClos.Begin(MainFrame.textBox);
        // sqlpass = textBox.Text;

        switch (AskType)
        {
            case inputType.AskNewSqlPassword when !MainFrame.textBox.IsEnabled:
                return;

            case inputType.AskNewSqlPassword:
            {
                if (Keyboard.IsKeyDown(F12))
                {
                    MainFrame.textBox.Text       = GeneratePass(3, 3, 3, 1);
                    MainFrame.textBox.CaretIndex = textBox.Text.Length;
                }

                if (ValidatePass(textBox.Text) != "Пароль корректен" || !Keyboard.IsKeyDown(Enter)) return;
                tlabel.Visibility               =  Visibility.Collapsed;
                NewSqlPass                      =  textBox.Text;
                MainFrame.textBoxClos.Completed += OnTextBoxClosOnCompleted;
                MainFrame.textBoxClos.Begin(MainFrame.textBox);
                break;
            }

            case inputType.AskCurrentSqlPassword:

                if (tlabel.Text != "Пароль принят" || !Keyboard.IsKeyDown(Enter)) return;
                tlabel.Visibility               =  Visibility.Collapsed;
                SqlPass                         =  textBox.Text;
                MainFrame.textBoxClos.Completed += OnTextBoxClosOnCompleted;
                MainFrame.textBoxClos.Begin(MainFrame.textBox);
                break;
            case inputType.AskMachinename:
                Console.WriteLine();
                break;
            case inputType.AskConfirmation:
                Console.WriteLine();
                break;
            case inputType.Empty:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnTextBoxClosOnCompleted(object o, EventArgs eventArgs)
    {
        MainFrame.textBox.IsEnabled = false;
        MainFrame.textBox.Clear();
        AskType = inputType.Empty;
    }

    private void textBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        switch (AskType)
        {
            //tlabel.Text = MainFrame.textBox.Text != "" ? !Regex.IsMatch(MainFrame.textBox.Text, "^[0-9A-Z!@#$%^&*()_+=?-]+$", RegexOptions.IgnoreCase) ? "Недопустимые символы" : "" : "";
            //tlabel.Visibility = Visibility.Visible;
            case inputType.AskNewSqlPassword when ValidatePass(textBox.Text) == "Пароль корректен":
                tlabel.Foreground = Brushes.GreenYellow;
                tlabel.Text       = ValidatePass(textBox.Text);
                break;
            case inputType.AskNewSqlPassword:
                tlabel.Foreground = Brushes.OrangeRed;
                tlabel.Text       = ValidatePass(textBox.Text);
                break;
            case inputType.AskCurrentSqlPassword when IsSqlPasswordOK(textBox.Text) && !Regex.Match(textBox.Text, "[\\s]").Success:
                tlabel.Foreground = Brushes.GreenYellow;
                tlabel.Text       = "Пароль принят";
                break;
            case inputType.AskCurrentSqlPassword:
                tlabel.Foreground = Brushes.OrangeRed;
                tlabel.Text       = "Неверный пароль";
                break;
            case inputType.AskMachinename:
                Console.WriteLine();
                break;
            case inputType.AskConfirmation:
                Console.WriteLine();
                break;
            case inputType.Empty:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void CloseMain()
    {
        SystemCommands.CloseWindow(MainFrame);
    }

    public void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
    {
        SystemCommands.CloseWindow(this);
    }

    public void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
    {
        if (textBox.IsEnabled) textBox.Focus();
        SystemCommands.MaximizeWindow(this);
    }

    public void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
    {
        if (textBox.IsEnabled) textBox.Focus();
        SystemCommands.MinimizeWindow(this);
    }

    public void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
    {
        if (textBox.IsEnabled) textBox.Focus();
        SystemCommands.RestoreWindow(this);
    }

    public static string ololo;

    public static string yolka
    {
        get =>
            // MainFrame.textBoxClos.Completed += (_, _) =>
            // {
            //     
            //     MainFrame.textBox.IsEnabled = false;
            //     MainFrame.textBox.Clear();
            // };
            // MainFrame.textBoxClos.Begin(MainFrame.textBox);
            ololo = "dfdf";
        set
        {
            MainFrame.textBoxClos.Completed += (_, _) =>
            {
                MainFrame.textBox.IsEnabled = false;
                MainFrame.textBox.Clear();
                value = MainFrame.textBox.Text;
            };
            MainFrame.textBoxClos.Begin(MainFrame.textBox);
        }
    }

    public static double length;

    public static double Penis
    {
        get => length;
        set
        {
            if (value > 20) length = value;
            else err();
        }
    }

    public static void err()
    {
        Console.WriteLine(Penis);
        log(@"Ошибко!", Brushes.Red);
    }

    private void MainWin_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.IsKeyDown(F1)) tempButtons.btn1.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(F2)) tempButtons.btn2.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(F3)) tempButtons.btn3.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(F4)) tempButtons.btn4.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(F5)) tempButtons.btn5.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(F6)) tempButtons.btn6.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(F7)) tempButtons.btn7.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(F8)) tempButtons.btn8.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        if (Keyboard.IsKeyDown(X) && Keyboard.IsKeyDown(LeftAlt)) Close();
        if (Keyboard.IsKeyDown(OemTilde) && Keyboard.IsKeyDown(LeftAlt))
            MainFrame.Dispatcher.InvokeAsync(() =>
            {
                var _ = AnimateFrameworkElement(MenuMain.PanelTopMain, 400);
            }, DispatcherPriority.Send);

        if (Keyboard.IsKeyDown(OemTilde) && Keyboard.IsKeyDown(LeftCtrl))
            switch (textBox.IsEnabled)
            {
                case false:
                    textBox.IsEnabled = true;
                    textBoxOpen.Begin(textBox);
                    textBox.Focus();
                    break;
                default:
                    textBox.IsEnabled = false;
                    textBoxClos.Begin(textBox);
                    break;
            }

        if (MenuMain.PanelTopAdd.IsEnabled)
        {
            if (Keyboard.IsKeyDown(D0) || Keyboard.IsKeyDown(NumPad0)) MenuMain.btnMenuAdd0.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            if (Keyboard.IsKeyDown(D1) || Keyboard.IsKeyDown(NumPad1)) MenuMain.btnMenuAdd1.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        if (MenuMain.PanelTopMain.IsEnabled == false) return;
        if (Keyboard.IsKeyDown(D3) || Keyboard.IsKeyDown(NumPad3)) MenuMain.btnMenu3.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(D4) || Keyboard.IsKeyDown(NumPad4)) MenuMain.btnMenu4.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

    [STAThread]
    private void MainWin_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (textBox.IsEnabled) textBox.Focus();
    }

    private void MainWindow_OnActivated(object sender, EventArgs e)
    {
        if (textBox.IsEnabled) textBox.Focus();
    }

    private void MainWindow_OnClosing(object sender, CancelEventArgs e)
    {
        //TODO Раскоментить в релизе
        e.Cancel = true;
        var result = CustomMessageBox.Show("Действительно закрыть приложение?", "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result.ToString() != "Yes") return;
        AnimateFrameworkElement(MenuMain.PanelTopMain, 500).ConfigureAwait(true);
        MainClos.Completed += (_, _) => Process.GetCurrentProcess().Kill();
        BeginAnimation(OpacityProperty, MainClos);
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        CreateVariablesInstance();
        CreateSlidePanelsInstance();
        CreatetempControlInstance();

        //TODO раскоментить в релизе
        MainOpen.Completed += async (_, _) =>
        {
            //log("Инициализация...", false);

            await Task.Delay(200).ConfigureAwait(true);
            //log("OK");
            await AnimateFrameworkElement(MenuMain.PanelTopMain, 500).ConfigureAwait(true);
        };
        BeginAnimation(OpacityProperty, MainOpen);
    }

    private void MainWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (textBox.IsEnabled) textBox.Focus();
    }

    [STAThread]
    private void MainWindowStateChangeRaised(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            MainWindowBorder.BorderThickness = new Thickness(7);
            RestoreButton.Visibility         = Visibility.Visible;
            MaximizeButton.Visibility        = Visibility.Collapsed;
        }
        else
        {
            MainWindowBorder.BorderThickness = new Thickness(0);
            RestoreButton.Visibility         = Visibility.Collapsed;
            MaximizeButton.Visibility        = Visibility.Visible;
        }
    }

    public static string InputText { get; set; }

    private void LabelVer_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ChangeTheme();
        if (textBox.IsEnabled) textBox.Focus();
    }

    public static bool IsEmpty;

    private void rtb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pt                  = e.GetPosition((UIElement)sender);
        var result              = VisualTreeHelper.HitTest(this, pt);
        if (result != null) obj = result.VisualHit.ToString();

        try
        {
            var PosCol       = rtb.GetPositionFromPoint(e.GetPosition(rtb)).Value.Column;
            var PosVisualCol = rtb.GetPositionFromPoint(e.GetPosition(rtb)).Value.VisualColumn;

            if (PosCol == 1 && PosVisualCol == 0) IsEmpty = true;
        }
        catch (Exception)
        {
            IsEmpty = true;
        }

        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (obj != "AvalonEdit.Rendering.TextView" || !IsEmpty) return;
        DragMove();
        IsEmpty = false;

        if (textBox.IsEnabled) textBox.Focus();
    }

    private void rtb_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var Sel = rtb.SelectedText;
        if (e.LeftButton == MouseButtonState.Released && Sel != "") rtb.ToClipSelection();
        if (textBox.IsEnabled) textBox.Focus();
    }
 
}
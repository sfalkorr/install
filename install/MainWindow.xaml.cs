using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;
using installEAS.Themes;
using installEAS.Controls;
using static installEAS.Helpers.Log;
using static installEAS.Helpers.Animate;
using static installEAS.Variables;

namespace installEAS;

public partial class MainWindow
{
    public static   MainWindow      MainFrame;
    public readonly DoubleAnimation MainOpen;
    public readonly DoubleAnimation MainClos;

    private          Timer      _timer;
    private readonly Storyboard textBoxClos;
    private readonly Storyboard textBoxOpen;
    //private readonly Storyboard shake;

    public MainWindow()
    {
        InitializeComponent();
        MainFrame                     =  this;
        Left                          =  5;
        Top                           =  5;
        StateChanged                  += MainWindowStateChangeRaised;
        SizeChanged                   += MainWin_SizeChanged;
        ThemesController.CurrentTheme =  ThemesController.ThemeTypes.ColorDark;
        switch (ThemesController.CurrentTheme)
        {
            case ThemesController.ThemeTypes.ColorDark:
            {
                controlFrom = "#FF242A31";
                controlTo   = "#00242A31";
                closeFrom   = "#FF902020";
                closeTo     = "#00202020";
                break;
            }
            case ThemesController.ThemeTypes.ColorBlue:
            {
                controlFrom = "#FF32506E";
                controlTo   = "#0032506E";
                closeFrom   = "#FF902020";
                closeTo     = "#0032506E";
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        MainOpen    = new DoubleAnimation { From = 0.1, To  = 0.98, Duration = new Duration(TimeSpan.FromMilliseconds(400)) };
        MainClos    = new DoubleAnimation { From = 0.97, To = 0.1, Duration  = new Duration(TimeSpan.FromMilliseconds(400)) };
        textBoxOpen = Resources["OpenTextBox"] as Storyboard;
        textBoxClos = Resources["CloseTextBox"] as Storyboard;
        //shake       = Resources[ "ShakeWindow" ] as Storyboard;

        textBox.IsEnabled                            = false;
        waitProgress.sprocketControl.IsIndeterminate = false;
        waitProgress.IsEnabled                       = false;

        labelVer.Content = $"InstallEAS v{AppVersion}";
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

    [STAThread]
    public void ToClip()
    {
        var text = rtb.Selection.Text;
        Clipboard.SetText(text);
        rtb.Selection.Select(rtb.CaretPosition, rtb.CaretPosition);
        if (textBox.IsEnabled) textBox.Focus();
    }


    public static string ololo;

    public static string yolka
    {
        get => ololo;
        set
        {
            if (value != "хуй") ololo = value;
            else log(@"Сам ты хуй!", Brushes.Red);
        }
    }

    // set => length = value > 20 ? value : throw new ValidationError. ;
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
        if (Keyboard.IsKeyDown(Key.X) && Keyboard.IsKeyDown(Key.LeftAlt)) Close();
        if (Keyboard.IsKeyDown(Key.OemTilde) && Keyboard.IsKeyDown(Key.LeftAlt))
            MainFrame.Dispatcher.InvokeAsync(() =>
            {
                var unused = AnimateFrameworkElement(MenuMain.PanelTopMain, 400);
            }, DispatcherPriority.Send);

        if (Keyboard.IsKeyDown(Key.OemTilde) && Keyboard.IsKeyDown(Key.LeftCtrl))
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
            if (Keyboard.IsKeyDown(Key.D0) || Keyboard.IsKeyDown(Key.NumPad0)) MenuMain.btnMenuAdd0.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            if (Keyboard.IsKeyDown(Key.D1) || Keyboard.IsKeyDown(Key.NumPad1)) MenuMain.btnMenuAdd1.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        if (MenuMain.PanelTopMain.IsEnabled == false) return;
        if (Keyboard.IsKeyDown(Key.D3) || Keyboard.IsKeyDown(Key.NumPad3)) MenuMain.btnMenu3.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(Key.D4) || Keyboard.IsKeyDown(Key.NumPad4)) MenuMain.btnMenu4.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

    [STAThread]
    private void MainWin_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        rtb.Document.PageWidth = 10000;
        if (_timer != null)
        {
            _timer.Dispose();
            _timer = null;
        }

        _timer = new Timer(_ =>
        {
            Dispatcher.InvokeOrExecute(() =>
            {
                rtb.Document.PageWidth = double.NaN;
                rtb.ScrollToEnd();
            }, DispatcherPriority.Send);
        }, null, 200, Timeout.Infinite);
    }

    private void MainWindow_OnActivated(object sender, EventArgs e)
    {
        if (textBox.IsEnabled) textBox.Focus();
    }

    private void MainWindow_OnClosing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        var result = CustomMessageBox.Show("Действительно закрыть приложение?", "Подтверждение выхода", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (result.ToString() != "OK") return;
        MainClos.Completed += (_, _) => Process.GetCurrentProcess().Kill();
        BeginAnimation(OpacityProperty, MainClos);
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        CreateVariablesInstance();
        UIControlSlidePanels.CreateSlidePanelsInstance();

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


    [STAThread]
    private new void MouseLeave(object sender, MouseEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var element = (FrameworkElement)sender;
            ColorAnimation(element.Name == "CloseButton" ? new InClassName(element, closeFrom, closeTo, 120) : new InClassName(element, controlFrom, controlTo, 120));
        }, DispatcherPriority.Background);
    }


    [STAThread]
    private void Rtb_OnMouseMove(object sender, MouseEventArgs e)
    {
        var PosCursor = rtb.CaretPosition.GetTextInRun(LogicalDirection.Forward);
        switch (e.LeftButton)
        {
            case MouseButtonState.Released when !rtb.Selection.IsEmpty:
                try
                {
                    ToClip();
                }
                catch (Exception ex)
                {
                    log(ex.Message);
                }

                break;
            case MouseButtonState.Pressed when PosCursor == "":
                DragMove();
                if (e.LeftButton != MouseButtonState.Released) return;
                var startPos = rtb.Document.ContentStart.GetPositionAtOffset(0);
                var endPos   = rtb.Document.ContentStart.GetPositionAtOffset(0);
                if (startPos != null)
                    if (endPos != null)
                        rtb.Selection.Select(startPos, endPos);
                if (textBox.IsEnabled) textBox.Focus();
                rtb.ScrollToEnd();
                break;
        }
    }

    private void TextBox_OnKeyDownKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        textBoxClos.Begin(textBox);

        textBox.Focus();
        if (textBox.Text != "") log(textBox.Text);
        textBox.IsEnabled = false;
        textBox.Clear();
    }


    private void LabelVer_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ThemesController.ChangeTheme();
    }
}
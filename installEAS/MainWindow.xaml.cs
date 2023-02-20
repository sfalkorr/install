using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AvalonEdit;
using AvalonEdit.Editing;
using AvalonEdit.Utils;
using AvalonEdit.Highlighting;
using AvalonEdit.Highlighting.Xshd;
using AvalonEdit.Rendering;
using AvalonEdit.Document;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;
using installEAS.Themes;
using installEAS.Controls;
using static installEAS.Themes.ThemesController;
using static installEAS.Helpers.Log;
using static installEAS.Helpers.Animate;
using static installEAS.Variables;
using static installEAS.Controls.SlidePanelsControl;
using static installEAS.Controls.tempControl;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Xml;
using static AvalonEdit.Highlighting.HighlightingManager;

namespace installEAS;

public partial class MainWindow
{
    public static    MainWindow      MainFrame;
    public static    DoubleAnimation MainOpen;
    public static    DoubleAnimation MainClos;
    private readonly Storyboard      textBoxClos;
    private readonly Storyboard      textBoxOpen;
    public static    string          obj = "";


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
        CurrentTheme =  ThemeTypes.ColorBlue;
        switch (CurrentTheme)
        {
            case ThemeTypes.ColorDark:
            {
                controlFrom = "#FF242A31";
                controlTo   = "#00242A31";
                closeFrom   = "#FF902020";
                closeTo     = "#00902020";
                break;
            }
            case ThemeTypes.ColorBlue:
            {
                controlFrom = "#FF32506E";
                controlTo   = "#0032506E";
                closeFrom   = "#FF902020";
                closeTo     = "#00902020";
                break;
            }
            case ThemeTypes.ColorGray:
            {
                controlFrom = "#FF50565D";
                controlTo   = "#0050565D";
                closeFrom   = "#FF902020";
                closeTo     = "#00902020";
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        MainOpen    = new DoubleAnimation { From = 0.1, To  = 0.97, Duration = new Duration(TimeSpan.FromMilliseconds(500)) };
        MainClos    = new DoubleAnimation { From = 0.97, To = 0.1, Duration  = new Duration(TimeSpan.FromMilliseconds(500)) };
        textBoxOpen = Resources["OpenTextBox"] as Storyboard;
        textBoxClos = Resources["CloseTextBox"] as Storyboard;

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
        get => ololo;
        set
        {
            if (value != "хуй") ololo = value;
            else log(@"Сам ты хуй!", Brushes.Red);
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
        if (Keyboard.IsKeyDown(Key.F1)) tempButtons.btn1.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(Key.F2)) tempButtons.btn2.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(Key.F3)) tempButtons.btn3.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(Key.F4)) tempButtons.btn4.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(Key.F5)) tempButtons.btn5.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(Key.F6)) tempButtons.btn6.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(Key.F7)) tempButtons.btn7.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (Keyboard.IsKeyDown(Key.F8)) tempButtons.btn8.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        if (Keyboard.IsKeyDown(Key.X) && Keyboard.IsKeyDown(Key.LeftAlt)) Close();
        if (Keyboard.IsKeyDown(Key.OemTilde) && Keyboard.IsKeyDown(Key.LeftAlt))
            MainFrame.Dispatcher.InvokeAsync(() =>
            {
                var _ = AnimateFrameworkElement(MenuMain.PanelTopAdd, 400);
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
        if (textBox.IsEnabled) textBox.Focus();
    }

    private void MainWindow_OnActivated(object sender, EventArgs e)
    {
        if (textBox.IsEnabled) textBox.Focus();
    }

    private void MainWindow_OnClosing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        var result = CustomMessageBox.Show("Действительно закрыть приложение?", "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result.ToString() != "Yes") return;
        MainClos.Completed += (_, _) => Process.GetCurrentProcess().Kill();
        BeginAnimation(OpacityProperty, MainClos);
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        CreateVariablesInstance();
        CreateSlidePanelsInstance();
        CreatetempControlInstance();
        MainOpen.Completed += async (_, _) =>
        {
            log("Инициализация...", false);

            await Task.Delay(1000).ConfigureAwait(true);
            log("OK");
            await AnimateFrameworkElement(MenuMain.PanelTopMain, 600).ConfigureAwait(true);
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

    private new void MouseLeave(object sender, MouseEventArgs e)
    {
        var element = (FrameworkElement)sender;
        ColorAnimation(element.Name == "CloseButton" ? new InClassName(element, closeFrom, closeTo, 120) : new InClassName(element, controlFrom, controlTo, 120));
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
        ChangeTheme();
        if (textBox.IsEnabled) textBox.Focus();
    }


    public static bool IsEmpty;

    [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
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
        catch (Exception) { IsEmpty = true; }

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
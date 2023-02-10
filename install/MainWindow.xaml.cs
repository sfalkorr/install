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
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;
using installEAS.Themes;
using installEAS.Controls;
using static installEAS.Helpers.Log;
using static installEAS.Helpers.Animate;
using static installEAS.Variables;
using System.Windows.Controls;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Indentation.CSharp;

namespace installEAS;

public partial class MainWindow
{
    public static MainWindow MainFrame;

    public static DoubleAnimation MainOpen;
    public static DoubleAnimation MainClos;

    //private          Timer      _timer;
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
        ThemesController.CurrentTheme =  ThemesController.ThemeTypes.ColorGray;
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
            case ThemesController.ThemeTypes.ColorGray:
            {
                controlFrom = "#FF50565D";
                controlTo   = "#0050565D";
                closeFrom   = "#FF902020";
                closeTo     = "#00202020";
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        MainOpen    = new DoubleAnimation { From = 0.1, To  = 0.97, Duration = new Duration(TimeSpan.FromMilliseconds(400)) };
        MainClos    = new DoubleAnimation { From = 0.97, To = 0.1, Duration  = new Duration(TimeSpan.FromMilliseconds(400)) };
        textBoxOpen = Resources["OpenTextBox"] as Storyboard;
        textBoxClos = Resources["CloseTextBox"] as Storyboard;

        textBox.IsEnabled                            = false;
        waitProgress.sprocketControl.IsIndeterminate = false;
        waitProgress.IsEnabled                       = false;
        labelVer.Content                             = $"InstallEAS v{AppVersion}";

        var SelectionBorder = new Pen();
        rtb.TextArea.Cursor                     = Cursors.Arrow;
        rtb.IsReadOnly                          = true;
        rtb.TextArea.MouseSelectionMode         = MouseSelectionMode.Drag;
        rtb.TextArea.Caret.CaretBrush           = Brushes.Transparent;
        rtb.TextArea.SelectionCornerRadius      = 1;
        rtb.TextArea.SelectionBorder            = SelectionBorder;
        rtb.Options.InheritWordWrapIndentation  = false;
        rtb.WordWrap                            = true;
        rtb.TextArea.SelectionBrush             = new SolidColorBrush(Color.FromArgb(200, 100, 100, 100));
        rtb.Options.EnableTextDragDrop          = false;
        rtb.Options.AllowScrollBelowDocument    = false;
        rtb.Options.HighlightCurrentLine        = false;
        rtb.Options.EnableHyperlinks            = false;
        rtb.Options.EnableRectangularSelection  = true;
        rtb.Options.EnableEmailHyperlinks       = false;
        rtb.Options.ShowBoxForControlCharacters = false;
        rtb.TextArea.OverstrikeMode             = false;

        //rtb.TextArea.DefaultInputHandler.NestedInputHandlers.Remove(rtb.TextArea.DefaultInputHandler.CaretNavigation );
        //rtb.IsHitTestVisible = false;
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
        //rtb.Document.PageWidth = 10000;
        //if (_timer != null)
        //{
        //    _timer.Dispose();
        //    _timer = null;
        //    //sv.Focus();
        //    //sv.ScrollToEnd();
        //    if (textBox.IsEnabled) textBox.Focus();
        //}

        //_timer = new Timer(_ =>
        //{
        //    Dispatcher.InvokeOrExecute(() =>
        //    {
        //        rtb.Document.PageWidth = double.NaN;
        //        rtb.ScrollToEnd();
        //        //sv.Focus();
        //        if (textBox.IsEnabled) textBox.Focus();
        //    }, DispatcherPriority.Send);
        //}, null, 200, Timeout.Infinite);
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
        SlidePanelsControl.CreateSlidePanelsInstance();
        tempControl.CreatetempControlInstance();
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
        var element = (FrameworkElement)sender;
        ColorAnimation(element.Name == "CloseButton" ? new InClassName(element, closeFrom, closeTo, 100) : new InClassName(element, controlFrom, controlTo, 100));
    }


    //private void Rtb_OnMouseMove(object sender, MouseEventArgs e)
    //{
    //var PosCursor = rtb.CaretPosition.GetTextInRun(LogicalDirection.Forward);
    //if (e.LeftButton == MouseButtonState.Pressed) MainFrame.DragMove();
    //case MouseButtonState.Released when !rtb.Selection.IsEmpty:
    //    try
    //    {
    //        ToClip();
    //        //sv.Focus();
    //        if (textBox.IsEnabled) textBox.Focus();
    //    }
    //    catch (Exception ex)
    //    {
    //        log(ex.Message);
    //    }
    //    break;
    //case MouseButtonState.Pressed when PosCursor == "":

    //    if (e.LeftButton != MouseButtonState.Released) return;
    //    //sv.Focus();
    //    if (textBox.IsEnabled) textBox.Focus();
    //        //var startPos = rtb.Document.ContentStart.GetPositionAtOffset(0);
    //        //var endPos   = rtb.Document.ContentStart.GetPositionAtOffset(0);
    //    //if (startPos != null)
    //        //if (endPos != null)
    //            //rtb.Selection.Select(startPos, endPos);
    //    if (textBox.IsEnabled) textBox.Focus();
    //    rtb.ScrollToEnd();
    //    break;
    //}

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


    public static bool IsEmpty;

    private void rtb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pt                  = e.GetPosition((UIElement)sender);
        var result              = VisualTreeHelper.HitTest(this, pt);
        if (result != null) obj = result.VisualHit.ToString();
        try
        {
            var pos          = rtb.GetPositionFromPoint(e.GetPosition(rtb));
            var PosCol       = pos.Value.Column;
            var PosVisualCol = pos.Value.VisualColumn;

            if (PosCol == 1 && PosVisualCol == 0) IsEmpty = true;
        }
        catch (Exception)
        {
            IsEmpty = true;
        }

        if (e.LeftButton != MouseButtonState.Pressed || obj != "ICSharpCode.AvalonEdit.Rendering.TextView" || !IsEmpty) return;
        DragMove();
        IsEmpty = false;
    }

    [STAThread]
    public void ToClip()
    {
        var text = rtb.SelectedText;
        Clipboard.SetText(text);
        rtb.TextArea.ClearSelection();
        //rtb.Selection.Select(rtb.CaretPosition, rtb.CaretPosition);
        //if (textBox.IsEnabled) textBox.Focus();
    }


    public static string obj = "";

    private void rtb_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        //var  pt = e.GetPosition( (UIElement)sender );
        //var result    = VisualTreeHelper.HitTest( this, pt );
        //if (result != null) obj = result.VisualHit.ToString();
        //var PosCol       = rtb.TextArea.Caret.Position.Column;
        //var PosVisualCol = rtb.TextArea.Caret.Position.VisualColumn;
        //var Sel          = rtb.SelectedText;

        //if (e.LeftButton == MouseButtonState.Pressed)
        //{
        //DragMove();
        //rtb.TextArea.MouseSelectionMode = MouseSelectionMode.None;

        //}

        //if (e.LeftButton == MouseButtonState.Pressed && PosCol == 1 && PosVisualCol == 0 && Sel == "" && obj == "ICSharpCode.AvalonEdit.Rendering.TextView")
        //Console.WriteLine( $"{PosCol} {PosVisualCol} {obj}" );
        //{

        //    this.DragMove();
        //    //rtb.LineDown();
        //    //rtb.TextArea.ClearSelection();

        //}
        //if (e.LeftButton == MouseButtonState.Released && Sel != "")
        //    ToClip();
        //Console.WriteLine( $"{PosCol} {PosVisualCol} {obj}");
    }

    private void rtb_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var Sel = rtb.SelectedText;
        if (e.LeftButton == MouseButtonState.Released && Sel != "") ToClip();
    }

    private void rtb_MouseHover(object sender, MouseEventArgs e)
    {
        //var  pt = e.GetPosition( (UIElement)sender );
        //var result    = VisualTreeHelper.HitTest( this, pt );
        //if (result != null) obj = result.VisualHit.ToString();
        //var PosCol       = rtb.TextArea.Caret.Position.Column;
        //var PosVisualCol = rtb.TextArea.Caret.Position.VisualColumn;
        //var Sel          = rtb.SelectedText;

        //if (result != null) obj = result.VisualHit.ToString();
        //var pos = rtb.GetPositionFromPoint( e.GetPosition( rtb ) );

        //if (e.LeftButton == MouseButtonState.Pressed) // && PosCol == 1 && PosVisualCol == 0 && Sel == "" && obj == "ICSharpCode.AvalonEdit.Rendering.TextView")
        //{
        //    //Console.WriteLine( $"{PosCol} {PosVisualCol} {obj}" );
        //    MainFrame.DragMove();
        //}

        //Console.WriteLine($"{pos} {obj}");
    }
}
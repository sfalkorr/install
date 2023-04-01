namespace installEAS.MessageBoxCustom;

internal partial class CustomMessageBoxWindow
{
    public static CustomMessageBoxWindow CustomMainFrame;

    internal string Caption
    {
        get => Title;
        set => Title = value;
    }

    internal string Message
    {
        get => TextBlock_Message.Text;
        set => TextBlock_Message.Text = value;
    }

    internal string OkButtonText
    {
        get => Button_OK.Content.ToString();
        set => Button_OK.Content = value.TryAddKeyboardAccellerator();
    }

    internal string CancelButtonText
    {
        get => Button_Cancel.Content.ToString();
        set => Button_Cancel.Content = value.TryAddKeyboardAccellerator();
    }

    internal string YesButtonText
    {
        get => Button_Yes.Content.ToString();
        set => Button_Yes.Content = value.TryAddKeyboardAccellerator();
    }

    internal string NoButtonText
    {
        get => Button_No.Content.ToString();
        set => Button_No.Content = value.TryAddKeyboardAccellerator();
    }

    public MessageBoxResult Result { get; set; }

    public static DoubleAnimation Open;
    public static DoubleAnimation Clos;

    internal CustomMessageBoxWindow(string message, string caption, MessageBoxButton button, MessageBoxImage image)
    {
        InitializeComponent();

        Open = new DoubleAnimation { From = 0.1, To = 0.97, Duration = new Duration(TimeSpan.FromMilliseconds(200)) };
        Clos = new DoubleAnimation { From = 0.97, To = 0.1, Duration = new Duration(TimeSpan.FromMilliseconds(200)) };

        CustomMainFrame = this;
        Message = message;
        Caption = caption;
        Image_MessageBox.Visibility = Visibility.Collapsed;
        DisplayImage(image);
        DisplayButtons(button);
    }

    private void DisplayButtons(MessageBoxButton button)
    {
        switch (button)
        {
            case MessageBoxButton.OKCancel:
                Button_OK.Visibility = Visibility.Visible;
                Button_OK.Focus();
                Button_Cancel.Visibility = Visibility.Visible;
                Button_Yes.Visibility = Visibility.Collapsed;
                Button_No.Visibility = Visibility.Collapsed;
                break;

            case MessageBoxButton.YesNo:
                Button_Yes.Visibility = Visibility.Visible;
                Button_Yes.Focus();
                Button_No.Visibility = Visibility.Visible;
                Button_OK.Visibility = Visibility.Collapsed;
                Button_Cancel.Visibility = Visibility.Collapsed;
                break;

            case MessageBoxButton.YesNoCancel:
                Button_Yes.Visibility = Visibility.Visible;
                Button_Yes.Focus();
                Button_No.Visibility = Visibility.Visible;
                Button_Cancel.Visibility = Visibility.Visible;
                Button_OK.Visibility = Visibility.Collapsed;
                break;

            default:
                Button_OK.Visibility = Visibility.Visible;
                Button_OK.Focus();
                Button_Yes.Visibility = Visibility.Collapsed;
                Button_No.Visibility = Visibility.Collapsed;
                Button_Cancel.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void DisplayImage(MessageBoxImage image)
    {
        Icon icon;

        switch (image)
        {
            case MessageBoxImage.None:
                return;

            case MessageBoxImage.Exclamation: // Enumeration value 48 - also covers "Warning"
                icon = SystemIcons.Exclamation;
                break;

            case MessageBoxImage.Error: // Enumeration value 16, also covers "Hand" and "Stop"
                icon = SystemIcons.Hand;
                break;

            case MessageBoxImage.Information: // Enumeration value 64 - also covers "Asterisk"
                icon = SystemIcons.Information;
                break;

            case MessageBoxImage.Question:
                icon = SystemIcons.Question;
                break;

            default:
                icon = SystemIcons.Information;
                break;
        }

        Image_MessageBox.Source = icon.ToImageSource();
        Image_MessageBox.Visibility = Visibility.Visible;
    }

    private void Button_OK_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.OK;
        Clos.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, Clos);
    }

    private void Button_Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Cancel;
        Clos.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, Clos);
    }

    private void Button_Yes_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        Clos.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, Clos);
    }

    private void Button_No_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        Clos.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, Clos);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CustomMessageBoxWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        BeginAnimation(OpacityProperty, Open);
    }
}

internal class MessageBoxData
{
    public string Message { get; set; } = "";
    public string Caption { get; set; } = "Message";
    public string YesButtonCaption { get; set; }
    public string NoButtonCaption { get; set; }
    public string CancelButtonCaption { get; set; }
    public string OkButtonCaption { get; set; }
    public MessageBoxResult Result { get; set; } = MessageBoxResult.None;
    public MessageBoxButton Buttons { get; set; } = MessageBoxButton.OK;
    public MessageBoxImage Image { get; set; } = MessageBoxImage.None;

    public MessageBoxResult ShowMessageBox()
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            ShowMessageBoxSTA();
        }
        else
        {
            var thread = new Thread(ShowMessageBoxSTA);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        return Result;
    }

    private void ShowMessageBoxSTA()
    {
        var msg = new CustomMessageBoxWindow(Message, Caption, Buttons, Image) { CaptionTextBlock = { Text = Caption } };
        msg.YesButtonText = YesButtonCaption ?? msg.YesButtonText;
        msg.NoButtonText = NoButtonCaption ?? msg.NoButtonText;
        msg.CancelButtonText = CancelButtonCaption ?? msg.CancelButtonText;
        msg.OkButtonText = OkButtonCaption ?? msg.OkButtonText;
        msg.WindowStartupLocation = Application.Current.MainWindow?.Visibility == Visibility.Visible ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
        msg.Owner = Application.Current.MainWindow?.Visibility == Visibility.Visible ? Application.Current.MainWindow : null;
        msg.ShowDialog();
        Result = msg.Result;
    }
}

internal static class Util
{
    internal static ImageSource ToImageSource(this Icon icon)
    {
        ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        return imageSource;
    }

    // ! Тут настраиваются хоткеи на кнопоськи в сочетании с alt

    internal static string TryAddKeyboardAccellerator(this string input)
    {
        const string accellerator = ""; //! This is the default WPF accellerator symbol - used to be & in WinForms
        if (input.Contains(accellerator)) return input;
        return accellerator + input;
    }
}

public static class CustomMessageBox
{
    public static MessageBoxResult Show(string messageBoxText)
    {
        var msgData = new MessageBoxData { Message = messageBoxText };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult Show(string messageBoxText, string caption)
    {
        var msgData = new MessageBoxData { Message = messageBoxText, Caption = caption };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult Show(Window owner, string messageBoxText)
    {
        var msgData = new MessageBoxData { Message = messageBoxText };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult Show(Window owner, string messageBoxText, string caption)
    {
        var msgData = new MessageBoxData { Message = messageBoxText, Caption = caption };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
    {
        var msgData = new MessageBoxData { Message = messageBoxText, Caption = caption, Buttons = button };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        var msgData = new MessageBoxData { Message = messageBoxText, Caption = caption, Buttons = button, Image = icon };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult ShowOK(string messageBoxText, string caption, string okButtonText)
    {
        var msgData = new MessageBoxData { Message = messageBoxText, Caption = caption, Buttons = MessageBoxButton.YesNoCancel, OkButtonCaption = okButtonText };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult ShowOK(string messageBoxText, string caption, string okButtonText, MessageBoxImage icon)
    {
        var msgData = new MessageBoxData
        {
            Message = messageBoxText,
            Caption = caption,
            Buttons = MessageBoxButton.OK,
            Image = icon,
            OkButtonCaption = okButtonText
        };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult ShowOKCancel(string messageBoxText, string caption, string okButtonText, string cancelButtonText)
    {
        var msgData = new MessageBoxData
        {
            Message = messageBoxText,
            Caption = caption,
            Buttons = MessageBoxButton.OKCancel,
            OkButtonCaption = okButtonText,
            CancelButtonCaption = cancelButtonText
        };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult ShowOKCancel(string messageBoxText, string caption, string okButtonText, string cancelButtonText, MessageBoxImage icon)
    {
        var msgData = new MessageBoxData
        {
            Message = messageBoxText,
            Caption = caption,
            Buttons = MessageBoxButton.OKCancel,
            Image = icon,
            OkButtonCaption = okButtonText,
            CancelButtonCaption = cancelButtonText
        };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult ShowYesNo(string messageBoxText, string caption, string yesButtonText, string noButtonText)
    {
        var msgData = new MessageBoxData
        {
            Message = messageBoxText,
            Caption = caption,
            Buttons = MessageBoxButton.YesNo,
            YesButtonCaption = yesButtonText,
            NoButtonCaption = noButtonText
        };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult ShowYesNo(string messageBoxText, string caption, string yesButtonText, string noButtonText, MessageBoxImage icon)
    {
        var msgData = new MessageBoxData
        {
            Message = messageBoxText,
            Caption = caption,
            Buttons = MessageBoxButton.YesNo,
            Image = icon,
            YesButtonCaption = yesButtonText,
            NoButtonCaption = noButtonText
        };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult ShowYesNoCancel(string messageBoxText, string caption, string yesButtonText, string noButtonText, string cancelButtonText)
    {
        var msgData = new MessageBoxData
        {
            Message = messageBoxText,
            Caption = caption,
            Buttons = MessageBoxButton.YesNoCancel,
            YesButtonCaption = yesButtonText,
            NoButtonCaption = noButtonText,
            CancelButtonCaption = cancelButtonText
        };
        return msgData.ShowMessageBox();
    }

    public static MessageBoxResult ShowYesNoCancel(string messageBoxText, string caption, string yesButtonText, string noButtonText, string cancelButtonText, MessageBoxImage icon)
    {
        var msgData = new MessageBoxData
        {
            Message = messageBoxText,
            Caption = caption,
            Buttons = MessageBoxButton.YesNoCancel,
            Image = icon,
            YesButtonCaption = yesButtonText,
            NoButtonCaption = noButtonText,
            CancelButtonCaption = cancelButtonText
        };

        return msgData.ShowMessageBox();
    }
}
namespace installEAS.Controls;

public partial class SlidePanelsControl
{
    public static void CreateSlidePanelsInstance()
    {
        var variablesInstance = CreateSlidePanelsInstance;
        Console.WriteLine(variablesInstance.Method);
    }

    public SlidePanelsControl()
    {
        InitializeComponent();
        PanelTopMain.IsEnabled = false;
        PanelTopAdd.IsEnabled  = false;
    }

    private void btnMainMenu1_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopMain, 400);
        }, DispatcherPriority.Send);
    }

    private void BtnMainMenu2_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void btnMainMenu3_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopMain, 300);
        }, DispatcherPriority.Background);
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopAdd, 300);
        }, DispatcherPriority.Send);
    }

    private void BtnMainMenu4_OnClick(object sender, RoutedEventArgs e)
    {
        CloseMain();
    }

    private void BtnMenuAdd0_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopAdd, 300);
        }, DispatcherPriority.Background);
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopMain, 300);
        }, DispatcherPriority.Send);
    }

    private void btnLabel2_Click(object sender, RoutedEventArgs e)
    {
        CustomMessageBox.ShowOK("InstallEASApp версия 7.0", "О программе", "OK", MessageBoxImage.Information);
        if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
        else MainFrame.Focus();
    }

    private void btnLabel1_Click(object sender, RoutedEventArgs e)
    {
        ChangeTheme();
        if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
        else MainFrame.Focus();
    }

    private void PanelTopLabel_OnMouseLeave(object sender, MouseEventArgs e)
    {
        MainFrame.Dispatcher.InvokeAsync(() =>
        {
            var _ = AnimateFrameworkElement(MenuMain.PanelTopLabel, 400);
        }, DispatcherPriority.Send);
    }
}
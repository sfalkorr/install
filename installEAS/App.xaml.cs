namespace installEAS;

// Пример защиты wpf приложения от множественного запуска. При запуске нового экземпляра проверяется, что такое приложение уже запущено, получает хэндл запущенного окна по его имени и выводит его на фронт, убивая новый экземпляр
// Для консоли не подойдет, там нужно брать ID процесса а не хэндл окна

public partial class App
{
    [DllImport("User32")]
    private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("USER32.DLL")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static Mutex Singleton { get; } = new(true, "installEAS"); // имя приложения

    protected override void OnStartup(StartupEventArgs e)
    {
        if (Singleton.WaitOne(TimeSpan.FromMilliseconds(0), true)) return;
        var handle = FindWindow(null, "installEASApp"); // имя окна
        if (handle == IntPtr.Zero) return;

        SetForegroundWindow(handle);
        ShowWindow(handle, 9);
        Process.GetCurrentProcess().Kill();
    }
}
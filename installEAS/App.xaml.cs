namespace installEAS;

public partial class App
{
    //[STAThread]
    [DllImport("User32")]
    private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("USER32.DLL")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static Mutex Singleton { get; } = new(true, "installEAS");

    protected override void OnStartup(StartupEventArgs e)
    {
        if (Singleton.WaitOne(TimeSpan.FromMilliseconds(0), true)) return;
        var handle = FindWindow(null, "installEASApp");
        if (handle == IntPtr.Zero) return;

        SetForegroundWindow(handle);
        ShowWindow(handle, 9);
        Process.GetCurrentProcess().Kill();
    }
}
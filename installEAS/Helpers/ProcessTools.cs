#pragma warning disable CS0649

namespace installEAS.Helpers;

public static class ProcessTools
{
    public static int TokenDuplicate { get; } = 2;
    public static uint MaximumAllowed { get; } = 33554432;
    public static int CreateNewConsole { get; } = 16;
    public static int IdlePriorityClass { get; } = 64;
    public static int NormalPriorityClass { get; } = 32;
    public static int HighPriorityClass { get; } = 128;
    public static int RealtimePriorityClass { get; } = 256;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hSnapshot);

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("advapi32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll")]
    private static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

    [DllImport("advapi32.dll")]
    private static extern bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType, int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [SuppressUnmanagedCodeSecurity]
    [DllImport("advapi32", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

    private static bool StartProcessAsUser(ImpersonalizationSource IS, string CommandLine, out PROCESS_INFORMATION procInfo)
    {
        var zero1 = IntPtr.Zero;
        var zero2 = IntPtr.Zero;
        var zero3 = IntPtr.Zero;
        procInfo = new PROCESS_INFORMATION();
        var consoleSessionId = WTSGetActiveConsoleSessionId();
        var processName = "";
        uint dwProcessId = 0;
        switch (IS)
        {
            case ImpersonalizationSource.Winlogon:
                processName = "winlogon";
                break;
            case ImpersonalizationSource.Explorer:
                processName = "explorer";
                break;
            default:
                dwProcessId = (uint)Process.GetCurrentProcess().Id;
                break;
        }

        if (IS != ImpersonalizationSource.CurrentProcess)
            foreach (var process in Process.GetProcessesByName(processName))
                if (process.SessionId == (int)consoleSessionId)
                    dwProcessId = (uint)process.Id;

        var num = OpenProcess(33554432U, false, dwProcessId);
        if (!OpenProcessToken(num, 2, ref zero2))
        {
            CloseHandle(num);
            return false;
        }

        var structure = new SECURITY_ATTRIBUTES();
        structure.Length = Marshal.SizeOf((object)structure);
        if (!DuplicateTokenEx(zero2, 33554432U, ref structure, 1, 1, ref zero1))
        {
            CloseHandle(num);
            CloseHandle(zero2);
            return false;
        }

        var lpStartupInfo = new STARTUPINFO();
        lpStartupInfo.cb = Marshal.SizeOf((object)lpStartupInfo);
        lpStartupInfo.lpDesktop = "winsta0\\default";
        const int dwCreationFlags = 48;
        var       processAsUser   = CreateProcessAsUser(zero1, null, CommandLine, ref structure, ref structure, false, dwCreationFlags, IntPtr.Zero, null, ref lpStartupInfo, out procInfo);
        CloseHandle(num);
        CloseHandle(zero2);
        CloseHandle(zero1);
        return processAsUser;
    }

    public static void RestartExplorer()
    {
        foreach (var process in Process.GetProcesses())
            if (process.ProcessName.ToLower().StartsWith("explorer"))
                process.Kill();
        Thread.Sleep(1000);
        var flag = Process.GetProcesses().Any(process => process.ProcessName.ToLower().StartsWith("explorer"));

        if (flag) return;
        StartProcessAsUser(ImpersonalizationSource.CurrentProcess, "explorer.exe", out var _);
    }

    public static void StartImpersonalized(string CommandLine)
    {
        StartProcessAsUser(ImpersonalizationSource.Winlogon, CommandLine, out var _);
    }

    public static Process StartHidden(string FileName, string Arguments, bool Elevated = false)
    {
        var startInfo = new ProcessStartInfo { FileName = FileName, Arguments = Arguments };
        if (Elevated) startInfo.Verb = "RunAs";
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        Process process = null;
        try
        {
            process = Process.Start(startInfo);
        }
        catch
        {
            // ignored
        }

        return process;
    }

    public static Process StartElevated(string FileName, string Arguments)
    {
        var startInfo = new ProcessStartInfo { FileName = FileName, Arguments = Arguments, Verb = "RunAs" };
        Process process = null;
        try
        {
            process = Process.Start(startInfo);
        }
        catch
        {
            // ignored
        }

        return process;
    }

    public static void OpenURL(string URL, bool AddUTMParams, string utm_content)
    {
        if (AddUTMParams)
        {
            var lower = Path.GetFileNameWithoutExtension(Environment.CurrentDirectory).Replace(" ", "").ToLower();
            URL = URL + "?utm_source=software&utm_medium=in-app&utm_campaign=" + lower;
            if (utm_content.Length > 0) URL = URL + "&utm_content=" + utm_content;
        }

        Process.Start(URL);
    }

    private struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public IntPtr LpSecurityDescriptor { get; }
        public bool BInheritHandle { get; }
    }

    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    private struct PROCESS_INFORMATION
    {
        public IntPtr HProcess { get; }
        public IntPtr HThread { get; }
        public uint DwProcessId { get; }
        public uint DwThreadId { get; }
    }

    private enum TOKEN_TYPE
    {
        TokenPrimary = 1,
        TokenImpersonation = 2
    }

    private enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }

    private enum ImpersonalizationSource
    {
        Winlogon = 1,
        Explorer = 2,
        CurrentProcess = 3
    }
}
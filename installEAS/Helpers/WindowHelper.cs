namespace installEAS.Helpers;

public abstract class WindowHelper
{
    /// <summary>
    /// Moves a specified Window to the center of a monitor where the cursor locates.
    /// </summary>
    /// <param name="window">Window to be moved</param>
    /// <returns>
    /// This should be called before Loaded event such as ArrangeOverride method.
    /// </returns>
    public bool MoveToCenter(Window window)
    {
        if (window.WindowStartupLocation != WindowStartupLocation.CenterScreen)
            return false;

        return !window.IsLoaded && MoveToCenterBase(window);
    }

    public static bool MoveToCenterBase(Window window)
    {
        if (!GetCursorPos(out var cursorPoint))
            return false;

        var monitorHandle = MonitorFromPoint(cursorPoint, MONITOR_DEFAULTTO.MONITOR_DEFAULTTONULL);
        if (monitorHandle == IntPtr.Zero)
            return false;

        var monitorInfo = new MONITORINFO() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitorHandle, ref monitorInfo))
            return false;

        var windowHandle = new WindowInteropHelper(window).EnsureHandle();
        if (windowHandle == IntPtr.Zero)
            return false;

        if (!GetWindowPlacement(windowHandle, out var windowPlacement))
            return false;

        var left = monitorInfo.rcWork.left + Math.Max(0, (int)((monitorInfo.rcWork.Width - windowPlacement.rcNormalPosition.Width) / 2D));
        var top  = monitorInfo.rcWork.top + Math.Max(0, (int)((monitorInfo.rcWork.Height - windowPlacement.rcNormalPosition.Height) / 2D));

        windowPlacement.rcNormalPosition = new RECT(left, top, windowPlacement.rcNormalPosition.Width, windowPlacement.rcNormalPosition.Height);

        return SetWindowPlacement(windowHandle, ref windowPlacement);
    }

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("User32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, MONITOR_DEFAULTTO dwFlags);

    private enum MONITOR_DEFAULTTO : uint
    {
        MONITOR_DEFAULTTONULL    = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002
    }

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public           uint cbSize;
        private readonly RECT rcMonitor;
        public readonly  RECT rcWork;
        private readonly uint dwFlags;
    }

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        private readonly uint  length;
        private readonly uint  flags;
        private readonly uint  showCmd;
        private readonly POINT ptMinPosition;
        private readonly POINT ptMaxPosition;
        public           RECT  rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        private readonly int x;
        private readonly int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct RECT
    {
        public readonly  int left;
        public readonly  int top;
        private readonly int right;
        private readonly int bottom;

        public int Width  => right - left;
        public int Height => bottom - top;

        public RECT(int x, int y, int width, int height)
        {
            left   = x;
            top    = y;
            right  = x + width;
            bottom = y + height;
        }
    }
}
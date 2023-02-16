using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace AvalonEdit.Utils;

internal static class Win32
{
    public static TimeSpan CaretBlinkTime => TimeSpan.FromMilliseconds(SafeNativeMethods.GetCaretBlinkTime());

    public static bool CreateCaret(Visual owner, Size size)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (PresentationSource.FromVisual(owner) is not HwndSource source) return false;
        var r = owner.PointToScreen(new Point(size.Width, size.Height)) - owner.PointToScreen(new Point(0, 0));
        return SafeNativeMethods.CreateCaret(source.Handle, IntPtr.Zero, (int)Math.Ceiling(r.X), (int)Math.Ceiling(r.Y));
    }

    public static bool SetCaretPosition(Visual owner, Point position)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (PresentationSource.FromVisual(owner) is not HwndSource source) return false;
        var pointOnRootVisual = owner.TransformToAncestor(source.RootVisual).Transform(position);
        var pointOnHwnd       = pointOnRootVisual.TransformToDevice(source.RootVisual);
        return SafeNativeMethods.SetCaretPos((int)pointOnHwnd.X, (int)pointOnHwnd.Y);
    }

    public static bool DestroyCaret()
    {
        return SafeNativeMethods.DestroyCaret();
    }

    [SuppressUnmanagedCodeSecurity]
    private static class SafeNativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetCaretBlinkTime();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCaretPos(int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyCaret();
    }
}
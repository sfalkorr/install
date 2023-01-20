using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace installEAS
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App :Application
    {
        [DllImport( "User32" )] private static extern int ShowWindow( IntPtr hwnd, int nCmdShow );
        [DllImport( "USER32.DLL", CharSet = CharSet.Unicode )] private static extern IntPtr FindWindow( String lpClassName, String lpWindowName );
        [DllImport( "USER32.DLL" )] private static extern bool SetForegroundWindow( IntPtr hWnd );

        public static Mutex Singleton { get; } = new( true, "installEAS" );

        protected override void OnStartup( StartupEventArgs e )
        {
            string path_app = Assembly.GetExecutingAssembly().GetName().CodeBase.Replace( "/", "\\" ).Replace( "file:\\\\\\", "" );
            if (path_app.Contains( "file" )) { Console.WriteLine( "Запрет запуска из сетевого пути" ); }
            Process[] p = Process.GetProcessesByName( "installEAS" );
            int hWnd = (int)p[0].MainWindowHandle;
            if (!Singleton.WaitOne( TimeSpan.FromMilliseconds( 0 ), true ))
            {
                IntPtr handle = FindWindow( null, "installEASApp" );
                if (handle == IntPtr.Zero)
                    return;
                SetForegroundWindow( handle );
                ShowWindow( handle, 9 );
                Process.GetCurrentProcess().Kill();
            }
        }
    }
}

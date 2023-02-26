using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;


namespace installEAS.Helpers;

public static class Functions
{

    /// <summary>
    /// path: yournamespace.resourcefolder.filename (like png) 
    /// </summary>
    public static BitmapSource GetImageSource( string path )
    {
        var bitmap = new BitmapImage();
        if (path == null) return null;
        bitmap.BeginInit();
        bitmap.StreamSource = Assembly.GetExecutingAssembly().GetManifestResourceStream( path );
        bitmap.CacheOption  = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        return bitmap;
    }

    /// <summary>
    /// path: yournamespace.resourcefolder.filename (like png) 
    /// </summary>
    public static Icon GetPngConvertToIco( string path )
    {
        if (path == null) return null;
        var bitmap = new Bitmap( Assembly.GetExecutingAssembly().GetManifestResourceStream( path ) );
        return Icon.FromHandle( bitmap.GetHicon() );
    }

    public static void ProcessKill(string processname)
    {
        Process.GetProcesses().Where(x => x.ProcessName.StartsWith(processname, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Kill());
    }

    public static string GetEmbeddedResource(string namespacename, string filename)
    {
        return new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{namespacename}.{filename}")).ReadToEnd();
    }


    public static async Task Sleep(int ms)
    {
        try
        {
            await Task.Run(() =>
            {
                Thread.Sleep(ms);
            }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    public static bool SetMachineName(string newName)
    {
        var key = Registry.LocalMachine;

        const string activeComputerName = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ActiveComputerName";
        var          activeCmpName      = key.CreateSubKey(activeComputerName);
        if (activeCmpName != null)
        {
            activeCmpName.SetValue("ComputerName", newName);
            activeCmpName.Close();
        }

        const string computerName = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName";
        var          cmpName      = key.CreateSubKey(computerName);
        if (cmpName != null)
        {
            cmpName.SetValue("ComputerName", newName);
            cmpName.Close();
        }

        const string _hostName = "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters\\";
        var          hostName  = key.CreateSubKey(_hostName);
        if (hostName == null) return true;
        hostName.SetValue("Hostname", newName);
        hostName.SetValue("NV Hostname", newName);
        hostName.Close();

        return true;
    }


    public static bool SetComputerName(string Name)
    {
        const string RegLocComputerName = @"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName";
        try
        {
            var compPath = "Win32_ComputerSystem.Name='" + Environment.MachineName + "'";
            var mo       = new ManagementObject(new ManagementPath(compPath));

            var inputArgs = mo.GetMethodParameters("Rename");
            inputArgs["Name"] = Name;
            var output = mo.InvokeMethod("Rename", inputArgs, null);
            if (output != null)
            {
                var retValue = (uint)Convert.ChangeType(output.Properties["ReturnValue"].Value, typeof(uint));
                if (retValue != 0) throw new Exception("Computer could not be changed due to unknown reason.");
            }

            var ComputerName = Registry.LocalMachine.OpenSubKey(RegLocComputerName);
            if (ComputerName == null) throw new Exception("Registry location '" + RegLocComputerName + "' is not readable.");
            if ((string)ComputerName.GetValue("ComputerName") != Name) throw new Exception("The computer name was set by WMI but was not updated in the registry location: '" + RegLocComputerName + "'");
            ComputerName.Close();
            ComputerName.Dispose();
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}
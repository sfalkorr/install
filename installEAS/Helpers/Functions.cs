using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


namespace installEAS.Helpers;

public static class Functions
{
    public static void ProcessKill(string processname)
    {
        Process.GetProcesses().Where(x => x.ProcessName.StartsWith(processname, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Kill());
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
}
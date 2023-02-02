using System;
using System.Diagnostics;
using System.Linq;


namespace installEAS.Helpers;

public static class Functions
{
    public static void ProcessKill(string processname)
    {
        Process.GetProcesses().Where(x => x.ProcessName.StartsWith(processname, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Kill());
    }
}
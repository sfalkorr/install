using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using static installEAS.Variables;
using static installEAS.Helpers.Log;

namespace installEAS.Helpers;

public abstract class ArchiveHelper
{
    public static ProcessStartInfo _processStartInfo = new();
    public static Match            _match;

    public static string ArchView(string source, string filenamepattern = default)
    {
        try
        {
            Process pProcess = new();
            pProcess.StartInfo.FileName               = SevenZPath;
            pProcess.StartInfo.Arguments              = "l " + "\"" + source + "\"";
            pProcess.StartInfo.UseShellExecute        = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.Start();
            var strOutput = pProcess.StandardOutput.ReadToEnd();
            pProcess.WaitForExit();
            if (filenamepattern != null)
            {
                _match = Regex.Match(strOutput, filenamepattern);
                if (_match.Success) return _match.Value;
            }
            else
            {
                return null;
            }

            return _match.Value;
        }
        catch (Exception e)
        {
            log(e.Message, Brushes.Red);
            return null;
        }
    }

    public static void ArchCreate7Zip(string sourceName, string targetName, ProcessWindowStyle WindowStyle = ProcessWindowStyle.Normal)
    {
        try
        {
            var p = _processStartInfo;
            p.FileName    = SevenZgPath;
            p.Arguments   = "a \"" + targetName + "\" \"" + sourceName + "\"";
            p.WindowStyle = WindowStyle;
            var x = Process.Start(p);
            x?.WaitForExit();
        }
        catch (Exception e)
        {
            log(e.Message, Brushes.Red);
        }
    }

    public static void ArchExtract(string source, string destination, string filemask = default, ProcessWindowStyle WindowStyle = ProcessWindowStyle.Normal)
    {
        if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);
        try
        {
            var pro = _processStartInfo;
            pro.WindowStyle = WindowStyle;
            pro.FileName    = SevenZgPath;
            pro.Arguments   = filemask != default ? "e \"" + source + "\" -o\"" + destination + "\"" + " \"" + filemask + "\"" + " -y" + " -r" : "e \"" + source + "\" -o\"" + destination + "\"" + " -y" + " -r";
            var x = Process.Start(pro);
            x?.WaitForExit();
        }
        catch (Exception e)
        {
            log(e.Message, Brushes.Red);
        }
    }
}
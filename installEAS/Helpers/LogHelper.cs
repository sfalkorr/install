using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using static System.String;
using static installEAS.MainWindow;

namespace installEAS.Helpers;

public static class Log
{
    public enum Level
    {
        Error       = 0,
        Information = 1,
        Warning     = 2
    }


    public static void ToEventLog(string @object, string msg, Level level)
    {
        EventLogEntryType lev = level switch
                                {
                                    Level.Error       => EventLogEntryType.Error,
                                    Level.Information => EventLogEntryType.Information,
                                    Level.Warning     => EventLogEntryType.Warning,
                                    _                 => 0
                                };
        var processModule = Process.GetCurrentProcess().MainModule;
        msg = $"{msg}\n{processModule?.FileName}\nSource: {@object}";
        const string source = "installEAS";
        if (!EventLog.SourceExists(source)) EventLog.CreateEventSource(source, "Application");
        EventLog.WriteEntry(source, msg, lev);
    }

    public static void log(string text, SolidColorBrush brush, bool newline = true)
    {
        if (text != null)
            MainFrame.Dispatcher.BeginInvoke(() =>
            {
                if (text != null) text = newline ? text + Environment.NewLine : text;
                MainFrame.rtb.AppendColorLine(text, brush);
                MainFrame.rtb.ScrollToEnd();
            });
    }

    public static void log(string text, bool newline = true)
    {
        if (text != null)
            MainFrame.Dispatcher.BeginInvoke(() =>
            {
                if (text != null) text = newline ? text + Environment.NewLine : text;
                MainFrame.rtb.AppendText(text);
                MainFrame.rtb.ScrollToEnd();
            });
    }


    public static void cLog(string msg, bool newline, ConsoleColor color)
    {
        msg                     = newline ? msg + Environment.NewLine : msg;
        Console.ForegroundColor = ConsoleColor.White;

        if (color != default)
        {
            Console.ForegroundColor = color;
            Console.Write(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }
        else
        {
            Console.Write(msg + Environment.NewLine);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public static void cLog(string msg, ConsoleColor color = default)
    {
        Console.ForegroundColor = ConsoleColor.White;
        if (color != default) Console.Write(msg);

        if (color == default) Console.Write(msg + Environment.NewLine);
        Console.ForegroundColor = ConsoleColor.White;
    }

    public static void cLog(string msg, bool newline)
    {
        Console.ForegroundColor = ConsoleColor.White;
        msg                     = newline ? msg + Environment.NewLine : msg;
        Console.Write(msg);
        Console.ForegroundColor = ConsoleColor.White;
    }
}
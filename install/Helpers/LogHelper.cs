using System;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;

namespace installEAS.Helpers;

public static class Log
{
    public static void log(string text, SolidColorBrush brush, bool newline = true)
    {
        MainWindow.MainFrame.Dispatcher.BeginInvoke(() =>
        {
            TextRange tr = new(MainWindow.MainFrame.rtb.Document.ContentEnd, MainWindow.MainFrame.rtb.Document.ContentEnd) { Text = newline ? text + Environment.NewLine : text + " " };
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            MainWindow.MainFrame.rtb.ScrollToEnd();
        });
    }

    public static void log(string text, bool newline = true)
    {
        MainWindow.MainFrame.Dispatcher.BeginInvoke(() =>
        {
            TextRange tr = new(MainWindow.MainFrame.rtb.Document.ContentEnd, MainWindow.MainFrame.rtb.Document.ContentEnd) { Text = newline ? text + Environment.NewLine : text + " " };
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, MainWindow.MainFrame.rtb.Foreground);
            MainWindow.MainFrame.rtb.ScrollToEnd();
        });
    }

    public static Task mLogAsync(string text, bool newline = true)
    {
        MainWindow.MainFrame.Dispatcher.BeginInvoke(() =>
        {
            TextRange tr = new(MainWindow.MainFrame.rtb.Document.ContentEnd, MainWindow.MainFrame.rtb.Document.ContentEnd) { Text = newline ? text + Environment.NewLine : text + " " };
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, MainWindow.MainFrame.rtb.Foreground);
            MainWindow.MainFrame.rtb.ScrollToEnd();
        });

        return Task.CompletedTask;
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
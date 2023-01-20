using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace installEAS
{
    public static class LogHelper
    {
        public static void mLog( string text, SolidColorBrush brush, bool newline = true )
        {
            MainWindow.MainFrame.Dispatcher.InvokeOrExecute(() =>
            {
                TextRange tr = new( MainWindow.MainFrame.rtb.Document.ContentEnd, MainWindow.MainFrame.rtb.Document.ContentEnd )
                {
                    Text = (newline) ? text + Environment.NewLine : text + " "
                };
                tr.ApplyPropertyValue( TextElement.ForegroundProperty, brush );
                MainWindow.MainFrame.rtb.ScrollToEnd();
            });
            //return Task.CompletedTask;
        }



        public static void mLog( string text, bool newline = true )
        {
            MainWindow.MainFrame.Dispatcher.InvokeOrExecute(() =>
            {
                TextRange tr = new( MainWindow.MainFrame.rtb.Document.ContentEnd, MainWindow.MainFrame.rtb.Document.ContentEnd )
                {
                    Text = (newline) ? text + Environment.NewLine : text + " "
                };
                tr.ApplyPropertyValue( TextElement.ForegroundProperty, Brushes.White );
                MainWindow.MainFrame.rtb.ScrollToEnd();
            } );

            //return Task.CompletedTask;
        }

        public static void clog( string msg, bool newline, ConsoleColor color = default )
        {
            Console.ForegroundColor = ConsoleColor.White;
            msg = (newline) ? msg + Environment.NewLine : msg;
            if (color != default) { Console.ForegroundColor = color; Console.Write( msg ); Console.ForegroundColor = ConsoleColor.White; }
            else { Console.Write( msg + Environment.NewLine ); Console.ForegroundColor = ConsoleColor.White; }
        }
        public static void clog( string msg, ConsoleColor color = default )
        {
            Console.ForegroundColor = ConsoleColor.White;
            if (color != default) { Console.ForegroundColor = color; Console.Write( msg ); }
            if (color == default) { Console.Write( msg + Environment.NewLine ); }
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void clog( string msg, bool newline )
        {
            Console.ForegroundColor = ConsoleColor.White;
            msg = (newline) ? msg + Environment.NewLine : msg;
            Console.Write( msg ); Console.ForegroundColor = ConsoleColor.White;
        }

    }
}

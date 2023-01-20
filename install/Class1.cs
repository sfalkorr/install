using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static installEAS.LogHelper;

namespace installEAS
{
    public static class Class1
    {

        public static Task Test()
        {
            mLog( "тест из другого класса", Brushes.Gold );
            //MainWindow.MainFrame.pb.SetPercent( 100, TimeSpan.FromMilliseconds( 10000 ) );
            
            return Task.CompletedTask;
        }

        public static Task Test2()
        {
            Test();
            //MainWindow.MainFrame.pb.SetPercent( 100, TimeSpan.FromMilliseconds( 10000 ) );

            return Task.CompletedTask;
        }

    }
}

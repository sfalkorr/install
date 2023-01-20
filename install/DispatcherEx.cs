using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace installEAS
{
    public static class DispatcherEx
    {
        public static void InvokeOrExecute( this Dispatcher dispatcher, Action action, DispatcherPriority priority = DispatcherPriority.Normal )
        {
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke( priority, action );
            }
        }
    }
}

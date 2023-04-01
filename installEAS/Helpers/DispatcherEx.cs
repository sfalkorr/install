namespace installEAS.Helpers;

public static class DispatcherEx
{
    public static void InvokeOrExecute(this Dispatcher dispatcher, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        if (dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(priority, action);
    }
}
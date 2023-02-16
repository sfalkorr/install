using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace AvalonEdit.Utils;

public abstract class WeakEventManagerBase<TManager, TEventSource> : WeakEventManager where TManager : WeakEventManagerBase<TManager, TEventSource>, new() where TEventSource : class
{
    protected WeakEventManagerBase()
    {
        Debug.Assert(GetType() == typeof(TManager));
    }

    [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
    public static void AddListener(TEventSource source, IWeakEventListener listener)
    {
        CurrentManager.ProtectedAddListener(source, listener);
    }

    [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
    public static void RemoveListener(TEventSource source, IWeakEventListener listener)
    {
        CurrentManager.ProtectedRemoveListener(source, listener);
    }

    protected sealed override void StartListening(object source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        StartListening((TEventSource)source);
    }

    protected sealed override void StopListening(object source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        StopListening((TEventSource)source);
    }

    protected abstract void StartListening(TEventSource source);

    protected abstract void StopListening(TEventSource source);

    [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
    protected static TManager CurrentManager
    {
        get
        {
            var managerType = typeof(TManager);
            var manager     = (TManager)GetCurrentManager(managerType);
            if (manager == null)
            {
                manager = new TManager();
                SetCurrentManager(managerType, manager);
            }

            return manager;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace AvalonEdit.Utils;

internal sealed class CallbackOnDispose : IDisposable
{
    private Action action;

    public CallbackOnDispose(Action action)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Dispose()
    {
        var a = Interlocked.Exchange(ref action, null);
        a?.Invoke();
    }
}

internal static class BusyManager
{
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Should always be used with 'var'")]
    public struct BusyLock : IDisposable
    {
        public static readonly BusyLock Failed = new(null);

        private readonly List<object> objectList;

        internal BusyLock(List<object> objectList)
        {
            this.objectList = objectList;
        }

        public bool Success => objectList != null;

        public void Dispose()
        {
            objectList?.RemoveAt(objectList.Count - 1);
        }
    }

    [ThreadStatic]
    private static List<object> _activeObjects;

    public static BusyLock Enter(object obj)
    {
        var activeObjects = _activeObjects ??= new List<object>();
        for (var i = 0; i < activeObjects.Count; i++)
            if (activeObjects[i] == obj)
                return BusyLock.Failed;
        activeObjects.Add(obj);
        return new BusyLock(activeObjects);
    }
}
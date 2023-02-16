using System;
using System.Collections.ObjectModel;

namespace AvalonEdit.Utils;

internal sealed class ObserveAddRemoveCollection<T> : Collection<T>
{
    private readonly Action<T> onAdd, onRemove;

    public ObserveAddRemoveCollection(Action<T> onAdd, Action<T> onRemove)
    {
        this.onAdd    = onAdd ?? throw new ArgumentNullException(nameof(onAdd));
        this.onRemove = onRemove ?? throw new ArgumentNullException(nameof(onRemove));
    }

    protected override void ClearItems()
    {
        if (onRemove != null)
            foreach (var val in this)
                onRemove(val);
        base.ClearItems();
    }

    protected override void InsertItem(int index, T item)
    {
        onAdd?.Invoke(item);
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        onRemove?.Invoke(this[index]);
        base.RemoveItem(index);
    }

    protected override void SetItem(int index, T item)
    {
        onRemove?.Invoke(this[index]);
        try
        {
            onAdd?.Invoke(item);
        }
        catch
        {
            RemoveAt(index);
            throw;
        }

        base.SetItem(index, item);
    }
}
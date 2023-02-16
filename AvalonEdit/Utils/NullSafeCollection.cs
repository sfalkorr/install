﻿using System;
using System.Collections.ObjectModel;

namespace AvalonEdit.Utils;

[Serializable]
public class NullSafeCollection<T> : Collection<T> where T : class
{
    protected override void InsertItem(int index, T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        base.SetItem(index, item);
    }
}
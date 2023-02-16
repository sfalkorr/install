using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AvalonEdit.Utils;

internal interface IFreezable
{
    bool IsFrozen { get; }

    void Freeze();
}

// internal static class FreezableHelper
// {
//     public static void ThrowIfFrozen(IFreezable freezable)
//     {
//         if (freezable.IsFrozen) throw new InvalidOperationException("Cannot mutate frozen " + freezable.GetType().Name);
//     }
//
//     public static IList<T> FreezeListAndElements<T>(IList<T> list)
//     {
//         if (list == null) return FreezeList((IList<T>)null);
//         foreach (var item in list) Freeze(item);
//         return FreezeList(list);
//     }
//
//     public static IList<T> FreezeList<T>(IList<T> list)
//     {
//         if (list == null || list.Count == 0) return Empty<T>.Array;
//         return list.IsReadOnly ? list : new ReadOnlyCollection<T>(list.ToArray());
//     }
//
//     public static void Freeze(object item)
//     {
//         if (item is IFreezable f) f.Freeze();
//     }
//
//     public static T FreezeAndReturn<T>(T item) where T : IFreezable
//     {
//         item.Freeze();
//         return item;
//     }
//
//     public static T GetFrozenClone<T>(T item) where T : IFreezable, ICloneable
//     {
//         if (item.IsFrozen) return item;
//         item = (T)item.Clone();
//         item.Freeze();
//
//         return item;
//     }
// }

[Serializable]
internal abstract class AbstractFreezable : IFreezable
{
    public bool IsFrozen { get; private set; }

    public void Freeze()
    {
        if (IsFrozen) return;
        FreezeInternal();
        IsFrozen = true;
    }

    protected virtual void FreezeInternal()
    {
    }
}
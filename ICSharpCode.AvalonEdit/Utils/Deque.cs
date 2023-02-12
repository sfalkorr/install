﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ICSharpCode.AvalonEdit.Utils;

/// <summary>
///     Double-ended queue.
/// </summary>
[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
[Serializable]
public sealed class Deque<T> : ICollection<T>
{
    private T[] arr = Empty<T>.Array;
    private int head, tail;

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public void Clear()
    {
        arr   = Empty<T>.Array;
        Count = 0;
        head  = 0;
        tail  = 0;
    }

    /// <summary>
    ///     Gets/Sets an element inside the deque.
    /// </summary>
    public T this[int index]
    {
        get
        {
            ThrowUtil.CheckInRangeInclusive(index, "index", 0, Count - 1);
            return arr[(head + index) % arr.Length];
        }
        set
        {
            ThrowUtil.CheckInRangeInclusive(index, "index", 0, Count - 1);
            arr[(head + index) % arr.Length] = value;
        }
    }

    /// <summary>
    ///     Adds an element to the end of the deque.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "PushBack")]
    public void PushBack(T item)
    {
        if (Count == arr.Length) SetCapacity(Math.Max(4, arr.Length * 2));
        arr[tail++] = item;
        if (tail == arr.Length) tail = 0;
        Count++;
    }

    /// <summary>
    ///     Pops an element from the end of the deque.
    /// </summary>
    public T PopBack()
    {
        if (Count == 0) throw new InvalidOperationException();
        if (tail == 0) tail = arr.Length - 1;
        else tail--;
        var val = arr[tail];
        arr[tail] = default; // allow GC to collect the element
        Count--;
        return val;
    }

    /// <summary>
    ///     Adds an element to the front of the deque.
    /// </summary>
    public void PushFront(T item)
    {
        if (Count == arr.Length) SetCapacity(Math.Max(4, arr.Length * 2));
        if (head == 0) head = arr.Length - 1;
        else head--;
        arr[head] = item;
        Count++;
    }

    /// <summary>
    ///     Pops an element from the end of the deque.
    /// </summary>
    public T PopFront()
    {
        if (Count == 0) throw new InvalidOperationException();
        var val = arr[head];
        arr[head] = default; // allow GC to collect the element
        head++;
        if (head == arr.Length) head = 0;
        Count--;
        return val;
    }

    private void SetCapacity(int capacity)
    {
        var newArr = new T[capacity];
        CopyTo(newArr, 0);
        head = 0;
        tail = Count == capacity ? 0 : Count;
        arr  = newArr;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        if (head < tail)
        {
            for (var i = head; i < tail; i++) yield return arr[i];
        }
        else
        {
            for (var i = head; i < arr.Length; i++) yield return arr[i];
            for (var i = 0; i < tail; i++) yield return arr[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    bool ICollection<T>.IsReadOnly => false;

    void ICollection<T>.Add(T item)
    {
        PushBack(item);
    }

    /// <inheritdoc />
    public bool Contains(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        foreach (var element in this)
            if (comparer.Equals(item, element))
                return true;
        return false;
    }

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (head < tail)
        {
            Array.Copy(arr, head, array, arrayIndex, tail - head);
        }
        else
        {
            var num1 = arr.Length - head;
            Array.Copy(arr, head, array, arrayIndex, num1);
            Array.Copy(arr, 0, array, arrayIndex + num1, tail);
        }
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException();
    }
}
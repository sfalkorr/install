using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AvalonEdit.Utils;

[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
[SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
[Serializable]
public sealed class ImmutableStack<T> : IEnumerable<T>
{
    [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "ImmutableStack is immutable")]
    [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
    public static readonly ImmutableStack<T> Empty = new();

    private readonly T                 value;
    private readonly ImmutableStack<T> next;

    private ImmutableStack()
    {
    }

    private ImmutableStack(T value, ImmutableStack<T> next)
    {
        this.value = value;
        this.next  = next;
    }

    public ImmutableStack<T> Push(T item)
    {
        return new ImmutableStack<T>(item, this);
    }

    public T Peek()
    {
        if (IsEmpty) throw new InvalidOperationException("Operation not valid on empty stack.");
        return value;
    }

    public T PeekOrDefault()
    {
        return value;
    }

    public ImmutableStack<T> Pop()
    {
        if (IsEmpty) throw new InvalidOperationException("Operation not valid on empty stack.");
        return next;
    }

    public bool IsEmpty => next == null;

    public IEnumerator<T> GetEnumerator()
    {
        var t = this;
        while (!t.IsEmpty)
        {
            yield return t.value;
            t = t.next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        var b = new StringBuilder("[Stack");
        foreach (var val in this)
        {
            b.Append(' ');
            b.Append(val);
        }

        b.Append(']');
        return b.ToString();
    }
}
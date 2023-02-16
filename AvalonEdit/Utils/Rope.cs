using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AvalonEdit.Utils;

[Serializable]
[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
public sealed class Rope<T> : IList<T>, ICloneable
{
    internal RopeNode<T> root;

    internal Rope(RopeNode<T> root)
    {
        this.root = root;
        root.CheckInvariants();
    }

    public Rope()
    {
        root = RopeNode<T>.emptyRopeNode;
        root.CheckInvariants();
    }

    public Rope(IEnumerable<T> input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input is Rope<T> inputRope)
        {
            inputRope.root.Publish();
            root = inputRope.root;
        }
        else
        {
            if (input is string text)
            {
                ((Rope<char>)(object)this).root = CharRope.InitFromString(text);
            }
            else
            {
                var arr = ToArray(input);
                root = RopeNode<T>.CreateFromArray(arr, 0, arr.Length);
            }
        }

        root.CheckInvariants();
    }

    public Rope(T[] array, int arrayIndex, int count)
    {
        VerifyArrayWithRange(array, arrayIndex, count);
        root = RopeNode<T>.CreateFromArray(array, arrayIndex, count);
        root.CheckInvariants();
    }

    [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    public Rope(int length, Func<Rope<T>> initializer)
    {
        if (initializer == null) throw new ArgumentNullException(nameof(initializer));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), length, "Length must not be negative");
        if (length == 0) root = RopeNode<T>.emptyRopeNode;
        else root             = new FunctionNode<T>(length, initializer);
        root.CheckInvariants();
    }

    private static T[] ToArray(IEnumerable<T> input)
    {
        var arr = input as T[];
        return arr ?? input.ToArray();
    }

    public Rope<T> Clone()
    {
        root.Publish();
        return new Rope<T>(root);
    }

    object ICloneable.Clone()
    {
        return Clone();
    }

    public void Clear()
    {
        root = RopeNode<T>.emptyRopeNode;
        OnChanged();
    }

    public int Length => root.length;

    public int Count => root.length;

    public void InsertRange(int index, Rope<T> newElements)
    {
        if (index < 0 || index > Length) throw new ArgumentOutOfRangeException(nameof(index), index, "0 <= index <= " + Length.ToString(CultureInfo.InvariantCulture));
        if (newElements == null) throw new ArgumentNullException(nameof(newElements));
        newElements.root.Publish();
        root = root.Insert(index, newElements.root);
        OnChanged();
    }

    public void InsertRange(int index, IEnumerable<T> newElements)
    {
        if (newElements == null) throw new ArgumentNullException(nameof(newElements));
        if (newElements is Rope<T> newElementsRope)
        {
            InsertRange(index, newElementsRope);
        }
        else
        {
            var arr = ToArray(newElements);
            InsertRange(index, arr, 0, arr.Length);
        }
    }

    public void InsertRange(int index, T[] array, int arrayIndex, int count)
    {
        if (index < 0 || index > Length) throw new ArgumentOutOfRangeException(nameof(index), index, "0 <= index <= " + Length.ToString(CultureInfo.InvariantCulture));
        VerifyArrayWithRange(array, arrayIndex, count);
        if (count > 0)
        {
            root = root.Insert(index, array, arrayIndex, count);
            OnChanged();
        }
    }

    public void AddRange(IEnumerable<T> newElements)
    {
        InsertRange(Length, newElements);
    }

    public void AddRange(Rope<T> newElements)
    {
        InsertRange(Length, newElements);
    }

    public void AddRange(T[] array, int arrayIndex, int count)
    {
        InsertRange(Length, array, arrayIndex, count);
    }

    public void RemoveRange(int index, int count)
    {
        VerifyRange(index, count);
        if (count > 0)
        {
            root = root.RemoveRange(index, count);
            OnChanged();
        }
    }

    public void SetRange(int index, T[] array, int arrayIndex, int count)
    {
        VerifyRange(index, count);
        VerifyArrayWithRange(array, arrayIndex, count);
        if (count > 0)
        {
            root = root.StoreElements(index, array, arrayIndex, count);
            OnChanged();
        }
    }

    public Rope<T> GetRange(int index, int count)
    {
        VerifyRange(index, count);
        var newRope  = Clone();
        var endIndex = index + count;
        newRope.RemoveRange(endIndex, newRope.Length - endIndex);
        newRope.RemoveRange(0, index);
        return newRope;
    }

    [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
    public static Rope<T> Concat(Rope<T> left, Rope<T> right)
    {
        if (left == null) throw new ArgumentNullException(nameof(left));
        if (right == null) throw new ArgumentNullException(nameof(right));
        left.root.Publish();
        right.root.Publish();
        return new Rope<T>(RopeNode<T>.Concat(left.root, right.root));
    }

    [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
    public static Rope<T> Concat(params Rope<T>[] ropes)
    {
        if (ropes == null) throw new ArgumentNullException(nameof(ropes));
        var result = new Rope<T>();
        foreach (var r in ropes) result.AddRange(r);
        return result;
    }

    #region Caches / Changed event

    internal struct RopeCacheEntry
    {
        internal readonly RopeNode<T> node;
        internal readonly int         nodeStartIndex;

        internal RopeCacheEntry(RopeNode<T> node, int nodeStartOffset)
        {
            this.node      = node;
            nodeStartIndex = nodeStartOffset;
        }

        internal bool IsInside(int offset)
        {
            return offset >= nodeStartIndex && offset < nodeStartIndex + node.length;
        }
    }

    [NonSerialized]
    private volatile ImmutableStack<RopeCacheEntry> lastUsedNodeStack;

    internal void OnChanged()
    {
        lastUsedNodeStack = null;

        root.CheckInvariants();
    }

    #endregion

    #region GetChar / SetChar

    public T this[int index]
    {
        get
        {
            if (unchecked((uint)index >= (uint)Length)) throw new ArgumentOutOfRangeException(nameof(index), index, "0 <= index < " + Length.ToString(CultureInfo.InvariantCulture));
            var entry = FindNodeUsingCache(index).PeekOrDefault();
            return entry.node.contents[index - entry.nodeStartIndex];
        }
        set
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index), index, "0 <= index < " + Length.ToString(CultureInfo.InvariantCulture));
            root = root.SetElement(index, value);
            OnChanged();
        }
    }

    internal ImmutableStack<RopeCacheEntry> FindNodeUsingCache(int index)
    {
        Debug.Assert(index >= 0 && index < Length);

        var stack    = lastUsedNodeStack;
        var oldStack = stack;

        if (stack == null) stack                             = ImmutableStack<RopeCacheEntry>.Empty.Push(new RopeCacheEntry(root, 0));
        while (!stack.PeekOrDefault().IsInside(index)) stack = stack.Pop();
        while (true)
        {
            var entry = stack.PeekOrDefault();
            if (entry.node.height == 0)
            {
                if (entry.node.contents == null) entry = new RopeCacheEntry(entry.node.GetContentNode(), entry.nodeStartIndex);
                if (entry.node.contents != null) break;
            }

            if (index - entry.nodeStartIndex >= entry.node.left.length) stack = stack.Push(new RopeCacheEntry(entry.node.right, entry.nodeStartIndex + entry.node.left.length));
            else stack                                                        = stack.Push(new RopeCacheEntry(entry.node.left, entry.nodeStartIndex));
        }

        if (oldStack != stack) lastUsedNodeStack = stack;

        Debug.Assert(stack.Peek().node.contents != null);
        return stack;
    }

    #endregion

    #region ToString / WriteTo

    internal void VerifyRange(int startIndex, int length)
    {
        if (startIndex < 0 || startIndex > Length) throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, "0 <= startIndex <= " + Length.ToString(CultureInfo.InvariantCulture));
        if (length < 0 || startIndex + length > Length) throw new ArgumentOutOfRangeException(nameof(length), length, "0 <= length, startIndex(" + startIndex + ")+length <= " + Length.ToString(CultureInfo.InvariantCulture));
    }

    internal static void VerifyArrayWithRange(T[] array, int arrayIndex, int count)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException("startIndex", arrayIndex, "0 <= arrayIndex <= " + array.Length.ToString(CultureInfo.InvariantCulture));
        if (count < 0 || arrayIndex + count > array.Length) throw new ArgumentOutOfRangeException(nameof(count), count, "0 <= length, arrayIndex(" + arrayIndex + ")+count <= " + array.Length.ToString(CultureInfo.InvariantCulture));
    }

    public override string ToString()
    {
        if (this is Rope<char> charRope) return charRope.ToString(0, Length);

        var b = new StringBuilder();
        foreach (var element in this)
        {
            if (b.Length == 0) b.Append('{');
            else b.Append(", ");
            b.Append(element);
        }

        b.Append('}');
        return b.ToString();
    }

    internal string GetTreeAsString()
    {
#if DEBUG
        return root.GetTreeAsString();
#else
        return "Not available in release build.";
#endif
    }

    #endregion

    bool ICollection<T>.IsReadOnly => false;

    public int IndexOf(T item)
    {
        return IndexOf(item, 0, Length);
    }

    public int IndexOf(T item, int startIndex, int count)
    {
        VerifyRange(startIndex, count);

        while (count > 0)
        {
            var entry           = FindNodeUsingCache(startIndex).PeekOrDefault();
            var contents        = entry.node.contents;
            var startWithinNode = startIndex - entry.nodeStartIndex;
            var nodeLength      = Math.Min(entry.node.length, startWithinNode + count);
            var r               = Array.IndexOf(contents, item, startWithinNode, nodeLength - startWithinNode);
            if (r >= 0) return entry.nodeStartIndex + r;
            count      -= nodeLength - startWithinNode;
            startIndex =  entry.nodeStartIndex + nodeLength;
        }

        return -1;
    }

    public int LastIndexOf(T item)
    {
        return LastIndexOf(item, 0, Length);
    }

    public int LastIndexOf(T item, int startIndex, int count)
    {
        VerifyRange(startIndex, count);

        var comparer = EqualityComparer<T>.Default;
        for (var i = startIndex + count - 1; i >= startIndex; i--)
            if (comparer.Equals(this[i], item))
                return i;
        return -1;
    }

    public void Insert(int index, T item)
    {
        InsertRange(index, new[] { item }, 0, 1);
    }

    public void RemoveAt(int index)
    {
        RemoveRange(index, 1);
    }

    public void Add(T item)
    {
        InsertRange(Length, new[] { item }, 0, 1);
    }

    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        CopyTo(0, array, arrayIndex, Length);
    }

    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        VerifyRange(index, count);
        VerifyArrayWithRange(array, arrayIndex, count);
        root.CopyTo(index, array, arrayIndex, count);
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    public IEnumerator<T> GetEnumerator()
    {
        root.Publish();
        return Enumerate(root);
    }

    public T[] ToArray()
    {
        var arr = new T[Length];
        root.CopyTo(0, arr, 0, arr.Length);
        return arr;
    }

    public T[] ToArray(int startIndex, int count)
    {
        VerifyRange(startIndex, count);
        var arr = new T[count];
        CopyTo(startIndex, arr, 0, count);
        return arr;
    }

    private static IEnumerator<T> Enumerate(RopeNode<T> node)
    {
        var stack = new Stack<RopeNode<T>>();
        while (node != null)
        {
            while (node.contents == null)
            {
                if (node.height == 0)
                {
                    node = node.GetContentNode();
                    continue;
                }

                Debug.Assert(node.right != null);
                stack.Push(node.right);
                node = node.left;
            }

            for (var i = 0; i < node.length; i++) yield return node.contents[i];
            node = stack.Count > 0 ? stack.Pop() : null;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
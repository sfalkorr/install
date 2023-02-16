using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AvalonEdit.Document;

using LineNode = DocumentLine;

internal sealed class DocumentLineTree : IList<DocumentLine>
{
    #region Constructor

    private readonly TextDocument document;
    private          LineNode     root;

    public DocumentLineTree(TextDocument document)
    {
        this.document = document;

        var emptyLine = new DocumentLine(document);
        root = emptyLine.InitLineNode();
    }

    #endregion

    #region Rotation callbacks

    internal static void UpdateAfterChildrenChange(LineNode node)
    {
        var totalCount  = 1;
        var totalLength = node.TotalLength;
        if (node.left != null)
        {
            totalCount  += node.left.nodeTotalCount;
            totalLength += node.left.nodeTotalLength;
        }

        if (node.right != null)
        {
            totalCount  += node.right.nodeTotalCount;
            totalLength += node.right.nodeTotalLength;
        }

        if (totalCount != node.nodeTotalCount || totalLength != node.nodeTotalLength)
        {
            node.nodeTotalCount  = totalCount;
            node.nodeTotalLength = totalLength;
            if (node.parent != null) UpdateAfterChildrenChange(node.parent);
        }
    }

    private static void UpdateAfterRotateLeft(LineNode node)
    {
        UpdateAfterChildrenChange(node);
    }

    private static void UpdateAfterRotateRight(LineNode node)
    {
        UpdateAfterChildrenChange(node);
    }

    #endregion

    #region RebuildDocument

    public void RebuildTree(List<DocumentLine> documentLines)
    {
        var nodes = new LineNode[documentLines.Count];
        for (var i = 0; i < documentLines.Count; i++)
        {
            var ls   = documentLines[i];
            var node = ls.InitLineNode();
            nodes[i] = node;
        }

        Debug.Assert(nodes.Length > 0);
        var height = GetTreeHeight(nodes.Length);
        Debug.WriteLine("DocumentLineTree will have height: " + height);
        root       = BuildTree(nodes, 0, nodes.Length, height);
        root.color = BLACK;
#if DEBUG
//        CheckProperties();
#endif
    }

    internal static int GetTreeHeight(int size)
    {
        if (size == 0) return 0;
        return GetTreeHeight(size / 2) + 1;
    }

    private LineNode BuildTree(LineNode[] nodes, int start, int end, int subtreeHeight)
    {
        Debug.Assert(start <= end);
        if (start == end) return null;
        var middle = (start + end) / 2;
        var node   = nodes[middle];
        node.left  = BuildTree(nodes, start, middle, subtreeHeight - 1);
        node.right = BuildTree(nodes, middle + 1, end, subtreeHeight - 1);
        if (node.left != null) node.left.parent   = node;
        if (node.right != null) node.right.parent = node;
        if (subtreeHeight == 1) node.color        = RED;
        UpdateAfterChildrenChange(node);
        return node;
    }

    #endregion

    #region GetNodeBy... / Get...FromNode

    private LineNode GetNodeByIndex(int index)
    {
        Debug.Assert(index >= 0);
        Debug.Assert(index < root.nodeTotalCount);
        var node = root;
        while (true)
            if (node.left != null && index < node.left.nodeTotalCount)
            {
                node = node.left;
            }
            else
            {
                if (node.left != null) index -= node.left.nodeTotalCount;
                if (index == 0) return node;
                index--;
                node = node.right;
            }
    }

    internal static int GetIndexFromNode(LineNode node)
    {
        var index = node.left?.nodeTotalCount ?? 0;
        while (node.parent != null)
        {
            if (node == node.parent.right)
            {
                if (node.parent.left != null) index += node.parent.left.nodeTotalCount;
                index++;
            }

            node = node.parent;
        }

        return index;
    }

    private LineNode GetNodeByOffset(int offset)
    {
        Debug.Assert(offset >= 0);
        Debug.Assert(offset <= root.nodeTotalLength);
        if (offset == root.nodeTotalLength) return root.RightMost;
        var node = root;
        while (true)
            if (node.left != null && offset < node.left.nodeTotalLength)
            {
                node = node.left;
            }
            else
            {
                if (node.left != null) offset -= node.left.nodeTotalLength;
                offset -= node.TotalLength;
                if (offset < 0) return node;
                node = node.right;
            }
    }

    internal static int GetOffsetFromNode(LineNode node)
    {
        var offset = node.left?.nodeTotalLength ?? 0;
        while (node.parent != null)
        {
            if (node == node.parent.right)
            {
                if (node.parent.left != null) offset += node.parent.left.nodeTotalLength;
                offset += node.parent.TotalLength;
            }

            node = node.parent;
        }

        return offset;
    }

    #endregion

    #region GetLineBy

    public DocumentLine GetByNumber(int number)
    {
        return GetNodeByIndex(number - 1);
    }

    public DocumentLine GetByOffset(int offset)
    {
        return GetNodeByOffset(offset);
    }

    #endregion

    #region LineCount

    public int LineCount => root.nodeTotalCount;

    #endregion

    #region CheckProperties

    #endregion

    #region Insert/Remove lines

    public void RemoveLine(DocumentLine line)
    {
        RemoveNode(line);
        line.isDeleted = true;
    }

    public DocumentLine InsertLineAfter(DocumentLine line, int totalLength)
    {
        var newLine = new DocumentLine(document) { TotalLength = totalLength };

        InsertAfter(line, newLine);
        return newLine;
    }

    private void InsertAfter(LineNode node, DocumentLine newLine)
    {
        var newNode = newLine.InitLineNode();
        if (node.right == null) InsertAsRight(node, newNode);
        else InsertAsLeft(node.right.LeftMost, newNode);
    }

    #endregion

    #region Red/Black Tree

    internal const bool RED   = true;
    internal const bool BLACK = false;

    private void InsertAsLeft(LineNode parentNode, LineNode newNode)
    {
        Debug.Assert(parentNode.left == null);
        parentNode.left = newNode;
        newNode.parent  = parentNode;
        newNode.color   = RED;
        UpdateAfterChildrenChange(parentNode);
        FixTreeOnInsert(newNode);
    }

    private void InsertAsRight(LineNode parentNode, LineNode newNode)
    {
        Debug.Assert(parentNode.right == null);
        parentNode.right = newNode;
        newNode.parent   = parentNode;
        newNode.color    = RED;
        UpdateAfterChildrenChange(parentNode);
        FixTreeOnInsert(newNode);
    }

    private void FixTreeOnInsert(LineNode node)
    {
        while (true)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.color == RED);
            Debug.Assert(node.left == null || node.left.color == BLACK);
            Debug.Assert(node.right == null || node.right.color == BLACK);

            var parentNode = node.parent;
            if (parentNode == null)
            {
                node.color = BLACK;
                return;
            }

            if (parentNode.color == BLACK) return;
            var grandparentNode = parentNode.parent;
            var uncleNode       = Sibling(parentNode);
            if (uncleNode is { color: RED })
            {
                parentNode.color      = BLACK;
                uncleNode.color       = BLACK;
                grandparentNode.color = RED;
                node                  = grandparentNode;
                continue;
            }

            if (node == parentNode.right && parentNode == grandparentNode.left)
            {
                RotateLeft(parentNode);
                node = node.left;
            }
            else if (node == parentNode.left && parentNode == grandparentNode.right)
            {
                RotateRight(parentNode);
                node = node.right;
            }

            parentNode      = node.parent;
            grandparentNode = parentNode.parent;

            parentNode.color      = BLACK;
            grandparentNode.color = RED;
            if (node == parentNode.left && parentNode == grandparentNode.left) { RotateRight(grandparentNode); }
            else
            {
                Debug.Assert(node == parentNode.right && parentNode == grandparentNode.right);
                RotateLeft(grandparentNode);
            }

            break;
        }
    }

    private void RemoveNode(LineNode removedNode)
    {
        if (removedNode.left != null && removedNode.right != null)
        {
            var leftMost = removedNode.right.LeftMost;
            RemoveNode(leftMost);

            ReplaceNode(removedNode, leftMost);
            leftMost.left = removedNode.left;
            if (leftMost.left != null) leftMost.left.parent = leftMost;
            leftMost.right = removedNode.right;
            if (leftMost.right != null) leftMost.right.parent = leftMost;
            leftMost.color = removedNode.color;

            UpdateAfterChildrenChange(leftMost);
            if (leftMost.parent != null) UpdateAfterChildrenChange(leftMost.parent);
            return;
        }

        var parentNode = removedNode.parent;
        var childNode  = removedNode.left ?? removedNode.right;
        ReplaceNode(removedNode, childNode);
        if (parentNode != null) UpdateAfterChildrenChange(parentNode);
        if (removedNode.color != BLACK) return;
        if (childNode is { color: RED }) childNode.color = BLACK;
        else FixTreeOnDelete(childNode, parentNode);
    }

    private void FixTreeOnDelete(LineNode node, LineNode parentNode)
    {
        Debug.Assert(node == null || node.parent == parentNode);
        if (parentNode == null) return;

        var sibling = Sibling(node, parentNode);
        if (sibling.color == RED)
        {
            parentNode.color = RED;
            sibling.color    = BLACK;
            if (node == parentNode.left) RotateLeft(parentNode);
            else RotateRight(parentNode);

            sibling = Sibling(node, parentNode);
        }

        if (parentNode.color == BLACK && sibling.color == BLACK && GetColor(sibling.left) == BLACK && GetColor(sibling.right) == BLACK)
        {
            sibling.color = RED;
            FixTreeOnDelete(parentNode, parentNode.parent);
            return;
        }

        if (parentNode.color == RED && sibling.color == BLACK && GetColor(sibling.left) == BLACK && GetColor(sibling.right) == BLACK)
        {
            sibling.color    = RED;
            parentNode.color = BLACK;
            return;
        }

        if (node == parentNode.left && sibling.color == BLACK && GetColor(sibling.left) == RED && GetColor(sibling.right) == BLACK)
        {
            sibling.color      = RED;
            sibling.left.color = BLACK;
            RotateRight(sibling);
        }
        else if (node == parentNode.right && sibling.color == BLACK && GetColor(sibling.right) == RED && GetColor(sibling.left) == BLACK)
        {
            sibling.color       = RED;
            sibling.right.color = BLACK;
            RotateLeft(sibling);
        }

        sibling = Sibling(node, parentNode);

        sibling.color    = parentNode.color;
        parentNode.color = BLACK;
        if (node == parentNode.left)
        {
            if (sibling.right != null)
            {
                Debug.Assert(sibling.right.color == RED);
                sibling.right.color = BLACK;
            }

            RotateLeft(parentNode);
        }
        else
        {
            if (sibling.left != null)
            {
                Debug.Assert(sibling.left.color == RED);
                sibling.left.color = BLACK;
            }

            RotateRight(parentNode);
        }
    }

    private void ReplaceNode(LineNode replacedNode, LineNode newNode)
    {
        if (replacedNode.parent == null)
        {
            Debug.Assert(replacedNode == root);
            root = newNode;
        }
        else
        {
            if (replacedNode.parent.left == replacedNode) replacedNode.parent.left = newNode;
            else replacedNode.parent.right                                         = newNode;
        }

        if (newNode != null) newNode.parent = replacedNode.parent;
        replacedNode.parent = null;
    }

    private void RotateLeft(LineNode p)
    {
        var q = p.right;
        Debug.Assert(q != null);
        Debug.Assert(q.parent == p);
        ReplaceNode(p, q);

        p.right = q.left;
        if (p.right != null) p.right.parent = p;
        q.left   = p;
        p.parent = q;
        UpdateAfterRotateLeft(p);
    }

    private void RotateRight(LineNode p)
    {
        var q = p.left;
        Debug.Assert(q != null);
        Debug.Assert(q.parent == p);
        ReplaceNode(p, q);

        p.left = q.right;
        if (p.left != null) p.left.parent = p;
        q.right  = p;
        p.parent = q;
        UpdateAfterRotateRight(p);
    }

    private static LineNode Sibling(LineNode node)
    {
        return node == node.parent.left ? node.parent.right : node.parent.left;
    }

    private static LineNode Sibling(LineNode node, LineNode parentNode)
    {
        Debug.Assert(node == null || node.parent == parentNode);
        return node == parentNode.left ? parentNode.right : parentNode.left;
    }

    private static bool GetColor(LineNode node)
    {
        return node is { color: true };
    }

    #endregion

    #region IList implementation

    DocumentLine IList<DocumentLine>.this[int index]
    {
        get
        {
            document.VerifyAccess();
            return GetByNumber(1 + index);
        }
        set => throw new NotSupportedException();
    }

    int ICollection<DocumentLine>.Count
    {
        get
        {
            document.VerifyAccess();
            return LineCount;
        }
    }

    bool ICollection<DocumentLine>.IsReadOnly => true;

    int IList<DocumentLine>.IndexOf(DocumentLine item)
    {
        document.VerifyAccess();
        if (item == null || item.IsDeleted) return -1;
        var index = item.LineNumber - 1;
        if (index < LineCount && GetNodeByIndex(index) == item) return index;
        return -1;
    }

    void IList<DocumentLine>.Insert(int index, DocumentLine item)
    {
        throw new NotSupportedException();
    }

    void IList<DocumentLine>.RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    void ICollection<DocumentLine>.Add(DocumentLine item)
    {
        throw new NotSupportedException();
    }

    void ICollection<DocumentLine>.Clear()
    {
        throw new NotSupportedException();
    }

    bool ICollection<DocumentLine>.Contains(DocumentLine item)
    {
        IList<DocumentLine> self = this;
        return self.IndexOf(item) >= 0;
    }

    void ICollection<DocumentLine>.CopyTo(DocumentLine[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length < LineCount) throw new ArgumentException("The array is too small", nameof(array));
        if (arrayIndex < 0 || arrayIndex + LineCount > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "Value must be between 0 and " + (array.Length - LineCount));
        foreach (var ls in this) array[arrayIndex++] = ls;
    }

    bool ICollection<DocumentLine>.Remove(DocumentLine item)
    {
        throw new NotSupportedException();
    }

    public IEnumerator<DocumentLine> GetEnumerator()
    {
        document.VerifyAccess();
        return Enumerate();
    }

    private IEnumerator<DocumentLine> Enumerate()
    {
        document.VerifyAccess();
        var line = root.LeftMost;
        while (line != null)
        {
            yield return line;
            line = line.NextLine;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}
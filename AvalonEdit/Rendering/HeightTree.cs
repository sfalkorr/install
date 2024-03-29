﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

internal sealed class HeightTree : ILineTracker, IDisposable
{
    #region Constructor

    private readonly TextDocument    document;
    private          HeightTreeNode  root;
    private          WeakLineTracker weakLineTracker;

    public HeightTree(TextDocument document, double defaultLineHeight)
    {
        this.document     = document;
        weakLineTracker   = WeakLineTracker.Register(document, this);
        DefaultLineHeight = defaultLineHeight;
        RebuildDocument();
    }

    public void Dispose()
    {
        weakLineTracker?.Deregister();
        root            = null;
        weakLineTracker = null;
    }

    public bool IsDisposed => root == null;

    private double defaultLineHeight;

    public double DefaultLineHeight
    {
        get => defaultLineHeight;
        set
        {
            var oldValue = defaultLineHeight;
            if (oldValue == value) return;
            defaultLineHeight = value;
            foreach (var node in AllNodes)
                if (node.lineNode.height == oldValue)
                {
                    node.lineNode.height = value;
                    UpdateAugmentedData(node, UpdateAfterChildrenChangeRecursionMode.IfRequired);
                }
        }
    }

    private HeightTreeNode GetNode(IDocumentLine ls)
    {
        return GetNodeByIndex(ls.LineNumber - 1);
    }

    #endregion

    #region RebuildDocument

    void ILineTracker.ChangeComplete(DocumentChangeEventArgs e)
    {
    }

    void ILineTracker.SetLineLength(DocumentLine ls, int newTotalLength)
    {
    }

    public void RebuildDocument()
    {
        foreach (var s in GetAllCollapsedSections())
        {
            s.Start = null;
            s.End   = null;
        }

        var nodes                                              = new HeightTreeNode[document.LineCount];
        var lineNumber                                         = 0;
        foreach (var ls in document.Lines) nodes[lineNumber++] = new HeightTreeNode(ls, defaultLineHeight);
        Debug.Assert(nodes.Length > 0);
        var height = DocumentLineTree.GetTreeHeight(nodes.Length);
        Debug.WriteLine("HeightTree will have height: " + height);
        root       = BuildTree(nodes, 0, nodes.Length, height);
        root.color = BLACK;
#if DEBUG
        CheckProperties();
#endif
    }

    private HeightTreeNode BuildTree(HeightTreeNode[] nodes, int start, int end, int subtreeHeight)
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
        UpdateAugmentedData(node, UpdateAfterChildrenChangeRecursionMode.None);
        return node;
    }

    #endregion

    #region Insert/Remove lines

    void ILineTracker.BeforeRemoveLine(DocumentLine line)
    {
        var node = GetNode(line);
        if (node.lineNode.collapsedSections != null)
            foreach (var cs in node.lineNode.collapsedSections.ToArray())
                if (cs.Start == line && cs.End == line)
                {
                    cs.Start = null;
                    cs.End   = null;
                }
                else if (cs.Start == line)
                {
                    Uncollapse(cs);
                    cs.Start = line.NextLine;
                    AddCollapsedSection(cs, cs.End.LineNumber - cs.Start.LineNumber + 1);
                }
                else if (cs.End == line)
                {
                    Uncollapse(cs);
                    cs.End = line.PreviousLine;
                    AddCollapsedSection(cs, cs.End.LineNumber - cs.Start.LineNumber + 1);
                }

        BeginRemoval();
        RemoveNode(node);
        node.lineNode.collapsedSections = null;
        EndRemoval();
    }

    void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
    {
        InsertAfter(GetNode(insertionPos), newLine);
#if DEBUG
        CheckProperties();
#endif
    }

    private HeightTreeNode InsertAfter(HeightTreeNode node, DocumentLine newLine)
    {
        var newNode = new HeightTreeNode(newLine, defaultLineHeight);
        if (node.right == null)
        {
            if (node.lineNode.collapsedSections != null)
                foreach (var cs in node.lineNode.collapsedSections.Where(cs => cs.End != node.documentLine))
                    newNode.AddDirectlyCollapsed(cs);

            InsertAsRight(node, newNode);
        }
        else
        {
            node = node.right.LeftMost;
            if (node.lineNode.collapsedSections != null)
                foreach (var cs in node.lineNode.collapsedSections)
                    if (cs.Start != node.documentLine)
                        newNode.AddDirectlyCollapsed(cs);

            InsertAsLeft(node, newNode);
        }

        return newNode;
    }

    #endregion

    #region Rotation callbacks

    private enum UpdateAfterChildrenChangeRecursionMode
    {
        None,
        IfRequired,
        WholeBranch
    }

    private static void UpdateAfterChildrenChange(HeightTreeNode node)
    {
        UpdateAugmentedData(node, UpdateAfterChildrenChangeRecursionMode.IfRequired);
    }

    private static void UpdateAugmentedData(HeightTreeNode node, UpdateAfterChildrenChangeRecursionMode mode)
    {
        var totalCount  = 1;
        var totalHeight = node.lineNode.TotalHeight;
        if (node.left != null)
        {
            totalCount  += node.left.totalCount;
            totalHeight += node.left.totalHeight;
        }

        if (node.right != null)
        {
            totalCount  += node.right.totalCount;
            totalHeight += node.right.totalHeight;
        }

        if (node.IsDirectlyCollapsed) totalHeight = 0;
        if (totalCount != node.totalCount || !totalHeight.IsClose(node.totalHeight) || mode == UpdateAfterChildrenChangeRecursionMode.WholeBranch)
        {
            node.totalCount  = totalCount;
            node.totalHeight = totalHeight;
            if (node.parent != null && mode != UpdateAfterChildrenChangeRecursionMode.None) UpdateAugmentedData(node.parent, mode);
        }
    }

    private void UpdateAfterRotateLeft(HeightTreeNode node)
    {
        var collapsedP = node.parent.collapsedSections;
        var collapsedQ = node.collapsedSections;
        node.parent.collapsedSections = collapsedQ;
        node.collapsedSections        = null;
        if (collapsedP != null)
            foreach (var cs in collapsedP)
            {
                if (node.parent.right != null) node.parent.right.AddDirectlyCollapsed(cs);
                node.parent.lineNode.AddDirectlyCollapsed(cs);
                if (node.right != null) node.right.AddDirectlyCollapsed(cs);
            }

        MergeCollapsedSectionsIfPossible(node);

        UpdateAfterChildrenChange(node);
    }

    private void UpdateAfterRotateRight(HeightTreeNode node)
    {
        var collapsedP = node.parent.collapsedSections;
        var collapsedQ = node.collapsedSections;
        node.parent.collapsedSections = collapsedQ;
        node.collapsedSections        = null;
        if (collapsedP != null)
            foreach (var cs in collapsedP)
            {
                if (node.parent.left != null) node.parent.left.AddDirectlyCollapsed(cs);
                node.parent.lineNode.AddDirectlyCollapsed(cs);
                if (node.left != null) node.left.AddDirectlyCollapsed(cs);
            }

        MergeCollapsedSectionsIfPossible(node);

        UpdateAfterChildrenChange(node);
    }

    private void BeforeNodeRemove(HeightTreeNode removedNode)
    {
        Debug.Assert(removedNode.left == null || removedNode.right == null);

        var collapsed = removedNode.collapsedSections;
        if (collapsed != null)
        {
            var childNode = removedNode.left ?? removedNode.right;
            if (childNode != null)
                foreach (var cs in collapsed)
                    childNode.AddDirectlyCollapsed(cs);
        }

        if (removedNode.parent != null) MergeCollapsedSectionsIfPossible(removedNode.parent);
    }

    private void BeforeNodeReplace(HeightTreeNode removedNode, HeightTreeNode newNode, HeightTreeNode newNodeOldParent)
    {
        Debug.Assert(removedNode != null);
        Debug.Assert(newNode != null);
        while (newNodeOldParent != removedNode)
        {
            if (newNodeOldParent.collapsedSections != null)
                foreach (var cs in newNodeOldParent.collapsedSections)
                    newNode.lineNode.AddDirectlyCollapsed(cs);

            newNodeOldParent = newNodeOldParent.parent;
        }

        if (newNode.collapsedSections != null)
            foreach (var cs in newNode.collapsedSections)
                newNode.lineNode.AddDirectlyCollapsed(cs);

        newNode.collapsedSections = removedNode.collapsedSections;
        MergeCollapsedSectionsIfPossible(newNode);
    }

    private bool                 inRemoval;
    private List<HeightTreeNode> nodesToCheckForMerging;

    private void BeginRemoval()
    {
        Debug.Assert(!inRemoval);
        nodesToCheckForMerging ??= new List<HeightTreeNode>();
        inRemoval              =   true;
    }

    private void EndRemoval()
    {
        Debug.Assert(inRemoval);
        inRemoval = false;
        foreach (var node in nodesToCheckForMerging) MergeCollapsedSectionsIfPossible(node);
        nodesToCheckForMerging.Clear();
    }

    private void MergeCollapsedSectionsIfPossible(HeightTreeNode node)
    {
        Debug.Assert(node != null);
        if (inRemoval)
        {
            nodesToCheckForMerging.Add(node);
            return;
        }

        var merged     = false;
        var collapsedL = node.lineNode.collapsedSections;
        if (collapsedL != null)
        {
            for (var i = collapsedL.Count - 1; i >= 0; i--)
            {
                var cs = collapsedL[i];
                if (cs.Start == node.documentLine || cs.End == node.documentLine) continue;
                if (node.left == null || (node.left.collapsedSections != null && node.left.collapsedSections.Contains(cs)))
                    if (node.right == null || (node.right.collapsedSections != null && node.right.collapsedSections.Contains(cs)))
                    {
                        node.left?.RemoveDirectlyCollapsed(cs);
                        node.right?.RemoveDirectlyCollapsed(cs);
                        collapsedL.RemoveAt(i);
                        node.AddDirectlyCollapsed(cs);
                        merged = true;
                    }
            }

            if (collapsedL.Count == 0) node.lineNode.collapsedSections = null;
        }

        if (merged && node.parent != null) MergeCollapsedSectionsIfPossible(node.parent);
    }

    #endregion

    #region GetNodeBy... / Get...FromNode

    private HeightTreeNode GetNodeByIndex(int index)
    {
        Debug.Assert(index >= 0);
        Debug.Assert(index < root.totalCount);
        var node = root;
        while (true)
            if (node.left != null && index < node.left.totalCount)
            {
                node = node.left;
            }
            else
            {
                if (node.left != null) index -= node.left.totalCount;
                if (index == 0) return node;
                index--;
                node = node.right;
            }
    }

    private HeightTreeNode GetNodeByVisualPosition(double position)
    {
        var node = root;
        while (true)
        {
            var positionAfterLeft = position;
            if (node.left != null)
            {
                positionAfterLeft -= node.left.totalHeight;
                if (positionAfterLeft < 0)
                {
                    node = node.left;
                    continue;
                }
            }

            var positionBeforeRight = positionAfterLeft - node.lineNode.TotalHeight;
            if (positionBeforeRight < 0) return node;
            if (node.right == null || node.right.totalHeight == 0)
            {
                if (node.lineNode.TotalHeight > 0 || node.left == null) return node;
                node = node.left;
            }
            else
            {
                position = positionBeforeRight;
                node     = node.right;
            }
        }
    }

    private static double GetVisualPositionFromNode(HeightTreeNode node)
    {
        var position = node.left?.totalHeight ?? 0;
        while (node.parent != null)
        {
            if (node.IsDirectlyCollapsed) position = 0;
            if (node == node.parent.right)
            {
                if (node.parent.left != null) position += node.parent.left.totalHeight;
                position += node.parent.lineNode.TotalHeight;
            }

            node = node.parent;
        }

        return position;
    }

    #endregion

    #region Public methods

    public DocumentLine GetLineByNumber(int number)
    {
        return GetNodeByIndex(number - 1).documentLine;
    }

    public DocumentLine GetLineByVisualPosition(double position)
    {
        return GetNodeByVisualPosition(position).documentLine;
    }

    public double GetVisualPosition(DocumentLine line)
    {
        return GetVisualPositionFromNode(GetNode(line));
    }

    public double GetHeight(DocumentLine line)
    {
        return GetNode(line).lineNode.height;
    }

    public void SetHeight(DocumentLine line, double val)
    {
        var node = GetNode(line);
        node.lineNode.height = val;
        UpdateAfterChildrenChange(node);
    }

    public bool GetIsCollapsed(int lineNumber)
    {
        var node = GetNodeByIndex(lineNumber - 1);
        return node.lineNode.IsDirectlyCollapsed || GetIsCollapedFromNode(node);
    }

    public CollapsedLineSection CollapseText(DocumentLine start, DocumentLine end)
    {
        if (!document.Lines.Contains(start)) throw new ArgumentException("Line is not part of this document", nameof(start));
        if (!document.Lines.Contains(end)) throw new ArgumentException("Line is not part of this document", nameof(end));
        var length = end.LineNumber - start.LineNumber + 1;
        if (length < 0) throw new ArgumentException("start must be a line before end");
        var section = new CollapsedLineSection(this, start, end);
        AddCollapsedSection(section, length);
#if DEBUG
        CheckProperties();
#endif
        return section;
    }

    #endregion

    #region LineCount & TotalHeight

    public int LineCount => root.totalCount;

    public double TotalHeight => root.totalHeight;

    #endregion

    #region GetAllCollapsedSections

    private IEnumerable<HeightTreeNode> AllNodes
    {
        get
        {
            if (root != null)
            {
                var node = root.LeftMost;
                while (node != null)
                {
                    yield return node;
                    node = node.Successor;
                }
            }
        }
    }

    internal IEnumerable<CollapsedLineSection> GetAllCollapsedSections()
    {
        var emptyCSList = new List<CollapsedLineSection>();
        return AllNodes.SelectMany(node => (node.lineNode.collapsedSections ?? emptyCSList).Concat(node.collapsedSections ?? emptyCSList)).Distinct();
    }

    #endregion

    #region CheckProperties

#if DEBUG
    [Conditional("DATACONSISTENCYTEST")]
    internal void CheckProperties()
    {
        CheckProperties(root);

        foreach (var cs in GetAllCollapsedSections())
        {
            Debug.Assert(GetNode(cs.Start).lineNode.collapsedSections.Contains(cs));
            Debug.Assert(GetNode(cs.End).lineNode.collapsedSections.Contains(cs));
            var endLine = cs.End.LineNumber;
            for (var i = cs.Start.LineNumber; i <= endLine; i++) CheckIsInSection(cs, GetLineByNumber(i));
        }

        // check red-black property:
        var blackCount = -1;
        CheckNodeProperties(root, null, RED, 0, ref blackCount);
    }

    private void CheckIsInSection(CollapsedLineSection cs, DocumentLine line)
    {
        var node = GetNode(line);
        if (node.lineNode.collapsedSections != null && node.lineNode.collapsedSections.Contains(cs)) return;
        while (node != null)
        {
            if (node.collapsedSections != null && node.collapsedSections.Contains(cs)) return;
            node = node.parent;
        }

        throw new InvalidOperationException(cs + " not found for line " + line);
    }

    private void CheckProperties(HeightTreeNode node)
    {
        var totalCount = 1;
        var totalHeight = node.lineNode.TotalHeight;
        if (node.lineNode.IsDirectlyCollapsed) Debug.Assert(node.lineNode.collapsedSections.Count > 0);
        if (node.left != null)
        {
            CheckProperties(node.left);
            totalCount += node.left.totalCount;
            totalHeight += node.left.totalHeight;

            CheckAllContainedIn(node.left.collapsedSections, node.lineNode.collapsedSections);
        }

        if (node.right != null)
        {
            CheckProperties(node.right);
            totalCount += node.right.totalCount;
            totalHeight += node.right.totalHeight;

            CheckAllContainedIn(node.right.collapsedSections, node.lineNode.collapsedSections);
        }

        if (node.left != null && node.right != null)
            if (node.left.collapsedSections != null && node.right.collapsedSections != null)
            {
                var intersection = node.left.collapsedSections.Intersect(node.right.collapsedSections);
                Debug.Assert(!intersection.Any());
            }

        if (node.IsDirectlyCollapsed)
        {
            Debug.Assert(node.collapsedSections.Count > 0);
            totalHeight = 0;
        }

        Debug.Assert(node.totalCount == totalCount);
        Debug.Assert(node.totalHeight.IsClose(totalHeight));
    }

    private static void CheckAllContainedIn(IEnumerable<CollapsedLineSection> list1, ICollection<CollapsedLineSection> list2)
    {
        list1 ??= new List<CollapsedLineSection>();
        list2 ??= new List<CollapsedLineSection>();
        foreach (var cs in list1) Debug.Assert(list2.Contains(cs));
    }

    /*
    1. A node is either red or black.
    2. The root is black.
    3. All leaves are black. (The leaves are the NIL children.)
    4. Both children of every red node are black. (So every red node must have a black parent.)
    5. Every simple path from a node to a descendant leaf contains the same number of black nodes. (Not counting the leaf node.)
     */
    private void CheckNodeProperties(HeightTreeNode node, HeightTreeNode parentNode, bool parentColor, int blackCount, ref int expectedBlackCount)
    {
        while (true)
        {
            if (node == null) return;

            Debug.Assert(node.parent == parentNode);

            if (parentColor == RED) Debug.Assert(node.color == BLACK);
            if (node.color == BLACK) blackCount++;
            if (node.left == null && node.right == null)
            {
                // node is a leaf node:
                if (expectedBlackCount == -1) expectedBlackCount = blackCount;
                else Debug.Assert(expectedBlackCount == blackCount);
            }

            CheckNodeProperties(node.left, node, node.color, blackCount, ref expectedBlackCount);
            var node1 = node;
            node = node.right;
            parentNode = node1;
            parentColor = node1.color;
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public string GetTreeAsString()
    {
        var b = new StringBuilder();
        AppendTreeToString(root, b, 0);
        return b.ToString();
    }

    private static void AppendTreeToString(HeightTreeNode node, StringBuilder b, int indent)
    {
        while (true)
        {
            b.Append(node.color == RED ? "RED   " : "BLACK ");
            b.AppendLine(node.ToString());
            indent += 2;
            if (node.left != null)
            {
                b.Append(' ', indent);
                b.Append("L: ");
                AppendTreeToString(node.left, b, indent);
            }

            if (node.right == null) return;
            b.Append(' ', indent);
            b.Append("R: ");
            node = node.right;
        }
    }
#endif

    #endregion

    #region Red/Black Tree

    private const bool RED   = true;
    private const bool BLACK = false;

    private void InsertAsLeft(HeightTreeNode parentNode, HeightTreeNode newNode)
    {
        Debug.Assert(parentNode.left == null);
        parentNode.left = newNode;
        newNode.parent  = parentNode;
        newNode.color   = RED;
        UpdateAfterChildrenChange(parentNode);
        FixTreeOnInsert(newNode);
    }

    private void InsertAsRight(HeightTreeNode parentNode, HeightTreeNode newNode)
    {
        Debug.Assert(parentNode.right == null);
        parentNode.right = newNode;
        newNode.parent   = parentNode;
        newNode.color    = RED;
        UpdateAfterChildrenChange(parentNode);
        FixTreeOnInsert(newNode);
    }

    private void FixTreeOnInsert(HeightTreeNode node)
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

    private void RemoveNode(HeightTreeNode removedNode)
    {
        if (removedNode.left != null && removedNode.right != null)
        {
            var leftMost         = removedNode.right.LeftMost;
            var parentOfLeftMost = leftMost.parent;
            RemoveNode(leftMost);

            BeforeNodeReplace(removedNode, leftMost, parentOfLeftMost);
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
        BeforeNodeRemove(removedNode);
        ReplaceNode(removedNode, childNode);
        if (parentNode != null) UpdateAfterChildrenChange(parentNode);
        if (removedNode.color != BLACK) return;
        if (childNode is { color: RED }) childNode.color = BLACK;
        else FixTreeOnDelete(childNode, parentNode);
    }

    private void FixTreeOnDelete(HeightTreeNode node, HeightTreeNode parentNode)
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

        switch (parentNode.color)
        {
            case BLACK when sibling.color == BLACK && GetColor(sibling.left) == BLACK && GetColor(sibling.right) == BLACK:
                sibling.color = RED;
                FixTreeOnDelete(parentNode, parentNode.parent);
                return;
            case RED when sibling.color == BLACK && GetColor(sibling.left) == BLACK && GetColor(sibling.right) == BLACK:
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

    private void ReplaceNode(HeightTreeNode replacedNode, HeightTreeNode newNode)
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

    private void RotateLeft(HeightTreeNode p)
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

    private void RotateRight(HeightTreeNode p)
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

    private static HeightTreeNode Sibling(HeightTreeNode node)
    {
        return node == node.parent.left ? node.parent.right : node.parent.left;
    }

    private static HeightTreeNode Sibling(HeightTreeNode node, HeightTreeNode parentNode)
    {
        Debug.Assert(node == null || node.parent == parentNode);
        return node == parentNode.left ? parentNode.right : parentNode.left;
    }

    private static bool GetColor(HeightTreeNode node)
    {
        return node?.color ?? BLACK;
    }

    #endregion

    #region Collapsing support

    private static bool GetIsCollapedFromNode(HeightTreeNode node)
    {
        while (node != null)
        {
            if (node.IsDirectlyCollapsed) return true;
            node = node.parent;
        }

        return false;
    }

    internal void AddCollapsedSection(CollapsedLineSection section, int sectionLength)
    {
        AddRemoveCollapsedSection(section, sectionLength, true);
    }

    private void AddRemoveCollapsedSection(CollapsedLineSection section, int sectionLength, bool add)
    {
        Debug.Assert(sectionLength > 0);

        var node = GetNode(section.Start);
        while (true)
        {
            if (add) node.lineNode.AddDirectlyCollapsed(section);
            else node.lineNode.RemoveDirectlyCollapsed(section);
            sectionLength -= 1;
            if (sectionLength == 0)
            {
                Debug.Assert(node.documentLine == section.End);
                break;
            }

            if (node.right != null)
            {
                if (node.right.totalCount < sectionLength)
                {
                    if (add) node.right.AddDirectlyCollapsed(section);
                    else node.right.RemoveDirectlyCollapsed(section);
                    sectionLength -= node.right.totalCount;
                }
                else
                {
                    AddRemoveCollapsedSectionDown(section, node.right, sectionLength, add);
                    break;
                }
            }

            var parentNode = node.parent;
            Debug.Assert(parentNode != null);
            while (parentNode.right == node)
            {
                node       = parentNode;
                parentNode = node.parent;
                Debug.Assert(parentNode != null);
            }

            node = parentNode;
        }

        UpdateAugmentedData(GetNode(section.Start), UpdateAfterChildrenChangeRecursionMode.WholeBranch);
        UpdateAugmentedData(GetNode(section.End), UpdateAfterChildrenChangeRecursionMode.WholeBranch);
    }

    private static void AddRemoveCollapsedSectionDown(CollapsedLineSection section, HeightTreeNode node, int sectionLength, bool add)
    {
        while (true)
        {
            if (node.left != null)
            {
                if (node.left.totalCount < sectionLength)
                {
                    if (add) node.left.AddDirectlyCollapsed(section);
                    else node.left.RemoveDirectlyCollapsed(section);
                    sectionLength -= node.left.totalCount;
                }
                else
                {
                    node = node.left;
                    Debug.Assert(node != null);
                    continue;
                }
            }

            if (add) node.lineNode.AddDirectlyCollapsed(section);
            else node.lineNode.RemoveDirectlyCollapsed(section);
            sectionLength -= 1;
            if (sectionLength == 0)
            {
                Debug.Assert(node.documentLine == section.End);
                break;
            }

            node = node.right;
            Debug.Assert(node != null);
        }
    }

    public void Uncollapse(CollapsedLineSection section)
    {
        var sectionLength = section.End.LineNumber - section.Start.LineNumber + 1;
        AddRemoveCollapsedSection(section, sectionLength, false);
    }

    #endregion
}
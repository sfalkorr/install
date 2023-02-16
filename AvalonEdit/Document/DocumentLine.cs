using System;
using System.Diagnostics;
using System.Globalization;

namespace AvalonEdit.Document;

public sealed partial class DocumentLine : IDocumentLine
{
    #region Constructor

#if DEBUG
    // Required for thread safety check which is done only in debug builds.
    // To save space, we don't store the document reference in release builds as we don't need it there.
    private readonly TextDocument document;
#endif

    internal bool isDeleted;

    internal DocumentLine(TextDocument document)
    {
#if DEBUG
        Debug.Assert(document != null);
        this.document = document;
#endif
    }

    [Conditional("DEBUG")]
    private void DebugVerifyAccess()
    {
#if DEBUG
        document.DebugVerifyAccess();
#endif
    }

    #endregion

    #region Events

    #endregion

    #region Properties stored in tree

    public bool IsDeleted
    {
        get
        {
            DebugVerifyAccess();
            return isDeleted;
        }
    }

    public int LineNumber
    {
        get
        {
            if (IsDeleted) throw new InvalidOperationException();
            return DocumentLineTree.GetIndexFromNode(this) + 1;
        }
    }

    public int Offset
    {
        get
        {
            if (IsDeleted) throw new InvalidOperationException();
            return DocumentLineTree.GetOffsetFromNode(this);
        }
    }

    public int EndOffset => Offset + Length;

    #endregion

    #region Length

    private int  totalLength;
    private byte delimiterLength;

    public int Length
    {
        get
        {
            DebugVerifyAccess();
            return totalLength - delimiterLength;
        }
    }

    public int TotalLength
    {
        get
        {
            DebugVerifyAccess();
            return totalLength;
        }
        internal set => totalLength = value;
    }

    public int DelimiterLength
    {
        get
        {
            DebugVerifyAccess();
            return delimiterLength;
        }
        internal set
        {
            Debug.Assert(value is >= 0 and <= 2);
            delimiterLength = (byte)value;
        }
    }

    #endregion

    #region Previous / Next Line

    public DocumentLine NextLine
    {
        get
        {
            DebugVerifyAccess();

            if (right != null) return right.LeftMost;

            var          node = this;
            DocumentLine oldNode;
            do
            {
                oldNode = node;
                node    = node.parent;
            }
            while (node != null && node.right == oldNode);

            return node;
        }
    }

    public DocumentLine PreviousLine
    {
        get
        {
            DebugVerifyAccess();

            if (left != null) return left.RightMost;

            var          node = this;
            DocumentLine oldNode;
            do
            {
                oldNode = node;
                node    = node.parent;
            }
            while (node != null && node.left == oldNode);

            return node;
        }
    }

    IDocumentLine IDocumentLine.NextLine => NextLine;

    IDocumentLine IDocumentLine.PreviousLine => PreviousLine;

    #endregion

    #region ToString

    public override string ToString()
    {
        return IsDeleted ? "[DocumentLine deleted]" : string.Format(CultureInfo.InvariantCulture, "[DocumentLine Number={0} Offset={1} Length={2}]", LineNumber, Offset, Length);
    }

    #endregion
}
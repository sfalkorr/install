namespace AvalonEdit.Document;

using LineNode = DocumentLine;

partial class DocumentLine
{
    internal DocumentLine left, right, parent;
    internal bool         color;


    internal void ResetLine()
    {
        totalLength = delimiterLength = 0;
        isDeleted   = color           = false;
        left        = right           = parent = null;
    }

    internal LineNode InitLineNode()
    {
        nodeTotalCount  = 1;
        nodeTotalLength = TotalLength;
        return this;
    }

    internal LineNode LeftMost
    {
        get
        {
            var node                       = this;
            while (node.left != null) node = node.left;
            return node;
        }
    }

    internal LineNode RightMost
    {
        get
        {
            var node                        = this;
            while (node.right != null) node = node.right;
            return node;
        }
    }

    internal int nodeTotalCount;

    internal int nodeTotalLength;
}
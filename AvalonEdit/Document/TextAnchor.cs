using System;
using AvalonEdit.Utils;

namespace AvalonEdit.Document;

public abstract class TextAnchor : ITextAnchor
{
    internal TextAnchorNode node;

    internal TextAnchor(TextDocument document)
    {
        Document = document;
    }

    public TextDocument Document { get; }

    public AnchorMovementType MovementType { get; set; }

    public bool SurviveDeletion { get; set; }

    public bool IsDeleted
    {
        get
        {
            Document.DebugVerifyAccess();
            return node == null;
        }
    }

    public event EventHandler Deleted;

    public int Offset
    {
        get
        {
            Document.DebugVerifyAccess();

            var n = node;
            if (n == null) throw new InvalidOperationException();

            var offset                 = n.length;
            if (n.left != null) offset += n.left.totalLength;
            while (n.parent != null)
            {
                if (n == n.parent.right)
                {
                    if (n.parent.left != null) offset += n.parent.left.totalLength;
                    offset += n.parent.length;
                }

                n = n.parent;
            }

            return offset;
        }
    }

    public int Line => Document.GetLineByOffset(Offset).LineNumber;

    public int Column
    {
        get
        {
            var offset = Offset;
            return offset - Document.GetLineByOffset(offset).Offset + 1;
        }
    }

    public TextLocation Location => Document.GetLocation(Offset);

    public override string ToString()
    {
        return "[TextAnchor Offset=" + Offset + "]";
    }

    protected virtual void OnDeleted()
    {
        Deleted?.Invoke(this, EventArgs.Empty);
    }
}
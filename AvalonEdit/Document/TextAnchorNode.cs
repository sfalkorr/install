using System;

namespace AvalonEdit.Document;

internal abstract class TextAnchorNode : WeakReference
{
    internal TextAnchorNode left, right, parent;
    internal bool           color;
    internal int            length;
    internal int            totalLength;

    protected TextAnchorNode(TextAnchor anchor) : base(anchor)
    {
    }


    public override string ToString()
    {
        return "[TextAnchorNode Length=" + length + " TotalLength=" + totalLength + " Target=" + Target + "]";
    }
}
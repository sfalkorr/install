using System.Diagnostics.CodeAnalysis;
using AvalonEdit.Document;

namespace AvalonEdit.Rendering;

public sealed class CollapsedLineSection
{
    private HeightTree heightTree;

#if DEBUG
    internal       string ID;
    private static int    nextId;
#else
    private const string ID = "";
#endif

    internal CollapsedLineSection(HeightTree heightTree, DocumentLine start, DocumentLine end)
    {
        this.heightTree = heightTree;
        Start           = start;
        End             = end;
#if DEBUG
        unchecked
        {
            ID = " #" + nextId++;
        }
#endif
    }

    public bool IsCollapsed => Start != null;

    public DocumentLine Start { get; internal set; }

    public DocumentLine End { get; internal set; }

    public void Uncollapse()
    {
        if (Start == null) return;

        if (!heightTree.IsDisposed)
        {
            heightTree.Uncollapse(this);
#if DEBUG
            heightTree.CheckProperties();
#endif
        }

        Start = null;
        End   = null;
    }

    [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
    public override string ToString()
    {
        return "[CollapsedSection" + ID + " Start=" + (Start != null ? Start.LineNumber.ToString() : "null") + " End=" + (End != null ? End.LineNumber.ToString() : "null") + "]";
    }
}
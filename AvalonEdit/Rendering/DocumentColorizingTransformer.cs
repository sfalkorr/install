using System;
using System.Linq;
using AvalonEdit.Document;

namespace AvalonEdit.Rendering;

public abstract class DocumentColorizingTransformer : ColorizingTransformer
{
    private DocumentLine currentDocumentLine;
    private int          firstLineStart;
    private int          currentDocumentLineStartOffset, currentDocumentLineEndOffset;

    protected ITextRunConstructionContext CurrentContext { get; private set; }

    protected override void Colorize(ITextRunConstructionContext context)
    {
        CurrentContext = context ?? throw new ArgumentNullException(nameof(context));

        currentDocumentLine          = context.VisualLine.FirstDocumentLine;
        firstLineStart               = currentDocumentLineStartOffset = currentDocumentLine.Offset;
        currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
        var currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;

        if (context.VisualLine.FirstDocumentLine == context.VisualLine.LastDocumentLine)
        {
            ColorizeLine(currentDocumentLine);
        }
        else
        {
            ColorizeLine(currentDocumentLine);
            foreach (var e in context.VisualLine.Elements.ToArray())
            {
                var elementOffset = firstLineStart + e.RelativeTextOffset;
                if (elementOffset < currentDocumentLineTotalEndOffset) continue;
                currentDocumentLine               = context.Document.GetLineByOffset(elementOffset);
                currentDocumentLineStartOffset    = currentDocumentLine.Offset;
                currentDocumentLineEndOffset      = currentDocumentLineStartOffset + currentDocumentLine.Length;
                currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;
                ColorizeLine(currentDocumentLine);
            }
        }

        currentDocumentLine = null;
        CurrentContext      = null;
    }

    protected abstract void ColorizeLine(DocumentLine line);


    protected void ChangeLinePart(int startOffset, int endOffset, Action<VisualLineElement> action)
    {
        if (startOffset < currentDocumentLineStartOffset || startOffset > currentDocumentLineEndOffset) throw new ArgumentOutOfRangeException(nameof(startOffset), startOffset, "Value must be between " + currentDocumentLineStartOffset + " and " + currentDocumentLineEndOffset);
        if (endOffset < startOffset || endOffset > currentDocumentLineEndOffset) throw new ArgumentOutOfRangeException(nameof(endOffset), endOffset, "Value must be between " + startOffset + " and " + currentDocumentLineEndOffset);
        var vl          = CurrentContext.VisualLine;
        var visualStart = vl.GetVisualColumn(startOffset - firstLineStart);
        var visualEnd   = vl.GetVisualColumn(endOffset - firstLineStart);
        if (visualStart < visualEnd) ChangeVisualElements(visualStart, visualEnd, action);
    }
}
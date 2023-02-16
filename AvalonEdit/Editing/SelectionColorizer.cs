using System;
using AvalonEdit.Rendering;

namespace AvalonEdit.Editing;

internal sealed class SelectionColorizer : ColorizingTransformer
{
    private TextArea textArea;

    public SelectionColorizer(TextArea textArea)
    {
        this.textArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
    }

    protected override void Colorize(ITextRunConstructionContext context)
    {
        if (textArea.SelectionForeground == null) return;

        var lineStartOffset = context.VisualLine.FirstDocumentLine.Offset;
        var lineEndOffset   = context.VisualLine.LastDocumentLine.Offset + context.VisualLine.LastDocumentLine.TotalLength;

        foreach (var segment in textArea.Selection.Segments)
        {
            var segmentStart = segment.StartOffset;
            var segmentEnd   = segment.EndOffset;
            if (segmentEnd <= lineStartOffset) continue;
            if (segmentStart >= lineEndOffset) continue;
            var startColumn = segmentStart < lineStartOffset ? 0 : context.VisualLine.ValidateVisualColumn(segment.StartOffset, segment.StartVisualColumn, textArea.Selection.EnableVirtualSpace);

            int endColumn;
            if (segmentEnd > lineEndOffset) endColumn = textArea.Selection.EnableVirtualSpace ? int.MaxValue : context.VisualLine.VisualLengthWithEndOfLineMarker;
            else endColumn                            = context.VisualLine.ValidateVisualColumn(segment.EndOffset, segment.EndVisualColumn, textArea.Selection.EnableVirtualSpace);

            ChangeVisualElements(startColumn, endColumn, element =>
            {
                element.TextRunProperties.SetForegroundBrush(textArea.SelectionForeground);
            });
        }
    }
}
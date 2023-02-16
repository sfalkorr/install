using System;
using System.Collections.Generic;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

internal sealed class SimpleSelection : Selection
{
    private readonly int startOffset, endOffset;

    internal SimpleSelection(TextArea textArea, TextViewPosition start, TextViewPosition end) : base(textArea)
    {
        StartPosition = start;
        EndPosition   = end;
        startOffset   = textArea.Document.GetOffset(start.Location);
        endOffset     = textArea.Document.GetOffset(end.Location);
    }

    public override IEnumerable<SelectionSegment> Segments => ExtensionMethods.Sequence(new SelectionSegment(startOffset, StartPosition.VisualColumn, endOffset, EndPosition.VisualColumn));

    public override ISegment SurroundingSegment => new SelectionSegment(startOffset, endOffset);

    public override void ReplaceSelectionWithText(string newText)
    {
        if (newText == null) throw new ArgumentNullException(nameof(newText));
        using (textArea.Document.RunUpdate())
        {
            var segmentsToDelete = textArea.GetDeletableSegments(SurroundingSegment);
            for (var i = segmentsToDelete.Length - 1; i >= 0; i--)
                if (i == segmentsToDelete.Length - 1)
                {
                    if (segmentsToDelete[i].Offset == SurroundingSegment.Offset && segmentsToDelete[i].Length == SurroundingSegment.Length) newText = AddSpacesIfRequired(newText, StartPosition, EndPosition);
                    if (string.IsNullOrEmpty(newText)) textArea.Caret.Position = StartPosition.CompareTo(EndPosition) <= 0 ? StartPosition : EndPosition;
                    else textArea.Caret.Offset                                 = segmentsToDelete[i].EndOffset;

                    textArea.Document.Replace(segmentsToDelete[i], newText);
                }
                else
                {
                    textArea.Document.Remove(segmentsToDelete[i]);
                }

            if (segmentsToDelete.Length != 0) textArea.ClearSelection();
        }
    }

    public override TextViewPosition StartPosition { get; }

    public override TextViewPosition EndPosition { get; }

    public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));
        int newStartOffset, newEndOffset;
        if (startOffset <= endOffset)
        {
            newStartOffset = e.GetNewOffset(startOffset);
            newEndOffset   = Math.Max(newStartOffset, e.GetNewOffset(endOffset, AnchorMovementType.BeforeInsertion));
        }
        else
        {
            newEndOffset   = e.GetNewOffset(endOffset);
            newStartOffset = Math.Max(newEndOffset, e.GetNewOffset(startOffset, AnchorMovementType.BeforeInsertion));
        }

        return Create(textArea, new TextViewPosition(textArea.Document.GetLocation(newStartOffset), StartPosition.VisualColumn), new TextViewPosition(textArea.Document.GetLocation(newEndOffset), EndPosition.VisualColumn));
    }

    public override bool IsEmpty => startOffset == endOffset && StartPosition.VisualColumn == EndPosition.VisualColumn;

    public override int Length => Math.Abs(endOffset - startOffset);

    public override Selection SetEndpoint(TextViewPosition endPosition)
    {
        return Create(textArea, StartPosition, endPosition);
    }

    public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
    {
        var document = textArea.Document;
        if (document == null) throw ThrowUtil.NoDocumentAssigned();
        return Create(textArea, StartPosition, endPosition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return startOffset * 27811 + endOffset + textArea.GetHashCode();
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is not SimpleSelection other) return false;
        return StartPosition.Equals(other.StartPosition) && EndPosition.Equals(other.EndPosition) && startOffset == other.startOffset && endOffset == other.endOffset && textArea == other.textArea;
    }

    public override string ToString()
    {
        return "[SimpleSelection Start=" + StartPosition + " End=" + EndPosition + "]";
    }
}
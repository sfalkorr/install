using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using AvalonEdit.Document;

namespace AvalonEdit.Rendering;

public abstract class VisualLineElement
{
    protected VisualLineElement(int visualLength, int documentLength)
    {
        if (visualLength < 1) throw new ArgumentOutOfRangeException(nameof(visualLength), visualLength, "Value must be at least 1");
        if (documentLength < 0) throw new ArgumentOutOfRangeException(nameof(documentLength), documentLength, "Value must be at least 0");
        VisualLength   = visualLength;
        DocumentLength = documentLength;
    }

    public int VisualLength { get; private set; }

    public int DocumentLength { get; private set; }

    [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "This property holds the start visual column, use GetVisualColumn to get inner visual columns.")]
    public int VisualColumn { get; internal set; }

    public int RelativeTextOffset { get; internal set; }

    public VisualLineElementTextRunProperties TextRunProperties { get; private set; }

    public Brush BackgroundBrush { get; set; }

    internal void SetTextRunProperties(VisualLineElementTextRunProperties p)
    {
        TextRunProperties = p;
    }
 
    public abstract TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context);

    public virtual TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int visualColumnLimit, ITextRunConstructionContext context)
    {
        return null;
    }

    public virtual bool CanSplit => false;

    public virtual void Split(int splitVisualColumn, IList<VisualLineElement> elements, int elementIndex)
    {
        throw new NotSupportedException();
    }

    protected void SplitHelper(VisualLineElement firstPart, VisualLineElement secondPart, int splitVisualColumn, int splitRelativeTextOffset)
    {
        if (firstPart == null) throw new ArgumentNullException(nameof(firstPart));
        if (secondPart == null) throw new ArgumentNullException(nameof(secondPart));
        var relativeSplitVisualColumn       = splitVisualColumn - VisualColumn;
        var relativeSplitRelativeTextOffset = splitRelativeTextOffset - RelativeTextOffset;

        if (relativeSplitVisualColumn <= 0 || relativeSplitVisualColumn >= VisualLength) throw new ArgumentOutOfRangeException(nameof(splitVisualColumn), splitVisualColumn, "Value must be between " + (VisualColumn + 1) + " and " + (VisualColumn + VisualLength - 1));
        if (relativeSplitRelativeTextOffset < 0 || relativeSplitRelativeTextOffset > DocumentLength) throw new ArgumentOutOfRangeException(nameof(splitRelativeTextOffset), splitRelativeTextOffset, "Value must be between " + RelativeTextOffset + " and " + (RelativeTextOffset + DocumentLength));
        var oldVisualLength       = VisualLength;
        var oldDocumentLength     = DocumentLength;
        var oldVisualColumn       = VisualColumn;
        var oldRelativeTextOffset = RelativeTextOffset;
        firstPart.VisualColumn        =   oldVisualColumn;
        secondPart.VisualColumn       =   oldVisualColumn + relativeSplitVisualColumn;
        firstPart.RelativeTextOffset  =   oldRelativeTextOffset;
        secondPart.RelativeTextOffset =   oldRelativeTextOffset + relativeSplitRelativeTextOffset;
        firstPart.VisualLength        =   relativeSplitVisualColumn;
        secondPart.VisualLength       =   oldVisualLength - relativeSplitVisualColumn;
        firstPart.DocumentLength      =   relativeSplitRelativeTextOffset;
        secondPart.DocumentLength     =   oldDocumentLength - relativeSplitRelativeTextOffset;
        firstPart.TextRunProperties   ??= TextRunProperties.Clone();
        secondPart.TextRunProperties  ??= TextRunProperties.Clone();
        firstPart.BackgroundBrush     =   BackgroundBrush;
        secondPart.BackgroundBrush    =   BackgroundBrush;
    }

    public virtual int GetVisualColumn(int relativeTextOffset)
    {
        if (relativeTextOffset >= RelativeTextOffset + DocumentLength) return VisualColumn + VisualLength;
        return VisualColumn;
    }

    public virtual int GetRelativeOffset(int visualColumn)
    {
        if (visualColumn >= VisualColumn + VisualLength) return RelativeTextOffset + DocumentLength;
        return RelativeTextOffset;
    }

    public virtual int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
    {
        var stop1 = VisualColumn;
        var stop2 = VisualColumn + VisualLength;
        if (direction == LogicalDirection.Backward)
        {
            if (visualColumn > stop2 && mode != CaretPositioningMode.WordStart && mode != CaretPositioningMode.WordStartOrSymbol) return stop2;
            if (visualColumn > stop1) return stop1;
        }
        else
        {
            if (visualColumn < stop1) return stop1;
            if (visualColumn < stop2 && mode != CaretPositioningMode.WordStart && mode != CaretPositioningMode.WordStartOrSymbol) return stop2;
        }

        return -1;
    }

    public virtual bool IsWhitespace(int visualColumn)
    {
        return false;
    }

    public virtual bool HandlesLineBorders => false;

    protected internal virtual void OnQueryCursor(QueryCursorEventArgs e)
    {
    }

    protected internal virtual void OnMouseDown(MouseButtonEventArgs e)
    {
    }

    protected internal virtual void OnMouseUp(MouseButtonEventArgs e)
    {
    }
}
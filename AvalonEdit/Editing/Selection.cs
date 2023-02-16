using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using AvalonEdit.Document;
using AvalonEdit.Utils;
using AvalonEdit.Highlighting;

namespace AvalonEdit.Editing;

public abstract class Selection
{
    public static Selection Create(TextArea textArea, int startOffset, int endOffset)
    {
        if (textArea == null) throw new ArgumentNullException(nameof(textArea));
        return startOffset == endOffset ? textArea.emptySelection : new SimpleSelection(textArea, new TextViewPosition(textArea.Document.GetLocation(startOffset)), new TextViewPosition(textArea.Document.GetLocation(endOffset)));
    }

    internal static Selection Create(TextArea textArea, TextViewPosition start, TextViewPosition end)
    {
        if (textArea == null) throw new ArgumentNullException(nameof(textArea));
        if (textArea.Document.GetOffset(start.Location) == textArea.Document.GetOffset(end.Location) && start.VisualColumn == end.VisualColumn) return textArea.emptySelection;
        return new SimpleSelection(textArea, start, end);
    }

    public static Selection Create(TextArea textArea, ISegment segment)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        return Create(textArea, segment.Offset, segment.EndOffset);
    }

    internal readonly TextArea textArea;

    protected Selection(TextArea textArea)
    {
        this.textArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
    }

    public abstract TextViewPosition StartPosition { get; }

    public abstract TextViewPosition EndPosition { get; }

    public abstract IEnumerable<SelectionSegment> Segments { get; }

    public abstract ISegment SurroundingSegment { get; }

    public abstract void ReplaceSelectionWithText(string newText);

    internal string AddSpacesIfRequired(string newText, TextViewPosition start, TextViewPosition end)
    {
        if (EnableVirtualSpace && InsertVirtualSpaces(newText, start, end))
        {
            var line     = textArea.Document.GetLineByNumber(start.Line);
            var lineText = textArea.Document.GetText(line);
            var vLine    = textArea.TextView.GetOrConstructVisualLine(line);
            var colDiff  = start.VisualColumn - vLine.VisualLengthWithEndOfLineMarker;
            if (colDiff > 0)
            {
                var additionalSpaces = "";
                if (!textArea.Options.ConvertTabsToSpaces && lineText.Trim('\t').Length == 0)
                {
                    var tabCount = colDiff / textArea.Options.IndentationSize;
                    additionalSpaces =  new string('\t', tabCount);
                    colDiff          -= tabCount * textArea.Options.IndentationSize;
                }

                additionalSpaces += new string(' ', colDiff);
                return additionalSpaces + newText;
            }
        }

        return newText;
    }

    private bool InsertVirtualSpaces(string newText, TextViewPosition start, TextViewPosition end)
    {
        return (!string.IsNullOrEmpty(newText) || !(IsInVirtualSpace(start) && IsInVirtualSpace(end))) && newText != "\r\n" && newText != "\n" && newText != "\r";
    }

    private bool IsInVirtualSpace(TextViewPosition pos)
    {
        return pos.VisualColumn > textArea.TextView.GetOrConstructVisualLine(textArea.Document.GetLineByNumber(pos.Line)).VisualLength;
    }

    public abstract Selection UpdateOnDocumentChange(DocumentChangeEventArgs e);

    public virtual bool IsEmpty => Length == 0;

    public virtual bool EnableVirtualSpace => textArea.Options.EnableVirtualSpace;

    public abstract int Length { get; }

    public abstract Selection SetEndpoint(TextViewPosition endPosition);

    public abstract Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition);

    public virtual bool IsMultiline
    {
        get
        {
            var surroundingSegment = SurroundingSegment;
            if (surroundingSegment == null) return false;
            var start    = surroundingSegment.Offset;
            var end      = start + surroundingSegment.Length;
            var document = textArea.Document;
            if (document == null) throw ThrowUtil.NoDocumentAssigned();
            return document.GetLineByOffset(start) != document.GetLineByOffset(end);
        }
    }

    public virtual string GetText()
    {
        var document = textArea.Document;
        if (document == null) throw ThrowUtil.NoDocumentAssigned();
        StringBuilder b    = null;
        string        text = null;
        foreach (var s in Segments)
        {
            if (text != null)
            {
                if (b == null) b = new StringBuilder(text);
                else b.Append(text);
            }

            text = document.GetText(s);
        }

        if (b == null) return text ?? string.Empty;
        if (text != null) b.Append(text);
        return b.ToString();
    }


    public abstract override bool Equals(object obj);

    public abstract override int GetHashCode();

    public virtual bool Contains(int offset)
    {
        if (IsEmpty) return false;
        return SurroundingSegment.Contains(offset, 0) && Segments.Cast<ISegment>().Any(s => s.Contains(offset, 0));
    }

    public virtual DataObject CreateDataObject(TextArea textArea)
    {
        var data = new DataObject();

        var text = TextUtilities.NormalizeNewLines(GetText(), Environment.NewLine);

        return data;
    }
}
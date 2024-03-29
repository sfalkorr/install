﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.Windows.Documents;
using AvalonEdit.Document;
using AvalonEdit.Rendering;

namespace AvalonEdit.Editing;

internal class TextRangeProvider : ITextRangeProvider
{
    private readonly TextArea     textArea;
    private readonly TextDocument doc;
    private          ISegment     segment;

    public TextRangeProvider(TextArea textArea, TextDocument doc, ISegment segment)
    {
        this.textArea = textArea;
        this.doc      = doc;
        this.segment  = segment;
    }

    public TextRangeProvider(TextArea textArea, TextDocument doc, int offset, int length)
    {
        this.textArea = textArea;
        this.doc      = doc;
        segment       = new AnchorSegment(doc, offset, length);
    }

    private string ID => $"({GetHashCode():x8}: {segment})";

    [Conditional("DEBUG")]
    private static void Log(string format, params object[] args)
    {
        Debug.WriteLine(format, args);
    }

    public void AddToSelection()
    {
        Log("{0}.AddToSelection()", ID);
    }

    public ITextRangeProvider Clone()
    {
        var result = new TextRangeProvider(textArea, doc, segment);
        Log("{0}.Clone() = {1}", ID, result.ID);
        return result;
    }

    public bool Compare(ITextRangeProvider range)
    {
        var other  = (TextRangeProvider)range;
        var result = doc == other.doc && segment.Offset == other.segment.Offset && segment.EndOffset == other.segment.EndOffset;
        Log("{0}.Compare({1}) = {2}", ID, other.ID, result);
        return result;
    }

    private int GetEndpoint(TextPatternRangeEndpoint endpoint)
    {
        switch (endpoint)
        {
            case TextPatternRangeEndpoint.Start:
                return segment.Offset;
            case TextPatternRangeEndpoint.End:
                return segment.EndOffset;
            default:
                throw new ArgumentOutOfRangeException(nameof(endpoint));
        }
    }

    public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
    {
        var other  = (TextRangeProvider)targetRange;
        var result = GetEndpoint(endpoint).CompareTo(other.GetEndpoint(targetEndpoint));
        Log("{0}.CompareEndpoints({1}, {2}, {3}) = {4}", ID, endpoint, other.ID, targetEndpoint, result);
        return result;
    }

    public void ExpandToEnclosingUnit(TextUnit unit)
    {
        Log("{0}.ExpandToEnclosingUnit({1})", ID, unit);
        switch (unit)
        {
            case TextUnit.Character:
                ExpandToEnclosingUnit(CaretPositioningMode.Normal);
                break;
            case TextUnit.Format:
            case TextUnit.Word:
                ExpandToEnclosingUnit(CaretPositioningMode.WordStartOrSymbol);
                break;
            case TextUnit.Line:
            case TextUnit.Paragraph:
                segment = doc.GetLineByOffset(segment.Offset);
                break;
            case TextUnit.Document:
                segment = new AnchorSegment(doc, 0, doc.TextLength);
                break;
        }
    }

    private void ExpandToEnclosingUnit(CaretPositioningMode mode)
    {
        var start = TextUtilities.GetNextCaretPosition(doc, segment.Offset + 1, LogicalDirection.Backward, mode);
        if (start < 0) return;
        var end = TextUtilities.GetNextCaretPosition(doc, start, LogicalDirection.Forward, mode);
        if (end < 0) return;
        segment = new AnchorSegment(doc, start, end - start);
    }

    public ITextRangeProvider FindAttribute(int attribute, object value, bool backward)
    {
        Log("{0}.FindAttribute({1}, {2}, {3})", ID, attribute, value, backward);
        return null;
    }

    public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
    {
        Log("{0}.FindText({1}, {2}, {3})", ID, text, backward, ignoreCase);
        var segmentText = doc.GetText(segment);
        var comparison  = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var pos         = backward ? segmentText.LastIndexOf(text, comparison) : segmentText.IndexOf(text, comparison);
        if (pos >= 0) return new TextRangeProvider(textArea, doc, segment.Offset + pos, text.Length);
        return null;
    }

    public object GetAttributeValue(int attribute)
    {
        Log("{0}.GetAttributeValue({1})", ID, attribute);
        return null;
    }

    public double[] GetBoundingRectangles()
    {
        Log("{0}.GetBoundingRectangles()", ID);
        var textView = textArea.TextView;
        var source   = PresentationSource.FromVisual(textArea);
        var result   = new List<double>();
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
        {
            var tl = textView.PointToScreen(rect.TopLeft);
            var br = textView.PointToScreen(rect.BottomRight);
            result.Add(tl.X);
            result.Add(tl.Y);
            result.Add(br.X - tl.X);
            result.Add(br.Y - tl.Y);
        }

        return result.ToArray();
    }

    public IRawElementProviderSimple[] GetChildren()
    {
        Log("{0}.GetChildren()", ID);
        return Array.Empty<IRawElementProviderSimple>();
    }

    public IRawElementProviderSimple GetEnclosingElement()
    {
        Log("{0}.GetEnclosingElement()", ID);
        if (UIElementAutomationPeer.FromElement(textArea) is not TextAreaAutomationPeer peer) throw new NotSupportedException();
        return peer.Provider;
    }

    public string GetText(int maxLength)
    {
        Log("{0}.GetText({1})", ID, maxLength);
        if (maxLength < 0) return doc.GetText(segment);
        return doc.GetText(segment.Offset, Math.Min(segment.Length, maxLength));
    }

    public int Move(TextUnit unit, int count)
    {
        Log("{0}.Move({1}, {2})", ID, unit, count);
        var movedCount = MoveEndpointByUnit(TextPatternRangeEndpoint.Start, unit, count);
        segment = new SimpleSegment(segment.Offset, 0);
        ExpandToEnclosingUnit(unit);
        return movedCount;
    }

    public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
    {
        var other = (TextRangeProvider)targetRange;
        Log("{0}.MoveEndpointByRange({1}, {2}, {3})", ID, endpoint, other.ID, targetEndpoint);
        SetEndpoint(endpoint, other.GetEndpoint(targetEndpoint));
    }

    private void SetEndpoint(TextPatternRangeEndpoint endpoint, int targetOffset)
    {
        if (endpoint == TextPatternRangeEndpoint.Start)
        {
            segment = new AnchorSegment(doc, targetOffset, Math.Max(0, segment.EndOffset - targetOffset));
        }
        else
        {
            var newStart = Math.Min(segment.Offset, targetOffset);
            segment = new AnchorSegment(doc, newStart, targetOffset - newStart);
        }
    }

    public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
    {
        Log("{0}.MoveEndpointByUnit({1}, {2}, {3})", ID, endpoint, unit, count);
        var offset = GetEndpoint(endpoint);
        switch (unit)
        {
            case TextUnit.Character:
                offset = MoveOffset(offset, CaretPositioningMode.Normal, count);
                break;
            case TextUnit.Format:
            case TextUnit.Word:
                offset = MoveOffset(offset, CaretPositioningMode.WordStart, count);
                break;
            case TextUnit.Line:
            case TextUnit.Paragraph:
                var line    = doc.GetLineByOffset(offset).LineNumber;
                var newLine = Math.Max(1, Math.Min(doc.LineCount, line + count));
                offset = doc.GetLineByNumber(newLine).Offset;
                break;
            case TextUnit.Document:
                offset = count < 0 ? 0 : doc.TextLength;
                break;
        }

        SetEndpoint(endpoint, offset);
        return count;
    }

    private int MoveOffset(int offset, CaretPositioningMode mode, int count)
    {
        var direction = count < 0 ? LogicalDirection.Backward : LogicalDirection.Forward;
        count = Math.Abs(count);
        for (var i = 0; i < count; i++)
        {
            var newOffset = TextUtilities.GetNextCaretPosition(doc, offset, direction, mode);
            if (newOffset == offset || newOffset < 0) break;
            offset = newOffset;
        }

        return offset;
    }

    public void RemoveFromSelection()
    {
        Log("{0}.RemoveFromSelection()", ID);
    }

    public void ScrollIntoView(bool alignToTop)
    {
        Log("{0}.ScrollIntoView({1})", ID, alignToTop);
    }

    public void Select()
    {
        Log("{0}.Select()", ID);
        textArea.Selection = new SimpleSelection(textArea, new TextViewPosition(doc.GetLocation(segment.Offset)), new TextViewPosition(doc.GetLocation(segment.EndOffset)));
    }
}
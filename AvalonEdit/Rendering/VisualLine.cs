﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

public sealed class VisualLine
{
    private enum LifetimePhase : byte
    {
        Generating,
        Transforming,
        Live,
        Disposed
    }

    private  TextView                textView;
    private  List<VisualLineElement> elements;
    internal bool                    hasInlineObjects;
    private  LifetimePhase           phase;

    public TextDocument Document { get; }

    public DocumentLine FirstDocumentLine { get; }

    public DocumentLine LastDocumentLine { get; private set; }

    public ReadOnlyCollection<VisualLineElement> Elements { get; private set; }

    private ReadOnlyCollection<TextLine> textLines;

    public ReadOnlyCollection<TextLine> TextLines
    {
        get
        {
            if (phase < LifetimePhase.Live) throw new InvalidOperationException();
            return textLines;
        }
    }

    public int StartOffset => FirstDocumentLine.Offset;

    public int VisualLength { get; private set; }

    public int VisualLengthWithEndOfLineMarker
    {
        get
        {
            var length = VisualLength;
            if (textView.Options.ShowEndOfLine && LastDocumentLine.NextLine != null) length++;
            return length;
        }
    }

    public double Height { get; private set; }

    public double VisualTop { get; internal set; }

    internal VisualLine(TextView textView, DocumentLine firstDocumentLine)
    {
        Debug.Assert(textView != null);
        Debug.Assert(firstDocumentLine != null);
        this.textView     = textView;
        Document          = textView.Document;
        FirstDocumentLine = firstDocumentLine;
    }

    internal void ConstructVisualElements(ITextRunConstructionContext context, VisualLineElementGenerator[] generators)
    {
        Debug.Assert(phase == LifetimePhase.Generating);
        foreach (var g in generators) g.StartGeneration(context);
        elements = new List<VisualLineElement>();
        PerformVisualElementConstruction(generators);
        foreach (var g in generators) g.FinishGeneration();

        var globalTextRunProperties = context.GlobalTextRunProperties;
        foreach (var element in elements) element.SetTextRunProperties(new VisualLineElementTextRunProperties(globalTextRunProperties));
        Elements = elements.AsReadOnly();
        CalculateOffsets();
        phase = LifetimePhase.Transforming;
    }

    private void PerformVisualElementConstruction(VisualLineElementGenerator[] generators)
    {
        var offset         = FirstDocumentLine.Offset;
        var currentLineEnd = offset + FirstDocumentLine.Length;
        LastDocumentLine = FirstDocumentLine;
        var askInterestOffset = 0;
        while (offset + askInterestOffset <= currentLineEnd)
        {
            var textPieceEndOffset = currentLineEnd;
            foreach (var g in generators)
            {
                g.cachedInterest = g.GetFirstInterestedOffset(offset + askInterestOffset);
                if (g.cachedInterest == -1) continue;
                if (g.cachedInterest < offset) throw new ArgumentOutOfRangeException(g.GetType().Name + ".GetFirstInterestedOffset", g.cachedInterest, "GetFirstInterestedOffset must not return an offset less than startOffset. Return -1 to signal no interest.");
                if (g.cachedInterest < textPieceEndOffset) textPieceEndOffset = g.cachedInterest;
            }

            Debug.Assert(textPieceEndOffset >= offset);
            if (textPieceEndOffset > offset)
            {
                var textPieceLength = textPieceEndOffset - offset;
                elements.Add(new VisualLineText(this, textPieceLength));
                offset = textPieceEndOffset;
            }

            askInterestOffset = 1;
            foreach (var g in generators)
                if (g.cachedInterest == offset)
                {
                    var element = g.ConstructElement(offset);
                    if (element == null) continue;
                    elements.Add(element);
                    if (element.DocumentLength <= 0) continue;
                    askInterestOffset =  0;
                    offset            += element.DocumentLength;
                    if (offset > currentLineEnd)
                    {
                        var newEndLine = Document.GetLineByOffset(offset);
                        currentLineEnd   = newEndLine.Offset + newEndLine.Length;
                        LastDocumentLine = newEndLine;
                        if (currentLineEnd < offset) throw new InvalidOperationException("The VisualLineElementGenerator " + g.GetType().Name + " produced an element which ends within the line delimiter");
                    }

                    break;
                }
        }
    }

    private void CalculateOffsets()
    {
        var visualOffset = 0;
        var textOffset   = 0;
        foreach (var element in elements)
        {
            element.VisualColumn       =  visualOffset;
            element.RelativeTextOffset =  textOffset;
            visualOffset               += element.VisualLength;
            textOffset                 += element.DocumentLength;
        }

        VisualLength = visualOffset;
        Debug.Assert(textOffset == LastDocumentLine.EndOffset - FirstDocumentLine.Offset);
    }

    internal void RunTransformers(ITextRunConstructionContext context, IVisualLineTransformer[] transformers)
    {
        Debug.Assert(phase == LifetimePhase.Transforming);
        foreach (var transformer in transformers) transformer.Transform(context, elements);
        if (elements.Any(e => e.TextRunProperties.TypographyProperties != null))
            foreach (var element in elements.Where(element => element.TextRunProperties.TypographyProperties == null))
                element.TextRunProperties.SetTypographyProperties(new DefaultTextRunTypographyProperties());

        phase = LifetimePhase.Live;
    }

    public void ReplaceElement(int elementIndex, params VisualLineElement[] newElements)
    {
        ReplaceElement(elementIndex, 1, newElements);
    }

    public void ReplaceElement(int elementIndex, int count, params VisualLineElement[] newElements)
    {
        if (phase != LifetimePhase.Transforming) throw new InvalidOperationException("This method may only be called by line transformers.");
        var oldDocumentLength                                                       = 0;
        for (var i = elementIndex; i < elementIndex + count; i++) oldDocumentLength += elements[i].DocumentLength;
        var newDocumentLength                                                       = newElements.Sum(newElement => newElement.DocumentLength);
        if (oldDocumentLength != newDocumentLength) throw new InvalidOperationException("Old elements have document length " + oldDocumentLength + ", but new elements have length " + newDocumentLength);
        elements.RemoveRange(elementIndex, count);
        elements.InsertRange(elementIndex, newElements);
        CalculateOffsets();
    }

    internal void SetTextLines(List<TextLine> textLines_)
    {
        this.textLines = textLines_.AsReadOnly();
        Height         = 0;
        foreach (var line in textLines_) Height += line.Height;
    }

    public int GetVisualColumn(int relativeTextOffset)
    {
        ThrowUtil.CheckNotNegative(relativeTextOffset, "relativeTextOffset");
        foreach (var element in elements.Where(element => element.RelativeTextOffset <= relativeTextOffset && element.RelativeTextOffset + element.DocumentLength >= relativeTextOffset)) return element.GetVisualColumn(relativeTextOffset);

        return VisualLength;
    }

    public int GetRelativeOffset(int visualColumn)
    {
        ThrowUtil.CheckNotNegative(visualColumn, "visualColumn");
        var documentLength = 0;
        foreach (var element in elements)
        {
            if (element.VisualColumn <= visualColumn && element.VisualColumn + element.VisualLength > visualColumn) return element.GetRelativeOffset(visualColumn);
            documentLength += element.DocumentLength;
        }

        return documentLength;
    }

    public TextLine GetTextLine(int visualColumn, bool isAtEndOfLine = false)
    {
        if (visualColumn < 0) throw new ArgumentOutOfRangeException(nameof(visualColumn));
        if (visualColumn >= VisualLengthWithEndOfLineMarker) return TextLines[TextLines.Count - 1];
        foreach (var line in TextLines)
            if (isAtEndOfLine ? visualColumn <= line.Length : visualColumn < line.Length) return line;
            else visualColumn -= line.Length;
        throw new InvalidOperationException("Shouldn't happen (VisualLength incorrect?)");
    }

    public double GetTextLineVisualYPosition(TextLine textLine, VisualYPosition yPositionMode)
    {
        if (textLine == null) throw new ArgumentNullException(nameof(textLine));
        var pos = VisualTop;
        foreach (var tl in TextLines)
            if (tl == textLine)
                return yPositionMode switch
                       {
                           VisualYPosition.LineTop    => pos,
                           VisualYPosition.LineMiddle => pos + tl.Height / 2,
                           VisualYPosition.LineBottom => pos + tl.Height,
                           VisualYPosition.TextTop    => pos + tl.Baseline - textView.DefaultBaseline,
                           VisualYPosition.TextBottom => pos + tl.Baseline - textView.DefaultBaseline + textView.DefaultLineHeight,
                           VisualYPosition.TextMiddle => pos + tl.Baseline - textView.DefaultBaseline + textView.DefaultLineHeight / 2,
                           VisualYPosition.Baseline   => pos + tl.Baseline,
                           _                          => throw new ArgumentException("Invalid yPositionMode:" + yPositionMode)
                       };
            else pos += tl.Height;

        throw new ArgumentException("textLine is not a line in this VisualLine");
    }

    public int GetTextLineVisualStartColumn(TextLine textLine)
    {
        if (!TextLines.Contains(textLine)) throw new ArgumentException("textLine is not a line in this VisualLine");
        var col = 0;
        foreach (var tl in TextLines)
            if (tl == textLine) break;
            else col += tl.Length;
        return col;
    }

    public TextLine GetTextLineByVisualYPosition(double visualTop)
    {
        const double epsilon = 0.0001;
        var          pos     = VisualTop;
        foreach (var tl in TextLines)
        {
            pos += tl.Height;
            if (visualTop + epsilon < pos) return tl;
        }

        return TextLines[TextLines.Count - 1];
    }

    public Point GetVisualPosition(int visualColumn, VisualYPosition yPositionMode)
    {
        var textLine = GetTextLine(visualColumn);
        var xPos     = GetTextLineVisualXPosition(textLine, visualColumn);
        var yPos     = GetTextLineVisualYPosition(textLine, yPositionMode);
        return new Point(xPos, yPos);
    }

    internal Point GetVisualPosition(int visualColumn, bool isAtEndOfLine, VisualYPosition yPositionMode)
    {
        var textLine = GetTextLine(visualColumn, isAtEndOfLine);
        var xPos     = GetTextLineVisualXPosition(textLine, visualColumn);
        var yPos     = GetTextLineVisualYPosition(textLine, yPositionMode);
        return new Point(xPos, yPos);
    }

    public double GetTextLineVisualXPosition(TextLine textLine, int visualColumn)
    {
        if (textLine == null) throw new ArgumentNullException(nameof(textLine));
        var xPos                                                 = textLine.GetDistanceFromCharacterHit(new CharacterHit(Math.Min(visualColumn, VisualLengthWithEndOfLineMarker), 0));
        if (visualColumn > VisualLengthWithEndOfLineMarker) xPos += (visualColumn - VisualLengthWithEndOfLineMarker) * textView.WideSpaceWidth;
        return xPos;
    }

    public int GetVisualColumn(Point point)
    {
        return GetVisualColumn(point, textView.Options.EnableVirtualSpace);
    }

    public int GetVisualColumn(Point point, bool allowVirtualSpace)
    {
        return GetVisualColumn(GetTextLineByVisualYPosition(point.Y), point.X, allowVirtualSpace);
    }

    internal int GetVisualColumn(Point point, bool allowVirtualSpace, out bool isAtEndOfLine)
    {
        var textLine = GetTextLineByVisualYPosition(point.Y);
        var vc       = GetVisualColumn(textLine, point.X, allowVirtualSpace);
        isAtEndOfLine = vc >= GetTextLineVisualStartColumn(textLine) + textLine.Length;
        return vc;
    }

    public int GetVisualColumn(TextLine textLine, double xPos, bool allowVirtualSpace)
    {
        if (xPos > textLine.WidthIncludingTrailingWhitespace)
            if (allowVirtualSpace && textLine == TextLines[TextLines.Count - 1])
            {
                var virtualX = (int)Math.Round((xPos - textLine.WidthIncludingTrailingWhitespace) / textView.WideSpaceWidth, MidpointRounding.AwayFromZero);
                return VisualLengthWithEndOfLineMarker + virtualX;
            }

        var ch = textLine.GetCharacterHitFromDistance(xPos);
        return ch.FirstCharacterIndex + ch.TrailingLength;
    }

    public int ValidateVisualColumn(TextViewPosition position, bool allowVirtualSpace)
    {
        return ValidateVisualColumn(Document.GetOffset(position.Location), position.VisualColumn, allowVirtualSpace);
    }

    public int ValidateVisualColumn(int offset, int visualColumn, bool allowVirtualSpace)
    {
        var firstDocumentLineOffset = FirstDocumentLine.Offset;
        if (visualColumn < 0) return GetVisualColumn(offset - firstDocumentLineOffset);

        var offsetFromVisualColumn = GetRelativeOffset(visualColumn);
        offsetFromVisualColumn += firstDocumentLineOffset;
        if (offsetFromVisualColumn != offset) return GetVisualColumn(offset - firstDocumentLineOffset);

        if (visualColumn > VisualLength && !allowVirtualSpace) return VisualLength;
        return visualColumn;
    }

    public int GetVisualColumnFloor(Point point)
    {
        return GetVisualColumnFloor(point, textView.Options.EnableVirtualSpace);
    }

    public int GetVisualColumnFloor(Point point, bool allowVirtualSpace)
    {
        bool tmp;
        return GetVisualColumnFloor(point, allowVirtualSpace, out tmp);
    }

    internal int GetVisualColumnFloor(Point point, bool allowVirtualSpace, out bool isAtEndOfLine)
    {
        var textLine = GetTextLineByVisualYPosition(point.Y);
        if (point.X > textLine.WidthIncludingTrailingWhitespace)
        {
            isAtEndOfLine = true;
            if (!allowVirtualSpace || textLine != TextLines[TextLines.Count - 1]) return GetTextLineVisualStartColumn(textLine) + textLine.Length;
            var virtualX = (int)((point.X - textLine.WidthIncludingTrailingWhitespace) / textView.WideSpaceWidth);
            return VisualLengthWithEndOfLineMarker + virtualX;

        }

        isAtEndOfLine = false;
        var ch = textLine.GetCharacterHitFromDistance(point.X);
        return ch.FirstCharacterIndex;
    }

    public TextViewPosition GetTextViewPosition(int visualColumn)
    {
        var documentOffset = GetRelativeOffset(visualColumn) + FirstDocumentLine.Offset;
        return new TextViewPosition(Document.GetLocation(documentOffset), visualColumn);
    }

    public TextViewPosition GetTextViewPosition(Point visualPosition, bool allowVirtualSpace)
    {
        var  visualColumn   = GetVisualColumn(visualPosition, allowVirtualSpace, out var isAtEndOfLine);
        var  documentOffset = GetRelativeOffset(visualColumn) + FirstDocumentLine.Offset;
        var  pos            = new TextViewPosition(Document.GetLocation(documentOffset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
        return pos;
    }

    public TextViewPosition GetTextViewPositionFloor(Point visualPosition, bool allowVirtualSpace)
    {
        var visualColumn   = GetVisualColumnFloor(visualPosition, allowVirtualSpace, out var isAtEndOfLine);
        var documentOffset = GetRelativeOffset(visualColumn) + FirstDocumentLine.Offset;
        var pos            = new TextViewPosition(Document.GetLocation(documentOffset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
        return pos;
    }

    public bool IsDisposed => phase == LifetimePhase.Disposed;

    internal void Dispose()
    {
        if (phase == LifetimePhase.Disposed) return;
        Debug.Assert(phase == LifetimePhase.Live);
        phase = LifetimePhase.Disposed;
        foreach (var textLine in TextLines) textLine.Dispose();
    }

    public int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode, bool allowVirtualSpace)
    {
        if (!HasStopsInVirtualSpace(mode)) allowVirtualSpace = false;

        if (elements.Count == 0)
        {
            if (allowVirtualSpace)
            {
                if (direction == LogicalDirection.Forward) return Math.Max(0, visualColumn + 1);
                if (visualColumn > 0) return visualColumn - 1;
                return -1;
            }

            switch (visualColumn)
            {
                case < 0 when direction == LogicalDirection.Forward:
                case > 0 when direction == LogicalDirection.Backward:
                    return 0;
                default:
                    return -1;
            }
        }

        int i;
        if (direction == LogicalDirection.Backward)
        {
            if (visualColumn > VisualLength && !elements[elements.Count - 1].HandlesLineBorders && HasImplicitStopAtLineEnd(mode))
            {
                if (allowVirtualSpace) return visualColumn - 1;
                return VisualLength;
            }

            for (i = elements.Count - 1; i >= 0; i--)
                if (elements[i].VisualColumn < visualColumn)
                    break;
            for (; i >= 0; i--)
            {
                var pos = elements[i].GetNextCaretPosition(Math.Min(visualColumn, elements[i].VisualColumn + elements[i].VisualLength + 1), direction, mode);
                if (pos >= 0) return pos;
            }

            if (visualColumn > 0 && !elements[0].HandlesLineBorders && HasImplicitStopAtLineStart(mode)) return 0;
        }
        else
        {
            if (visualColumn < 0 && !elements[0].HandlesLineBorders && HasImplicitStopAtLineStart(mode)) return 0;
            for (i = 0; i < elements.Count; i++)
                if (elements[i].VisualColumn + elements[i].VisualLength > visualColumn)
                    break;
            for (; i < elements.Count; i++)
            {
                var pos = elements[i].GetNextCaretPosition(Math.Max(visualColumn, elements[i].VisualColumn - 1), direction, mode);
                if (pos >= 0) return pos;
            }

            if ((!allowVirtualSpace && elements[elements.Count - 1].HandlesLineBorders) || !HasImplicitStopAtLineEnd(mode)) return -1;
            if (visualColumn < VisualLength) return VisualLength;
            if (allowVirtualSpace) return visualColumn + 1;
        }

        return -1;
    }

    private static bool HasStopsInVirtualSpace(CaretPositioningMode mode)
    {
        return mode is CaretPositioningMode.Normal or CaretPositioningMode.EveryCodepoint;
    }

    private static bool HasImplicitStopAtLineStart(CaretPositioningMode mode)
    {
        return mode is CaretPositioningMode.Normal or CaretPositioningMode.EveryCodepoint;
    }

    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "mode", Justification = "make method consistent with HasImplicitStopAtLineStart; might depend on mode in the future")]
    private static bool HasImplicitStopAtLineEnd(CaretPositioningMode mode)
    {
        return true;
    }

    private VisualLineDrawingVisual visual;

    internal VisualLineDrawingVisual Render()
    {
        Debug.Assert(phase == LifetimePhase.Live);
        return visual ??= new VisualLineDrawingVisual(this, textView.FlowDirection);
    }
}

internal sealed class VisualLineDrawingVisual : DrawingVisual
{
    public readonly VisualLine VisualLine;
    public readonly double     Height;
    internal        bool       IsAdded;

    public VisualLineDrawingVisual(VisualLine visualLine, FlowDirection flow)
    {
        VisualLine = visualLine;
        var    drawingContext = RenderOpen();
        double pos            = 0;
        foreach (var textLine in visualLine.TextLines)
        {
            textLine.Draw(drawingContext, new Point(0, pos), flow == FlowDirection.LeftToRight ? InvertAxes.None : InvertAxes.Horizontal);
            pos += textLine.Height;
        }

        Height = pos;
        drawingContext.Close();
    }

    protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
    {
        return null;
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return null;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AvalonEdit.Document;
using AvalonEdit.Editing;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

public sealed class BackgroundGeometryBuilder
{
    public double CornerRadius { get; set; }

    public bool AlignToWholePixels { get; set; }

    public double BorderThickness { get; set; }

    public bool ExtendToFullWidthAtLineEnd { get; set; }

    public BackgroundGeometryBuilder()
    {
    }

    public void AddSegment(TextView textView, ISegment segment)
    {
        if (textView == null) throw new ArgumentNullException(nameof(textView));
        var pixelSize = PixelSnapHelpers.GetPixelSize(textView);
        foreach (var r in GetRectsForSegment(textView, segment, ExtendToFullWidthAtLineEnd)) AddRectangle(pixelSize, r);
    }

    public void AddRectangle(TextView textView, Rect rectangle)
    {
        AddRectangle(PixelSnapHelpers.GetPixelSize(textView), rectangle);
    }

    private void AddRectangle(Size pixelSize, Rect r)
    {
        if (AlignToWholePixels)
        {
            var halfBorder = 0.5 * BorderThickness;
            AddRectangle(PixelSnapHelpers.Round(r.Left - halfBorder, pixelSize.Width) + halfBorder, PixelSnapHelpers.Round(r.Top - halfBorder, pixelSize.Height) + halfBorder, PixelSnapHelpers.Round(r.Right + halfBorder, pixelSize.Width) - halfBorder, PixelSnapHelpers.Round(r.Bottom + halfBorder, pixelSize.Height) - halfBorder);
        }
        else
        {
            AddRectangle(r.Left, r.Top, r.Right, r.Bottom);
        }
    }

    public static IEnumerable<Rect> GetRectsForSegment(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd = false)
    {
        if (textView == null) throw new ArgumentNullException(nameof(textView));
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        return GetRectsForSegmentImpl(textView, segment, extendToFullWidthAtLineEnd);
    }

    private static IEnumerable<Rect> GetRectsForSegmentImpl(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd)
    {
        var segmentStart = segment.Offset;
        var segmentEnd   = segment.Offset + segment.Length;

        segmentStart = segmentStart.CoerceValue(0, textView.Document.TextLength);
        segmentEnd   = segmentEnd.CoerceValue(0, textView.Document.TextLength);

        TextViewPosition start;
        TextViewPosition end;

        if (segment is SelectionSegment sel)
        {
            start = new TextViewPosition(textView.Document.GetLocation(sel.StartOffset), sel.StartVisualColumn);
            end   = new TextViewPosition(textView.Document.GetLocation(sel.EndOffset), sel.EndVisualColumn);
        }
        else
        {
            start = new TextViewPosition(textView.Document.GetLocation(segmentStart));
            end   = new TextViewPosition(textView.Document.GetLocation(segmentEnd));
        }

        foreach (var vl in textView.VisualLines)
        {
            var vlStartOffset = vl.FirstDocumentLine.Offset;
            if (vlStartOffset > segmentEnd) break;
            var vlEndOffset = vl.LastDocumentLine.Offset + vl.LastDocumentLine.Length;
            if (vlEndOffset < segmentStart) continue;

            var segmentStartVC = segmentStart < vlStartOffset ? 0 : vl.ValidateVisualColumn(start, extendToFullWidthAtLineEnd);

            int segmentEndVC;
            if (segmentEnd > vlEndOffset) segmentEndVC = extendToFullWidthAtLineEnd ? int.MaxValue : vl.VisualLengthWithEndOfLineMarker;
            else segmentEndVC                          = vl.ValidateVisualColumn(end, extendToFullWidthAtLineEnd);

            foreach (var rect in ProcessTextLines(textView, vl, segmentStartVC, segmentEndVC)) yield return rect;
        }
    }

    public static IEnumerable<Rect> GetRectsFromVisualSegment(TextView textView, VisualLine line, int startVC, int endVC)
    {
        if (textView == null) throw new ArgumentNullException(nameof(textView));
        if (line == null) throw new ArgumentNullException(nameof(line));
        return ProcessTextLines(textView, line, startVC, endVC);
    }

    private static IEnumerable<Rect> ProcessTextLines(TextView textView, VisualLine visualLine, int segmentStartVC, int segmentEndVC)
    {
        var lastTextLine = visualLine.TextLines.Last();
        var scrollOffset = textView.ScrollOffset;

        for (var i = 0; i < visualLine.TextLines.Count; i++)
        {
            var line           = visualLine.TextLines[i];
            var y              = visualLine.GetTextLineVisualYPosition(line, VisualYPosition.LineTop);
            var visualStartCol = visualLine.GetTextLineVisualStartColumn(line);
            var visualEndCol   = visualStartCol + line.Length;
            if (line == lastTextLine) visualEndCol -= 1;
            else visualEndCol                      -= line.TrailingWhitespaceLength;

            if (segmentEndVC < visualStartCol) break;
            if (lastTextLine != line && segmentStartVC > visualEndCol) continue;
            var segmentStartVCInLine = Math.Max(segmentStartVC, visualStartCol);
            var segmentEndVCInLine   = Math.Min(segmentEndVC, visualEndCol);
            y -= scrollOffset.Y;
            var lastRect = Rect.Empty;
            if (segmentStartVCInLine == segmentEndVCInLine)
            {
                var pos = visualLine.GetTextLineVisualXPosition(line, segmentStartVCInLine);
                pos -= scrollOffset.X;
                if (segmentEndVCInLine == visualEndCol && i < visualLine.TextLines.Count - 1 && segmentEndVC > segmentEndVCInLine && line.TrailingWhitespaceLength == 0) continue;
                if (segmentStartVCInLine == visualStartCol && i > 0 && segmentStartVC < segmentStartVCInLine && visualLine.TextLines[i - 1].TrailingWhitespaceLength == 0) continue;
                lastRect = new Rect(pos, y, textView.EmptyLineSelectionWidth, line.Height);
            }
            else
            {
                if (segmentStartVCInLine <= visualEndCol)
                    foreach (var b in line.GetTextBounds(segmentStartVCInLine, segmentEndVCInLine - segmentStartVCInLine))
                    {
                        var left  = b.Rectangle.Left - scrollOffset.X;
                        var right = b.Rectangle.Right - scrollOffset.X;
                        if (!lastRect.IsEmpty) yield return lastRect;
                        lastRect = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
                    }
            }

            if (segmentEndVC > visualEndCol)
            {
                double left, right;
                if (segmentStartVC > visualLine.VisualLengthWithEndOfLineMarker) left = visualLine.GetTextLineVisualXPosition(lastTextLine, segmentStartVC);
                else left                                                             = line == lastTextLine ? line.WidthIncludingTrailingWhitespace : line.Width;
                if (line != lastTextLine || segmentEndVC == int.MaxValue) right = Math.Max(((IScrollInfo)textView).ExtentWidth, ((IScrollInfo)textView).ViewportWidth);
                else right                                                      = visualLine.GetTextLineVisualXPosition(lastTextLine, segmentEndVC);
                var extendSelection = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
                if (!lastRect.IsEmpty)
                {
                    if (extendSelection.IntersectsWith(lastRect))
                    {
                        lastRect.Union(extendSelection);
                        yield return lastRect;
                    }
                    else
                    {
                        yield return lastRect;
                        yield return extendSelection;
                    }
                }
                else
                {
                    yield return extendSelection;
                }
            }
            else
            {
                yield return lastRect;
            }
        }
    }

    private PathFigureCollection figures = new();
    private PathFigure           figure;
    private int                  insertionIndex;
    private double               lastTop,  lastBottom;
    private double               lastLeft, lastRight;

    public void AddRectangle(double left, double top, double right, double bottom)
    {
        if (!top.IsClose(lastBottom)) CloseFigure();
        if (figure == null)
        {
            figure = new PathFigure { StartPoint = new Point(left, top + CornerRadius) };
            if (Math.Abs(left - right) > CornerRadius)
            {
                figure.Segments.Add(MakeArc(left + CornerRadius, top, SweepDirection.Clockwise));
                figure.Segments.Add(MakeLineSegment(right - CornerRadius, top));
                figure.Segments.Add(MakeArc(right, top + CornerRadius, SweepDirection.Clockwise));
            }

            figure.Segments.Add(MakeLineSegment(right, bottom - CornerRadius));
            insertionIndex = figure.Segments.Count;
        }
        else
        {
            if (!lastRight.IsClose(right))
            {
                var cr   = right < lastRight ? -CornerRadius : CornerRadius;
                var dir1 = right < lastRight ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
                var dir2 = right < lastRight ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
                figure.Segments.Insert(insertionIndex++, MakeArc(lastRight + cr, lastBottom, dir1));
                figure.Segments.Insert(insertionIndex++, MakeLineSegment(right - cr, top));
                figure.Segments.Insert(insertionIndex++, MakeArc(right, top + CornerRadius, dir2));
            }

            figure.Segments.Insert(insertionIndex++, MakeLineSegment(right, bottom - CornerRadius));
            figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft, lastTop + CornerRadius));
            if (!lastLeft.IsClose(left))
            {
                var cr   = left < lastLeft ? CornerRadius : -CornerRadius;
                var dir1 = left < lastLeft ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
                var dir2 = left < lastLeft ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
                figure.Segments.Insert(insertionIndex, MakeArc(lastLeft, lastBottom - CornerRadius, dir1));
                figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft - cr, lastBottom));
                figure.Segments.Insert(insertionIndex, MakeArc(left + cr, lastBottom, dir2));
            }
        }

        lastTop    = top;
        lastBottom = bottom;
        lastLeft   = left;
        lastRight  = right;
    }

    private ArcSegment MakeArc(double x, double y, SweepDirection dir)
    {
        var arc = new ArcSegment(new Point(x, y), new Size(CornerRadius, CornerRadius), 0, false, dir, true);
        arc.Freeze();
        return arc;
    }

    private static LineSegment MakeLineSegment(double x, double y)
    {
        var ls = new LineSegment(new Point(x, y), true);
        ls.Freeze();
        return ls;
    }

    public void CloseFigure()
    {
        if (figure == null) return;
        figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft, lastTop + CornerRadius));
        if (Math.Abs(lastLeft - lastRight) > CornerRadius)
        {
            figure.Segments.Insert(insertionIndex, MakeArc(lastLeft, lastBottom - CornerRadius, SweepDirection.Clockwise));
            figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft + CornerRadius, lastBottom));
            figure.Segments.Insert(insertionIndex, MakeArc(lastRight - CornerRadius, lastBottom, SweepDirection.Clockwise));
        }

        figure.IsClosed = true;
        figures.Add(figure);
        figure = null;
    }

    public Geometry CreateGeometry()
    {
        CloseFigure();
        if (figures.Count == 0) return null;
        var g = new PathGeometry(figures);
        g.Freeze();
        return g;
    }
}
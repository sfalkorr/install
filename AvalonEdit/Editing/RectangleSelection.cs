﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

public sealed class RectangleSelection : Selection
{
    #region Commands

    // public static readonly RoutedUICommand BoxSelectLeftByCharacter = Command("BoxSelectLeftByCharacter");
    //
    // public static readonly RoutedUICommand BoxSelectRightByCharacter = Command("BoxSelectRightByCharacter");
    //
    // public static readonly RoutedUICommand BoxSelectLeftByWord = Command("BoxSelectLeftByWord");
    //
    // public static readonly RoutedUICommand BoxSelectRightByWord = Command("BoxSelectRightByWord");
    //
    // public static readonly RoutedUICommand BoxSelectUpByLine = Command("BoxSelectUpByLine");
    //
    // public static readonly RoutedUICommand BoxSelectDownByLine = Command("BoxSelectDownByLine");
    //
    // public static readonly RoutedUICommand BoxSelectToLineStart = Command("BoxSelectToLineStart");
    //
    // public static readonly RoutedUICommand BoxSelectToLineEnd = Command("BoxSelectToLineEnd");

    // private static RoutedUICommand Command(string name)
    // {
    //     return new RoutedUICommand(name, name, typeof(RectangleSelection));
    // }

    #endregion

    private          TextDocument document;
    private readonly int          startLine,     endLine;
    private readonly double       startXPos,     endXPos;
    private readonly int          topLeftOffset, bottomRightOffset;

    private readonly List<SelectionSegment> segments = new();

    #region Constructors

    public RectangleSelection(TextArea textArea, TextViewPosition start, TextViewPosition end) : base(textArea)
    {
        InitDocument();
        startLine = start.Line;
        endLine   = end.Line;
        startXPos = GetXPos(textArea, start);
        endXPos   = GetXPos(textArea, end);
        CalculateSegments();
        topLeftOffset     = segments.First().StartOffset;
        bottomRightOffset = segments.Last().EndOffset;

        StartPosition = start;
        EndPosition   = end;
    }

    private RectangleSelection(TextArea textArea, int startLine, double startXPos, TextViewPosition end) : base(textArea)
    {
        InitDocument();
        this.startLine = startLine;
        endLine        = end.Line;
        this.startXPos = startXPos;
        endXPos        = GetXPos(textArea, end);
        CalculateSegments();
        topLeftOffset     = segments.First().StartOffset;
        bottomRightOffset = segments.Last().EndOffset;

        StartPosition = GetStart();
        EndPosition   = end;
    }

    private RectangleSelection(TextArea textArea, TextViewPosition start, int endLine, double endXPos) : base(textArea)
    {
        InitDocument();
        startLine    = start.Line;
        this.endLine = endLine;
        startXPos    = GetXPos(textArea, start);
        this.endXPos = endXPos;
        CalculateSegments();
        topLeftOffset     = segments.First().StartOffset;
        bottomRightOffset = segments.Last().EndOffset;

        StartPosition = start;
        EndPosition   = GetEnd();
    }

    private void InitDocument()
    {
        document = textArea.Document;
        if (document == null) throw ThrowUtil.NoDocumentAssigned();
    }

    private static double GetXPos(TextArea textArea, TextViewPosition pos)
    {
        var documentLine = textArea.Document.GetLineByNumber(pos.Line);
        var visualLine   = textArea.TextView.GetOrConstructVisualLine(documentLine);
        var vc           = visualLine.ValidateVisualColumn(pos, true);
        var textLine     = visualLine.GetTextLine(vc, pos.IsAtEndOfLine);
        return visualLine.GetTextLineVisualXPosition(textLine, vc);
    }

    private void CalculateSegments()
    {
        var nextLine = document.GetLineByNumber(Math.Min(startLine, endLine));
        do
        {
            var vl      = textArea.TextView.GetOrConstructVisualLine(nextLine);
            var startVC = vl.GetVisualColumn(new Point(startXPos, 0), true);
            var endVC   = vl.GetVisualColumn(new Point(endXPos, 0), true);

            var baseOffset  = vl.FirstDocumentLine.Offset;
            var startOffset = baseOffset + vl.GetRelativeOffset(startVC);
            var endOffset   = baseOffset + vl.GetRelativeOffset(endVC);
            segments.Add(new SelectionSegment(startOffset, startVC, endOffset, endVC));

            nextLine = vl.LastDocumentLine.NextLine;
        }
        while (nextLine != null && nextLine.LineNumber <= Math.Max(startLine, endLine));
    }

    private TextViewPosition GetStart()
    {
        var segment = startLine < endLine ? segments.First() : segments.Last();
        return startXPos < endXPos ? new TextViewPosition(document.GetLocation(segment.StartOffset), segment.StartVisualColumn) : new TextViewPosition(document.GetLocation(segment.EndOffset), segment.EndVisualColumn);
    }

    private TextViewPosition GetEnd()
    {
        var segment = startLine < endLine ? segments.Last() : segments.First();
        return startXPos < endXPos ? new TextViewPosition(document.GetLocation(segment.EndOffset), segment.EndVisualColumn) : new TextViewPosition(document.GetLocation(segment.StartOffset), segment.StartVisualColumn);
    }

    #endregion

    public override string GetText()
    {
        var b = new StringBuilder();
        foreach (var s in Segments)
        {
            if (b.Length > 0) b.AppendLine();
            b.Append(document.GetText(s));
        }

        return b.ToString();
    }

    public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
    {
        return SetEndpoint(endPosition);
    }

    public override int Length { get { return Segments.Sum(s => s.Length); } }

    public override bool EnableVirtualSpace => true;

    public override ISegment SurroundingSegment => new SimpleSegment(topLeftOffset, bottomRightOffset - topLeftOffset);

    public override IEnumerable<SelectionSegment> Segments => segments;

    public override TextViewPosition StartPosition { get; }

    public override TextViewPosition EndPosition { get; }

    public override bool Equals(object obj)
    {
        var r = obj as RectangleSelection;
        return r != null && r.textArea == textArea && r.topLeftOffset == topLeftOffset && r.bottomRightOffset == bottomRightOffset && r.startLine == startLine && r.endLine == endLine && r.startXPos == startXPos && r.endXPos == endXPos;
    }

    public override int GetHashCode()
    {
        return topLeftOffset ^ bottomRightOffset;
    }

    public override Selection SetEndpoint(TextViewPosition endPosition)
    {
        return new RectangleSelection(textArea, startLine, startXPos, endPosition);
    }

    private int GetVisualColumnFromXPos(int line, double xPos)
    {
        var vl = textArea.TextView.GetOrConstructVisualLine(textArea.Document.GetLineByNumber(line));
        return vl.GetVisualColumn(new Point(xPos, 0), true);
    }

    public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
    {
        var newStartLocation = textArea.Document.GetLocation(e.GetNewOffset(topLeftOffset, AnchorMovementType.AfterInsertion));
        var newEndLocation   = textArea.Document.GetLocation(e.GetNewOffset(bottomRightOffset, AnchorMovementType.BeforeInsertion));

        return new RectangleSelection(textArea, new TextViewPosition(newStartLocation, GetVisualColumnFromXPos(newStartLocation.Line, startXPos)), new TextViewPosition(newEndLocation, GetVisualColumnFromXPos(newEndLocation.Line, endXPos)));
    }

    public override void ReplaceSelectionWithText(string newText)
    {
        if (newText == null) throw new ArgumentNullException(nameof(newText));
        using (textArea.Document.RunUpdate())
        {
            var              start = new TextViewPosition(document.GetLocation(topLeftOffset), GetVisualColumnFromXPos(startLine, startXPos));
            var              end   = new TextViewPosition(document.GetLocation(bottomRightOffset), GetVisualColumnFromXPos(endLine, endXPos));
            int              insertionLength;
            var              totalInsertionLength = 0;
            var              firstInsertionLength = 0;
            var              editOffset           = Math.Min(topLeftOffset, bottomRightOffset);
            TextViewPosition pos;
            if (NewLineFinder.NextNewLine(newText, 0) == SimpleSegment.Invalid)
            {
                foreach (var lineSegment in Segments.Reverse())
                {
                    ReplaceSingleLineText(textArea, lineSegment, newText, out insertionLength);
                    totalInsertionLength += insertionLength;
                    firstInsertionLength =  insertionLength;
                }

                var newEndOffset = editOffset + totalInsertionLength;
                pos = new TextViewPosition(document.GetLocation(editOffset + firstInsertionLength));

                textArea.Selection = new RectangleSelection(textArea, pos, Math.Max(startLine, endLine), GetXPos(textArea, pos));
            }
            else
            {
                var lines = newText.Split(NewLineFinder.NewlineStrings, segments.Count, StringSplitOptions.None);
                var line  = Math.Min(startLine, endLine);
                for (var i = lines.Length - 1; i >= 0; i--)
                {
                    ReplaceSingleLineText(textArea, segments[i], lines[i], out insertionLength);
                    firstInsertionLength = insertionLength;
                }

                pos = new TextViewPosition(document.GetLocation(editOffset + firstInsertionLength));
                textArea.ClearSelection();
            }

            textArea.Caret.Position = textArea.TextView.GetPosition(new Point(GetXPos(textArea, pos), textArea.TextView.GetVisualTopByDocumentLine(Math.Max(startLine, endLine)))).GetValueOrDefault();
        }
    }

    private void ReplaceSingleLineText(TextArea textArea_, SelectionSegment lineSegment, string newText, out int insertionLength)
    {
        if (lineSegment.Length == 0)
        {
            if (newText.Length > 0 && textArea_.ReadOnlySectionProvider.CanInsert(lineSegment.StartOffset))
            {
                newText = AddSpacesIfRequired(newText, new TextViewPosition(document.GetLocation(lineSegment.StartOffset), lineSegment.StartVisualColumn), new TextViewPosition(document.GetLocation(lineSegment.EndOffset), lineSegment.EndVisualColumn));
                textArea_.Document.Insert(lineSegment.StartOffset, newText);
            }
        }
        else
        {
            var segmentsToDelete = textArea_.GetDeletableSegments(lineSegment);
            for (var i = segmentsToDelete.Length - 1; i >= 0; i--)
                if (i == segmentsToDelete.Length - 1)
                {
                    if (segmentsToDelete[i].Offset == SurroundingSegment.Offset && segmentsToDelete[i].Length == SurroundingSegment.Length) newText = AddSpacesIfRequired(newText, new TextViewPosition(document.GetLocation(lineSegment.StartOffset), lineSegment.StartVisualColumn), new TextViewPosition(document.GetLocation(lineSegment.EndOffset), lineSegment.EndVisualColumn));
                    textArea_.Document.Replace(segmentsToDelete[i], newText);
                }
                else
                {
                    textArea_.Document.Remove(segmentsToDelete[i]);
                }
        }

        insertionLength = newText.Length;
    }

    // public static bool PerformRectangularPaste(TextArea textArea, TextViewPosition startPosition, string text, bool selectInsertedText)
    // {
    //     if (textArea == null) throw new ArgumentNullException(nameof(textArea));
    //     if (text == null) throw new ArgumentNullException(nameof(text));
    //     var newLineCount = text.Count(c => c == '\n');
    //     var endLocation  = new TextLocation(startPosition.Line + newLineCount, startPosition.Column);
    //     if (endLocation.Line > textArea.Document.LineCount) return false;
    //     var endOffset = textArea.Document.GetOffset(endLocation);
    //     if (!textArea.Selection.EnableVirtualSpace && textArea.Document.GetLocation(endOffset) != endLocation) return false;
    //     var rsel = new RectangleSelection(textArea, startPosition, endLocation.Line, GetXPos(textArea, startPosition));
    //     rsel.ReplaceSelectionWithText(text);
    //     if (!selectInsertedText || textArea.Selection is not RectangleSelection sel) return true;
    //     textArea.Selection = new RectangleSelection(textArea, startPosition, sel.endLine, sel.endXPos);
    //
    //     return true;
    // }

    public const string RectangularSelectionDataType = "AvalonEditRectangularSelection";

    public override DataObject CreateDataObject(TextArea textArea)
    {
        var data = base.CreateDataObject(textArea);

        return data;
    }

    public override string ToString()
    {
        return $"[RectangleSelection {startLine} {topLeftOffset} {startXPos} to {endLine} {bottomRightOffset} {endXPos}]";
    }
}
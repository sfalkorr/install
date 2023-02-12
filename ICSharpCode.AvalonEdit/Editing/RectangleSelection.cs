// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing;

/// <summary>
///     Rectangular selection ("box selection").
/// </summary>
public sealed class RectangleSelection : Selection
{
    #region Commands

    /// <summary>
    ///     Expands the selection left by one character, creating a rectangular selection.
    ///     Key gesture: Alt+Shift+Left
    /// </summary>
    public static readonly RoutedUICommand BoxSelectLeftByCharacter = Command("BoxSelectLeftByCharacter");

    /// <summary>
    ///     Expands the selection right by one character, creating a rectangular selection.
    ///     Key gesture: Alt+Shift+Right
    /// </summary>
    public static readonly RoutedUICommand BoxSelectRightByCharacter = Command("BoxSelectRightByCharacter");

    /// <summary>
    ///     Expands the selection left by one word, creating a rectangular selection.
    ///     Key gesture: Ctrl+Alt+Shift+Left
    /// </summary>
    public static readonly RoutedUICommand BoxSelectLeftByWord = Command("BoxSelectLeftByWord");

    /// <summary>
    ///     Expands the selection right by one word, creating a rectangular selection.
    ///     Key gesture: Ctrl+Alt+Shift+Right
    /// </summary>
    public static readonly RoutedUICommand BoxSelectRightByWord = Command("BoxSelectRightByWord");

    /// <summary>
    ///     Expands the selection up by one line, creating a rectangular selection.
    ///     Key gesture: Alt+Shift+Up
    /// </summary>
    public static readonly RoutedUICommand BoxSelectUpByLine = Command("BoxSelectUpByLine");

    /// <summary>
    ///     Expands the selection down by one line, creating a rectangular selection.
    ///     Key gesture: Alt+Shift+Down
    /// </summary>
    public static readonly RoutedUICommand BoxSelectDownByLine = Command("BoxSelectDownByLine");

    /// <summary>
    ///     Expands the selection to the start of the line, creating a rectangular selection.
    ///     Key gesture: Alt+Shift+Home
    /// </summary>
    public static readonly RoutedUICommand BoxSelectToLineStart = Command("BoxSelectToLineStart");

    /// <summary>
    ///     Expands the selection to the end of the line, creating a rectangular selection.
    ///     Key gesture: Alt+Shift+End
    /// </summary>
    public static readonly RoutedUICommand BoxSelectToLineEnd = Command("BoxSelectToLineEnd");

    private static RoutedUICommand Command(string name)
    {
        return new RoutedUICommand(name, name, typeof(RectangleSelection));
    }

    #endregion

    private          TextDocument document;
    private readonly int          startLine,     endLine;
    private readonly double       startXPos,     endXPos;
    private readonly int          topLeftOffset, bottomRightOffset;

    private readonly List<SelectionSegment> segments = new();

    #region Constructors

    /// <summary>
    ///     Creates a new rectangular selection.
    /// </summary>
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
        if (startXPos < endXPos) return new TextViewPosition(document.GetLocation(segment.StartOffset), segment.StartVisualColumn);
        return new TextViewPosition(document.GetLocation(segment.EndOffset), segment.EndVisualColumn);
    }

    private TextViewPosition GetEnd()
    {
        var segment = startLine < endLine ? segments.Last() : segments.First();
        if (startXPos < endXPos) return new TextViewPosition(document.GetLocation(segment.EndOffset), segment.EndVisualColumn);
        return new TextViewPosition(document.GetLocation(segment.StartOffset), segment.StartVisualColumn);
    }

    #endregion

    /// <inheritdoc />
    public override string GetText()
    {
        var b = new StringBuilder();
        foreach (ISegment s in Segments)
        {
            if (b.Length > 0) b.AppendLine();
            b.Append(document.GetText(s));
        }

        return b.ToString();
    }

    /// <inheritdoc />
    public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
    {
        return SetEndpoint(endPosition);
    }

    /// <inheritdoc />
    public override int Length { get { return Segments.Sum(s => s.Length); } }

    /// <inheritdoc />
    public override bool EnableVirtualSpace => true;

    /// <inheritdoc />
    public override ISegment SurroundingSegment => new SimpleSegment(topLeftOffset, bottomRightOffset - topLeftOffset);

    /// <inheritdoc />
    public override IEnumerable<SelectionSegment> Segments => segments;

    /// <inheritdoc />
    public override TextViewPosition StartPosition { get; }

    /// <inheritdoc />
    public override TextViewPosition EndPosition { get; }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        var r = obj as RectangleSelection;
        return r != null && r.textArea == textArea && r.topLeftOffset == topLeftOffset && r.bottomRightOffset == bottomRightOffset && r.startLine == startLine && r.endLine == endLine && r.startXPos == startXPos && r.endXPos == endXPos;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return topLeftOffset ^ bottomRightOffset;
    }

    /// <inheritdoc />
    public override Selection SetEndpoint(TextViewPosition endPosition)
    {
        return new RectangleSelection(textArea, startLine, startXPos, endPosition);
    }

    private int GetVisualColumnFromXPos(int line, double xPos)
    {
        var vl = textArea.TextView.GetOrConstructVisualLine(textArea.Document.GetLineByNumber(line));
        return vl.GetVisualColumn(new Point(xPos, 0), true);
    }

    /// <inheritdoc />
    public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
    {
        var newStartLocation = textArea.Document.GetLocation(e.GetNewOffset(topLeftOffset, AnchorMovementType.AfterInsertion));
        var newEndLocation   = textArea.Document.GetLocation(e.GetNewOffset(bottomRightOffset, AnchorMovementType.BeforeInsertion));

        return new RectangleSelection(textArea, new TextViewPosition(newStartLocation, GetVisualColumnFromXPos(newStartLocation.Line, startXPos)), new TextViewPosition(newEndLocation, GetVisualColumnFromXPos(newEndLocation.Line, endXPos)));
    }

    /// <inheritdoc />
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
                // insert same text into every line
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

    private void ReplaceSingleLineText(TextArea textArea, SelectionSegment lineSegment, string newText, out int insertionLength)
    {
        if (lineSegment.Length == 0)
        {
            if (newText.Length > 0 && textArea.ReadOnlySectionProvider.CanInsert(lineSegment.StartOffset))
            {
                newText = AddSpacesIfRequired(newText, new TextViewPosition(document.GetLocation(lineSegment.StartOffset), lineSegment.StartVisualColumn), new TextViewPosition(document.GetLocation(lineSegment.EndOffset), lineSegment.EndVisualColumn));
                textArea.Document.Insert(lineSegment.StartOffset, newText);
            }
        }
        else
        {
            var segmentsToDelete = textArea.GetDeletableSegments(lineSegment);
            for (var i = segmentsToDelete.Length - 1; i >= 0; i--)
                if (i == segmentsToDelete.Length - 1)
                {
                    if (segmentsToDelete[i].Offset == SurroundingSegment.Offset && segmentsToDelete[i].Length == SurroundingSegment.Length) newText = AddSpacesIfRequired(newText, new TextViewPosition(document.GetLocation(lineSegment.StartOffset), lineSegment.StartVisualColumn), new TextViewPosition(document.GetLocation(lineSegment.EndOffset), lineSegment.EndVisualColumn));
                    textArea.Document.Replace(segmentsToDelete[i], newText);
                }
                else
                {
                    textArea.Document.Remove(segmentsToDelete[i]);
                }
        }

        insertionLength = newText.Length;
    }

    /// <summary>
    ///     Performs a rectangular paste operation.
    /// </summary>
    public static bool PerformRectangularPaste(TextArea textArea, TextViewPosition startPosition, string text, bool selectInsertedText)
    {
        if (textArea == null) throw new ArgumentNullException(nameof(textArea));
        if (text == null) throw new ArgumentNullException(nameof(text));
        var newLineCount = text.Count(c => c == '\n'); // TODO might not work in all cases, but single \r line endings are really rare today.
        var endLocation  = new TextLocation(startPosition.Line + newLineCount, startPosition.Column);
        if (endLocation.Line <= textArea.Document.LineCount)
        {
            var endOffset = textArea.Document.GetOffset(endLocation);
            if (textArea.Selection.EnableVirtualSpace || textArea.Document.GetLocation(endOffset) == endLocation)
            {
                var rsel = new RectangleSelection(textArea, startPosition, endLocation.Line, GetXPos(textArea, startPosition));
                rsel.ReplaceSelectionWithText(text);
                if (selectInsertedText && textArea.Selection is RectangleSelection)
                {
                    var sel = (RectangleSelection)textArea.Selection;
                    textArea.Selection = new RectangleSelection(textArea, startPosition, sel.endLine, sel.endXPos);
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Gets the name of the entry in the DataObject that signals rectangle selections.
    /// </summary>
    public const string RectangularSelectionDataType = "AvalonEditRectangularSelection";

    /// <inheritdoc />
    public override DataObject CreateDataObject(TextArea textArea)
    {
        var data = base.CreateDataObject(textArea);

        return data;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        // It's possible that ToString() gets called on old (invalid) selections, e.g. for "change from... to..." debug message
        // make sure we don't crash even when the desired locations don't exist anymore.
        return string.Format("[RectangleSelection {0} {1} {2} to {3} {4} {5}]", startLine, topLeftOffset, startXPos, endLine, bottomRightOffset, endXPos);
    }
}
﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing;

/// <summary>
///     A simple selection.
/// </summary>
internal sealed class SimpleSelection : Selection
{
    private readonly int startOffset, endOffset;

    /// <summary>
    ///     Creates a new SimpleSelection instance.
    /// </summary>
    internal SimpleSelection(TextArea textArea, TextViewPosition start, TextViewPosition end) : base(textArea)
    {
        StartPosition = start;
        EndPosition   = end;
        startOffset   = textArea.Document.GetOffset(start.Location);
        endOffset     = textArea.Document.GetOffset(end.Location);
    }

    /// <inheritdoc />
    public override IEnumerable<SelectionSegment> Segments => ExtensionMethods.Sequence(new SelectionSegment(startOffset, StartPosition.VisualColumn, endOffset, EndPosition.VisualColumn));

    /// <inheritdoc />
    public override ISegment SurroundingSegment => new SelectionSegment(startOffset, endOffset);

    /// <inheritdoc />
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
                    if (string.IsNullOrEmpty(newText))
                    {
                        // place caret at the beginning of the selection
                        if (StartPosition.CompareTo(EndPosition) <= 0) textArea.Caret.Position = StartPosition;
                        else textArea.Caret.Position                                           = EndPosition;
                    }
                    else
                    {
                        // place caret so that it ends up behind the new text
                        textArea.Caret.Offset = segmentsToDelete[i].EndOffset;
                    }

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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool IsEmpty => startOffset == endOffset && StartPosition.VisualColumn == EndPosition.VisualColumn;

    /// <inheritdoc />
    public override int Length => Math.Abs(endOffset - startOffset);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return startOffset * 27811 + endOffset + textArea.GetHashCode();
        }
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (obj is not SimpleSelection other) return false;
        return StartPosition.Equals(other.StartPosition) && EndPosition.Equals(other.EndPosition) && startOffset == other.startOffset && endOffset == other.endOffset && textArea == other.textArea;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "[SimpleSelection Start=" + StartPosition + " End=" + EndPosition + "]";
    }
}
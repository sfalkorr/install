using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting;

public class HighlightedLine
{
    public HighlightedLine(IDocument document, IDocumentLine documentLine)
    {
        Document     = document ?? throw new ArgumentNullException(nameof(document));
        DocumentLine = documentLine;
        Sections     = new NullSafeCollection<HighlightedSection>();
    }

    public IDocument Document { get; }

    public IDocumentLine DocumentLine { get; }

    public IList<HighlightedSection> Sections { get; }

    public void ValidateInvariants()
    {
        var line            = this;
        var lineStartOffset = line.DocumentLine.Offset;
        var lineEndOffset   = line.DocumentLine.EndOffset;
        for (var i = 0; i < line.Sections.Count; i++)
        {
            var s1 = line.Sections[i];
            if (s1.Offset < lineStartOffset || s1.Length < 0 || s1.Offset + s1.Length > lineEndOffset) throw new InvalidOperationException("Section is outside line bounds");
            for (var j = i + 1; j < line.Sections.Count; j++)
            {
                var s2 = line.Sections[j];
                if (s2.Offset >= s1.Offset + s1.Length)
                {
                }
                else if (s2.Offset >= s1.Offset && s2.Offset + s2.Length <= s1.Offset + s1.Length)
                {
                }
                else
                {
                    throw new InvalidOperationException("Sections are overlapping or incorrectly sorted.");
                }
            }
        }
    }

    #region Merge

    public void MergeWith(HighlightedLine additionalLine)
    {
        if (additionalLine == null) return;
#if DEBUG
        ValidateInvariants();
        additionalLine.ValidateInvariants();
#endif

        var pos                     = 0;
        var activeSectionEndOffsets = new Stack<int>();
        var lineEndOffset           = DocumentLine.EndOffset;
        activeSectionEndOffsets.Push(lineEndOffset);
        foreach (var newSection in additionalLine.Sections)
        {
            var newSectionStart = newSection.Offset;
            while (pos < Sections.Count)
            {
                var s = Sections[pos];
                if (newSection.Offset < s.Offset) break;
                while (s.Offset > activeSectionEndOffsets.Peek()) activeSectionEndOffsets.Pop();
                activeSectionEndOffsets.Push(s.Offset + s.Length);
                pos++;
            }

            var insertionStack = new Stack<int>(activeSectionEndOffsets.Reverse());
            int i;
            for (i = pos; i < Sections.Count; i++)
            {
                var s = Sections[i];
                if (newSection.Offset + newSection.Length <= s.Offset) break;
                Insert(ref i, ref newSectionStart, s.Offset, newSection.Color, insertionStack);

                while (s.Offset > insertionStack.Peek()) insertionStack.Pop();
                insertionStack.Push(s.Offset + s.Length);
            }

            Insert(ref i, ref newSectionStart, newSection.Offset + newSection.Length, newSection.Color, insertionStack);
        }

#if DEBUG
        ValidateInvariants();
#endif
    }

    private void Insert(ref int pos, ref int newSectionStart, int insertionEndPos, HighlightingColor color, Stack<int> insertionStack)
    {
        if (newSectionStart >= insertionEndPos) return;

        while (insertionStack.Peek() <= newSectionStart) insertionStack.Pop();
        while (insertionStack.Peek() < insertionEndPos)
        {
            var end = insertionStack.Pop();
            if (end > newSectionStart)
            {
                Sections.Insert(pos++, new HighlightedSection { Offset = newSectionStart, Length = end - newSectionStart, Color = color });
                newSectionStart = end;
            }
        }

        if (insertionEndPos > newSectionStart)
        {
            Sections.Insert(pos++, new HighlightedSection { Offset = newSectionStart, Length = insertionEndPos - newSectionStart, Color = color });
            newSectionStart = insertionEndPos;
        }
    }

    #endregion

    #region WriteTo / ToHtml

    #endregion

    [Obsolete("Use ToRichText() instead")]
    public HighlightedInlineBuilder ToInlineBuilder()
    {
        var builder     = new HighlightedInlineBuilder(Document.GetText(DocumentLine));
        var startOffset = DocumentLine.Offset;
        foreach (var section in Sections) builder.SetHighlighting(section.Offset - startOffset, section.Length, section.Color);
        return builder;
    }
}
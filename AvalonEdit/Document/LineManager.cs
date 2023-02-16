using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AvalonEdit.Document;

internal sealed class LineManager
{
    #region Constructor

    private readonly TextDocument     document;
    private readonly DocumentLineTree documentLineTree;

    private ILineTracker[] lineTrackers;

    internal void UpdateListOfLineTrackers()
    {
        lineTrackers = document.LineTrackers.ToArray();
    }

    public LineManager(DocumentLineTree documentLineTree, TextDocument document)
    {
        this.document         = document;
        this.documentLineTree = documentLineTree;
        UpdateListOfLineTrackers();

        Rebuild();
    }

    #endregion

    #region Change events

    #endregion

    #region Rebuild

    public void Rebuild()
    {
        var ls = documentLineTree.GetByNumber(1);
        for (var line = ls.NextLine; line != null; line = line.NextLine)
        {
            line.isDeleted = true;
            line.parent    = line.left = line.right = null;
        }

        ls.ResetLine();
        var ds               = NewLineFinder.NextNewLine(document, 0);
        var lines            = new List<DocumentLine>();
        var lastDelimiterEnd = 0;
        while (ds != SimpleSegment.Invalid)
        {
            ls.TotalLength     = ds.Offset + ds.Length - lastDelimiterEnd;
            ls.DelimiterLength = ds.Length;
            lastDelimiterEnd   = ds.Offset + ds.Length;
            lines.Add(ls);

            ls = new DocumentLine(document);
            ds = NewLineFinder.NextNewLine(document, lastDelimiterEnd);
        }

        ls.TotalLength = document.TextLength - lastDelimiterEnd;
        lines.Add(ls);
        documentLineTree.RebuildTree(lines);
        foreach (var lineTracker in lineTrackers) lineTracker.RebuildDocument();
    }

    #endregion

    #region Remove

    #endregion

    #region Insert

    public void Insert(int offset, ITextSource text)
    {
        var line       = documentLineTree.GetByOffset(offset);
        var lineOffset = line.Offset;

        Debug.Assert(offset <= lineOffset + line.TotalLength);
        if (offset > lineOffset + line.Length)
        {
            Debug.Assert(line.DelimiterLength == 2);
            SetLineLength(line, line.TotalLength - 1);
            line = InsertLineAfter(line, 1);
            line = SetLineLength(line, 1);
        }

        var ds = NewLineFinder.NextNewLine(text, 0);
        if (ds == SimpleSegment.Invalid)
        {
            SetLineLength(line, line.TotalLength + text.TextLength);
            return;
        }

        var lastDelimiterEnd = 0;
        while (ds != SimpleSegment.Invalid)
        {
            var lineBreakOffset = offset + ds.Offset + ds.Length;
            lineOffset = line.Offset;
            var lengthAfterInsertionPos = lineOffset + line.TotalLength - (offset + lastDelimiterEnd);
            line = SetLineLength(line, lineBreakOffset - lineOffset);
            var newLine = InsertLineAfter(line, lengthAfterInsertionPos);
            newLine = SetLineLength(newLine, lengthAfterInsertionPos);

            line             = newLine;
            lastDelimiterEnd = ds.Offset + ds.Length;

            ds = NewLineFinder.NextNewLine(text, lastDelimiterEnd);
        }

        if (lastDelimiterEnd != text.TextLength) SetLineLength(line, line.TotalLength + text.TextLength - lastDelimiterEnd);
    }

    private DocumentLine InsertLineAfter(DocumentLine line, int length)
    {
        var newLine = documentLineTree.InsertLineAfter(line, length);
        foreach (var lt in lineTrackers) lt.LineInserted(line, newLine);
        return newLine;
    }

    #endregion

    #region SetLineLength

    private DocumentLine SetLineLength(DocumentLine line, int newTotalLength)
    {
        var delta = newTotalLength - line.TotalLength;
        if (delta != 0)
        {
            foreach (var lt in lineTrackers) lt.SetLineLength(line, newTotalLength);
            line.TotalLength = newTotalLength;
            DocumentLineTree.UpdateAfterChildrenChange(line);
        }

        if (newTotalLength == 0)
        {
            line.DelimiterLength = 0;
        }
        else
        {
            var lineOffset = line.Offset;
            var lastChar   = document.GetCharAt(lineOffset + newTotalLength - 1);
            if (lastChar == '\r')
            {
                line.DelimiterLength = 1;
            }
            else if (lastChar == '\n')
            {
                if (newTotalLength >= 2 && document.GetCharAt(lineOffset + newTotalLength - 2) == '\r')
                {
                    line.DelimiterLength = 2;
                }
                else if (newTotalLength == 1 && lineOffset > 0 && document.GetCharAt(lineOffset - 1) == '\r')
                {
                    var previousLine = line.PreviousLine;
                    return SetLineLength(previousLine, previousLine.TotalLength + 1);
                }
                else
                {
                    line.DelimiterLength = 1;
                }
            }
            else
            {
                line.DelimiterLength = 0;
            }
        }

        return line;
    }

    #endregion

    #region ChangeComplete

    public void ChangeComplete(DocumentChangeEventArgs e)
    {
        foreach (var lt in lineTrackers) lt.ChangeComplete(e);
    }

    #endregion
}
using System;
using System.Text;

namespace AvalonEdit.Document;

internal static class NewLineFinder
{
    private static readonly char[] newline = { '\r', '\n' };

    internal static readonly string[] NewlineStrings = { "\r\n", "\r", "\n" };

    internal static SimpleSegment NextNewLine(string text, int offset)
    {
        var pos = text.IndexOfAny(newline, offset);
        if (pos < 0) return SimpleSegment.Invalid;
        if (text[pos] != '\r') return new SimpleSegment(pos, 1);
        if (pos + 1 < text.Length && text[pos + 1] == '\n') return new SimpleSegment(pos, 2);
        return new SimpleSegment(pos, 1);
    }

    internal static SimpleSegment NextNewLine(ITextSource text, int offset)
    {
        var textLength = text.TextLength;
        var pos        = text.IndexOfAny(newline, offset, textLength - offset);
        if (pos < 0) return SimpleSegment.Invalid;
        if (text.GetCharAt(pos) != '\r') return new SimpleSegment(pos, 1);
        if (pos + 1 < textLength && text.GetCharAt(pos + 1) == '\n') return new SimpleSegment(pos, 2);
        return new SimpleSegment(pos, 1);
    }
}

partial class TextUtilities
{
    public static bool IsNewLine(string newLine)
    {
        return newLine is "\r\n" or "\n" or "\r";
    }

    public static string NormalizeNewLines(string input, string newLine)
    {
        if (input == null) return null;
        if (!IsNewLine(newLine)) throw new ArgumentException("newLine must be one of the known newline sequences");
        var ds = NewLineFinder.NextNewLine(input, 0);
        if (ds == SimpleSegment.Invalid) return input;
        var b             = new StringBuilder(input.Length);
        var lastEndOffset = 0;
        do
        {
            b.Append(input, lastEndOffset, ds.Offset - lastEndOffset);
            b.Append(newLine);
            lastEndOffset = ds.EndOffset;
            ds            = NewLineFinder.NextNewLine(input, lastEndOffset);
        }
        while (ds != SimpleSegment.Invalid);

        b.Append(input, lastEndOffset, input.Length - lastEndOffset);
        return b.ToString();
    }

    public static string GetNewLineFromDocument(IDocument document, int lineNumber)
    {
        var line = document.GetLineByNumber(lineNumber);
        if (line.DelimiterLength != 0) return document.GetText(line.Offset + line.Length, line.DelimiterLength);
        line = line.PreviousLine;
        return line == null ? Environment.NewLine : document.GetText(line.Offset + line.Length, line.DelimiterLength);
    }
}
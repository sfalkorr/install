using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace AvalonEdit.Document;

public interface ITextSource
{
    ITextSourceVersion Version { get; }

    ITextSource CreateSnapshot();

    ITextSource CreateSnapshot(int offset, int length);

    TextReader CreateReader();

    TextReader CreateReader(int offset, int length);

    int TextLength { get; }

    [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
    string Text { get; }

    char GetCharAt(int offset);

    string GetText(int offset, int length);

    string GetText(ISegment segment);

    void WriteTextTo(TextWriter writer);

    void WriteTextTo(TextWriter writer, int offset, int length);

    int IndexOf(char c, int startIndex, int count);

    int IndexOfAny(char[] anyOf, int startIndex, int count);

    int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType);

    int LastIndexOf(char c, int startIndex, int count);

    int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType);
}

public interface ITextSourceVersion
{
    bool BelongsToSameDocumentAs(ITextSourceVersion other);

    int CompareAge(ITextSourceVersion other);

    IEnumerable<TextChangeEventArgs> GetChangesTo(ITextSourceVersion other);

    int MoveOffsetTo(ITextSourceVersion other, int oldOffset, AnchorMovementType movement = AnchorMovementType.Default);
}

[Serializable]
public class StringTextSource : ITextSource
{
    public static readonly StringTextSource Empty = new(string.Empty);

    public StringTextSource(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public StringTextSource(string text, ITextSourceVersion version)
    {
        Text    = text ?? throw new ArgumentNullException(nameof(text));
        Version = version;
    }

    public ITextSourceVersion Version { get; }

    public int TextLength => Text.Length;

    public string Text { get; }

    public ITextSource CreateSnapshot()
    {
        return this;
    }

    public ITextSource CreateSnapshot(int offset, int length)
    {
        return new StringTextSource(Text.Substring(offset, length));
    }

    public TextReader CreateReader()
    {
        return new StringReader(Text);
    }

    public TextReader CreateReader(int offset, int length)
    {
        return new StringReader(Text.Substring(offset, length));
    }

    public void WriteTextTo(TextWriter writer)
    {
        writer.Write(Text);
    }

    public void WriteTextTo(TextWriter writer, int offset, int length)
    {
        writer.Write(Text.Substring(offset, length));
    }

    public char GetCharAt(int offset)
    {
        return Text[offset];
    }

    public string GetText(int offset, int length)
    {
        return Text.Substring(offset, length);
    }

    public string GetText(ISegment segment)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        return Text.Substring(segment.Offset, segment.Length);
    }

    public int IndexOf(char c, int startIndex, int count)
    {
        return Text.IndexOf(c, startIndex, count);
    }

    public int IndexOfAny(char[] anyOf, int startIndex, int count)
    {
        return Text.IndexOfAny(anyOf, startIndex, count);
    }

    public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
    {
        return Text.IndexOf(searchText, startIndex, count, comparisonType);
    }

    public int LastIndexOf(char c, int startIndex, int count)
    {
        return Text.LastIndexOf(c, startIndex + count - 1, count);
    }

    public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
    {
        return Text.LastIndexOf(searchText, startIndex + count - 1, count, comparisonType);
    }
}
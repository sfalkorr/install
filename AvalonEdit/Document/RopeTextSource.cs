using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using AvalonEdit.Utils;

namespace AvalonEdit.Document;

[Serializable]
public sealed class RopeTextSource : ITextSource
{
    private readonly Rope<char> rope;

    public RopeTextSource(Rope<char> rope)
    {
        if (rope == null) throw new ArgumentNullException(nameof(rope));
        this.rope = rope.Clone();
    }

    public RopeTextSource(Rope<char> rope, ITextSourceVersion version)
    {
        if (rope == null) throw new ArgumentNullException(nameof(rope));
        this.rope = rope.Clone();
        Version   = version;
    }

    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not a property because it creates a clone")]
    public Rope<char> GetRope()
    {
        return rope.Clone();
    }

    public string Text => rope.ToString();

    public int TextLength => rope.Length;

    public char GetCharAt(int offset)
    {
        return rope[offset];
    }

    public string GetText(int offset, int length)
    {
        return rope.ToString(offset, length);
    }

    public string GetText(ISegment segment)
    {
        return rope.ToString(segment.Offset, segment.Length);
    }

    public TextReader CreateReader()
    {
        return new RopeTextReader(rope);
    }

    public TextReader CreateReader(int offset, int length)
    {
        return new RopeTextReader(rope.GetRange(offset, length));
    }

    public ITextSource CreateSnapshot()
    {
        return this;
    }

    public ITextSource CreateSnapshot(int offset, int length)
    {
        return new RopeTextSource(rope.GetRange(offset, length));
    }

    public int IndexOf(char c, int startIndex, int count)
    {
        return rope.IndexOf(c, startIndex, count);
    }

    public int IndexOfAny(char[] anyOf, int startIndex, int count)
    {
        return rope.IndexOfAny(anyOf, startIndex, count);
    }

    public int LastIndexOf(char c, int startIndex, int count)
    {
        return rope.LastIndexOf(c, startIndex, count);
    }

    public ITextSourceVersion Version { get; }

    public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
    {
        return rope.IndexOf(searchText, startIndex, count, comparisonType);
    }

    public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
    {
        return rope.LastIndexOf(searchText, startIndex, count, comparisonType);
    }

    public void WriteTextTo(TextWriter writer)
    {
        rope.WriteTo(writer, 0, rope.Length);
    }

    public void WriteTextTo(TextWriter writer, int offset, int length)
    {
        rope.WriteTo(writer, offset, length);
    }
}
using System;

namespace AvalonEdit.Utils;

public struct StringSegment : IEquatable<StringSegment>
{
    public StringSegment(string text, int offset, int count)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (offset < 0 || offset > text.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        if (offset + count > text.Length) throw new ArgumentOutOfRangeException(nameof(count));
        Text   = text;
        Offset = offset;
        Count  = count;
    }

    public StringSegment(string text)
    {
        Text   = text ?? throw new ArgumentNullException(nameof(text));
        Offset = 0;
        Count  = text.Length;
    }

    public string Text { get; }

    public int Offset { get; }

    public int Count { get; }

    #region Equals and GetHashCode implementation

    public override bool Equals(object obj)
    {
        return obj is StringSegment && Equals((StringSegment)obj);
    }

    public bool Equals(StringSegment other)
    {
        return ReferenceEquals(Text, other.Text) && Offset == other.Offset && Count == other.Count;
    }

    public override int GetHashCode()
    {
        return Text.GetHashCode() ^ Offset ^ Count;
    }

    public static bool operator ==(StringSegment left, StringSegment right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StringSegment left, StringSegment right)
    {
        return !left.Equals(right);
    }

    #endregion
}
using System;
using System.ComponentModel;
using System.Globalization;

namespace AvalonEdit.Document;

[Serializable]
//[TypeConverter(typeof(TextLocationConverter))]
public struct TextLocation : IComparable<TextLocation>, IEquatable<TextLocation>
{
    public static readonly TextLocation Empty = new(0, 0);

    public TextLocation(int line, int column)
    {
        Line   = line;
        Column = column;
    }

    public int Line { get; }

    public int Column { get; }

    public bool IsEmpty => Column <= 0 && Line <= 0;

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "(Line {1}, Col {0})", Column, Line);
    }

    public override int GetHashCode()
    {
        return unchecked((191 * Column.GetHashCode()) ^ Line.GetHashCode());
    }

    public override bool Equals(object obj)
    {
        if (!(obj is TextLocation)) return false;
        return (TextLocation)obj == this;
    }

    public bool Equals(TextLocation other)
    {
        return this == other;
    }

    public static bool operator ==(TextLocation left, TextLocation right)
    {
        return left.Column == right.Column && left.Line == right.Line;
    }

    public static bool operator !=(TextLocation left, TextLocation right)
    {
        return left.Column != right.Column || left.Line != right.Line;
    }

    public static bool operator <(TextLocation left, TextLocation right)
    {
        if (left.Line < right.Line) return true;
        if (left.Line == right.Line) return left.Column < right.Column;
        return false;
    }

    public static bool operator >(TextLocation left, TextLocation right)
    {
        if (left.Line > right.Line) return true;
        if (left.Line == right.Line) return left.Column > right.Column;
        return false;
    }

    public static bool operator <=(TextLocation left, TextLocation right)
    {
        return !(left > right);
    }

    public static bool operator >=(TextLocation left, TextLocation right)
    {
        return !(left < right);
    }

    public int CompareTo(TextLocation other)
    {
        if (this == other) return 0;
        if (this < other) return -1;
        return 1;
    }
}

// public class TextLocationConverter : TypeConverter
// {
//     public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
//     {
//         return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
//     }
//
//     public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
//     {
//         return destinationType == typeof(TextLocation) || base.CanConvertTo(context, destinationType);
//     }
//
//     public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
//     {
//         if (value is not string s) return base.ConvertFrom(context, culture, value);
//         var parts = s.Split(';', ',');
//         return parts.Length == 2 ? new TextLocation(int.Parse(parts[0], culture), int.Parse(parts[1], culture)) : base.ConvertFrom(context, culture, value);
//     }
//
//     public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
//     {
//         if (value is not TextLocation loc || destinationType != typeof(string)) return base.ConvertTo(context, culture, value, destinationType);
//         return loc.Line.ToString(culture) + ";" + loc.Column.ToString(culture);
//     }
// }

public interface ISegment
{
    int Offset { get; }

    int Length { get; }

    int EndOffset { get; }
}

public static class ISegmentExtensions
{
    public static bool Contains(this ISegment segment, int offset, int length)
    {
        return segment.Offset <= offset && offset + length <= segment.EndOffset;
    }

    public static bool Contains(this ISegment thisSegment, ISegment segment)
    {
        return segment != null && thisSegment.Offset <= segment.Offset && segment.EndOffset <= thisSegment.EndOffset;
    }
}
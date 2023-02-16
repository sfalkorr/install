using System;
using System.Globalization;
using AvalonEdit.Document;

namespace AvalonEdit;

public struct TextViewPosition : IEquatable<TextViewPosition>, IComparable<TextViewPosition>
{
    public TextLocation Location
    {
        get => new(Line, Column);
        set
        {
            Line   = value.Line;
            Column = value.Column;
        }
    }

    public int Line { get; set; }

    public int Column { get; set; }

    public int VisualColumn { get; set; }

    public bool IsAtEndOfLine { get; set; }

    public TextViewPosition(int line, int column, int visualColumn = -1)
    {
        Line          = line;
        Column        = column;
        VisualColumn  = visualColumn;
        IsAtEndOfLine = false;
    }

    public TextViewPosition(TextLocation location, int visualColumn = -1)
    {
        Line          = location.Line;
        Column        = location.Column;
        VisualColumn  = visualColumn;
        IsAtEndOfLine = false;
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "[TextViewPosition Line={0} Column={1} VisualColumn={2} IsAtEndOfLine={3}]", Line, Column, VisualColumn, IsAtEndOfLine);
    }

    #region Equals and GetHashCode implementation

    public override bool Equals(object obj)
    {
        return obj is TextViewPosition position && Equals(position);
    }

    public override int GetHashCode()
    {
        var hashCode = IsAtEndOfLine ? 115817 : 0;
        unchecked
        {
            hashCode += 1000000007 * Line.GetHashCode();
            hashCode += 1000000009 * Column.GetHashCode();
            hashCode += 1000000021 * VisualColumn.GetHashCode();
        }

        return hashCode;
    }

    public bool Equals(TextViewPosition other)
    {
        return Line == other.Line && Column == other.Column && VisualColumn == other.VisualColumn && IsAtEndOfLine == other.IsAtEndOfLine;
    }

    public static bool operator ==(TextViewPosition left, TextViewPosition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextViewPosition left, TextViewPosition right)
    {
        return !left.Equals(right);
    }

    #endregion

    public int CompareTo(TextViewPosition other)
    {
        var r = Location.CompareTo(other.Location);
        if (r != 0) return r;
        r = VisualColumn.CompareTo(other.VisualColumn);
        if (r != 0) return r;
        return IsAtEndOfLine switch
               {
                   true when !other.IsAtEndOfLine => -1,
                   false when other.IsAtEndOfLine => 1,
                   _                              => 0
               };
    }
}
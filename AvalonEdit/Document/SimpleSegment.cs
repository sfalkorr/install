using System;
using System.Diagnostics;
using System.Globalization;
using AvalonEdit.Utils;

namespace AvalonEdit.Document;

internal readonly struct SimpleSegment : IEquatable<SimpleSegment>, ISegment
{
    public static readonly SimpleSegment Invalid = new(-1, -1);

    public static SimpleSegment GetOverlap(ISegment segment1, ISegment segment2)
    {
        var start = Math.Max(segment1.Offset, segment2.Offset);
        var end   = Math.Min(segment1.EndOffset, segment2.EndOffset);
        return end < start ? Invalid : new SimpleSegment(start, end - start);
    }

    public readonly int Offset, Length;

    int ISegment.Offset => Offset;

    int ISegment.Length => Length;

    public int EndOffset => Offset + Length;

    public SimpleSegment(int offset, int length)
    {
        Offset = offset;
        Length = length;
    }

    public SimpleSegment(ISegment segment)
    {
        Debug.Assert(segment != null);
        Offset = segment.Offset;
        Length = segment.Length;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Offset + 10301 * Length;
        }
    }

    public override bool Equals(object obj)
    {
        return obj is SimpleSegment && Equals((SimpleSegment)obj);
    }

    public bool Equals(SimpleSegment other)
    {
        return Offset == other.Offset && Length == other.Length;
    }

    public static bool operator ==(SimpleSegment left, SimpleSegment right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SimpleSegment left, SimpleSegment right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return "[Offset=" + Offset.ToString(CultureInfo.InvariantCulture) + ", Length=" + Length.ToString(CultureInfo.InvariantCulture) + "]";
    }
}

public sealed class AnchorSegment : ISegment
{
    private readonly TextAnchor start, end;

    public int Offset => start.Offset;

    public int Length => Math.Max(0, end.Offset - start.Offset);

    public int EndOffset => Math.Max(start.Offset, end.Offset);

    public AnchorSegment(TextAnchor start, TextAnchor end)
    {
        if (start == null) throw new ArgumentNullException(nameof(start));
        if (end == null) throw new ArgumentNullException(nameof(end));
        if (!start.SurviveDeletion) throw new ArgumentException("Anchors for AnchorSegment must use SurviveDeletion", nameof(start));
        if (!end.SurviveDeletion) throw new ArgumentException("Anchors for AnchorSegment must use SurviveDeletion", nameof(end));
        this.start = start;
        this.end   = end;
    }

    public AnchorSegment(TextDocument document, ISegment segment) : this(document, ThrowUtil.CheckNotNull(segment, "segment").Offset, segment.Length)
    {
    }

    public AnchorSegment(TextDocument document, int offset, int length)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        start                 = document.CreateAnchor(offset);
        start.SurviveDeletion = true;
        start.MovementType    = AnchorMovementType.AfterInsertion;
        end                   = document.CreateAnchor(offset + length);
        end.SurviveDeletion   = true;
        end.MovementType      = AnchorMovementType.BeforeInsertion;
    }

    public override string ToString()
    {
        return "[Offset=" + Offset.ToString(CultureInfo.InvariantCulture) + ", EndOffset=" + EndOffset.ToString(CultureInfo.InvariantCulture) + "]";
    }
}
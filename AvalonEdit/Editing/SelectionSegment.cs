using System;
using AvalonEdit.Document;

namespace AvalonEdit.Editing;

public class SelectionSegment : ISegment
{
    public SelectionSegment(int startOffset, int endOffset)
    {
        StartOffset       = Math.Min(startOffset, endOffset);
        EndOffset         = Math.Max(startOffset, endOffset);
        StartVisualColumn = EndVisualColumn = -1;
    }

    public SelectionSegment(int startOffset, int startVC, int endOffset, int endVC)
    {
        if (startOffset < endOffset || (startOffset == endOffset && startVC <= endVC))
        {
            StartOffset       = startOffset;
            StartVisualColumn = startVC;
            EndOffset         = endOffset;
            EndVisualColumn   = endVC;
        }
        else
        {
            StartOffset       = endOffset;
            StartVisualColumn = endVC;
            EndOffset         = startOffset;
            EndVisualColumn   = startVC;
        }
    }

    public int StartOffset { get; }

    public int EndOffset { get; }

    public int StartVisualColumn { get; }

    public int EndVisualColumn { get; }

    int ISegment.Offset => StartOffset;

    public int Length => EndOffset - StartOffset;

    public override string ToString()
    {
        return $"[SelectionSegment StartOffset={StartOffset}, EndOffset={EndOffset}, StartVC={StartVisualColumn}, EndVC={EndVisualColumn}]";
    }
}
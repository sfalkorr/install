﻿using AvalonEdit.Document;

namespace AvalonEdit.Highlighting;

public class HighlightedSection : ISegment
{
    public int Offset { get; set; }

    public int Length { get; set; }

    int ISegment.EndOffset => Offset + Length;

    public HighlightingColor Color { get; set; }

    public override string ToString()
    {
        return $"[HighlightedSection ({Offset}-{Offset + Length})={Color}]";
    }
}
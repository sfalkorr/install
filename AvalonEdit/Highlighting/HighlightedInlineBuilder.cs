using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting;

[Obsolete("Use RichText / RichTextModel instead")]
public sealed class HighlightedInlineBuilder
{
    private static HighlightingBrush MakeBrush(Brush b)
    {
        if (b is SolidColorBrush scb) return new SimpleHighlightingBrush(scb);
        return null;
    }

    private List<int>               stateChangeOffsets = new();
    private List<HighlightingColor> stateChanges       = new();

    private int GetIndexForOffset(int offset)
    {
        if (offset < 0 || offset > Text.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        var index = stateChangeOffsets.BinarySearch(offset);
        if (index >= 0) return index;
        index = ~index;
        if (offset >= Text.Length) return index;
        stateChanges.Insert(index, stateChanges[index - 1].Clone());
        stateChangeOffsets.Insert(index, offset);

        return index;
    }

    public HighlightedInlineBuilder(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        stateChangeOffsets.Add(0);
        stateChanges.Add(new HighlightingColor());
    }


    private HighlightedInlineBuilder(string text, List<int> offsets, List<HighlightingColor> states)
    {
        Text               = text;
        stateChangeOffsets = offsets;
        stateChanges       = states;
    }

    public string Text { get; }

    public void SetHighlighting(int offset, int length, HighlightingColor color)
    {
        if (color == null) throw new ArgumentNullException(nameof(color));
        if (color.Foreground == null && color.Background == null && color.FontStyle == null && color.FontWeight == null && color.Underline == null) return;
        var startIndex = GetIndexForOffset(offset);
        var endIndex   = GetIndexForOffset(offset + length);
        for (var i = startIndex; i < endIndex; i++) stateChanges[i].MergeWith(color);
    }

    public void SetForeground(int offset, int length, Brush brush)
    {
        var startIndex                                                         = GetIndexForOffset(offset);
        var endIndex                                                           = GetIndexForOffset(offset + length);
        var hbrush                                                             = MakeBrush(brush);
        for (var i = startIndex; i < endIndex; i++) stateChanges[i].Foreground = hbrush;
    }

    public void SetBackground(int offset, int length, Brush brush)
    {
        var startIndex                                                         = GetIndexForOffset(offset);
        var endIndex                                                           = GetIndexForOffset(offset + length);
        var hbrush                                                             = MakeBrush(brush);
        for (var i = startIndex; i < endIndex; i++) stateChanges[i].Background = hbrush;
    }

    public void SetFontWeight(int offset, int length, FontWeight weight)
    {
        var startIndex                                                         = GetIndexForOffset(offset);
        var endIndex                                                           = GetIndexForOffset(offset + length);
        for (var i = startIndex; i < endIndex; i++) stateChanges[i].FontWeight = weight;
    }

    public void SetFontStyle(int offset, int length, FontStyle style)
    {
        var startIndex                                                        = GetIndexForOffset(offset);
        var endIndex                                                          = GetIndexForOffset(offset + length);
        for (var i = startIndex; i < endIndex; i++) stateChanges[i].FontStyle = style;
    }


    public HighlightedInlineBuilder Clone()
    {
        return new HighlightedInlineBuilder(Text, stateChangeOffsets.ToList(), stateChanges.Select(sc => sc.Clone()).ToList());
    }
}
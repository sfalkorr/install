﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Highlighting;

/// <summary>
///     Represents a immutable piece text with highlighting information.
/// </summary>
public class RichText
{
    /// <summary>
    ///     The empty string without any formatting information.
    /// </summary>
    public static readonly RichText Empty = new(string.Empty);

    internal readonly int[]               stateChangeOffsets;
    internal readonly HighlightingColor[] stateChanges;

    /// <summary>
    ///     Creates a RichText instance with the given text and RichTextModel.
    /// </summary>
    /// <param name="text">
    ///     The text to use in this RichText instance.
    /// </param>
    /// <param name="model">
    ///     The model that contains the formatting to use for this RichText instance.
    ///     <c>model.DocumentLength</c> should correspond to <c>text.Length</c>.
    ///     This parameter may be null, in which case the RichText instance just holds plain text.
    /// </param>
    public RichText(string text, RichTextModel model = null)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        Text = text;
        if (model != null)
        {
            var sections = model.GetHighlightedSections(0, text.Length).ToArray();
            stateChangeOffsets = new int[sections.Length];
            stateChanges       = new HighlightingColor[sections.Length];
            for (var i = 0; i < sections.Length; i++)
            {
                stateChangeOffsets[i] = sections[i].Offset;
                stateChanges[i]       = sections[i].Color;
            }
        }
        else
        {
            stateChangeOffsets = new[] { 0 };
            stateChanges       = new[] { HighlightingColor.Empty };
        }
    }

    internal RichText(string text, int[] offsets, HighlightingColor[] states)
    {
        Text = text;
        Debug.Assert(offsets[0] == 0);
        Debug.Assert(offsets.Last() <= text.Length);
        stateChangeOffsets = offsets;
        stateChanges       = states;
    }

    /// <summary>
    ///     Gets the text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Gets the text length.
    /// </summary>
    public int Length => Text.Length;

    private int GetIndexForOffset(int offset)
    {
        if (offset < 0 || offset > Text.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        var index = Array.BinarySearch(stateChangeOffsets, offset);
        if (index < 0)
            // If no color change exists directly at offset,
            // return the index of the color segment that contains offset.
            index = ~index - 1;
        return index;
    }

    private int GetEnd(int index)
    {
        // Gets the end of the color segment no. index.
        if (index + 1 < stateChangeOffsets.Length) return stateChangeOffsets[index + 1];
        return Text.Length;
    }

    /// <summary>
    ///     Gets the HighlightingColor for the specified offset.
    /// </summary>
    public HighlightingColor GetHighlightingAt(int offset)
    {
        return stateChanges[GetIndexForOffset(offset)];
    }

    /// <summary>
    ///     Retrieves the highlighted sections in the specified range.
    ///     The highlighted sections will be sorted by offset, and there will not be any nested or overlapping sections.
    /// </summary>
    public IEnumerable<HighlightedSection> GetHighlightedSections(int offset, int length)
    {
        var index     = GetIndexForOffset(offset);
        var pos       = offset;
        var endOffset = offset + length;
        while (pos < endOffset)
        {
            var endPos = Math.Min(endOffset, GetEnd(index));
            yield return new HighlightedSection { Offset = pos, Length = endPos - pos, Color = stateChanges[index] };
            pos = endPos;
            index++;
        }
    }

    /// <summary>
    ///     Creates a new RichTextModel with the formatting from this RichText.
    /// </summary>
    public RichTextModel ToRichTextModel()
    {
        return new RichTextModel(stateChangeOffsets, stateChanges.Select(ch => ch.Clone()).ToArray());
    }

    /// <summary>
    ///     Gets the text.
    /// </summary>
    public override string ToString()
    {
        return Text;
    }

    /// <summary>
    ///     Creates WPF Run instances that can be used for TextBlock.Inlines.
    /// </summary>
    public Run[] CreateRuns()
    {
        var runs = new Run[stateChanges.Length];
        for (var i = 0; i < runs.Length; i++)
        {
            var startOffset = stateChangeOffsets[i];
            var endOffset   = i + 1 < stateChangeOffsets.Length ? stateChangeOffsets[i + 1] : Text.Length;
            var r           = new Run(Text.Substring(startOffset, endOffset - startOffset));
            var state       = stateChanges[i];
            ApplyColorToTextElement(r, state);
            runs[i] = r;
        }

        return runs;
    }

    internal static void ApplyColorToTextElement(TextElement r, HighlightingColor state)
    {
        if (state.Foreground != null) r.Foreground = state.Foreground.GetBrush(null);
        if (state.Background != null) r.Background = state.Background.GetBrush(null);
        if (state.FontWeight != null) r.FontWeight = state.FontWeight.Value;
        if (state.FontStyle != null) r.FontStyle   = state.FontStyle.Value;
    }

    /// <summary>
    ///     Produces HTML code for the line, with &lt;span style="..."&gt; tags.
    /// </summary>
    public string ToHtml(HtmlOptions options = null)
    {
        var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options))
        {
            htmlWriter.Write(this);
        }

        return stringWriter.ToString();
    }

    /// <summary>
    ///     Produces HTML code for a section of the line, with &lt;span style="..."&gt; tags.
    /// </summary>
    public string ToHtml(int offset, int length, HtmlOptions options = null)
    {
        var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options))
        {
            htmlWriter.Write(this, offset, length);
        }

        return stringWriter.ToString();
    }

    /// <summary>
    ///     Creates a substring of this rich text.
    /// </summary>
    public RichText Substring(int offset, int length)
    {
        if (offset == 0 && length == Length) return this;
        var newText = Text.Substring(offset, length);
        var model   = ToRichTextModel();
        var map     = new OffsetChangeMap(2);
        map.Add(new OffsetChangeMapEntry(offset + length, Text.Length - offset - length, 0));
        map.Add(new OffsetChangeMapEntry(0, offset, 0));
        model.UpdateOffsets(map);
        return new RichText(newText, model);
    }

    /// <summary>
    ///     Concatenates the specified rich texts.
    /// </summary>
    public static RichText Concat(params RichText[] texts)
    {
        if (texts == null || texts.Length == 0) return Empty;
        if (texts.Length == 1) return texts[0];
        var newText = string.Concat(texts.Select(txt => txt.Text));
        var model   = texts[0].ToRichTextModel();
        var offset  = texts[0].Length;
        for (var i = 1; i < texts.Length; i++)
        {
            model.Append(offset, texts[i].stateChangeOffsets, texts[i].stateChanges);
            offset += texts[i].Length;
        }

        return new RichText(newText, model);
    }

    /// <summary>
    ///     Concatenates the specified rich texts.
    /// </summary>
    public static RichText operator +(RichText a, RichText b)
    {
        return Concat(a, b);
    }

    /// <summary>
    ///     Implicit conversion from string to RichText.
    /// </summary>
    public static implicit operator RichText(string text)
    {
        if (text != null) return new RichText(text);
        return null;
    }
}
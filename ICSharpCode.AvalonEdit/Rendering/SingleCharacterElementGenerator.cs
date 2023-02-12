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
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering;
// This class is internal because it does not need to be accessed by the user - it can be configured using TextEditorOptions.

/// <summary>
///     Element generator that displays · for spaces and » for tabs and a box for control characters.
/// </summary>
/// <remarks>
///     This element generator is present in every TextView by default; the enabled features can be configured using the
///     <see cref="TextEditorOptions" />.
/// </remarks>
[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
internal sealed class SingleCharacterElementGenerator : VisualLineElementGenerator, IBuiltinElementGenerator
{
    /// <summary>
    ///     Gets/Sets whether to show · for spaces.
    /// </summary>
    public bool ShowSpaces { get; set; }

    /// <summary>
    ///     Gets/Sets whether to show » for tabs.
    /// </summary>
    public bool ShowTabs { get; set; }

    /// <summary>
    ///     Gets/Sets whether to show a box with the hex code for control characters.
    /// </summary>
    public bool ShowBoxForControlCharacters { get; set; }

    /// <summary>
    ///     Creates a new SingleCharacterElementGenerator instance.
    /// </summary>
    public SingleCharacterElementGenerator()
    {
        ShowSpaces                  = true;
        ShowTabs                    = true;
        ShowBoxForControlCharacters = true;
    }

    void IBuiltinElementGenerator.FetchOptions(TextEditorOptions options)
    {
        ShowSpaces                  = options.ShowSpaces;
        ShowTabs                    = options.ShowTabs;
        ShowBoxForControlCharacters = options.ShowBoxForControlCharacters;
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        var endLine      = CurrentContext.VisualLine.LastDocumentLine;
        var relevantText = CurrentContext.GetText(startOffset, endLine.EndOffset - startOffset);

        for (var i = 0; i < relevantText.Count; i++)
        {
            var c = relevantText.Text[relevantText.Offset + i];
            switch (c)
            {
                case ' ':
                    if (ShowSpaces) return startOffset + i;
                    break;
                case '\t':
                    if (ShowTabs) return startOffset + i;
                    break;
                default:
                    if (ShowBoxForControlCharacters && char.IsControl(c)) return startOffset + i;
                    break;
            }
        }

        return -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        var c = CurrentContext.Document.GetCharAt(offset);
        if (ShowSpaces && c == ' ') return new SpaceTextElement(CurrentContext.TextView.cachedElements.GetTextForNonPrintableCharacter("\u00B7", CurrentContext));

        if (ShowTabs && c == '\t') return new TabTextElement(CurrentContext.TextView.cachedElements.GetTextForNonPrintableCharacter("\u00BB", CurrentContext));

        if (ShowBoxForControlCharacters && char.IsControl(c))
        {
            var p = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
            p.SetForegroundBrush(Brushes.White);
            var textFormatter = TextFormatterFactory.Create(CurrentContext.TextView);
            var text          = FormattedTextElement.PrepareText(textFormatter, TextUtilities.GetControlCharacterName(c), p);
            return new SpecialCharacterBoxElement(text);
        }

        return null;
    }

    private sealed class SpaceTextElement : FormattedTextElement
    {
        public SpaceTextElement(TextLine textLine) : base(textLine, 1)
        {
            BreakBefore = LineBreakCondition.BreakPossible;
            BreakAfter  = LineBreakCondition.BreakDesired;
        }

        public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
        {
            if (mode == CaretPositioningMode.Normal || mode == CaretPositioningMode.EveryCodepoint) return base.GetNextCaretPosition(visualColumn, direction, mode);
            return -1;
        }

        public override bool IsWhitespace(int visualColumn)
        {
            return true;
        }
    }

    private sealed class TabTextElement : VisualLineElement
    {
        internal readonly TextLine text;

        public TabTextElement(TextLine text) : base(2, 1)
        {
            this.text = text;
        }

        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            // the TabTextElement consists of two TextRuns:
            // first a TabGlyphRun, then TextCharacters '\t' to let WPF handle the tab indentation
            if (startVisualColumn == VisualColumn) return new TabGlyphRun(this, TextRunProperties);
            if (startVisualColumn == VisualColumn + 1) return new TextCharacters("\t", 0, 1, TextRunProperties);
            throw new ArgumentOutOfRangeException(nameof(startVisualColumn));
        }

        public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
        {
            if (mode == CaretPositioningMode.Normal || mode == CaretPositioningMode.EveryCodepoint) return base.GetNextCaretPosition(visualColumn, direction, mode);
            return -1;
        }

        public override bool IsWhitespace(int visualColumn)
        {
            return true;
        }
    }

    private sealed class TabGlyphRun : TextEmbeddedObject
    {
        private readonly TabTextElement element;

        public TabGlyphRun(TabTextElement element, TextRunProperties properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            Properties   = properties;
            this.element = element;
        }

        public override LineBreakCondition BreakBefore => LineBreakCondition.BreakPossible;

        public override LineBreakCondition BreakAfter => LineBreakCondition.BreakRestrained;

        public override bool HasFixedSize => true;

        public override CharacterBufferReference CharacterBufferReference => new();

        public override int Length => 1;

        public override TextRunProperties Properties { get; }

        public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
        {
            var width = Math.Min(0, element.text.WidthIncludingTrailingWhitespace - 1);
            return new TextEmbeddedObjectMetrics(width, element.text.Height, element.text.Baseline);
        }

        public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
        {
            var width = Math.Min(0, element.text.WidthIncludingTrailingWhitespace - 1);
            return new Rect(0, 0, width, element.text.Height);
        }

        public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
        {
            origin.Y -= element.text.Baseline;
            element.text.Draw(drawingContext, origin, InvertAxes.None);
        }
    }

    private sealed class SpecialCharacterBoxElement : FormattedTextElement
    {
        public SpecialCharacterBoxElement(TextLine text) : base(text, 1)
        {
        }

        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            return new SpecialCharacterTextRun(this, TextRunProperties);
        }
    }

    private sealed class SpecialCharacterTextRun : FormattedTextRun
    {
        private static readonly SolidColorBrush darkGrayBrush;

        static SpecialCharacterTextRun()
        {
            darkGrayBrush = new SolidColorBrush(Color.FromArgb(200, 128, 128, 128));
            darkGrayBrush.Freeze();
        }

        public SpecialCharacterTextRun(FormattedTextElement element, TextRunProperties properties) : base(element, properties)
        {
        }

        public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
        {
            var newOrigin = new Point(origin.X + 1.5, origin.Y);
            var metrics   = base.Format(double.PositiveInfinity);
            var r         = new Rect(newOrigin.X - 0.5, newOrigin.Y - metrics.Baseline, metrics.Width + 2, metrics.Height);
            drawingContext.DrawRoundedRectangle(darkGrayBrush, null, r, 2.5, 2.5);
            base.Draw(drawingContext, newOrigin, rightToLeft, sideways);
        }

        public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
        {
            var metrics = base.Format(remainingParagraphWidth);
            return new TextEmbeddedObjectMetrics(metrics.Width + 3, metrics.Height, metrics.Baseline);
        }

        public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
        {
            var r = base.ComputeBoundingBox(rightToLeft, sideways);
            r.Width += 3;
            return r;
        }
    }
}
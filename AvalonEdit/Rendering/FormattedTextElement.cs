using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

public class FormattedTextElement : VisualLineElement
{
    internal readonly FormattedText formattedText;
    internal          string        text;
    internal          TextLine      textLine;

    // public FormattedTextElement(string text, int documentLength) : base(1, documentLength)
    // {
    //     this.text   = text ?? throw new ArgumentNullException(nameof(text));
    //     BreakBefore = LineBreakCondition.BreakPossible;
    //     BreakAfter  = LineBreakCondition.BreakPossible;
    // }

    public FormattedTextElement(TextLine text, int documentLength) : base(1, documentLength)
    {
        textLine    = text ?? throw new ArgumentNullException(nameof(text));
        BreakBefore = LineBreakCondition.BreakPossible;
        BreakAfter  = LineBreakCondition.BreakPossible;
    }

    // public FormattedTextElement(FormattedText text, int documentLength) : base(1, documentLength)
    // {
    //     formattedText = text ?? throw new ArgumentNullException(nameof(text));
    //     BreakBefore   = LineBreakCondition.BreakPossible;
    //     BreakAfter    = LineBreakCondition.BreakPossible;
    // }

    public LineBreakCondition BreakBefore { get; set; }

    public LineBreakCondition BreakAfter { get; set; }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        if (textLine != null) return new FormattedTextRun(this, TextRunProperties);
        var formatter = TextFormatterFactory.Create(context.TextView);
        textLine = PrepareText(formatter, text, TextRunProperties);
        text     = null;

        return new FormattedTextRun(this, TextRunProperties);
    }

    public static TextLine PrepareText(TextFormatter formatter, string text, TextRunProperties properties)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (properties == null) throw new ArgumentNullException(nameof(properties));
        return formatter.FormatLine(new SimpleTextSource(text, properties), 0, 32000, new VisualLineTextParagraphProperties { defaultTextRunProperties = properties, textWrapping = TextWrapping.NoWrap, tabSize = 40 }, null);
    }
}

public class FormattedTextRun : TextEmbeddedObject
{
    public FormattedTextRun(FormattedTextElement element, TextRunProperties properties)
    {
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        Element    = element ?? throw new ArgumentNullException(nameof(element));
    }

    public FormattedTextElement Element { get; }

    public override LineBreakCondition BreakBefore => Element.BreakBefore;

    public override LineBreakCondition BreakAfter => Element.BreakAfter;

    public override bool HasFixedSize => true;

    public override CharacterBufferReference CharacterBufferReference => new();

    public override int Length => Element.VisualLength;

    public override TextRunProperties Properties { get; }

    public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
    {
        var formattedText = Element.formattedText;
        if (formattedText != null) return new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height, formattedText.Baseline);

        var text = Element.textLine;
        return new TextEmbeddedObjectMetrics(text.WidthIncludingTrailingWhitespace, text.Height, text.Baseline);
    }

    public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
    {
        var formattedText = Element.formattedText;
        if (formattedText != null) return new Rect(0, 0, formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);

        var text = Element.textLine;
        return new Rect(0, 0, text.WidthIncludingTrailingWhitespace, text.Height);
    }

    public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
    {
        if (Element.formattedText != null)
        {
            origin.Y -= Element.formattedText.Baseline;
            drawingContext.DrawText(Element.formattedText, origin);
        }
        else
        {
            origin.Y -= Element.textLine.Baseline;
            Element.textLine.Draw(drawingContext, origin, InvertAxes.None);
        }
    }
}
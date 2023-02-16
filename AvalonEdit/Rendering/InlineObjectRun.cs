using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace AvalonEdit.Rendering;

/*
public class InlineObjectElement : VisualLineElement
{
    public UIElement Element { get; }

    public InlineObjectElement(int documentLength, UIElement element) : base(1, documentLength)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
    }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        return new InlineObjectRun(1, TextRunProperties, Element);
    }
}
*/

public class InlineObjectRun : TextEmbeddedObject
{
    internal Size desiredSize;

    public InlineObjectRun(int length, TextRunProperties properties, UIElement element)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), length, "Value must be positive");

        Length     = length;
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        Element    = element ?? throw new ArgumentNullException(nameof(element));
    }

    public UIElement Element { get; }

    public VisualLine VisualLine { get; internal set; }

    public override LineBreakCondition BreakBefore => LineBreakCondition.BreakDesired;

    public override LineBreakCondition BreakAfter => LineBreakCondition.BreakDesired;

    public override bool HasFixedSize => true;

    public override CharacterBufferReference CharacterBufferReference => new();

    public override int Length { get; }

    public override TextRunProperties Properties { get; }

    public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
    {
        var baseline                         = TextBlock.GetBaselineOffset(Element);
        if (double.IsNaN(baseline)) baseline = desiredSize.Height;
        return new TextEmbeddedObjectMetrics(desiredSize.Width, desiredSize.Height, baseline);
    }

    public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
    {
        if (!Element.IsArrangeValid) return Rect.Empty;
        var baseline                         = TextBlock.GetBaselineOffset(Element);
        if (double.IsNaN(baseline)) baseline = desiredSize.Height;
        return new Rect(new Point(0, -baseline), desiredSize);
    }

    public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
    {
    }
}
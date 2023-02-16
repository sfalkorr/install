using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

public class VisualLineElementTextRunProperties : TextRunProperties, ICloneable
{
    private Brush                       backgroundBrush;
    private BaselineAlignment           baselineAlignment;
    private CultureInfo                 cultureInfo;
    private double                      fontHintingEmSize;
    private double                      fontRenderingEmSize;
    private Brush                       foregroundBrush;
    private Typeface                    typeface;
    private TextDecorationCollection    textDecorations;
    private TextEffectCollection        textEffects;
    private TextRunTypographyProperties typographyProperties;
    private NumberSubstitution          numberSubstitution;

    public VisualLineElementTextRunProperties(TextRunProperties textRunProperties)
    {
        if (textRunProperties == null) throw new ArgumentNullException(nameof(textRunProperties));
        backgroundBrush     = textRunProperties.BackgroundBrush;
        baselineAlignment   = textRunProperties.BaselineAlignment;
        cultureInfo         = textRunProperties.CultureInfo;
        fontHintingEmSize   = textRunProperties.FontHintingEmSize;
        fontRenderingEmSize = textRunProperties.FontRenderingEmSize;
        foregroundBrush     = textRunProperties.ForegroundBrush;
        typeface            = textRunProperties.Typeface;
        textDecorations     = textRunProperties.TextDecorations;
        if (textDecorations is { IsFrozen: false }) textDecorations = textDecorations.Clone();
        textEffects = textRunProperties.TextEffects;
        if (textEffects is { IsFrozen: false }) textEffects = textEffects.Clone();
        typographyProperties = textRunProperties.TypographyProperties;
        numberSubstitution   = textRunProperties.NumberSubstitution;
    }

    public virtual VisualLineElementTextRunProperties Clone()
    {
        return new VisualLineElementTextRunProperties(this);
    }

    object ICloneable.Clone()
    {
        return Clone();
    }

    public override Brush BackgroundBrush => backgroundBrush;

    public void SetBackgroundBrush(Brush value)
    {
        ExtensionMethods.CheckIsFrozen(value);
        backgroundBrush = value;
    }

    public override BaselineAlignment BaselineAlignment => baselineAlignment;

    public void SetBaselineAlignment(BaselineAlignment value)
    {
        baselineAlignment = value;
    }

    public override CultureInfo CultureInfo => cultureInfo;

    public void SetCultureInfo(CultureInfo value)
    {
        cultureInfo = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override double FontHintingEmSize => fontHintingEmSize;

    public void SetFontHintingEmSize(double value)
    {
        fontHintingEmSize = value;
    }

    public override double FontRenderingEmSize => fontRenderingEmSize;

    public void SetFontRenderingEmSize(double value)
    {
        fontRenderingEmSize = value;
    }

    public override Brush ForegroundBrush => foregroundBrush;

    public void SetForegroundBrush(Brush value)
    {
        ExtensionMethods.CheckIsFrozen(value);
        foregroundBrush = value;
    }

    public override Typeface Typeface => typeface;

    public void SetTypeface(Typeface value)
    {
        typeface = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override TextDecorationCollection TextDecorations => textDecorations;

    public void SetTextDecorations(TextDecorationCollection value)
    {
        ExtensionMethods.CheckIsFrozen(value);
        textDecorations = textDecorations == null ? value : new TextDecorationCollection(textDecorations.Union(value));
    }

    public override TextEffectCollection TextEffects => textEffects;

    public void SetTextEffects(TextEffectCollection value)
    {
        ExtensionMethods.CheckIsFrozen(value);
        textEffects = value;
    }

    public override TextRunTypographyProperties TypographyProperties => typographyProperties;

    public void SetTypographyProperties(TextRunTypographyProperties value)
    {
        typographyProperties = value;
    }

    public override NumberSubstitution NumberSubstitution => numberSubstitution;

    public void SetNumberSubstitution(NumberSubstitution value)
    {
        numberSubstitution = value;
    }
}
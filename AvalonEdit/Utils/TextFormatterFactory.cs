using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace AvalonEdit.Utils;

internal static class TextFormatterFactory
{
    public static TextFormatter Create(DependencyObject owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        return TextFormatter.Create(TextOptions.GetTextFormattingMode(owner));
    }

    public static bool PropertyChangeAffectsTextFormatter(DependencyProperty dp)
    {
        return dp == TextOptions.TextFormattingModeProperty;
    }

    public static FormattedText CreateFormattedText(FrameworkElement element, string text, Typeface typeface, double? emSize, Brush foreground)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (text == null) throw new ArgumentNullException(nameof(text));
        typeface   ??= element.CreateTypeface();
        emSize     ??= TextBlock.GetFontSize(element);
        foreground ??= TextBlock.GetForeground(element);
        return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, emSize.Value, foreground, null, TextOptions.GetTextFormattingMode(element), VisualTreeHelper.GetDpi(element).PixelsPerDip);
    }
}
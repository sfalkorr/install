using System;
using System.Collections.Generic;
using System.Windows.Media.TextFormatting;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

internal sealed class TextViewCachedElements : IDisposable
{
    private TextFormatter                formatter;
    private Dictionary<string, TextLine> nonPrintableCharacterTexts;

    public TextLine GetTextForNonPrintableCharacter(string text, ITextRunConstructionContext context)
    {
        nonPrintableCharacterTexts ??= new Dictionary<string, TextLine>();
        if (nonPrintableCharacterTexts.TryGetValue(text, out var textLine)) return textLine;
        var p = new VisualLineElementTextRunProperties(context.GlobalTextRunProperties);
        p.SetForegroundBrush(context.TextView.NonPrintableCharacterBrush);
        formatter                        ??= TextFormatterFactory.Create(context.TextView);
        textLine                         =   FormattedTextElement.PrepareText(formatter, text, p);
        nonPrintableCharacterTexts[text] =   textLine;

        return textLine;
    }

    public void Dispose()
    {
        if (nonPrintableCharacterTexts != null)
            foreach (var line in nonPrintableCharacterTexts.Values)
                line.Dispose();
        formatter?.Dispose();
    }
}
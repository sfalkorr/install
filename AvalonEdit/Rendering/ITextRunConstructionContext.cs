using System.Windows.Media.TextFormatting;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

public interface ITextRunConstructionContext
{
    TextDocument Document { get; }

    TextView TextView { get; }

    VisualLine VisualLine { get; }

    TextRunProperties GlobalTextRunProperties { get; }

    StringSegment GetText(int offset, int length);
}
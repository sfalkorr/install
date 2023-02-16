using System.Windows.Media;

namespace AvalonEdit.Rendering;

public interface IBackgroundRenderer
{
    KnownLayer Layer { get; }

    void Draw(TextView textView, DrawingContext drawingContext);
}
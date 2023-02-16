using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AvalonEdit.Rendering;

internal sealed class TextLayer : Layer
{
    internal int index;

    public TextLayer(TextView textView) : base(textView, KnownLayer.Text)
    {
    }

    private List<VisualLineDrawingVisual> visuals = new();

    internal void SetVisualLines(IEnumerable<VisualLine> visualLines)
    {
        foreach (var v in visuals.Where(v => v.VisualLine.IsDisposed)) RemoveVisualChild(v);
        visuals.Clear();
        foreach (var newLine in visualLines)
        {
            var v = newLine.Render();
            if (!v.IsAdded)
            {
                AddVisualChild(v);
                v.IsAdded = true;
            }

            visuals.Add(v);
        }

        InvalidateArrange();
    }

    protected override int VisualChildrenCount => visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        return visuals[index];
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        textView.ArrangeTextLayer(visuals);
    }
}
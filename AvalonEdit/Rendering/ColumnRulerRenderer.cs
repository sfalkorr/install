using System;
using System.Windows;
using System.Windows.Media;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

internal sealed class ColumnRulerRenderer : IBackgroundRenderer
{
    private Pen      pen;
    private int      column;
    private TextView textView;

    public static readonly Color DefaultForeground = Colors.LightGray;

    public ColumnRulerRenderer(TextView textView)
    {
        if (textView == null) throw new ArgumentNullException(nameof(textView));

        pen = new Pen(new SolidColorBrush(DefaultForeground), 1);
        pen.Freeze();
        this.textView = textView;
        this.textView.BackgroundRenderers.Add(this);
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void SetRuler(int column, Pen pen)
    {
        if (this.column != column)
        {
            this.column = column;
            textView.InvalidateLayer(Layer);
        }

        if (this.pen == pen) return;
        this.pen = pen;
        textView.InvalidateLayer(Layer);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (column < 1) return;
        var offset     = textView.WideSpaceWidth * column;
        var pixelSize  = PixelSnapHelpers.GetPixelSize(textView);
        var markerXPos = PixelSnapHelpers.PixelAlign(offset, pixelSize.Width);
        markerXPos -= textView.ScrollOffset.X;
        var start = new Point(markerXPos, 0);
        var end   = new Point(markerXPos, Math.Max(textView.DocumentHeight, textView.ActualHeight));

        drawingContext.DrawLine(pen, start, end);
    }
}
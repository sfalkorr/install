using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AvalonEdit.Rendering;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

internal sealed class CaretLayer : Layer
{
    private TextArea textArea;

    private bool isVisible;
    private Rect caretRectangle;

    private DispatcherTimer caretBlinkTimer = new();
    private bool            blink;

    public CaretLayer(TextArea textArea) : base(textArea.TextView, KnownLayer.Caret)
    {
        this.textArea        =  textArea;
        IsHitTestVisible     =  false;
        caretBlinkTimer.Tick += caretBlinkTimer_Tick;
    }

    private void caretBlinkTimer_Tick(object sender, EventArgs e)
    {
        blink = !blink;
        InvalidateVisual();
    }

    public void Show(Rect caretRectangle_)
    {
        caretRectangle = caretRectangle_;
        isVisible      = true;
        StartBlinkAnimation();
        InvalidateVisual();
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;
        StopBlinkAnimation();
        InvalidateVisual();
    }

    private void StartBlinkAnimation()
    {
        var blinkTime = Win32.CaretBlinkTime;
        blink = true;
        if (!(blinkTime.TotalMilliseconds > 0)) return;
        caretBlinkTimer.Interval = blinkTime;
        caretBlinkTimer.Start();
    }

    private void StopBlinkAnimation()
    {
        caretBlinkTimer.Stop();
    }

    internal Brush CaretBrush;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (!isVisible || !blink) return;
        var caretBrush = CaretBrush ?? (Brush)textView.GetValue(TextBlock.ForegroundProperty);

        if (textArea.OverstrikeMode)
            if (caretBrush is SolidColorBrush scBrush)
            {
                var brushColor = scBrush.Color;
                var newColor   = Color.FromArgb(100, brushColor.R, brushColor.G, brushColor.B);
                caretBrush = new SolidColorBrush(newColor);
                caretBrush.Freeze();
            }

        var r = new Rect(caretRectangle.X - textView.HorizontalOffset, caretRectangle.Y - textView.VerticalOffset, caretRectangle.Width, caretRectangle.Height);
        drawingContext.DrawRectangle(caretBrush, null, PixelSnapHelpers.Round(r, PixelSnapHelpers.GetPixelSize(this)));
    }
}
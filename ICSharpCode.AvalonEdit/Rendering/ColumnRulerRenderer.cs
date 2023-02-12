﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering;

/// <summary>
///     Renders a ruler at a certain column.
/// </summary>
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

        if (this.pen != pen)
        {
            this.pen = pen;
            textView.InvalidateLayer(Layer);
        }
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
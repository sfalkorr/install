﻿using System;
using System.Windows;
using System.Windows.Media;
using AvalonEdit.Rendering;

namespace AvalonEdit.Editing;

internal sealed class SelectionLayer : Layer, IWeakEventListener
{
    private readonly TextArea textArea;

    public SelectionLayer(TextArea textArea) : base(textArea.TextView, KnownLayer.Selection)
    {
        IsHitTestVisible = false;

        this.textArea = textArea;
        TextViewWeakEventManager.VisualLinesChanged.AddListener(textView, this);
        TextViewWeakEventManager.ScrollOffsetChanged.AddListener(textView, this);
    }

    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        if (managerType != typeof(TextViewWeakEventManager.VisualLinesChanged) && managerType != typeof(TextViewWeakEventManager.ScrollOffsetChanged)) return false;
        InvalidateVisual();
        return true;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var selectionBorder = textArea.SelectionBorder;

        var geoBuilder = new BackgroundGeometryBuilder { AlignToWholePixels = true, BorderThickness = selectionBorder?.Thickness ?? 0, ExtendToFullWidthAtLineEnd = textArea.Selection.EnableVirtualSpace, CornerRadius = textArea.SelectionCornerRadius };
        foreach (var segment in textArea.Selection.Segments) geoBuilder.AddSegment(textView, segment);
        var geometry = geoBuilder.CreateGeometry();
        if (geometry != null) drawingContext.DrawGeometry(textArea.SelectionBrush, selectionBorder, geometry);
    }
}
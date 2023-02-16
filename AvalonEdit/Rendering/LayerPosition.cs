using System;
using System.Windows;

namespace AvalonEdit.Rendering;

public enum KnownLayer
{
    Background,
    Selection,
    Text,
    Caret
}

public enum LayerInsertionPosition
{
    Below,
    Replace,
    Above
}

internal sealed class LayerPosition : IComparable<LayerPosition>
{
    internal static readonly DependencyProperty LayerPositionProperty = DependencyProperty.RegisterAttached("LayerPosition", typeof(LayerPosition), typeof(LayerPosition));

    public static void SetLayerPosition(UIElement layer, LayerPosition value)
    {
        layer.SetValue(LayerPositionProperty, value);
    }

    public static LayerPosition GetLayerPosition(UIElement layer)
    {
        return (LayerPosition)layer.GetValue(LayerPositionProperty);
    }

    internal readonly KnownLayer             KnownLayer;
    internal readonly LayerInsertionPosition Position;

    public LayerPosition(KnownLayer knownLayer, LayerInsertionPosition position)
    {
        KnownLayer = knownLayer;
        Position   = position;
    }

    public int CompareTo(LayerPosition other)
    {
        var r = KnownLayer.CompareTo(other.KnownLayer);
        return r != 0 ? r : Position.CompareTo(other.Position);
    }
}
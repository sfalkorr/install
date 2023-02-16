using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using AvalonEdit.Rendering;

namespace AvalonEdit.Highlighting;

[Serializable]
public abstract class HighlightingBrush
{
    public abstract Brush GetBrush(ITextRunConstructionContext context);

    public virtual Color? GetColor(ITextRunConstructionContext context)
    {
        if (GetBrush(context) is SolidColorBrush scb) return scb.Color;
        return null;
    }
}

[Serializable]
public sealed class SimpleHighlightingBrush : HighlightingBrush, ISerializable
{
    private readonly SolidColorBrush brush;

    internal SimpleHighlightingBrush(SolidColorBrush brush)
    {
        brush.Freeze();
        this.brush = brush;
    }

    public SimpleHighlightingBrush(Color color) : this(new SolidColorBrush(color))
    {
    }

    public override Brush GetBrush(ITextRunConstructionContext context)
    {
        return brush;
    }

    public override string ToString()
    {
        return brush.ToString();
    }

    private SimpleHighlightingBrush(SerializationInfo info, StreamingContext context)
    {
        brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(info.GetString("color")));
        brush.Freeze();
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("color", brush.Color.ToString(CultureInfo.InvariantCulture));
    }

    public override bool Equals(object obj)
    {
        return obj is SimpleHighlightingBrush other && brush.Color.Equals(other.brush.Color);
    }

    public override int GetHashCode()
    {
        return brush.Color.GetHashCode();
    }
}

[Serializable]
internal sealed class SystemColorHighlightingBrush : HighlightingBrush, ISerializable
{
    private readonly PropertyInfo property;

    public SystemColorHighlightingBrush(PropertyInfo property)
    {
        Debug.Assert(property.ReflectedType == typeof(SystemColors));
        Debug.Assert(typeof(Brush).IsAssignableFrom(property.PropertyType));
        this.property = property;
    }

    public override Brush GetBrush(ITextRunConstructionContext context)
    {
        return (Brush)property.GetValue(null, null);
    }

    public override string ToString()
    {
        return property.Name;
    }

    private SystemColorHighlightingBrush(SerializationInfo info, StreamingContext context)
    {
        property = typeof(SystemColors).GetProperty(info.GetString("propertyName"));
        if (property == null) throw new ArgumentException("Error deserializing SystemColorHighlightingBrush");
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("propertyName", property.Name);
    }

    public override bool Equals(object obj)
    {
        return obj is SystemColorHighlightingBrush other && Equals(property, other.property);
    }

    public override int GetHashCode()
    {
        return property.GetHashCode();
    }
}
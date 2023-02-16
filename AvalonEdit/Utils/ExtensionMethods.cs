using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace AvalonEdit.Utils;

public static class ExtensionMethods
{
    #region Epsilon / IsClose / CoerceValue

    public const double Epsilon = 0.01;

    public static bool IsClose(this double d1, double d2)
    {
        if (d1.Equals(d2)) return true;
        return Math.Abs(d1 - d2) < Epsilon;
    }

    public static bool IsClose(this Size d1, Size d2)
    {
        return IsClose(d1.Width, d2.Width) && IsClose(d1.Height, d2.Height);
    }

    public static bool IsClose(this Vector d1, Vector d2)
    {
        return IsClose(d1.X, d2.X) && IsClose(d1.Y, d2.Y);
    }

    public static double CoerceValue(this double value, double minimum, double maximum)
    {
        return Math.Max(Math.Min(value, maximum), minimum);
    }

    public static int CoerceValue(this int value, int minimum, int maximum)
    {
        return Math.Max(Math.Min(value, maximum), minimum);
    }

    #endregion

    #region CreateTypeface

    public static Typeface CreateTypeface(this FrameworkElement fe)
    {
        return new Typeface((FontFamily)fe.GetValue(TextBlock.FontFamilyProperty), (FontStyle)fe.GetValue(TextBlock.FontStyleProperty), (FontWeight)fe.GetValue(TextBlock.FontWeightProperty), (FontStretch)fe.GetValue(TextBlock.FontStretchProperty));
    }

    #endregion

    #region AddRange / Sequence

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
    {
        foreach (var e in elements) collection.Add(e);
    }

    public static IEnumerable<T> Sequence<T>(T value)
    {
        yield return value;
    }

    #endregion

    #region XML reading

    public static string GetAttributeOrNull(this XmlElement element, string attributeName)
    {
        var attr = element.GetAttributeNode(attributeName);
        return attr?.Value;
    }

    public static bool? GetBoolAttribute(this XmlElement element, string attributeName)
    {
        var attr = element.GetAttributeNode(attributeName);
        return attr != null ? XmlConvert.ToBoolean(attr.Value) : null;
    }

    public static bool? GetBoolAttribute(this XmlReader reader, string attributeName)
    {
        var attributeValue = reader.GetAttribute(attributeName);
        if (attributeValue == null) return null;
        return XmlConvert.ToBoolean(attributeValue);
    }

    #endregion

    #region DPI independence

    // public static Rect TransformToDevice(this Rect rect, Visual visual)
    // {
    //     var matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
    //     return Rect.Transform(rect, matrix);
    // }
    //
    // public static Rect TransformFromDevice(this Rect rect, Visual visual)
    // {
    //     var matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
    //     return Rect.Transform(rect, matrix);
    // }
    //
    // public static Size TransformToDevice(this Size size, Visual visual)
    // {
    //     var matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
    //     return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
    // }
    //
    // public static Size TransformFromDevice(this Size size, Visual visual)
    // {
    //     var matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
    //     return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
    // }

    public static Point TransformToDevice(this Point point, Visual visual)
    {
        var matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
        return new Point(point.X * matrix.M11, point.Y * matrix.M22);
    }

    // public static Point TransformFromDevice(this Point point, Visual visual)
    // {
    //     var matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
    //     return new Point(point.X * matrix.M11, point.Y * matrix.M22);
    // }

    #endregion

    #region System.Drawing <-> WPF conversions

    public static System.Drawing.Point ToSystemDrawing(this Point p)
    {
        return new System.Drawing.Point((int)p.X, (int)p.Y);
    }

    public static Point ToWpf(this System.Drawing.Point p)
    {
        return new Point(p.X, p.Y);
    }

    public static Size ToWpf(this System.Drawing.Size s)
    {
        return new Size(s.Width, s.Height);
    }

    public static Rect ToWpf(this Rectangle rect)
    {
        return new Rect(rect.Location.ToWpf(), rect.Size.ToWpf());
    }

    #endregion

    public static IEnumerable<DependencyObject> VisualAncestorsAndSelf(this DependencyObject obj)
    {
        while (obj != null)
        {
            yield return obj;
            if (obj is Visual or Visual3D) obj           = VisualTreeHelper.GetParent(obj);
            else if (obj is FrameworkContentElement) obj = ((FrameworkContentElement)obj).Parent;
            else break;
        }
    }

    [Conditional("DEBUG")]
    public static void CheckIsFrozen(Freezable f)
    {
        if (f is { IsFrozen: false }) Debug.WriteLine("Performance warning: Not frozen: " + f);
    }

    [Conditional("DEBUG")]
    public static void Log(bool condition, string format, params object[] args)
    {
        if (!condition) return;
        var output = DateTime.Now.ToString("hh:MM:ss") + ": " + string.Format(format, args) + Environment.NewLine + Environment.StackTrace;
        Console.WriteLine(output);
        Debug.WriteLine(output);
    }
}
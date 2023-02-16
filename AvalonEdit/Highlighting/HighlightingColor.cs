using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Media;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting;

[Serializable]
public class HighlightingColor : ISerializable, IFreezable, ICloneable, IEquatable<HighlightingColor>
{
    //internal static readonly HighlightingColor Empty = FreezableHelper.FreezeAndReturn(new HighlightingColor());

    private string            name;
    private FontFamily        fontFamily;
    private int?              fontSize;
    private FontWeight?       fontWeight;
    private FontStyle?        fontStyle;
    private bool?             underline;
    private bool?             strikethrough;
    private HighlightingBrush foreground;
    private HighlightingBrush background;

    public string Name
    {
        get => name;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            name = value;
        }
    }

    public FontFamily FontFamily
    {
        get => fontFamily;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            fontFamily = value;
        }
    }

    public int? FontSize
    {
        get => fontSize;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            fontSize = value;
        }
    }

    public FontWeight? FontWeight
    {
        get => fontWeight;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            fontWeight = value;
        }
    }

    public FontStyle? FontStyle
    {
        get => fontStyle;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            fontStyle = value;
        }
    }

    public bool? Underline
    {
        get => underline;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            underline = value;
        }
    }

    public bool? Strikethrough
    {
        get => strikethrough;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            strikethrough = value;
        }
    }

    public HighlightingBrush Foreground
    {
        get => foreground;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            foreground = value;
        }
    }

    public HighlightingBrush Background
    {
        get => background;
        set
        {
            if (IsFrozen) throw new InvalidOperationException();
            background = value;
        }
    }

    public HighlightingColor()
    {
    }

    protected HighlightingColor(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        Name = info.GetString("Name");
        if (info.GetBoolean("HasWeight")) FontWeight           = System.Windows.FontWeight.FromOpenTypeWeight(info.GetInt32("Weight"));
        if (info.GetBoolean("HasStyle")) FontStyle             = (FontStyle?)new FontStyleConverter().ConvertFromInvariantString(info.GetString("Style"));
        if (info.GetBoolean("HasUnderline")) Underline         = info.GetBoolean("Underline");
        if (info.GetBoolean("HasStrikethrough")) Strikethrough = info.GetBoolean("Strikethrough");
        Foreground = (HighlightingBrush)info.GetValue("Foreground", typeof(SimpleHighlightingBrush));
        Background = (HighlightingBrush)info.GetValue("Background", typeof(SimpleHighlightingBrush));
        if (info.GetBoolean("HasFamily")) FontFamily = new FontFamily(info.GetString("Family"));
        if (info.GetBoolean("HasSize")) FontSize     = info.GetInt32("Size");
    }

    [SecurityCritical]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        info.AddValue("Name", Name);
        info.AddValue("HasWeight", FontWeight.HasValue);
        if (FontWeight.HasValue) info.AddValue("Weight", FontWeight.Value.ToOpenTypeWeight());
        info.AddValue("HasStyle", FontStyle.HasValue);
        if (FontStyle.HasValue) info.AddValue("Style", FontStyle.Value.ToString());
        info.AddValue("HasUnderline", Underline.HasValue);
        if (Underline.HasValue) info.AddValue("Underline", Underline.Value);
        info.AddValue("HasStrikethrough", Strikethrough.HasValue);
        if (Strikethrough.HasValue) info.AddValue("Strikethrough", Strikethrough.Value);
        info.AddValue("Foreground", Foreground);
        info.AddValue("Background", Background);
        info.AddValue("HasFamily", FontFamily != null);
        if (FontFamily != null) info.AddValue("Family", FontFamily.FamilyNames.FirstOrDefault());
        info.AddValue("HasSize", FontSize.HasValue);
        if (FontSize.HasValue) info.AddValue("Size", FontSize.Value.ToString());
    }

    [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "CSS usually uses lowercase, and all possible values are English-only")]
    public virtual string ToCss()
    {
        var b = new StringBuilder();
        if (Foreground != null)
        {
            var c = Foreground.GetColor(null);
            if (c != null) b.AppendFormat(CultureInfo.InvariantCulture, "color: #{0:x2}{1:x2}{2:x2}; ", c.Value.R, c.Value.G, c.Value.B);
        }

        if (Background != null)
        {
            var c = Background.GetColor(null);
            if (c != null) b.AppendFormat(CultureInfo.InvariantCulture, "background-color: #{0:x2}{1:x2}{2:x2}; ", c.Value.R, c.Value.G, c.Value.B);
        }

        if (FontWeight != null)
        {
            b.Append("font-weight: ");
            b.Append(FontWeight.Value.ToString().ToLowerInvariant());
            b.Append("; ");
        }

        if (FontStyle != null)
        {
            b.Append("font-style: ");
            b.Append(FontStyle.Value.ToString().ToLowerInvariant());
            b.Append("; ");
        }

        if (Underline != null)
        {
            b.Append("text-decoration: ");
            b.Append(Underline.Value ? "underline" : "none");
            b.Append("; ");
        }

        if (Strikethrough != null)
        {
            if (Underline == null) b.Append("text-decoration:  ");

            b.Remove(b.Length - 1, 1);
            b.Append(Strikethrough.Value ? " line-through" : " none");
            b.Append("; ");
        }

        return b.ToString();
    }

    public override string ToString()
    {
        return "[" + GetType().Name + " " + (string.IsNullOrEmpty(Name) ? ToCss() : Name) + "]";
    }

    public virtual void Freeze()
    {
        IsFrozen = true;
    }

    public bool IsFrozen { get; private set; }

    public virtual HighlightingColor Clone()
    {
        var c = (HighlightingColor)MemberwiseClone();
        c.IsFrozen = false;
        return c;
    }

    object ICloneable.Clone()
    {
        return Clone();
    }

    public sealed override bool Equals(object obj)
    {
        return Equals(obj as HighlightingColor);
    }

    public virtual bool Equals(HighlightingColor other)
    {
        if (other == null) return false;
        return name == other.name && fontWeight == other.fontWeight && fontStyle == other.fontStyle && underline == other.underline && strikethrough == other.strikethrough && Equals(foreground, other.foreground) && Equals(background, other.background) && Equals(fontFamily, other.fontFamily) && Equals(FontSize, other.FontSize);
    }

    public override int GetHashCode()
    {
        var hashCode = 0;
        unchecked
        {
            if (name != null) hashCode += 1000000007 * name.GetHashCode();
            hashCode += 1000000009 * fontWeight.GetHashCode();
            hashCode += 1000000021 * fontStyle.GetHashCode();
            if (foreground != null) hashCode += 1000000033 * foreground.GetHashCode();
            if (background != null) hashCode += 1000000087 * background.GetHashCode();
            if (fontFamily != null) hashCode += 1000000123 * fontFamily.GetHashCode();
            if (fontSize != null) hashCode   += 1000000167 * fontSize.GetHashCode();
        }

        return hashCode;
    }

    public void MergeWith(HighlightingColor color)
    {
        //FreezableHelper.ThrowIfFrozen(this);
        if (color.fontWeight != null) fontWeight       = color.fontWeight;
        if (color.fontStyle != null) fontStyle         = color.fontStyle;
        if (color.foreground != null) foreground       = color.foreground;
        if (color.background != null) background       = color.background;
        if (color.underline != null) underline         = color.underline;
        if (color.strikethrough != null) strikethrough = color.strikethrough;
        if (color.fontFamily != null) fontFamily       = color.fontFamily;
        if (color.fontSize != null) fontSize           = color.fontSize;
    }

    internal bool IsEmptyForMerge => fontWeight == null && fontStyle == null && underline == null && strikethrough == null && foreground == null && background == null && fontFamily == null && fontSize == null;
}
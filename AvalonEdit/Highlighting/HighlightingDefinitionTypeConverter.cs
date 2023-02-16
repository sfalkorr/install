using System;
using System.ComponentModel;
using System.Globalization;

namespace AvalonEdit.Highlighting;

public sealed class HighlightingDefinitionTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string definitionName) return HighlightingManager.Instance.GetDefinition(definitionName);
        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is IHighlightingDefinition definition && destinationType == typeof(string)) return definition.Name;
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
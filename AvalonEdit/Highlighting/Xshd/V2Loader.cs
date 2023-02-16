using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting.Xshd;

internal static class V2Loader
{
    private static XmlSchemaSet schemaSet;

    private static XmlSchemaSet SchemaSet => schemaSet ??= HighlightingLoader.LoadSchemaSet(new XmlTextReader(Resources.OpenStream("ModeV2.xsd")));

    public static XshdSyntaxDefinition LoadDefinition(XmlReader reader, bool skipValidation)
    {
        reader = HighlightingLoader.GetValidatingReader(reader, true, skipValidation ? null : SchemaSet);
        reader.Read();
        return ParseDefinition(reader);
    }

    private static XshdSyntaxDefinition ParseDefinition(XmlReader reader)
    {
        Debug.Assert(reader.LocalName == "SyntaxDefinition");
        var def        = new XshdSyntaxDefinition { Name = reader.GetAttribute("name") };
        var extensions = reader.GetAttribute("extensions");
        if (extensions != null) def.Extensions.AddRange(extensions.Split(';'));
        ParseElements(def.Elements, reader);
        Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
        Debug.Assert(reader.LocalName == "SyntaxDefinition");
        return def;
    }

    private static void ParseElements(ICollection<XshdElement> c, XmlReader reader)
    {
        if (reader.IsEmptyElement) return;
        while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
        {
            Debug.Assert(reader.NodeType == XmlNodeType.Element);
            switch (reader.Name)
            {
                case "RuleSet":
                    c.Add(ParseRuleSet(reader));
                    break;
                case "Property":
                    c.Add(ParseProperty(reader));
                    break;
                case "Color":
                    c.Add(ParseNamedColor(reader));
                    break;
                case "Keywords":
                    c.Add(ParseKeywords(reader));
                    break;
                case "Span":
                    c.Add(ParseSpan(reader));
                    break;
                case "Import":
                    c.Add(ParseImport(reader));
                    break;
                case "Rule":
                    c.Add(ParseRule(reader));
                    break;
                default:
                    throw new NotSupportedException("Unknown element " + reader.Name);
            }
        }
    }

    private static XshdElement ParseProperty(XmlReader reader)
    {
        var property = new XshdProperty();
        SetPosition(property, reader);
        property.Name  = reader.GetAttribute("name");
        property.Value = reader.GetAttribute("value");
        return property;
    }

    private static XshdRuleSet ParseRuleSet(XmlReader reader)
    {
        var ruleSet = new XshdRuleSet();
        SetPosition(ruleSet, reader);
        ruleSet.Name       = reader.GetAttribute("name");
        ruleSet.IgnoreCase = reader.GetBoolAttribute("ignoreCase");

        CheckElementName(reader, ruleSet.Name);
        ParseElements(ruleSet.Elements, reader);
        return ruleSet;
    }

    private static XshdRule ParseRule(XmlReader reader)
    {
        var rule = new XshdRule();
        SetPosition(rule, reader);
        rule.ColorReference = ParseColorReference(reader);
        if (reader.IsEmptyElement) return rule;
        reader.Read();
        if (reader.NodeType != XmlNodeType.Text) return rule;
        rule.Regex     = reader.ReadContentAsString();
        rule.RegexType = XshdRegexType.IgnorePatternWhitespace;

        return rule;
    }

    private static XshdKeywords ParseKeywords(XmlReader reader)
    {
        var keywords = new XshdKeywords();
        SetPosition(keywords, reader);
        keywords.ColorReference = ParseColorReference(reader);
        reader.Read();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            Debug.Assert(reader.NodeType == XmlNodeType.Element);
            keywords.Words.Add(reader.ReadElementString());
        }

        return keywords;
    }

    private static XshdImport ParseImport(XmlReader reader)
    {
        var import = new XshdImport();
        SetPosition(import, reader);
        import.RuleSetReference = ParseRuleSetReference(reader);
        if (!reader.IsEmptyElement) reader.Skip();
        return import;
    }

    private static XshdSpan ParseSpan(XmlReader reader)
    {
        var span = new XshdSpan();
        SetPosition(span, reader);
        span.BeginRegex         = reader.GetAttribute("begin");
        span.EndRegex           = reader.GetAttribute("end");
        span.Multiline          = reader.GetBoolAttribute("multiline") ?? false;
        span.SpanColorReference = ParseColorReference(reader);
        span.RuleSetReference   = ParseRuleSetReference(reader);
        if (reader.IsEmptyElement) return span;
        reader.Read();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            Debug.Assert(reader.NodeType == XmlNodeType.Element);
            switch (reader.Name)
            {
                case "Begin":
                    if (span.BeginRegex != null) throw Error(reader, "Duplicate Begin regex");
                    span.BeginColorReference = ParseColorReference(reader);
                    span.BeginRegex          = reader.ReadElementString();
                    span.BeginRegexType      = XshdRegexType.IgnorePatternWhitespace;
                    break;
                case "End":
                    if (span.EndRegex != null) throw Error(reader, "Duplicate End regex");
                    span.EndColorReference = ParseColorReference(reader);
                    span.EndRegex          = reader.ReadElementString();
                    span.EndRegexType      = XshdRegexType.IgnorePatternWhitespace;
                    break;
                case "RuleSet":
                    if (span.RuleSetReference.ReferencedElement != null) throw Error(reader, "Cannot specify both inline RuleSet and RuleSet reference");
                    span.RuleSetReference = new XshdReference<XshdRuleSet>(ParseRuleSet(reader));
                    reader.Read();
                    break;
                default:
                    throw new NotSupportedException("Unknown element " + reader.Name);
            }
        }

        return span;
    }

    private static Exception Error(XmlReader reader, string message)
    {
        return Error(reader as IXmlLineInfo, message);
    }

    private static Exception Error(IXmlLineInfo lineInfo, string message)
    {
        return lineInfo != null ? new HighlightingDefinitionInvalidException(HighlightingLoader.FormatExceptionMessage(message, lineInfo.LineNumber, lineInfo.LinePosition)) : new HighlightingDefinitionInvalidException(message);
    }

    private static void SetPosition(XshdElement element, XmlReader reader)
    {
        if (reader is not IXmlLineInfo lineInfo) return;
        element.LineNumber   = lineInfo.LineNumber;
        element.ColumnNumber = lineInfo.LinePosition;
    }

    private static XshdReference<XshdRuleSet> ParseRuleSetReference(XmlReader reader)
    {
        var ruleSet = reader.GetAttribute("ruleSet");
        if (ruleSet == null) return new XshdReference<XshdRuleSet>();
        var pos = ruleSet.LastIndexOf('/');
        return pos >= 0 ? new XshdReference<XshdRuleSet>(ruleSet.Substring(0, pos), ruleSet.Substring(pos + 1)) : new XshdReference<XshdRuleSet>(null, ruleSet);
    }

    private static void CheckElementName(XmlReader reader, string name)
    {
        if (name == null) return;
        if (name.Length == 0) throw Error(reader, "The empty string is not a valid name.");
        if (name.IndexOf('/') >= 0) throw Error(reader, "Element names must not contain a slash.");
    }

    #region ParseColor

    private static XshdColor ParseNamedColor(XmlReader reader)
    {
        var color = ParseColorAttributes(reader);
        color.Name = reader.GetAttribute("name");
        CheckElementName(reader, color.Name);
        color.ExampleText = reader.GetAttribute("exampleText");
        return color;
    }

    private static XshdReference<XshdColor> ParseColorReference(XmlReader reader)
    {
        var color = reader.GetAttribute("color");
        if (color == null) return new XshdReference<XshdColor>(ParseColorAttributes(reader));
        var pos = color.LastIndexOf('/');
        return pos >= 0 ? new XshdReference<XshdColor>(color.Substring(0, pos), color.Substring(pos + 1)) : new XshdReference<XshdColor>(null, color);
    }

    private static XshdColor ParseColorAttributes(XmlReader reader)
    {
        var color = new XshdColor();
        SetPosition(color, reader);
        var position = reader as IXmlLineInfo;
        color.Foreground    = ParseColor(position, reader.GetAttribute("foreground"));
        color.Background    = ParseColor(position, reader.GetAttribute("background"));
        color.FontWeight    = ParseFontWeight(reader.GetAttribute("fontWeight"));
        color.FontStyle     = ParseFontStyle(reader.GetAttribute("fontStyle"));
        color.Underline     = reader.GetBoolAttribute("underline");
        color.Strikethrough = reader.GetBoolAttribute("strikethrough");
        color.FontFamily    = ParseFontFamily(position, reader.GetAttribute("fontFamily"));
        color.FontSize      = ParseFontSize(position, reader.GetAttribute("fontSize"));
        return color;
    }

    internal static readonly ColorConverter      ColorConverter      = new();
    internal static readonly FontWeightConverter FontWeightConverter = new();
    internal static readonly FontStyleConverter  FontStyleConverter  = new();

    private static HighlightingBrush ParseColor(IXmlLineInfo lineInfo, string color)
    {
        if (string.IsNullOrEmpty(color)) return null;
        return color.StartsWith("SystemColors.", StringComparison.Ordinal) ? GetSystemColorBrush(lineInfo, color) : FixedColorHighlightingBrush((Color?)ColorConverter.ConvertFromInvariantString(color));
    }

    private static int? ParseFontSize(IXmlLineInfo lineInfo, string size)
    {
        int value;
        return int.TryParse(size, out value) ? value : null;
    }

    private static FontFamily ParseFontFamily(IXmlLineInfo lineInfo, string family)
    {
        return !string.IsNullOrEmpty(family) ? new FontFamily(family) : null;
    }

    internal static SystemColorHighlightingBrush GetSystemColorBrush(IXmlLineInfo lineInfo, string name)
    {
        Debug.Assert(name.StartsWith("SystemColors.", StringComparison.Ordinal));
        var shortName = name.Substring(13);
        var property  = typeof(SystemColors).GetProperty(shortName + "Brush");
        if (property == null) throw Error(lineInfo, "Cannot find '" + name + "'.");
        return new SystemColorHighlightingBrush(property);
    }

    private static HighlightingBrush FixedColorHighlightingBrush(Color? color)
    {
        return color == null ? null : new SimpleHighlightingBrush(color.Value);
    }

    private static FontWeight? ParseFontWeight(string fontWeight)
    {
        if (string.IsNullOrEmpty(fontWeight)) return null;
        return (FontWeight?)FontWeightConverter.ConvertFromInvariantString(fontWeight);
    }

    private static FontStyle? ParseFontStyle(string fontStyle)
    {
        if (string.IsNullOrEmpty(fontStyle)) return null;
        return (FontStyle?)FontStyleConverter.ConvertFromInvariantString(fontStyle);
    }

    #endregion
}
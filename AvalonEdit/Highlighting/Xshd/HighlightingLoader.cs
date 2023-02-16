using System;
using System.Xml;
using System.Xml.Schema;

namespace AvalonEdit.Highlighting.Xshd;

public static class HighlightingLoader
{
    #region XSHD loading

    public static XshdSyntaxDefinition LoadXshd(XmlReader reader)
    {
        return LoadXshd(reader, false);
    }

    internal static XshdSyntaxDefinition LoadXshd(XmlReader reader, bool skipValidation)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        try
        {
            reader.MoveToContent();
            return V2Loader.LoadDefinition(reader, skipValidation);
        }
        catch (XmlSchemaException ex)
        {
            throw WrapException(ex, ex.LineNumber, ex.LinePosition);
        }
        catch (XmlException ex)
        {
            throw WrapException(ex, ex.LineNumber, ex.LinePosition);
        }
    }

    private static Exception WrapException(Exception ex, int lineNumber, int linePosition)
    {
        return new HighlightingDefinitionInvalidException(FormatExceptionMessage(ex.Message, lineNumber, linePosition), ex);
    }

    internal static string FormatExceptionMessage(string message, int lineNumber, int linePosition)
    {
        if (lineNumber <= 0) return message;
        return "Error at position (line " + lineNumber + ", column " + linePosition + "):\n" + message;
    }

    internal static XmlReader GetValidatingReader(XmlReader input, bool ignoreWhitespace, XmlSchemaSet schemaSet)
    {
        var settings = new XmlReaderSettings { CloseInput = true, IgnoreComments = true, IgnoreWhitespace = ignoreWhitespace };
        if (schemaSet == null) return XmlReader.Create(input, settings);
        settings.Schemas        = schemaSet;
        settings.ValidationType = ValidationType.Schema;

        return XmlReader.Create(input, settings);
    }

    internal static XmlSchemaSet LoadSchemaSet(XmlReader schemaInput)
    {
        var schemaSet = new XmlSchemaSet();
        schemaSet.Add(null, schemaInput);
        schemaSet.ValidationEventHandler += (sender, args) => throw new HighlightingDefinitionInvalidException(args.Message);
        return schemaSet;
    }

    #endregion

    #region Load Highlighting from XSHD

    public static IHighlightingDefinition Load(XshdSyntaxDefinition syntaxDefinition, IHighlightingDefinitionReferenceResolver resolver)
    {
        if (syntaxDefinition == null) throw new ArgumentNullException(nameof(syntaxDefinition));
        return new XmlHighlightingDefinition(syntaxDefinition, resolver);
    }

    public static IHighlightingDefinition Load(XmlReader reader, IHighlightingDefinitionReferenceResolver resolver)
    {
        return Load(LoadXshd(reader), resolver);
    }

    #endregion
}
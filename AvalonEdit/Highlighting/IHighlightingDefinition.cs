using System.Collections.Generic;
using System.ComponentModel;

namespace AvalonEdit.Highlighting;

[TypeConverter(typeof(HighlightingDefinitionTypeConverter))]
public interface IHighlightingDefinition
{
    string Name { get; }

    HighlightingRuleSet MainRuleSet { get; }

    HighlightingRuleSet GetNamedRuleSet(string name);

    HighlightingColor GetNamedColor(string name);

    IEnumerable<HighlightingColor> NamedHighlightingColors { get; }

    IDictionary<string, string> Properties { get; }
}
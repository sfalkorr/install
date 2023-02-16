using System;
using System.Collections.Generic;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting;

[Serializable]
public class HighlightingRuleSet
{
    public HighlightingRuleSet()
    {
        Spans = new NullSafeCollection<HighlightingSpan>();
        Rules = new NullSafeCollection<HighlightingRule>();
    }

    public string Name { get; set; }

    public IList<HighlightingSpan> Spans { get; private set; }

    public IList<HighlightingRule> Rules { get; private set; }

    public override string ToString()
    {
        return "[" + GetType().Name + " " + Name + "]";
    }
}
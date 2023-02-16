using System;
using System.Diagnostics.CodeAnalysis;

namespace AvalonEdit.Highlighting.Xshd;

public enum XshdRegexType
{
    Default,
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "Using the same case as the RegexOption")]
    IgnorePatternWhitespace
}

[Serializable]
public class XshdSpan : XshdElement
{
    public string BeginRegex { get; set; }

    public XshdRegexType BeginRegexType { get; set; }

    public string EndRegex { get; set; }

    public XshdRegexType EndRegexType { get; set; }

    public bool Multiline { get; set; }

    public XshdReference<XshdRuleSet> RuleSetReference { get; set; }

    public XshdReference<XshdColor> SpanColorReference { get; set; }

    public XshdReference<XshdColor> BeginColorReference { get; set; }

    public XshdReference<XshdColor> EndColorReference { get; set; }

    public override object AcceptVisitor(IXshdVisitor visitor)
    {
        return visitor.VisitSpan(this);
    }
}
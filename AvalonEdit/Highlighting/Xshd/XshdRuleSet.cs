using System;
using System.Collections.Generic;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting.Xshd;

[Serializable]
public class XshdRuleSet : XshdElement
{
    public string Name { get; set; }

    public bool? IgnoreCase { get; set; }

    private readonly NullSafeCollection<XshdElement> elements = new();

    public IList<XshdElement> Elements => elements;

    public void AcceptElements(IXshdVisitor visitor)
    {
        foreach (var element in Elements) element.AcceptVisitor(visitor);
    }

    public override object AcceptVisitor(IXshdVisitor visitor)
    {
        return visitor.VisitRuleSet(this);
    }
}
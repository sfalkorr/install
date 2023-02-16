using System;

namespace AvalonEdit.Highlighting.Xshd;

[Serializable]
public class XshdImport : XshdElement
{
    public XshdReference<XshdRuleSet> RuleSetReference { get; set; }

    public override object AcceptVisitor(IXshdVisitor visitor)
    {
        return visitor.VisitImport(this);
    }
}
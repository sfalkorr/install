using System;
using System.Collections.Generic;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting.Xshd;

[Serializable]
public class XshdKeywords : XshdElement
{
    public XshdReference<XshdColor> ColorReference { get; set; }

    private readonly NullSafeCollection<string> words = new();

    public IList<string> Words => words;

    public override object AcceptVisitor(IXshdVisitor visitor)
    {
        return visitor.VisitKeywords(this);
    }
}
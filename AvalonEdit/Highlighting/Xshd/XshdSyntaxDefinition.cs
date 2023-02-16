using System;
using System.Collections.Generic;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting.Xshd;

[Serializable]
public class XshdSyntaxDefinition
{
    public XshdSyntaxDefinition()
    {
        Elements   = new NullSafeCollection<XshdElement>();
        Extensions = new NullSafeCollection<string>();
    }

    public string Name { get; set; }

    public IList<string> Extensions { get; private set; }

    public IList<XshdElement> Elements { get; private set; }

    public void AcceptElements(IXshdVisitor visitor)
    {
        foreach (var element in Elements) element.AcceptVisitor(visitor);
    }
}
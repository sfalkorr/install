﻿using System;

namespace AvalonEdit.Highlighting.Xshd;

[Serializable]
public class XshdProperty : XshdElement
{
    public string Name { get; set; }

    public string Value { get; set; }

    public XshdProperty()
    {
    }

    public override object AcceptVisitor(IXshdVisitor visitor)
    {
        return null;
    }
}
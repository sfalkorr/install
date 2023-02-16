using System;

namespace AvalonEdit.Highlighting.Xshd;

[Serializable]
public struct XshdReference<T> : IEquatable<XshdReference<T>> where T : XshdElement
{
    public string ReferencedDefinition { get; }

    public string ReferencedElement { get; }

    public T InlineElement { get; }

    public XshdReference(string referencedDefinition, string referencedElement)
    {
        ReferencedDefinition = referencedDefinition;
        ReferencedElement    = referencedElement ?? throw new ArgumentNullException(nameof(referencedElement));
        InlineElement        = null;
    }

    public XshdReference(T inlineElement)
    {
        ReferencedDefinition = null;
        ReferencedElement    = null;
        InlineElement        = inlineElement ?? throw new ArgumentNullException(nameof(inlineElement));
    }

    public object AcceptVisitor(IXshdVisitor visitor)
    {
        return InlineElement?.AcceptVisitor(visitor);
    }

    #region Equals and GetHashCode implementation

    public override bool Equals(object obj)
    {
        return obj is XshdReference<T> && Equals((XshdReference<T>)obj);
    }

    public bool Equals(XshdReference<T> other)
    {
        return ReferencedDefinition == other.ReferencedDefinition && ReferencedElement == other.ReferencedElement && InlineElement == other.InlineElement;
    }

    public override int GetHashCode()
    {
        return GetHashCode(ReferencedDefinition) ^ GetHashCode(ReferencedElement) ^ GetHashCode(InlineElement);
    }

    private static int GetHashCode(object o)
    {
        return o != null ? o.GetHashCode() : 0;
    }

    public static bool operator ==(XshdReference<T> left, XshdReference<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(XshdReference<T> left, XshdReference<T> right)
    {
        return !left.Equals(right);
    }

    #endregion
}
﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd;

/// <summary>
///     A reference to an xshd color, or an inline xshd color.
/// </summary>
[Serializable]
public struct XshdReference<T> : IEquatable<XshdReference<T>> where T : XshdElement
{
    /// <summary>
    ///     Gets the reference.
    /// </summary>
    public string ReferencedDefinition { get; }

    /// <summary>
    ///     Gets the reference.
    /// </summary>
    public string ReferencedElement { get; }

    /// <summary>
    ///     Gets the inline element.
    /// </summary>
    public T InlineElement { get; }

    /// <summary>
    ///     Creates a new XshdReference instance.
    /// </summary>
    public XshdReference(string referencedDefinition, string referencedElement)
    {
        if (referencedElement == null) throw new ArgumentNullException(nameof(referencedElement));
        ReferencedDefinition = referencedDefinition;
        ReferencedElement    = referencedElement;
        InlineElement        = null;
    }

    /// <summary>
    ///     Creates a new XshdReference instance.
    /// </summary>
    public XshdReference(T inlineElement)
    {
        if (inlineElement == null) throw new ArgumentNullException(nameof(inlineElement));
        ReferencedDefinition = null;
        ReferencedElement    = null;
        InlineElement        = inlineElement;
    }

    /// <summary>
    ///     Applies the visitor to the inline element, if there is any.
    /// </summary>
    public object AcceptVisitor(IXshdVisitor visitor)
    {
        if (InlineElement != null) return InlineElement.AcceptVisitor(visitor);
        return null;
    }

    #region Equals and GetHashCode implementation

    // The code in this region is useful if you want to use this structure in collections.
    // If you don't need it, you can just remove the region and the ": IEquatable<XshdColorReference>" declaration.

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (obj is XshdReference<T>) return Equals((XshdReference<T>)obj); // use Equals method below
        return false;
    }

    /// <summary>
    ///     Equality operator.
    /// </summary>
    public bool Equals(XshdReference<T> other)
    {
        // add comparisions for all members here
        return ReferencedDefinition == other.ReferencedDefinition && ReferencedElement == other.ReferencedElement && InlineElement == other.InlineElement;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // combine the hash codes of all members here (e.g. with XOR operator ^)
        return GetHashCode(ReferencedDefinition) ^ GetHashCode(ReferencedElement) ^ GetHashCode(InlineElement);
    }

    private static int GetHashCode(object o)
    {
        return o != null ? o.GetHashCode() : 0;
    }

    /// <summary>
    ///     Equality operator.
    /// </summary>
    public static bool operator ==(XshdReference<T> left, XshdReference<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Inequality operator.
    /// </summary>
    public static bool operator !=(XshdReference<T> left, XshdReference<T> right)
    {
        return !left.Equals(right);
    }

    #endregion
}
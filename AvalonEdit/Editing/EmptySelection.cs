using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

internal sealed class EmptySelection : Selection
{
    public EmptySelection(TextArea textArea) : base(textArea)
    {
    }

    public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
    {
        return this;
    }

    public override TextViewPosition StartPosition => new(TextLocation.Empty);

    public override TextViewPosition EndPosition => new(TextLocation.Empty);

    public override ISegment SurroundingSegment => null;

    public override Selection SetEndpoint(TextViewPosition endPosition)
    {
        throw new NotSupportedException();
    }

    public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
    {
        var document = textArea.Document;
        if (document == null) throw ThrowUtil.NoDocumentAssigned();
        return Create(textArea, startPosition, endPosition);
    }

    public override IEnumerable<SelectionSegment> Segments => Empty<SelectionSegment>.Array;

    public override string GetText()
    {
        return string.Empty;
    }

    public override void ReplaceSelectionWithText(string newText)
    {
        if (newText == null) throw new ArgumentNullException(nameof(newText));
        newText = AddSpacesIfRequired(newText, textArea.Caret.Position, textArea.Caret.Position);
        if (newText.Length > 0)
            if (textArea.ReadOnlySectionProvider.CanInsert(textArea.Caret.Offset))
                textArea.Document.Insert(textArea.Caret.Offset, newText);

        textArea.Caret.VisualColumn = -1;
    }

    public override int Length => 0;

    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(this);
    }

    public override bool Equals(object obj)
    {
        return this == obj;
    }
}
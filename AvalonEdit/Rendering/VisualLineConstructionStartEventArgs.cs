using System;
using AvalonEdit.Document;

namespace AvalonEdit.Rendering;

public class VisualLineConstructionStartEventArgs : EventArgs
{
    public DocumentLine FirstLineInView { get; }

    public VisualLineConstructionStartEventArgs(DocumentLine firstLineInView)
    {
        FirstLineInView = firstLineInView ?? throw new ArgumentNullException(nameof(firstLineInView));
    }
}
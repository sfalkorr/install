using System;

namespace AvalonEdit.Document;

public sealed class WeakLineTracker : ILineTracker
{
    private TextDocument  textDocument;
    private WeakReference targetObject;

    private WeakLineTracker(TextDocument textDocument, ILineTracker targetTracker)
    {
        this.textDocument = textDocument;
        targetObject      = new WeakReference(targetTracker);
    }

    public static WeakLineTracker Register(TextDocument textDocument, ILineTracker targetTracker)
    {
        if (textDocument == null) throw new ArgumentNullException(nameof(textDocument));
        if (targetTracker == null) throw new ArgumentNullException(nameof(targetTracker));
        var wlt = new WeakLineTracker(textDocument, targetTracker);
        textDocument.LineTrackers.Add(wlt);
        return wlt;
    }

    public void Deregister()
    {
        if (textDocument != null)
        {
            textDocument.LineTrackers.Remove(this);
            textDocument = null;
        }
    }

    void ILineTracker.BeforeRemoveLine(DocumentLine line)
    {
        if (targetObject.Target is ILineTracker targetTracker) targetTracker.BeforeRemoveLine(line);
        else Deregister();
    }

    void ILineTracker.SetLineLength(DocumentLine line, int newTotalLength)
    {
        if (targetObject.Target is ILineTracker targetTracker) targetTracker.SetLineLength(line, newTotalLength);
        else Deregister();
    }

    void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
    {
        if (targetObject.Target is ILineTracker targetTracker) targetTracker.LineInserted(insertionPos, newLine);
        else Deregister();
    }

    void ILineTracker.RebuildDocument()
    {
        if (targetObject.Target is ILineTracker targetTracker) targetTracker.RebuildDocument();
        else Deregister();
    }

    void ILineTracker.ChangeComplete(DocumentChangeEventArgs e)
    {
        if (targetObject.Target is ILineTracker targetTracker) targetTracker.ChangeComplete(e);
        else Deregister();
    }
}
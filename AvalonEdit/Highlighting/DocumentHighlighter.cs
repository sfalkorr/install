using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AvalonEdit.Document;
using AvalonEdit.Utils;
using SpanStack = AvalonEdit.Utils.ImmutableStack<AvalonEdit.Highlighting.HighlightingSpan>;

namespace AvalonEdit.Highlighting;

public sealed class DocumentHighlighter : ILineTracker, IHighlighter
{
    private readonly IHighlightingDefinition definition;
    private readonly HighlightingEngine      engine;
    private readonly WeakLineTracker         weakLineTracker;
    private          bool                    isHighlighting;
    private          bool                    isInHighlightingGroup;
    private          bool                    isDisposed;

    public IDocument Document { get; }

    public DocumentHighlighter(TextDocument document, IHighlightingDefinition definition)
    {
        Document        = document ?? throw new ArgumentNullException(nameof(document));
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        engine          = new HighlightingEngine(definition.MainRuleSet);
        document.VerifyAccess();
        weakLineTracker = WeakLineTracker.Register(document, this);
        InvalidateSpanStacks();
    }

    public void Dispose()
    {
        weakLineTracker?.Deregister();
        isDisposed = true;
    }

    void ILineTracker.BeforeRemoveLine(DocumentLine line)
    {
        CheckIsHighlighting();
        var number = line.LineNumber;
    }

    void ILineTracker.SetLineLength(DocumentLine line, int newTotalLength)
    {
        CheckIsHighlighting();
        var number = line.LineNumber;

        if (number < firstInvalidLine) firstInvalidLine = number;
    }

    void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
    {
        CheckIsHighlighting();
        Debug.Assert(insertionPos.LineNumber + 1 == newLine.LineNumber);
        var lineNumber = newLine.LineNumber;

        if (lineNumber < firstInvalidLine) firstInvalidLine = lineNumber;
    }

    void ILineTracker.RebuildDocument()
    {
        InvalidateSpanStacks();
    }

    void ILineTracker.ChangeComplete(DocumentChangeEventArgs e)
    {
    }

    private ImmutableStack<HighlightingSpan> initialSpanStack = SpanStack.Empty;

    public ImmutableStack<HighlightingSpan> InitialSpanStack
    {
        get => initialSpanStack;
        set
        {
            initialSpanStack = value ?? SpanStack.Empty;
            InvalidateHighlighting();
        }
    }

    public void InvalidateHighlighting()
    {
        InvalidateSpanStacks();
        OnHighlightStateChanged(1, Document.LineCount);
    }

    private void InvalidateSpanStacks()
    {
        CheckIsHighlighting();

        firstInvalidLine = 1;
    }

    private int firstInvalidLine;

    public HighlightedLine HighlightLine(int lineNumber)
    {
        ThrowUtil.CheckInRangeInclusive(lineNumber, "lineNumber", 1, Document.LineCount);
        CheckIsHighlighting();
        isHighlighting = true;
        try
        {
            HighlightUpTo(lineNumber - 1);
            var line   = Document.GetLineByNumber(lineNumber);
            var result = engine.HighlightLine(Document, line);
            return result;
        }
        finally
        {
            isHighlighting = false;
        }
    }

    public SpanStack GetSpanStack(int lineNumber)
    {
        ThrowUtil.CheckInRangeInclusive(lineNumber, "lineNumber", 0, Document.LineCount);
        if (firstInvalidLine <= lineNumber) UpdateHighlightingState(lineNumber);
        return null;
    }

    public IEnumerable<HighlightingColor> GetColorStack(int lineNumber)
    {
        return GetSpanStack(lineNumber).Select(s => s.SpanColor).Where(s => s != null);
    }

    private void CheckIsHighlighting()
    {
        if (isDisposed) throw new ObjectDisposedException("DocumentHighlighter");
        if (isHighlighting) throw new InvalidOperationException("Invalid call - a highlighting operation is currently running.");
    }

    public void UpdateHighlightingState(int lineNumber)
    {
        CheckIsHighlighting();
        isHighlighting = true;
        try
        {
            HighlightUpTo(lineNumber);
        }
        finally
        {
            isHighlighting = false;
        }
    }

    private void HighlightUpTo(int targetLineNumber)
    {
        for (var currentLine = 0; currentLine <= targetLineNumber; currentLine++)
        {
            if (firstInvalidLine > currentLine)
            {
                if (firstInvalidLine <= targetLineNumber) currentLine = firstInvalidLine;
                else break;
            }

            engine.ScanLine(Document, Document.GetLineByNumber(currentLine));
        }
    }

    public event HighlightingStateChangedEventHandler HighlightingStateChanged;

    private void OnHighlightStateChanged(int fromLineNumber, int toLineNumber)
    {
        if (HighlightingStateChanged != null) HighlightingStateChanged(fromLineNumber, toLineNumber);
    }

    public HighlightingColor DefaultTextColor => null;

    public void BeginHighlighting()
    {
        if (isInHighlightingGroup) throw new InvalidOperationException("Highlighting group is already open");
        isInHighlightingGroup = true;
    }

    public void EndHighlighting()
    {
        if (!isInHighlightingGroup) throw new InvalidOperationException("Highlighting group is not open");
        isInHighlightingGroup = false;
    }

    public HighlightingColor GetNamedColor(string name)
    {
        return definition.GetNamedColor(name);
    }
}
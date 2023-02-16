using System;
using System.Collections.Generic;
using AvalonEdit.Document;

namespace AvalonEdit.Highlighting;

public interface IHighlighter : IDisposable
{
    IDocument Document { get; }

    IEnumerable<HighlightingColor> GetColorStack(int lineNumber);

    HighlightedLine HighlightLine(int lineNumber);

    void UpdateHighlightingState(int lineNumber);

    event HighlightingStateChangedEventHandler HighlightingStateChanged;

    void BeginHighlighting();

    void EndHighlighting();

    HighlightingColor GetNamedColor(string name);

    HighlightingColor DefaultTextColor { get; }
}

public delegate void HighlightingStateChangedEventHandler(int fromLineNumber, int toLineNumber);
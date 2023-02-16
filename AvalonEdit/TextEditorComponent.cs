using System;
using System.ComponentModel;
using AvalonEdit.Document;
using AvalonEdit.Editing;
using AvalonEdit.Rendering;

namespace AvalonEdit;

public interface ITextEditorComponent : IServiceProvider
{
    TextDocument Document { get; }

    event EventHandler DocumentChanged;

    TextEditorOptions Options { get; }

    event PropertyChangedEventHandler OptionChanged;
}
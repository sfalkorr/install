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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Editing;

/// <summary>
///     Contains the predefined input handlers.
/// </summary>
public class TextAreaDefaultInputHandler : TextAreaInputHandler
{
    /// <summary>
    ///     Gets the caret navigation input handler.
    /// </summary>
    public TextAreaInputHandler CaretNavigation { get; }

    /// <summary>
    ///     Gets the editing input handler.
    /// </summary>
    public TextAreaInputHandler Editing { get; }

    /// <summary>
    ///     Gets the mouse selection input handler.
    /// </summary>
    public ITextAreaInputHandler MouseSelection { get; }

    /// <summary>
    ///     Creates a new TextAreaDefaultInputHandler instance.
    /// </summary>
    public TextAreaDefaultInputHandler(TextArea textArea) : base(textArea)
    {
        NestedInputHandlers.Add(MouseSelection = new SelectionMouseHandler(textArea));


    }

    internal static KeyBinding CreateFrozenKeyBinding(ICommand command, ModifierKeys modifiers, Key key)
    {
        var kb = new KeyBinding(command, key, modifiers);
        // Mark KeyBindings as frozen because they're shared between multiple editor instances.
        // KeyBinding derives from Freezable only in .NET 4, so we have to use this little trick:
        if ((object)kb is Freezable f) f.Freeze();
        return kb;
    }

    internal static void WorkaroundWPFMemoryLeak(List<InputBinding> inputBindings)
    {
        // Work around WPF memory leak:
        // KeyBinding retains a reference to whichever UIElement it is used in first.
        // Using a dummy element for this purpose ensures that we don't leak
        // a real text editor (with a potentially large document).
        var dummyElement = new UIElement();
        dummyElement.InputBindings.AddRange(inputBindings);
    }

    #region Undo / Redo






    }







    #endregion
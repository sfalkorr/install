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
using System.Collections.Generic;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing;

/// <summary>
///     A set of input bindings and event handlers for the text area.
/// </summary>
/// <remarks>
///     <para>
///         There is one active input handler per text area (<see cref="Editing.TextArea.ActiveInputHandler" />), plus
///         a number of active stacked input handlers.
///     </para>
///     <para>
///         The text area also stores a reference to a default input handler, but that is not necessarily active.
///     </para>
///     <para>
///         Stacked input handlers work in addition to the set of currently active handlers (without detaching them).
///         They are detached in the reverse order of being attached.
///     </para>
/// </remarks>
public interface ITextAreaInputHandler
{
    /// <summary>
    ///     Gets the text area that the input handler belongs to.
    /// </summary>
    TextArea TextArea { get; }

    /// <summary>
    ///     Attaches an input handler to the text area.
    /// </summary>
    void Attach();

    /// <summary>
    ///     Detaches the input handler from the text area.
    /// </summary>
    void Detach();
}

/// <summary>
///     Stacked input handler.
///     Uses OnEvent-methods instead of registering event handlers to ensure that the events are handled in the correct order.
/// </summary>
public abstract class TextAreaStackedInputHandler : ITextAreaInputHandler
{
    /// <inheritdoc />
    public TextArea TextArea { get; }

    /// <summary>
    ///     Creates a new TextAreaInputHandler.
    /// </summary>
    protected TextAreaStackedInputHandler(TextArea textArea)
    {
        if (textArea == null) throw new ArgumentNullException(nameof(textArea));
        TextArea = textArea;
    }

    /// <inheritdoc />
    public virtual void Attach()
    {
    }

    /// <inheritdoc />
    public virtual void Detach()
    {
    }

    /// <summary>
    ///     Called for the PreviewKeyDown event.
    /// </summary>
    public virtual void OnPreviewKeyDown(KeyEventArgs e)
    {
    }

    /// <summary>
    ///     Called for the PreviewKeyUp event.
    /// </summary>
    public virtual void OnPreviewKeyUp(KeyEventArgs e)
    {
    }
}

/// <summary>
///     Default-implementation of <see cref="ITextAreaInputHandler" />.
/// </summary>
/// <remarks>
///     <inheritdoc cref="ITextAreaInputHandler" />
/// </remarks>
public class TextAreaInputHandler : ITextAreaInputHandler
{
    private readonly ObserveAddRemoveCollection<CommandBinding>        commandBindings;
    private readonly ObserveAddRemoveCollection<InputBinding>          inputBindings;
    private readonly ObserveAddRemoveCollection<ITextAreaInputHandler> nestedInputHandlers;

    /// <summary>
    ///     Creates a new TextAreaInputHandler.
    /// </summary>
    public TextAreaInputHandler(TextArea textArea)
    {
        if (textArea == null) throw new ArgumentNullException(nameof(textArea));
        TextArea            = textArea;
        commandBindings     = new ObserveAddRemoveCollection<CommandBinding>(CommandBinding_Added, CommandBinding_Removed);
        inputBindings       = new ObserveAddRemoveCollection<InputBinding>(InputBinding_Added, InputBinding_Removed);
        nestedInputHandlers = new ObserveAddRemoveCollection<ITextAreaInputHandler>(NestedInputHandler_Added, NestedInputHandler_Removed);
    }

    /// <inheritdoc />
    public TextArea TextArea { get; }

    /// <summary>
    ///     Gets whether the input handler is currently attached to the text area.
    /// </summary>
    public bool IsAttached { get; private set; }

    #region CommandBindings / InputBindings

    /// <summary>
    ///     Gets the command bindings of this input handler.
    /// </summary>
    public ICollection<CommandBinding> CommandBindings => commandBindings;

    private void CommandBinding_Added(CommandBinding commandBinding)
    {
        if (IsAttached) TextArea.CommandBindings.Add(commandBinding);
    }

    private void CommandBinding_Removed(CommandBinding commandBinding)
    {
        if (IsAttached) TextArea.CommandBindings.Remove(commandBinding);
    }

    /// <summary>
    ///     Gets the input bindings of this input handler.
    /// </summary>
    public ICollection<InputBinding> InputBindings => inputBindings;

    private void InputBinding_Added(InputBinding inputBinding)
    {
        if (IsAttached) TextArea.InputBindings.Add(inputBinding);
    }

    private void InputBinding_Removed(InputBinding inputBinding)
    {
        if (IsAttached) TextArea.InputBindings.Remove(inputBinding);
    }

    /// <summary>
    ///     Adds a command and input binding.
    /// </summary>
    /// <param name="command">The command ID.</param>
    /// <param name="modifiers">The modifiers of the keyboard shortcut.</param>
    /// <param name="key">The key of the keyboard shortcut.</param>
    /// <param name="handler">The event handler to run when the command is executed.</param>
    public void AddBinding(ICommand command, ModifierKeys modifiers, Key key, ExecutedRoutedEventHandler handler)
    {
        CommandBindings.Add(new CommandBinding(command, handler));
        InputBindings.Add(new KeyBinding(command, key, modifiers));
    }

    #endregion

    #region NestedInputHandlers

    /// <summary>
    ///     Gets the collection of nested input handlers. NestedInputHandlers are activated and deactivated
    ///     together with this input handler.
    /// </summary>
    public ICollection<ITextAreaInputHandler> NestedInputHandlers => nestedInputHandlers;

    private void NestedInputHandler_Added(ITextAreaInputHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (handler.TextArea != TextArea) throw new ArgumentException("The nested handler must be working for the same text area!");
        if (IsAttached) handler.Attach();
    }

    private void NestedInputHandler_Removed(ITextAreaInputHandler handler)
    {
        if (IsAttached) handler.Detach();
    }

    #endregion

    #region Attach/Detach

    /// <inheritdoc />
    public virtual void Attach()
    {
        if (IsAttached) throw new InvalidOperationException("Input handler is already attached");
        IsAttached = true;

        TextArea.CommandBindings.AddRange(commandBindings);
        TextArea.InputBindings.AddRange(inputBindings);
        foreach (var handler in nestedInputHandlers) handler.Attach();
    }

    /// <inheritdoc />
    public virtual void Detach()
    {
        if (!IsAttached) throw new InvalidOperationException("Input handler is not attached");
        IsAttached = false;

        foreach (var b in commandBindings) TextArea.CommandBindings.Remove(b);
        foreach (var b in inputBindings) TextArea.InputBindings.Remove(b);
        foreach (var handler in nestedInputHandlers) handler.Detach();
    }

    #endregion
}
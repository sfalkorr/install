using System;
using System.Collections.Generic;
using System.Windows.Input;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

public interface ITextAreaInputHandler
{
    TextArea TextArea { get; }

    void Attach();

    void Detach();
}

public abstract class TextAreaStackedInputHandler : ITextAreaInputHandler
{
    public TextArea TextArea { get; }

    protected TextAreaStackedInputHandler(TextArea textArea)
    {
        TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
    }

    public virtual void Attach()
    {
    }

    public virtual void Detach()
    {
    }

    public virtual void OnPreviewKeyDown(KeyEventArgs e)
    {
    }

    public virtual void OnPreviewKeyUp(KeyEventArgs e)
    {
    }
}

public class TextAreaInputHandler : ITextAreaInputHandler
{
    private readonly ObserveAddRemoveCollection<CommandBinding>        commandBindings;
    private readonly ObserveAddRemoveCollection<InputBinding>          inputBindings;
    private readonly ObserveAddRemoveCollection<ITextAreaInputHandler> nestedInputHandlers;

    public TextAreaInputHandler(TextArea textArea)
    {
        TextArea            = textArea ?? throw new ArgumentNullException(nameof(textArea));
        commandBindings     = new ObserveAddRemoveCollection<CommandBinding>(CommandBinding_Added, CommandBinding_Removed);
        inputBindings       = new ObserveAddRemoveCollection<InputBinding>(InputBinding_Added, InputBinding_Removed);
        nestedInputHandlers = new ObserveAddRemoveCollection<ITextAreaInputHandler>(NestedInputHandler_Added, NestedInputHandler_Removed);
    }

    public TextArea TextArea { get; }

    public bool IsAttached { get; private set; }

    #region CommandBindings / InputBindings

    public ICollection<CommandBinding> CommandBindings => commandBindings;

    private void CommandBinding_Added(CommandBinding commandBinding)
    {
        if (IsAttached) TextArea.CommandBindings.Add(commandBinding);
    }

    private void CommandBinding_Removed(CommandBinding commandBinding)
    {
        if (IsAttached) TextArea.CommandBindings.Remove(commandBinding);
    }

    public ICollection<InputBinding> InputBindings => inputBindings;

    private void InputBinding_Added(InputBinding inputBinding)
    {
        if (IsAttached) TextArea.InputBindings.Add(inputBinding);
    }

    private void InputBinding_Removed(InputBinding inputBinding)
    {
        if (IsAttached) TextArea.InputBindings.Remove(inputBinding);
    }

    public void AddBinding(ICommand command, ModifierKeys modifiers, Key key, ExecutedRoutedEventHandler handler)
    {
        CommandBindings.Add(new CommandBinding(command, handler));
        InputBindings.Add(new KeyBinding(command, key, modifiers));
    }

    #endregion

    #region NestedInputHandlers

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

    public virtual void Attach()
    {
        if (IsAttached) throw new InvalidOperationException("Input handler is already attached");
        IsAttached = true;

        TextArea.CommandBindings.AddRange(commandBindings);
        TextArea.InputBindings.AddRange(inputBindings);
        foreach (var handler in nestedInputHandlers) handler.Attach();
    }

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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using AvalonEdit.Document;

namespace AvalonEdit.Editing;

public class TextAreaDefaultInputHandler : TextAreaInputHandler
{
    public TextAreaInputHandler CaretNavigation { get; }

    public TextAreaInputHandler Editing { get; }

    public ITextAreaInputHandler MouseSelection { get; }

    public TextAreaDefaultInputHandler(TextArea textArea) : base(textArea)
    {
        NestedInputHandlers.Add(MouseSelection = new SelectionMouseHandler(textArea));
    }

    internal static KeyBinding CreateFrozenKeyBinding(ICommand command, ModifierKeys modifiers, Key key)
    {
        var kb = new KeyBinding(command, key, modifiers);
        if ((object)kb is Freezable f) f.Freeze();
        return kb;
    }

    internal static void WorkaroundWPFMemoryLeak(List<InputBinding> inputBindings)
    {
        var dummyElement = new UIElement();
        dummyElement.InputBindings.AddRange(inputBindings);
    }

    #region Undo / Redo

}

#endregion
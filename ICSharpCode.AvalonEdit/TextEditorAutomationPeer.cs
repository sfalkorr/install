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

using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit;

/// <summary>
///     Exposes <see cref="ICSharpCode.AvalonEdit.TextEditor" /> to automation.
/// </summary>
public class TextEditorAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    /// <summary>
    ///     Creates a new TextEditorAutomationPeer instance.
    /// </summary>
    public TextEditorAutomationPeer(FrameworkElement owner) : base(owner)
    {
        Debug.WriteLine("TextEditorAutomationPeer was created");
    }

    private TextEditor TextEditor => (TextEditor)Owner;

    void IValueProvider.SetValue(string value)
    {
        TextEditor.Text = value;
    }

    string IValueProvider.Value => TextEditor.Text;

    bool IValueProvider.IsReadOnly => TextEditor.IsReadOnly;

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Document;
    }

    /// <inheritdoc />
    public override object GetPattern(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Value) return this;

        if (patternInterface == PatternInterface.Scroll)
        {
            var scrollViewer = TextEditor.ScrollViewer;
            if (scrollViewer != null) return FromElement(scrollViewer);
        }

        if (patternInterface == PatternInterface.Text) return FromElement(TextEditor.TextArea);

        return base.GetPattern(patternInterface);
    }

    internal void RaiseIsReadOnlyChanged(bool oldValue, bool newValue)
    {
        RaisePropertyChangedEvent(ValuePatternIdentifiers.IsReadOnlyProperty, Boxes.Box(oldValue), Boxes.Box(newValue));
    }
}
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ICSharpCode.AvalonEdit.Editing;

internal class ImeSupport
{
    private readonly TextArea   textArea;
    private          IntPtr     currentContext;
    private          IntPtr     previousContext;
    private          IntPtr     defaultImeWnd;
    private          HwndSource hwndSource;
    private          bool       isReadOnly;

    public ImeSupport(TextArea textArea)
    {
        this.textArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
        InputMethod.SetIsInputMethodSuspended(this.textArea, textArea.Options.EnableImeSupport);
        // We listen to CommandManager.RequerySuggested for both caret offset changes and changes to the set of read-only sections.
        // This is because there's no dedicated event for read-only section changes; but RequerySuggested needs to be raised anyways
        // to invalidate the Paste command.
        CommandManager.RequerySuggested += OnRequerySuggested;
        textArea.OptionChanged          += TextAreaOptionChanged;
    }

    private void OnRequerySuggested(object sender, EventArgs e)
    {
        UpdateImeEnabled();
    }

    private void TextAreaOptionChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "EnableImeSupport") return;
        InputMethod.SetIsInputMethodSuspended(textArea, textArea.Options.EnableImeSupport);
        UpdateImeEnabled();
    }

    public void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        UpdateImeEnabled();
    }

    public void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        if (Equals(e.OldFocus, textArea) && currentContext != IntPtr.Zero) ImeNativeWrapper.NotifyIme(currentContext);
        ClearContext();
    }

    private void UpdateImeEnabled()
    {
        if (textArea.Options.EnableImeSupport && textArea.IsKeyboardFocused)
        {
            var newReadOnly = !textArea.ReadOnlySectionProvider.CanInsert(textArea.Caret.Offset);
            if (hwndSource != null && isReadOnly == newReadOnly) return;
            ClearContext(); // clear existing context (on read-only change)
            isReadOnly = newReadOnly;
            CreateContext();
        }
        else
        {
            ClearContext();
        }
    }

    private void ClearContext()
    {
        if (hwndSource == null) return;
        ImeNativeWrapper.ImmAssociateContext(hwndSource.Handle, previousContext);
        ImeNativeWrapper.ImmReleaseContext(defaultImeWnd, currentContext);
        currentContext = IntPtr.Zero;
        defaultImeWnd  = IntPtr.Zero;
        hwndSource.RemoveHook(WndProc);
        hwndSource = null;
    }

    private void CreateContext()
    {
        hwndSource = (HwndSource)PresentationSource.FromVisual(textArea);
        if (hwndSource == null) return;
        if (isReadOnly)
        {
            defaultImeWnd  = IntPtr.Zero;
            currentContext = IntPtr.Zero;
        }
        else
        {
            defaultImeWnd  = ImeNativeWrapper.ImmGetDefaultIMEWnd(IntPtr.Zero);
            currentContext = ImeNativeWrapper.ImmGetContext(defaultImeWnd);
        }

        previousContext = ImeNativeWrapper.ImmAssociateContext(hwndSource.Handle, currentContext);
        hwndSource.AddHook(WndProc);
        // UpdateCompositionWindow() will be called by the caret becoming visible

        var threadMgr = ImeNativeWrapper.GetTextFrameworkThreadManager();
        if (threadMgr != null)
            // Even though the docs says passing null is invalid, this seems to help
            // activating the IME on the default input context that is shared with WPF
            threadMgr.SetFocus(IntPtr.Zero);
    }

    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case ImeNativeWrapper.WM_INPUTLANGCHANGE:
                // Don't mark the message as handled; other windows
                // might want to handle it as well.

                // If we have a context, recreate it
                if (hwndSource != null)
                {
                    ClearContext();
                    CreateContext();
                }

                break;
            case ImeNativeWrapper.WM_IME_COMPOSITION:
                UpdateCompositionWindow();
                break;
        }

        return IntPtr.Zero;
    }

    public void UpdateCompositionWindow()
    {
        if (currentContext == IntPtr.Zero) return;
        ImeNativeWrapper.SetCompositionFont(hwndSource, currentContext, textArea);
        ImeNativeWrapper.SetCompositionWindow(hwndSource, currentContext, textArea);
    }
}
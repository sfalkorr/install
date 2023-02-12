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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit;

/// <summary>
///     The text editor control.
///     Contains a scrollable TextArea.
/// </summary>
[Localizability(LocalizationCategory.Text)] [ContentProperty("Text")]
public class TextEditor : Control, ITextEditorComponent, IServiceProvider, IWeakEventListener
{
    #region Constructors

    static TextEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TextEditor), new FrameworkPropertyMetadata(typeof(TextEditor)));
        FocusableProperty.OverrideMetadata(typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.True));
    }

    /// <summary>
    ///     Creates a new TextEditor instance.
    /// </summary>
    public TextEditor() : this(new TextArea())
    {
    }

    /// <summary>
    ///     Creates a new TextEditor instance.
    /// </summary>
    protected TextEditor(TextArea textArea)
    {
        if (textArea == null) throw new ArgumentNullException(nameof(textArea));
        TextArea = textArea;

        textArea.TextView.Services.AddService(typeof(TextEditor), this);

        SetCurrentValue(OptionsProperty, textArea.Options);
        SetCurrentValue(DocumentProperty, new TextDocument());
    }

    #endregion

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TextEditorAutomationPeer(this);
    }

    /// Forward focus to TextArea.
    /// <inheritdoc />
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        if (e.NewFocus == this)
        {
            Keyboard.Focus(TextArea);
            e.Handled = true;
        }
    }

    #region Document property

    /// <summary>
    ///     Document property.
    /// </summary>
    public static readonly DependencyProperty DocumentProperty = TextView.DocumentProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(OnDocumentChanged));

    /// <summary>
    ///     Gets/Sets the document displayed by the text editor.
    ///     This is a dependency property.
    /// </summary>
    public TextDocument Document { get => (TextDocument)GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }

    /// <summary>
    ///     Occurs when the document property has changed.
    /// </summary>
    public event EventHandler DocumentChanged;

    /// <summary>
    ///     Raises the <see cref="DocumentChanged" /> event.
    /// </summary>
    protected virtual void OnDocumentChanged(EventArgs e)
    {
        if (DocumentChanged != null) DocumentChanged(this, e);
    }

    private static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        ((TextEditor)dp).OnDocumentChanged((TextDocument)e.OldValue, (TextDocument)e.NewValue);
    }

    private void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
    {
        if (oldValue != null)
        {
            TextDocumentWeakEventManager.TextChanged.RemoveListener(oldValue, this);

        }

        TextArea.Document = newValue;
        if (newValue != null)
        {
            TextDocumentWeakEventManager.TextChanged.AddListener(newValue, this);

        }

        OnDocumentChanged(EventArgs.Empty);
        OnTextChanged(EventArgs.Empty);
    }

    #endregion

    #region Options property

    /// <summary>
    ///     Options property.
    /// </summary>
    public static readonly DependencyProperty OptionsProperty = TextView.OptionsProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(OnOptionsChanged));

    /// <summary>
    ///     Gets/Sets the options currently used by the text editor.
    /// </summary>
    public TextEditorOptions Options { get => (TextEditorOptions)GetValue(OptionsProperty); set => SetValue(OptionsProperty, value); }

    /// <summary>
    ///     Occurs when a text editor option has changed.
    /// </summary>
    public event PropertyChangedEventHandler OptionChanged;

    /// <summary>
    ///     Raises the <see cref="OptionChanged" /> event.
    /// </summary>
    protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
    {
        if (OptionChanged != null) OptionChanged(this, e);
    }

    private static void OnOptionsChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        ((TextEditor)dp).OnOptionsChanged((TextEditorOptions)e.OldValue, (TextEditorOptions)e.NewValue);
    }

    private void OnOptionsChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
    {
        if (oldValue != null) PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
        TextArea.Options = newValue;
        if (newValue != null) PropertyChangedWeakEventManager.AddListener(newValue, this);
        OnOptionChanged(new PropertyChangedEventArgs(null));
    }

    /// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent" />
    protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        if (managerType == typeof(PropertyChangedWeakEventManager))
        {
            OnOptionChanged((PropertyChangedEventArgs)e);
            return true;
        }

        if (managerType == typeof(TextDocumentWeakEventManager.TextChanged))
        {
            OnTextChanged(e);
            return true;
        }

        if (managerType == typeof(PropertyChangedEventManager)) return HandleIsOriginalChanged((PropertyChangedEventArgs)e);

        return false;
    }

    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        return ReceiveWeakEvent(managerType, sender, e);
    }

    #endregion

    #region Text property

    /// <summary>
    ///     Gets/Sets the text of the current document.
    /// </summary>
    [Localizability(LocalizationCategory.Text)] [DefaultValue("")]
    public string Text
    {
        get
        {
            var document = Document;
            return document != null ? document.Text : string.Empty;
        }
        set
        {
            var document = GetDocument();
            document.Text = value ?? string.Empty;
            // after replacing the full text, the caret is positioned at the end of the document
            // - reset it to the beginning.
            CaretOffset = 0;

        }
    }

    private TextDocument GetDocument()
    {
        var document = Document;
        if (document == null) throw ThrowUtil.NoDocumentAssigned();
        return document;
    }

    /// <summary>
    ///     Occurs when the Text property changes.
    /// </summary>
    public event EventHandler TextChanged;

    /// <summary>
    ///     Raises the <see cref="TextChanged" /> event.
    /// </summary>
    protected virtual void OnTextChanged(EventArgs e)
    {
        if (TextChanged != null) TextChanged(this, e);
    }

    #endregion

    #region TextArea / ScrollViewer properties

    /// <summary>
    ///     Is called after the template was applied.
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ScrollViewer = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);
    }

    /// <summary>
    ///     Gets the text area.
    /// </summary>
    public TextArea TextArea { get; }

    /// <summary>
    ///     Gets the scroll viewer used by the text editor.
    ///     This property can return null if the template has not been applied / does not contain a scroll viewer.
    /// </summary>
    internal ScrollViewer ScrollViewer { get; private set; }

    private bool CanExecute(RoutedUICommand command)
    {
        return command.CanExecute(null, TextArea);
    }

    private void Execute(RoutedUICommand command)
    {
        command.Execute(null, TextArea);
    }

    #endregion

    #region Syntax highlighting

    /// <summary>
    ///     The <see cref="SyntaxHighlighting" /> property.
    /// </summary>
    public static readonly DependencyProperty SyntaxHighlightingProperty = DependencyProperty.Register(nameof(SyntaxHighlighting), typeof(IHighlightingDefinition), typeof(TextEditor), new FrameworkPropertyMetadata(OnSyntaxHighlightingChanged));


    /// <summary>
    ///     Gets/sets the syntax highlighting definition used to colorize the text.
    /// </summary>
    public IHighlightingDefinition SyntaxHighlighting { get => (IHighlightingDefinition)GetValue(SyntaxHighlightingProperty); set => SetValue(SyntaxHighlightingProperty, value); }

    private IVisualLineTransformer colorizer;

    private static void OnSyntaxHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TextEditor)d).OnSyntaxHighlightingChanged(e.NewValue as IHighlightingDefinition);
    }

    private void OnSyntaxHighlightingChanged(IHighlightingDefinition newValue)
    {
        if (colorizer != null)
        {
            TextArea.TextView.LineTransformers.Remove(colorizer);
            colorizer = null;
        }

        if (newValue != null)
        {
            colorizer = CreateColorizer(newValue);
            if (colorizer != null) TextArea.TextView.LineTransformers.Insert(0, colorizer);
        }
    }

    /// <summary>
    ///     Creates the highlighting colorizer for the specified highlighting definition.
    ///     Allows derived classes to provide custom colorizer implementations for special highlighting definitions.
    /// </summary>
    /// <returns></returns>
    protected virtual IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition)
    {
        if (highlightingDefinition == null) throw new ArgumentNullException(nameof(highlightingDefinition));
        return new HighlightingColorizer(highlightingDefinition);
    }

    #endregion

    #region WordWrap

    /// <summary>
    ///     Word wrap dependency property.
    /// </summary>
    public static readonly DependencyProperty WordWrapProperty = DependencyProperty.Register(nameof(WordWrap), typeof(bool), typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.False));

    /// <summary>
    ///     Specifies whether the text editor uses word wrapping.
    /// </summary>
    /// <remarks>
    ///     Setting WordWrap=true has the same effect as setting HorizontalScrollBarVisibility=Disabled and will override the
    ///     HorizontalScrollBarVisibility setting.
    /// </remarks>
    public bool WordWrap { get => (bool)GetValue(WordWrapProperty); set => SetValue(WordWrapProperty, Boxes.Box(value)); }

    #endregion

    #region IsReadOnly

    /// <summary>
    ///     IsReadOnly dependency property.
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.False, OnIsReadOnlyChanged));

    /// <summary>
    ///     Specifies whether the user can change the text editor content.
    ///     Setting this property will replace the
    ///     <see cref="Editing.TextArea.ReadOnlySectionProvider">TextArea.ReadOnlySectionProvider</see>.
    /// </summary>
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, Boxes.Box(value)); }

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor editor)
        {
            if ((bool)e.NewValue) editor.TextArea.ReadOnlySectionProvider = ReadOnlySectionDocument.Instance;
            else editor.TextArea.ReadOnlySectionProvider                  = NoReadOnlySections.Instance;

            if (UIElementAutomationPeer.FromElement(editor) is TextEditorAutomationPeer peer) peer.RaiseIsReadOnlyChanged((bool)e.OldValue, (bool)e.NewValue);
        }
    }

    #endregion

    #region IsModified

    /// <summary>
    ///     Dependency property for <see cref="IsModified" />
    /// </summary>
    public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register(nameof(IsModified), typeof(bool), typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.False, OnIsModifiedChanged));

    /// <summary>
    ///     Gets/Sets the 'modified' flag.
    /// </summary>
    public bool IsModified { get => (bool)GetValue(IsModifiedProperty); set => SetValue(IsModifiedProperty, Boxes.Box(value)); }

    private static void OnIsModifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor editor)
        {
            var document = editor.Document;
            if (document != null)
            {


            }
        }
    }

    private bool HandleIsOriginalChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IsOriginalFile")
        {
            var document = Document;
            return true;
        }

        return false;
    }

    #endregion

    #region ShowLineNumbers

    /// <summary>
    ///     ShowLineNumbers dependency property.
    /// </summary>
    public static readonly DependencyProperty ShowLineNumbersProperty = DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.False, OnShowLineNumbersChanged));

    /// <summary>
    ///     Specifies whether line numbers are shown on the left to the text view.
    /// </summary>
    public bool ShowLineNumbers { get => (bool)GetValue(ShowLineNumbersProperty); set => SetValue(ShowLineNumbersProperty, Boxes.Box(value)); }

    private static void OnShowLineNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor      = (TextEditor)d;
        var leftMargins = editor.TextArea.LeftMargins;
        if ((bool)e.NewValue)
        {
            var lineNumbers = new LineNumberMargin();
            var line        = (Line)DottedLineMargin.Create();
            leftMargins.Insert(0, lineNumbers);
            leftMargins.Insert(1, line);
            var lineNumbersForeground = new Binding("LineNumbersForeground") { Source = editor };
            line.SetBinding(Shape.StrokeProperty, lineNumbersForeground);
            lineNumbers.SetBinding(ForegroundProperty, lineNumbersForeground);
        }
        else
        {
            for (var i = 0; i < leftMargins.Count; i++)
                if (leftMargins[i] is LineNumberMargin)
                {
                    leftMargins.RemoveAt(i);
                    if (i < leftMargins.Count && DottedLineMargin.IsDottedLineMargin(leftMargins[i])) leftMargins.RemoveAt(i);
                    break;
                }
        }
    }

    #endregion

    #region LineNumbersForeground

    /// <summary>
    ///     LineNumbersForeground dependency property.
    /// </summary>
    public static readonly DependencyProperty LineNumbersForegroundProperty = DependencyProperty.Register(nameof(LineNumbersForeground), typeof(Brush), typeof(TextEditor), new FrameworkPropertyMetadata(Brushes.Gray, OnLineNumbersForegroundChanged));

    /// <summary>
    ///     Gets/sets the Brush used for displaying the foreground color of line numbers.
    /// </summary>
    public Brush LineNumbersForeground { get => (Brush)GetValue(LineNumbersForegroundProperty); set => SetValue(LineNumbersForegroundProperty, value); }

    private static void OnLineNumbersForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor           = (TextEditor)d;
        var lineNumberMargin = editor.TextArea.LeftMargins.FirstOrDefault(margin => margin is LineNumberMargin) as LineNumberMargin;
        ;

        if (lineNumberMargin != null) lineNumberMargin.SetValue(ForegroundProperty, e.NewValue);
    }

    #endregion

    #region TextBoxBase-like methods

    /// <summary>
    ///     Appends text to the end of the document.
    /// </summary>
    public void AppendText(string textData)
    {
        var document = GetDocument();
        document.Insert(document.TextLength, textData);
    }

    public void AppendColorLine(string textData, Brush LineColor, DependencyObject d = default)
    {
        d = this;
        var editor       = (TextEditor)d;
        var setlinecolor = new LineColorizer(editor.LineCount);
        setlinecolor.LineColor = LineColor;
        editor.TextArea.TextView.LineTransformers.Add(setlinecolor);
        editor.AppendText(textData);
    }

    /// <summary>
    ///     Begins a group of document changes.
    /// </summary>
    public void BeginChange()
    {
        GetDocument().BeginUpdate();
    }

    /// <summary>
    ///     Copies the current selection to the clipboard.
    /// </summary>
    public void Copy()
    {
        Execute(ApplicationCommands.Copy);
    }

    /// <summary>
    ///     Removes the current selection and copies it to the clipboard.
    /// </summary>
    public void Cut()
    {
        Execute(ApplicationCommands.Cut);
    }

    /// <summary>
    ///     Begins a group of document changes and returns an object that ends the group of document
    ///     changes when it is disposed.
    /// </summary>
    public IDisposable DeclareChangeBlock()
    {
        return GetDocument().RunUpdate();
    }

    /// <summary>
    ///     Removes the current selection without copying it to the clipboard.
    /// </summary>
    public void Delete()
    {
        Execute(ApplicationCommands.Delete);
    }

    /// <summary>
    ///     Ends the current group of document changes.
    /// </summary>
    public void EndChange()
    {
        GetDocument().EndUpdate();
    }

    /// <summary>
    ///     Scrolls one line down.
    /// </summary>
    public void LineDown()
    {
        if (ScrollViewer != null) ScrollViewer.LineDown();
    }

    /// <summary>
    ///     Scrolls to the left.
    /// </summary>
    public void LineLeft()
    {
        if (ScrollViewer != null) ScrollViewer.LineLeft();
    }

    /// <summary>
    ///     Scrolls to the right.
    /// </summary>
    public void LineRight()
    {
        if (ScrollViewer != null) ScrollViewer.LineRight();
    }

    /// <summary>
    ///     Scrolls one line up.
    /// </summary>
    public void LineUp()
    {
        if (ScrollViewer != null) ScrollViewer.LineUp();
    }

    /// <summary>
    ///     Scrolls one page down.
    /// </summary>
    public void PageDown()
    {
        if (ScrollViewer != null) ScrollViewer.PageDown();
    }

    /// <summary>
    ///     Scrolls one page up.
    /// </summary>
    public void PageUp()
    {
        if (ScrollViewer != null) ScrollViewer.PageUp();
    }

    /// <summary>
    ///     Scrolls one page left.
    /// </summary>
    public void PageLeft()
    {
        if (ScrollViewer != null) ScrollViewer.PageLeft();
    }

    /// <summary>
    ///     Scrolls one page right.
    /// </summary>
    public void PageRight()
    {
        if (ScrollViewer != null) ScrollViewer.PageRight();
    }

    /// <summary>
    ///     Pastes the clipboard content.
    /// </summary>
    public void Paste()
    {
        Execute(ApplicationCommands.Paste);
    }

    /// <summary>
    ///     Redoes the most recent undone command.
    /// </summary>
    /// <returns>True is the redo operation was successful, false is the redo stack is empty.</returns>
    public bool Redo()
    {
        if (CanExecute(ApplicationCommands.Redo))
        {
            Execute(ApplicationCommands.Redo);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Scrolls to the end of the document.
    /// </summary>
    public void ScrollToEnd()
    {
        ApplyTemplate(); // ensure scrollViewer is created
        if (ScrollViewer != null) ScrollViewer.ScrollToEnd();
    }

    /// <summary>
    ///     Scrolls to the start of the document.
    /// </summary>
    public void ScrollToHome()
    {
        ApplyTemplate(); // ensure scrollViewer is created
        if (ScrollViewer != null) ScrollViewer.ScrollToHome();
    }

    /// <summary>
    ///     Scrolls to the specified position in the document.
    /// </summary>
    public void ScrollToHorizontalOffset(double offset)
    {
        ApplyTemplate(); // ensure scrollViewer is created
        if (ScrollViewer != null) ScrollViewer.ScrollToHorizontalOffset(offset);
    }

    /// <summary>
    ///     Scrolls to the specified position in the document.
    /// </summary>
    public void ScrollToVerticalOffset(double offset)
    {
        ApplyTemplate(); // ensure scrollViewer is created
        if (ScrollViewer != null) ScrollViewer.ScrollToVerticalOffset(offset);
    }

    /// <summary>
    ///     Selects the entire text.
    /// </summary>
    public void SelectAll()
    {
        Execute(ApplicationCommands.SelectAll);
    }

    /// <summary>
    ///     Undoes the most recent command.
    /// </summary>
    /// <returns>True is the undo operation was successful, false is the undo stack is empty.</returns>
    public bool Undo()
    {
        if (CanExecute(ApplicationCommands.Undo))
        {
            Execute(ApplicationCommands.Undo);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets if the most recent undone command can be redone.
    /// </summary>
    public bool CanRedo => CanExecute(ApplicationCommands.Redo);

    /// <summary>
    ///     Gets if the most recent command can be undone.
    /// </summary>
    public bool CanUndo => CanExecute(ApplicationCommands.Undo);

    /// <summary>
    ///     Gets the vertical size of the document.
    /// </summary>
    public double ExtentHeight => ScrollViewer != null ? ScrollViewer.ExtentHeight : 0;

    /// <summary>
    ///     Gets the horizontal size of the current document region.
    /// </summary>
    public double ExtentWidth => ScrollViewer != null ? ScrollViewer.ExtentWidth : 0;

    /// <summary>
    ///     Gets the horizontal size of the viewport.
    /// </summary>
    public double ViewportHeight => ScrollViewer != null ? ScrollViewer.ViewportHeight : 0;

    /// <summary>
    ///     Gets the horizontal size of the viewport.
    /// </summary>
    public double ViewportWidth => ScrollViewer != null ? ScrollViewer.ViewportWidth : 0;

    /// <summary>
    ///     Gets the vertical scroll position.
    /// </summary>
    public double VerticalOffset => ScrollViewer != null ? ScrollViewer.VerticalOffset : 0;

    /// <summary>
    ///     Gets the horizontal scroll position.
    /// </summary>
    public double HorizontalOffset => ScrollViewer != null ? ScrollViewer.HorizontalOffset : 0;

    #endregion

    #region TextBox methods

    /// <summary>
    ///     Gets/Sets the selected text.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SelectedText
    {
        get
        {
            // We'll get the text from the whole surrounding segment.
            // This is done to ensure that SelectedText.Length == SelectionLength.
            if (TextArea.Document != null && !TextArea.Selection.IsEmpty) return TextArea.Document.GetText(TextArea.Selection.SurroundingSegment);
            return string.Empty;
        }
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (TextArea.Document != null)
            {
                var offset = SelectionStart;
                var length = SelectionLength;
                TextArea.Document.Replace(offset, length, value);
                // keep inserted text selected
                TextArea.Selection = Selection.Create(TextArea, offset, offset + value.Length);
            }
        }
    }

    /// <summary>
    ///     Gets/sets the caret position.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CaretOffset { get => TextArea.Caret.Offset; set => TextArea.Caret.Offset = value; }

    /// <summary>
    ///     Gets/sets the start position of the selection.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectionStart
    {
        get
        {
            if (TextArea.Selection.IsEmpty) return TextArea.Caret.Offset;
            return TextArea.Selection.SurroundingSegment.Offset;
        }
        set => Select(value, SelectionLength);
    }

    /// <summary>
    ///     Gets/sets the length of the selection.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectionLength
    {
        get
        {
            if (!TextArea.Selection.IsEmpty) return TextArea.Selection.SurroundingSegment.Length;
            return 0;
        }
        set => Select(SelectionStart, value);
    }

    /// <summary>
    ///     Selects the specified text section.
    /// </summary>
    public void Select(int start, int length)
    {
        var documentLength = Document != null ? Document.TextLength : 0;
        if (start < 0 || start > documentLength) throw new ArgumentOutOfRangeException(nameof(start), start, "Value must be between 0 and " + documentLength);
        if (length < 0 || start + length > documentLength) throw new ArgumentOutOfRangeException(nameof(length), length, "Value must be between 0 and " + (documentLength - start));
        TextArea.Selection    = Selection.Create(TextArea, start, start + length);
        TextArea.Caret.Offset = start + length;
    }

    /// <summary>
    ///     Gets the number of lines in the document.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LineCount
    {
        get
        {
            var document = Document;
            if (document != null) return document.LineCount;
            return 1;
        }
    }

    /// <summary>
    ///     Clears the text.
    /// </summary>
    public void Clear()
    {
        Text = string.Empty;
    }

    #endregion

    #region Loading from stream

    /// <summary>
    ///     Loads the text from the stream, auto-detecting the encoding.
    /// </summary>
    /// <remarks>
    ///     This method sets <see cref="IsModified" /> to false.
    /// </remarks>
    public void Load(Stream stream)
    {
        using (var reader = FileReader.OpenStream(stream, Encoding ?? Encoding.UTF8))
        {
            Text = reader.ReadToEnd();
            SetCurrentValue(EncodingProperty, reader.CurrentEncoding); // assign encoding after ReadToEnd() so that the StreamReader can autodetect the encoding
        }

        SetCurrentValue(IsModifiedProperty, Boxes.False);
    }

    /// <summary>
    ///     Loads the text from the stream, auto-detecting the encoding.
    /// </summary>
    public void Load(string fileName)
    {
        if (fileName == null) throw new ArgumentNullException(nameof(fileName));
        using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Load(fs);
        }
    }

    /// <summary>
    ///     Encoding dependency property.
    /// </summary>
    public static readonly DependencyProperty EncodingProperty = DependencyProperty.Register(nameof(Encoding), typeof(Encoding), typeof(TextEditor));

    /// <summary>
    ///     Gets/sets the encoding used when the file is saved.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Load(Stream)" /> method autodetects the encoding of the file and sets this property accordingly.
    ///     The <see cref="Save(Stream)" /> method uses the encoding specified in this property.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Encoding Encoding { get => (Encoding)GetValue(EncodingProperty); set => SetValue(EncodingProperty, value); }

    /// <summary>
    ///     Saves the text to the stream.
    /// </summary>
    /// <remarks>
    ///     This method sets <see cref="IsModified" /> to false.
    /// </remarks>
    public void Save(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var encoding = Encoding;
        var document = Document;
        var writer   = encoding != null ? new StreamWriter(stream, encoding) : new StreamWriter(stream);
        if (document != null) document.WriteTextTo(writer);
        writer.Flush();
        // do not close the stream
        SetCurrentValue(IsModifiedProperty, Boxes.False);
    }

    /// <summary>
    ///     Saves the text to the file.
    /// </summary>
    public void Save(string fileName)
    {
        if (fileName == null) throw new ArgumentNullException(nameof(fileName));
        using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            Save(fs);
        }
    }

    #endregion

    #region MouseHover events

    /// <summary>
    ///     The PreviewMouseHover event.
    /// </summary>
    public static readonly RoutedEvent PreviewMouseHoverEvent = TextView.PreviewMouseHoverEvent.AddOwner(typeof(TextEditor));

    /// <summary>
    ///     The MouseHover event.
    /// </summary>
    public static readonly RoutedEvent MouseHoverEvent = TextView.MouseHoverEvent.AddOwner(typeof(TextEditor));


    /// <summary>
    ///     The PreviewMouseHoverStopped event.
    /// </summary>
    public static readonly RoutedEvent PreviewMouseHoverStoppedEvent = TextView.PreviewMouseHoverStoppedEvent.AddOwner(typeof(TextEditor));

    /// <summary>
    ///     The MouseHoverStopped event.
    /// </summary>
    public static readonly RoutedEvent MouseHoverStoppedEvent = TextView.MouseHoverStoppedEvent.AddOwner(typeof(TextEditor));


    /// <summary>
    ///     Occurs when the mouse has hovered over a fixed location for some time.
    /// </summary>
    public event MouseEventHandler PreviewMouseHover { add => AddHandler(PreviewMouseHoverEvent, value); remove => RemoveHandler(PreviewMouseHoverEvent, value); }

    /// <summary>
    ///     Occurs when the mouse has hovered over a fixed location for some time.
    /// </summary>
    public event MouseEventHandler MouseHover { add => AddHandler(MouseHoverEvent, value); remove => RemoveHandler(MouseHoverEvent, value); }

    /// <summary>
    ///     Occurs when the mouse had previously hovered but now started moving again.
    /// </summary>
    public event MouseEventHandler PreviewMouseHoverStopped { add => AddHandler(PreviewMouseHoverStoppedEvent, value); remove => RemoveHandler(PreviewMouseHoverStoppedEvent, value); }

    /// <summary>
    ///     Occurs when the mouse had previously hovered but now started moving again.
    /// </summary>
    public event MouseEventHandler MouseHoverStopped { add => AddHandler(MouseHoverStoppedEvent, value); remove => RemoveHandler(MouseHoverStoppedEvent, value); }

    #endregion

    #region ScrollBarVisibility

    /// <summary>
    ///     Dependency property for <see cref="HorizontalScrollBarVisibility" />
    /// </summary>
    public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty = ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(ScrollBarVisibility.Visible));

    /// <summary>
    ///     Gets/Sets the horizontal scroll bar visibility.
    /// </summary>
    public ScrollBarVisibility HorizontalScrollBarVisibility { get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); set => SetValue(HorizontalScrollBarVisibilityProperty, value); }

    /// <summary>
    ///     Dependency property for <see cref="VerticalScrollBarVisibility" />
    /// </summary>
    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty = ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(ScrollBarVisibility.Visible));

    /// <summary>
    ///     Gets/Sets the vertical scroll bar visibility.
    /// </summary>
    public ScrollBarVisibility VerticalScrollBarVisibility { get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); set => SetValue(VerticalScrollBarVisibilityProperty, value); }

    #endregion

    object IServiceProvider.GetService(Type serviceType)
    {
        return TextArea.GetService(serviceType);
    }

    /// <summary>
    ///     Gets the text view position from a point inside the editor.
    /// </summary>
    /// <param name="point">
    ///     The position, relative to top left
    ///     corner of TextEditor control
    /// </param>
    /// <returns>The text view position, or null if the point is outside the document.</returns>
    public TextViewPosition? GetPositionFromPoint(Point point)
    {
        if (Document == null) return null;
        var textView = TextArea.TextView;
        return textView.GetPosition(TranslatePoint(point, textView) + textView.ScrollOffset);
    }

    /// <summary>
    ///     Scrolls to the specified line.
    ///     This method requires that the TextEditor was already assigned a size (WPF layout must have run prior).
    /// </summary>
    public void ScrollToLine(int line)
    {
        ScrollTo(line, -1);
    }

    /// <summary>
    ///     Scrolls to the specified line/column.
    ///     This method requires that the TextEditor was already assigned a size (WPF layout must have run prior).
    /// </summary>
    public void ScrollTo(int line, int column)
    {
        const double MinimumScrollFraction = 0.3;
        ScrollTo(line, column, VisualYPosition.LineMiddle, null != ScrollViewer ? ScrollViewer.ViewportHeight / 2 : 0.0, MinimumScrollFraction);
    }

    /// <summary>
    ///     Scrolls to the specified line/column.
    ///     This method requires that the TextEditor was already assigned a size (WPF layout must have run prior).
    /// </summary>
    /// <param name="line">Line to scroll to.</param>
    /// <param name="column">Column to scroll to (important if wrapping is 'on', and for the horizontal scroll position).</param>
    /// <param name="yPositionMode">The mode how to reference the Y position of the line.</param>
    /// <param name="referencedVerticalViewPortOffset">Offset from the top of the viewport to where the referenced line/column should be positioned.</param>
    /// <param name="minimumScrollFraction">The minimum vertical and/or horizontal scroll offset, expressed as fraction of the height or width of the viewport window, respectively.</param>
    public void ScrollTo(int line, int column, VisualYPosition yPositionMode, double referencedVerticalViewPortOffset, double minimumScrollFraction)
    {
        var textView = TextArea.TextView;
        var document = textView.Document;
        if (ScrollViewer != null && document != null)
        {
            if (line < 1) line                  = 1;
            if (line > document.LineCount) line = document.LineCount;

            IScrollInfo scrollInfo = textView;
            if (!scrollInfo.CanHorizontallyScroll)
            {
                // Word wrap is enabled. Ensure that we have up-to-date info about line height so that we scroll
                // to the correct position.
                // This avoids that the user has to repeat the ScrollTo() call several times when there are very long lines.
                var vl              = textView.GetOrConstructVisualLine(document.GetLineByNumber(line));
                var remainingHeight = referencedVerticalViewPortOffset;

                while (remainingHeight > 0)
                {
                    var prevLine = vl.FirstDocumentLine.PreviousLine;
                    if (prevLine == null) break;
                    vl              =  textView.GetOrConstructVisualLine(prevLine);
                    remainingHeight -= vl.Height;
                }
            }

            var p           = TextArea.TextView.GetVisualPosition(new TextViewPosition(line, Math.Max(1, column)), yPositionMode);
            var verticalPos = p.Y - referencedVerticalViewPortOffset;
            if (Math.Abs(verticalPos - ScrollViewer.VerticalOffset) > minimumScrollFraction * ScrollViewer.ViewportHeight) ScrollViewer.ScrollToVerticalOffset(Math.Max(0, verticalPos));
            if (column > 0)
            {
                if (p.X > ScrollViewer.ViewportWidth - Caret.MinimumDistanceToViewBorder * 2)
                {
                    var horizontalPos = Math.Max(0, p.X - ScrollViewer.ViewportWidth / 2);
                    if (Math.Abs(horizontalPos - ScrollViewer.HorizontalOffset) > minimumScrollFraction * ScrollViewer.ViewportWidth) ScrollViewer.ScrollToHorizontalOffset(horizontalPos);
                }
                else
                {
                    ScrollViewer.ScrollToHorizontalOffset(0);
                }
            }
        }
    }
}
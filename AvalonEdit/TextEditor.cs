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
using AvalonEdit.Document;
using AvalonEdit.Editing;
using AvalonEdit.Highlighting;
using AvalonEdit.Rendering;
using AvalonEdit.Utils;

namespace AvalonEdit;

[Localizability(LocalizationCategory.Text)] [ContentProperty("Text")]
public sealed class TextEditor : Control, ITextEditorComponent, IWeakEventListener
{
    #region Constructors

    static TextEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TextEditor), new FrameworkPropertyMetadata(typeof(TextEditor)));
        FocusableProperty.OverrideMetadata(typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.True));
    }

    public TextEditor() : this(new TextArea()) { }

    private TextEditor(TextArea textArea)
    {
        TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));

        textArea.TextView.Services.AddService(typeof(TextEditor), this);

        SetCurrentValue(OptionsProperty, textArea.Options);
        SetCurrentValue(DocumentProperty, new TextDocument());
    }

    #endregion

    protected override AutomationPeer OnCreateAutomationPeer() { return null; }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        if (!Equals(e.NewFocus, this)) return;
        Keyboard.Focus(TextArea);
        e.Handled = true;
    }

    #region Document property

    public static readonly DependencyProperty DocumentProperty = TextView.DocumentProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(OnDocumentChanged));

    public TextDocument Document { get => (TextDocument)GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }

    public event EventHandler DocumentChanged;

    private void OnDocumentChanged(EventArgs e) { DocumentChanged?.Invoke(this, e); }

    private static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e) { ((TextEditor)dp).OnDocumentChanged((TextDocument)e.OldValue, (TextDocument)e.NewValue); }

    private void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
    {
        if (oldValue != null) TextDocumentWeakEventManager.TextChanged.RemoveListener(oldValue, this);

        TextArea.Document = newValue;
        if (newValue != null) TextDocumentWeakEventManager.TextChanged.AddListener(newValue, this);

        OnDocumentChanged(EventArgs.Empty);
        OnTextChanged(EventArgs.Empty);
    }

    #endregion

    #region Options property

    public static readonly DependencyProperty OptionsProperty = TextView.OptionsProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(OnOptionsChanged));

    public TextEditorOptions Options { get => (TextEditorOptions)GetValue(OptionsProperty); set => SetValue(OptionsProperty, value); }

    public event PropertyChangedEventHandler OptionChanged;

    private void OnOptionChanged(PropertyChangedEventArgs e) { OptionChanged?.Invoke(this, e); }

    private static void OnOptionsChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e) { ((TextEditor)dp).OnOptionsChanged((TextEditorOptions)e.OldValue, (TextEditorOptions)e.NewValue); }

    private void OnOptionsChanged(INotifyPropertyChanged oldValue, TextEditorOptions newValue)
    {
        if (oldValue != null) PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
        TextArea.Options = newValue;
        if (newValue != null) PropertyChangedWeakEventManager.AddListener(newValue, this);
        OnOptionChanged(new PropertyChangedEventArgs(null));
    }

    private bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        if (managerType == typeof(PropertyChangedWeakEventManager))
        {
            OnOptionChanged((PropertyChangedEventArgs)e);
            return true;
        }

        if (managerType != typeof(TextDocumentWeakEventManager.TextChanged)) return managerType == typeof(PropertyChangedEventManager) && HandleIsOriginalChanged((PropertyChangedEventArgs)e);
        OnTextChanged(e);
        return true;
    }

    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e) { return ReceiveWeakEvent(managerType, sender, e); }

    #endregion

    #region Text property

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
            CaretOffset   = 0;
        }
    }

    public TextDocument GetDocument()
    {
        var document = Document;
        if (document == null) throw ThrowUtil.NoDocumentAssigned();
        return document;
    }

    public event EventHandler TextChanged;

    private void OnTextChanged(EventArgs e) { TextChanged?.Invoke(this, e); }

    #endregion

    #region TextArea / ScrollViewer properties

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ScrollViewer = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);
    }

    public TextArea TextArea { get; }

    internal ScrollViewer ScrollViewer { get; private set; }

    private void Execute(RoutedCommand command) { command.Execute(null, TextArea); }

    #endregion

    #region Syntax highlighting

    public static readonly DependencyProperty SyntaxHighlightingProperty = DependencyProperty.Register(nameof(SyntaxHighlighting), typeof(IHighlightingDefinition), typeof(TextEditor), new FrameworkPropertyMetadata(OnSyntaxHighlightingChanged));


    public IHighlightingDefinition SyntaxHighlighting { get => (IHighlightingDefinition)GetValue(SyntaxHighlightingProperty); set => SetValue(SyntaxHighlightingProperty, value); }

    private IVisualLineTransformer colorizer;

    private static void OnSyntaxHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { ((TextEditor)d).OnSyntaxHighlightingChanged(e.NewValue as IHighlightingDefinition); }

    private void OnSyntaxHighlightingChanged(IHighlightingDefinition newValue)
    {
        if (colorizer != null)
        {
            TextArea.TextView.LineTransformers.Remove(colorizer);
            colorizer = null;
        }

        if (newValue == null) return;
        colorizer = CreateColorizer(newValue);
        if (colorizer != null) TextArea.TextView.LineTransformers.Insert(0, colorizer);
    }

    private IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition)
    {
        if (highlightingDefinition == null) throw new ArgumentNullException(nameof(highlightingDefinition));
        return new HighlightingColorizer(highlightingDefinition);
    }

    #endregion

    #region WordWrap

    public static readonly DependencyProperty WordWrapProperty = DependencyProperty.Register(nameof(WordWrap), typeof(bool), typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.False));

    public bool WordWrap { get => (bool)GetValue(WordWrapProperty); set => SetValue(WordWrapProperty, Boxes.Box(value)); }

    #endregion

    #region IsReadOnly

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.False, OnIsReadOnlyChanged));

    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, Boxes.Box(value)); }

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextEditor editor) return;
        if ((bool)e.NewValue) editor.TextArea.ReadOnlySectionProvider = ReadOnlySectionDocument.Instance;
        else editor.TextArea.ReadOnlySectionProvider                  = NoReadOnlySections.Instance;
    }

    #endregion

    #region IsModified

    public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register(nameof(IsModified), typeof(bool), typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.False, OnIsModifiedChanged));

    public bool IsModified { get => (bool)GetValue(IsModifiedProperty); set => SetValue(IsModifiedProperty, Boxes.Box(value)); }

    private static void OnIsModifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextEditor editor) return;
        var document = editor.Document;
        if (document != null)
        {
        }
    }

    private bool HandleIsOriginalChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "IsOriginalFile") return false;
        var document = Document;
        return true;
    }

    #endregion

    #region ShowLineNumbers

    public static readonly DependencyProperty ShowLineNumbersProperty = DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(TextEditor), new FrameworkPropertyMetadata(Boxes.False, OnShowLineNumbersChanged));

    public bool ShowLineNumbers { get => (bool)GetValue(ShowLineNumbersProperty); set => SetValue(ShowLineNumbersProperty, Boxes.Box(value)); }

    private static void OnShowLineNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor      = (TextEditor)d;
        var leftMargins = editor.TextArea.LeftMargins;
        if ((bool)e.NewValue)
        {
        }
        else
        {
        }
    }

    #endregion

    #region LineNumbersForeground

    public static readonly DependencyProperty LineNumbersForegroundProperty = DependencyProperty.Register(nameof(LineNumbersForeground), typeof(Brush), typeof(TextEditor), new FrameworkPropertyMetadata(Brushes.Gray, OnLineNumbersForegroundChanged));

    public Brush LineNumbersForeground { get => (Brush)GetValue(LineNumbersForegroundProperty); set => SetValue(LineNumbersForegroundProperty, value); }

    private static void OnLineNumbersForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
    }

    #endregion

    #region TextBoxBase-like methods

    public void AppendText(string textData)
    {
        var document = GetDocument();
        document.Insert(document.TextLength, textData);
    }

    public void AppendColorLine(string textData, Brush LineColor)
    {
        var setlinecolor = new LineColorizer(LineCount) { LineColor = LineColor };
        TextArea.TextView.LineTransformers.Add(setlinecolor);
        AppendText(textData);
    }


    public void ScrollToEnd()
    {
        ApplyTemplate();
        ScrollViewer?.ScrollToEnd();
    }

    public void ScrollToHome()
    {
        ApplyTemplate();
        ScrollViewer?.ScrollToHome();
    }

    public void ScrollToHorizontalOffset(double offset)
    {
        ApplyTemplate();
        ScrollViewer?.ScrollToHorizontalOffset(offset);
    }

    public void ScrollToVerticalOffset(double offset)
    {
        ApplyTemplate();
        ScrollViewer?.ScrollToVerticalOffset(offset);
    }

    public void SelectAll() { Execute(ApplicationCommands.SelectAll); }

    #endregion

    #region TextBox methods

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SelectedText
    {
        get
        {
            if (TextArea.Document != null && !TextArea.Selection.IsEmpty) return TextArea.Document.GetText(TextArea.Selection.SurroundingSegment);
            return string.Empty;
        }
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (TextArea.Document == null) return;
            var offset = SelectionStart;
            var length = SelectionLength;
            TextArea.Document.Replace(offset, length, value);
            TextArea.Selection = Selection.Create(TextArea, offset, offset + value.Length);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CaretOffset { get => TextArea.Caret.Offset; set => TextArea.Caret.Offset = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectionStart { get => TextArea.Selection.IsEmpty ? TextArea.Caret.Offset : TextArea.Selection.SurroundingSegment.Offset; set => Select(value, SelectionLength); }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectionLength { get => !TextArea.Selection.IsEmpty ? TextArea.Selection.SurroundingSegment.Length : 0; set => Select(SelectionStart, value); }

    public void Select(int start, int length)
    {
        var documentLength = Document?.TextLength ?? 0;
        if (start < 0 || start > documentLength) throw new ArgumentOutOfRangeException(nameof(start), start, "Value must be between 0 and " + documentLength);
        if (length < 0 || start + length > documentLength) throw new ArgumentOutOfRangeException(nameof(length), length, "Value must be between 0 and " + (documentLength - start));
        TextArea.Selection    = Selection.Create(TextArea, start, start + length);
        TextArea.Caret.Offset = start + length;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LineCount
    {
        get
        {
            var document = Document;
            return document?.LineCount ?? 1;
        }
    }

    public void Clear() { Text = string.Empty; }

    public void ToClipSelection()
    {
        var text = SelectedText;
        Clipboard.SetText(text);
        TextArea.ClearSelection();
    }

    #endregion

    #region Loading from stream

    public static readonly DependencyProperty EncodingProperty = DependencyProperty.Register(nameof(Encoding), typeof(Encoding), typeof(TextEditor));

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Encoding Encoding { get => (Encoding)GetValue(EncodingProperty); set => SetValue(EncodingProperty, value); }

    public void Save(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var encoding = Encoding;
        var document = Document;
        var writer   = encoding != null ? new StreamWriter(stream, encoding) : new StreamWriter(stream);
        document?.WriteTextTo(writer);
        writer.Flush();
        SetCurrentValue(IsModifiedProperty, Boxes.False);
    }

    #endregion

    #region MouseHover events

    public static readonly RoutedEvent PreviewMouseHoverEvent = TextView.PreviewMouseHoverEvent.AddOwner(typeof(TextEditor));

    public static readonly RoutedEvent MouseHoverEvent = TextView.MouseHoverEvent.AddOwner(typeof(TextEditor));


    public static readonly RoutedEvent PreviewMouseHoverStoppedEvent = TextView.PreviewMouseHoverStoppedEvent.AddOwner(typeof(TextEditor));

    public static readonly RoutedEvent MouseHoverStoppedEvent = TextView.MouseHoverStoppedEvent.AddOwner(typeof(TextEditor));


    public event MouseEventHandler PreviewMouseHover { add => AddHandler(PreviewMouseHoverEvent, value); remove => RemoveHandler(PreviewMouseHoverEvent, value); }

    public event MouseEventHandler MouseHover { add => AddHandler(MouseHoverEvent, value); remove => RemoveHandler(MouseHoverEvent, value); }

    public event MouseEventHandler PreviewMouseHoverStopped { add => AddHandler(PreviewMouseHoverStoppedEvent, value); remove => RemoveHandler(PreviewMouseHoverStoppedEvent, value); }

    public event MouseEventHandler MouseHoverStopped { add => AddHandler(MouseHoverStoppedEvent, value); remove => RemoveHandler(MouseHoverStoppedEvent, value); }

    #endregion

    #region ScrollBarVisibility

    public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty = ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(ScrollBarVisibility.Visible));

    public ScrollBarVisibility HorizontalScrollBarVisibility { get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); set => SetValue(HorizontalScrollBarVisibilityProperty, value); }

    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty = ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(typeof(TextEditor), new FrameworkPropertyMetadata(ScrollBarVisibility.Visible));

    public ScrollBarVisibility VerticalScrollBarVisibility { get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); set => SetValue(VerticalScrollBarVisibilityProperty, value); }

    #endregion

    object IServiceProvider.GetService(Type serviceType) { return TextArea.GetService(serviceType); }

    public TextViewPosition? GetPositionFromPoint(Point point)
    {
        if (Document == null) return null;
        var textView = TextArea.TextView;
        return textView.GetPosition(TranslatePoint(point, textView) + textView.ScrollOffset);
    }

    public void ScrollToLine(int line) { ScrollTo(line, -1); }

    public void ScrollTo(int line, int column)
    {
        const double MinimumScrollFraction = 0.3;
        ScrollTo(line, column, VisualYPosition.LineMiddle, null != ScrollViewer ? ScrollViewer.ViewportHeight / 2 : 0.0, MinimumScrollFraction);
    }

    public void ScrollTo(int line, int column, VisualYPosition yPositionMode, double referencedVerticalViewPortOffset, double minimumScrollFraction)
    {
        var textView = TextArea.TextView;
        var document = textView.Document;
        if (ScrollViewer == null || document == null) return;
        if (line < 1) line                  = 1;
        if (line > document.LineCount) line = document.LineCount;

        IScrollInfo scrollInfo = textView;
        if (!scrollInfo.CanHorizontallyScroll)
        {
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
        if (column <= 0) return;
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
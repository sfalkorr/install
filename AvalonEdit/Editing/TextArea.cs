using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using AvalonEdit.Document;
using AvalonEdit.Rendering;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

public class TextArea : Control, IScrollInfo, IWeakEventListener, ITextEditorComponent
{
    #region Constructor

    static TextArea()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TextArea), new FrameworkPropertyMetadata(typeof(TextArea)));
        KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(TextArea), new FrameworkPropertyMetadata(Boxes.True));
        KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TextArea), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
        FocusableProperty.OverrideMetadata(typeof(TextArea), new FrameworkPropertyMetadata(Boxes.True));
    }

    public TextArea() : this(new TextView())
    {
    }

    protected TextArea(TextView textView)
    {
        TextView = textView ?? throw new ArgumentNullException(nameof(textView));
        Options  = textView.Options;

        selection = emptySelection = new EmptySelection(this);

        textView.Services.AddService(typeof(TextArea), this);

        textView.LineTransformers.Add(new SelectionColorizer(this));
        textView.InsertLayer(new SelectionLayer(this), KnownLayer.Selection, LayerInsertionPosition.Replace);

        Caret                 =  new Caret(this);
        Caret.PositionChanged += (sender, e) => RequestSelectionValidation();
        Caret.PositionChanged += CaretPositionChanged;
        AttachTypingEvents();

        LeftMargins.CollectionChanged += leftMargins_CollectionChanged;

        DefaultInputHandler = new TextAreaDefaultInputHandler(this);
        ActiveInputHandler  = DefaultInputHandler;
    }

    #endregion

    #region InputHandler management

    public TextAreaDefaultInputHandler DefaultInputHandler { get; }

    private ITextAreaInputHandler activeInputHandler;
    private bool                  isChangingInputHandler;

    public ITextAreaInputHandler ActiveInputHandler
    {
        get => activeInputHandler;
        set
        {
            if (value != null && value.TextArea != this) throw new ArgumentException("The input handler was created for a different text area than this one.");
            if (isChangingInputHandler) throw new InvalidOperationException("Cannot set ActiveInputHandler recursively");
            if (activeInputHandler == value) return;
            isChangingInputHandler = true;
            try
            {
                PopStackedInputHandler(StackedInputHandlers.LastOrDefault());
                Debug.Assert(StackedInputHandlers.IsEmpty);

                activeInputHandler?.Detach();
                activeInputHandler = value;
                value?.Attach();
            }
            finally
            {
                isChangingInputHandler = false;
            }

            ActiveInputHandlerChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler ActiveInputHandlerChanged;

    public ImmutableStack<TextAreaStackedInputHandler> StackedInputHandlers { get; private set; } = ImmutableStack<TextAreaStackedInputHandler>.Empty;

    public void PushStackedInputHandler(TextAreaStackedInputHandler inputHandler)
    {
        if (inputHandler == null) throw new ArgumentNullException(nameof(inputHandler));
        StackedInputHandlers = StackedInputHandlers.Push(inputHandler);
        inputHandler.Attach();
    }

    public void PopStackedInputHandler(TextAreaStackedInputHandler inputHandler)
    {
        if (StackedInputHandlers.Any(i => i == inputHandler))
        {
            ITextAreaInputHandler oldHandler;
            do
            {
                oldHandler           = StackedInputHandlers.Peek();
                StackedInputHandlers = StackedInputHandlers.Pop();
                oldHandler.Detach();
            } while (oldHandler != inputHandler);
        }
    }

    #endregion

    #region Document property

    public static readonly DependencyProperty DocumentProperty = TextView.DocumentProperty.AddOwner(typeof(TextArea), new FrameworkPropertyMetadata(OnDocumentChanged));

    public TextDocument Document
    {
        get => (TextDocument)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public event EventHandler DocumentChanged;

    private static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        ((TextArea)dp).OnDocumentChanged((TextDocument)e.OldValue, (TextDocument)e.NewValue);
    }

    private void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
    {
        if (oldValue != null)
        {
            TextDocumentWeakEventManager.Changing.RemoveListener(oldValue, this);
            TextDocumentWeakEventManager.Changed.RemoveListener(oldValue, this);
            TextDocumentWeakEventManager.UpdateStarted.RemoveListener(oldValue, this);
            TextDocumentWeakEventManager.UpdateFinished.RemoveListener(oldValue, this);
        }

        TextView.Document = newValue;
        if (newValue != null)
        {
            TextDocumentWeakEventManager.Changing.AddListener(newValue, this);
            TextDocumentWeakEventManager.Changed.AddListener(newValue, this);
            TextDocumentWeakEventManager.UpdateStarted.AddListener(newValue, this);
            TextDocumentWeakEventManager.UpdateFinished.AddListener(newValue, this);
        }

        Caret.Location = new TextLocation(1, 1);
        ClearSelection();
        if (DocumentChanged != null) DocumentChanged(this, EventArgs.Empty);
        CommandManager.InvalidateRequerySuggested();
    }

    #endregion

    #region Options property

    public static readonly DependencyProperty OptionsProperty = TextView.OptionsProperty.AddOwner(typeof(TextArea), new FrameworkPropertyMetadata(OnOptionsChanged));

    public TextEditorOptions Options
    {
        get => (TextEditorOptions)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public event PropertyChangedEventHandler OptionChanged;

    protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
    {
        if (OptionChanged != null) OptionChanged(this, e);
    }

    private static void OnOptionsChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        ((TextArea)dp).OnOptionsChanged((TextEditorOptions)e.OldValue, (TextEditorOptions)e.NewValue);
    }

    private void OnOptionsChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
    {
        if (oldValue != null) PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
        TextView.Options = newValue;
        if (newValue != null) PropertyChangedWeakEventManager.AddListener(newValue, this);
        OnOptionChanged(new PropertyChangedEventArgs(null));
    }

    #endregion

    #region ReceiveWeakEvent

    protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        if (managerType == typeof(TextDocumentWeakEventManager.Changing))
        {
            OnDocumentChanging();
            return true;
        }

        if (managerType == typeof(TextDocumentWeakEventManager.Changed))
        {
            OnDocumentChanged((DocumentChangeEventArgs)e);
            return true;
        }

        if (managerType == typeof(TextDocumentWeakEventManager.UpdateStarted))
        {
            OnUpdateStarted();
            return true;
        }

        if (managerType == typeof(TextDocumentWeakEventManager.UpdateFinished))
        {
            OnUpdateFinished();
            return true;
        }

        if (managerType == typeof(PropertyChangedWeakEventManager))
        {
            OnOptionChanged((PropertyChangedEventArgs)e);
            return true;
        }

        return false;
    }

    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        return ReceiveWeakEvent(managerType, sender, e);
    }

    #endregion

    #region Caret handling on document changes

    private void OnDocumentChanging()
    {
        Caret.OnDocumentChanging();
    }

    private void OnDocumentChanged(DocumentChangeEventArgs e)
    {
        Caret.OnDocumentChanged(e);
        Selection = selection.UpdateOnDocumentChange(e);
    }

    private void OnUpdateStarted()
    {
    }

    private void OnUpdateFinished()
    {
        Caret.OnDocumentUpdateFinished();
    }

    #endregion

    #region TextView property

    private IScrollInfo scrollInfo;

    public TextView TextView { get; }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        scrollInfo = TextView;
        ApplyScrollInfo();
    }

    #endregion

    #region Selection property

    internal readonly Selection emptySelection;
    private           Selection selection;

    public event EventHandler SelectionChanged;

    public Selection Selection
    {
        get => selection;
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.textArea != this) throw new ArgumentException("Cannot use a Selection instance that belongs to another text area.");
            if (Equals(selection, value)) return;
            if (TextView != null)
            {
                var oldSegment = selection.SurroundingSegment;
                var newSegment = value.SurroundingSegment;
                if (!Selection.EnableVirtualSpace && selection is SimpleSelection && value is SimpleSelection && oldSegment != null && newSegment != null)
                {
                    var oldSegmentOffset = oldSegment.Offset;
                    var newSegmentOffset = newSegment.Offset;
                    if (oldSegmentOffset != newSegmentOffset) TextView.Redraw(Math.Min(oldSegmentOffset, newSegmentOffset), Math.Abs(oldSegmentOffset - newSegmentOffset), DispatcherPriority.Render);
                    var oldSegmentEndOffset = oldSegment.EndOffset;
                    var newSegmentEndOffset = newSegment.EndOffset;
                    if (oldSegmentEndOffset != newSegmentEndOffset) TextView.Redraw(Math.Min(oldSegmentEndOffset, newSegmentEndOffset), Math.Abs(oldSegmentEndOffset - newSegmentEndOffset), DispatcherPriority.Render);
                }
                else
                {
                    TextView.Redraw(oldSegment, DispatcherPriority.Render);
                    TextView.Redraw(newSegment, DispatcherPriority.Render);
                }
            }

            selection = value;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public void ClearSelection()
    {
        Selection = emptySelection;
    }

    public static readonly DependencyProperty SelectionBrushProperty = DependencyProperty.Register(nameof(SelectionBrush), typeof(Brush), typeof(TextArea));

    public Brush SelectionBrush
    {
        get => (Brush)GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public static readonly DependencyProperty SelectionForegroundProperty = DependencyProperty.Register(nameof(SelectionForeground), typeof(Brush), typeof(TextArea));

    public Brush SelectionForeground
    {
        get => (Brush)GetValue(SelectionForegroundProperty);
        set => SetValue(SelectionForegroundProperty, value);
    }

    public static readonly DependencyProperty SelectionBorderProperty = DependencyProperty.Register(nameof(SelectionBorder), typeof(Pen), typeof(TextArea));

    public Pen SelectionBorder
    {
        get => (Pen)GetValue(SelectionBorderProperty);
        set => SetValue(SelectionBorderProperty, value);
    }

    public static readonly DependencyProperty SelectionCornerRadiusProperty = DependencyProperty.Register(nameof(SelectionCornerRadius), typeof(double), typeof(TextArea), new FrameworkPropertyMetadata(3.0));

    public double SelectionCornerRadius
    {
        get => (double)GetValue(SelectionCornerRadiusProperty);
        set => SetValue(SelectionCornerRadiusProperty, value);
    }

    [DefaultValue(MouseSelectionMode.WholeWord)]
    public MouseSelectionMode MouseSelectionMode
    {
        get
        {
            if (DefaultInputHandler.MouseSelection is SelectionMouseHandler mouseHandler) return mouseHandler.MouseSelectionMode;
            return MouseSelectionMode.None;
        }
        set
        {
            if (DefaultInputHandler.MouseSelection is SelectionMouseHandler mouseHandler) mouseHandler.MouseSelectionMode = value;
        }
    }

    #endregion

    #region Force caret to stay inside selection

    private bool ensureSelectionValidRequested;
    private int  allowCaretOutsideSelection;

    private void RequestSelectionValidation()
    {
        if (!ensureSelectionValidRequested && allowCaretOutsideSelection == 0)
        {
            ensureSelectionValidRequested = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(EnsureSelectionValid));
        }
    }

    private void EnsureSelectionValid()
    {
        ensureSelectionValidRequested = false;
        if (allowCaretOutsideSelection == 0)
            if (!selection.IsEmpty && !selection.Contains(Caret.Offset))
            {
                Debug.WriteLine("Resetting selection because caret is outside");
                ClearSelection();
            }
    }

    public IDisposable AllowCaretOutsideSelection()
    {
        VerifyAccess();
        allowCaretOutsideSelection++;
        return new CallbackOnDispose(delegate
        {
            VerifyAccess();
            allowCaretOutsideSelection--;
            RequestSelectionValidation();
        });
    }

    #endregion

    #region Properties

    public Caret Caret { get; }

    private void CaretPositionChanged(object sender, EventArgs e)
    {
        if (TextView == null) return;

        TextView.HighlightedLine = Caret.Line;
    }

    public ObservableCollection<UIElement> LeftMargins { get; } = new();

    private void leftMargins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (var c in e.OldItems.OfType<ITextViewConnect>())
                c.RemoveFromTextView(TextView);

        if (e.NewItems != null)
            foreach (var c in e.NewItems.OfType<ITextViewConnect>())
                c.AddToTextView(TextView);
    }

    private IReadOnlySectionProvider readOnlySectionProvider = NoReadOnlySections.Instance;

    public IReadOnlySectionProvider ReadOnlySectionProvider
    {
        get => readOnlySectionProvider;
        set
        {
            readOnlySectionProvider = value ?? throw new ArgumentNullException(nameof(value));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    #endregion

    #region IScrollInfo implementation

    private ScrollViewer scrollOwner;
    private bool         canVerticallyScroll, canHorizontallyScroll;

    private void ApplyScrollInfo()
    {
        if (scrollInfo != null)
        {
            scrollInfo.ScrollOwner           = scrollOwner;
            scrollInfo.CanVerticallyScroll   = canVerticallyScroll;
            scrollInfo.CanHorizontallyScroll = canHorizontallyScroll;
            scrollOwner                      = null;
        }
    }

    bool IScrollInfo.CanVerticallyScroll
    {
        get => scrollInfo is { CanVerticallyScroll: true };
        set
        {
            canVerticallyScroll = value;
            if (scrollInfo != null) scrollInfo.CanVerticallyScroll = value;
        }
    }

    bool IScrollInfo.CanHorizontallyScroll
    {
        get => scrollInfo is { CanHorizontallyScroll: true };
        set
        {
            canHorizontallyScroll = value;
            if (scrollInfo != null) scrollInfo.CanHorizontallyScroll = value;
        }
    }

    double IScrollInfo.ExtentWidth => scrollInfo?.ExtentWidth ?? 0;

    double IScrollInfo.ExtentHeight => scrollInfo?.ExtentHeight ?? 0;

    double IScrollInfo.ViewportWidth => scrollInfo?.ViewportWidth ?? 0;

    double IScrollInfo.ViewportHeight => scrollInfo?.ViewportHeight ?? 0;

    double IScrollInfo.HorizontalOffset => scrollInfo?.HorizontalOffset ?? 0;

    double IScrollInfo.VerticalOffset => scrollInfo?.VerticalOffset ?? 0;

    ScrollViewer IScrollInfo.ScrollOwner
    {
        get => scrollInfo?.ScrollOwner;
        set
        {
            if (scrollInfo != null) scrollInfo.ScrollOwner = value;
            else scrollOwner                               = value;
        }
    }

    void IScrollInfo.LineUp()
    {
        if (scrollInfo != null) scrollInfo.LineUp();
    }

    void IScrollInfo.LineDown()
    {
        if (scrollInfo != null) scrollInfo.LineDown();
    }

    void IScrollInfo.LineLeft()
    {
        if (scrollInfo != null) scrollInfo.LineLeft();
    }

    void IScrollInfo.LineRight()
    {
        if (scrollInfo != null) scrollInfo.LineRight();
    }

    void IScrollInfo.PageUp()
    {
        if (scrollInfo != null) scrollInfo.PageUp();
    }

    void IScrollInfo.PageDown()
    {
        if (scrollInfo != null) scrollInfo.PageDown();
    }

    void IScrollInfo.PageLeft()
    {
        if (scrollInfo != null) scrollInfo.PageLeft();
    }

    void IScrollInfo.PageRight()
    {
        if (scrollInfo != null) scrollInfo.PageRight();
    }

    void IScrollInfo.MouseWheelUp()
    {
        if (scrollInfo != null) scrollInfo.MouseWheelUp();
    }

    void IScrollInfo.MouseWheelDown()
    {
        if (scrollInfo != null) scrollInfo.MouseWheelDown();
    }

    void IScrollInfo.MouseWheelLeft()
    {
        if (scrollInfo != null) scrollInfo.MouseWheelLeft();
    }

    void IScrollInfo.MouseWheelRight()
    {
        if (scrollInfo != null) scrollInfo.MouseWheelRight();
    }

    void IScrollInfo.SetHorizontalOffset(double offset)
    {
        if (scrollInfo != null) scrollInfo.SetHorizontalOffset(offset);
    }

    void IScrollInfo.SetVerticalOffset(double offset)
    {
        if (scrollInfo != null) scrollInfo.SetVerticalOffset(offset);
    }

    Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle)
    {
        if (scrollInfo != null) return scrollInfo.MakeVisible(visual, rectangle);
        return Rect.Empty;
    }

    #endregion

    #region Focus Handling (Show/Hide Caret)

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        Caret.Show();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        Caret.Hide();
    }

    #endregion

    #region OnTextInput / RemoveSelectedText / ReplaceSelectionWithText

    public event TextCompositionEventHandler TextEntering;

    public event TextCompositionEventHandler TextEntered;

    protected virtual void OnTextEntering(TextCompositionEventArgs e)
    {
        if (TextEntering != null) TextEntering(this, e);
    }

    protected virtual void OnTextEntered(TextCompositionEventArgs e)
    {
        TextEntered?.Invoke(this, e);
    }

    protected override void OnTextInput(TextCompositionEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Handled || Document == null) return;
        if (string.IsNullOrEmpty(e.Text) || e.Text is "\x1b" or "\b") return;
        HideMouseCursor();
        PerformTextInput(e);
        e.Handled = true;
    }

    public void PerformTextInput(TextCompositionEventArgs e)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));
        if (Document == null) throw ThrowUtil.NoDocumentAssigned();
        OnTextEntering(e);
        if (e.Handled) return;
        if (e.Text is "\n" or "\r" or "\r\n")
        {
            ReplaceSelectionWithNewLine();
        }
        else
        {
            if (OverstrikeMode && Selection.IsEmpty && Document.GetLineByNumber(Caret.Line).EndOffset > Caret.Offset) EditingCommands.SelectRightByCharacter.Execute(null, this);
            ReplaceSelectionWithText(e.Text);
        }

        OnTextEntered(e);
        Caret.BringCaretToView();
    }

    private void ReplaceSelectionWithNewLine()
    {
        var newLine = TextUtilities.GetNewLineFromDocument(Document, Caret.Line);
    }

    internal void RemoveSelectedText()
    {
        if (Document == null) throw ThrowUtil.NoDocumentAssigned();
        selection.ReplaceSelectionWithText(string.Empty);
#if DEBUG
        if (selection.IsEmpty) return;
        foreach (var s in selection.Segments)
            Debug.Assert(!ReadOnlySectionProvider.GetDeletableSegments(s).Any());
#endif
    }

    internal void ReplaceSelectionWithText(string newText)
    {
        if (newText == null) throw new ArgumentNullException(nameof(newText));
        if (Document == null) throw ThrowUtil.NoDocumentAssigned();
        selection.ReplaceSelectionWithText(newText);
    }

    internal ISegment[] GetDeletableSegments(ISegment segment)
    {
        var deletableSegments = ReadOnlySectionProvider.GetDeletableSegments(segment);
        if (deletableSegments == null) throw new InvalidOperationException("ReadOnlySectionProvider.GetDeletableSegments returned null");
        var array     = deletableSegments.ToArray();
        var lastIndex = segment.Offset;
        foreach (var t in array)
        {
            if (t.Offset < lastIndex) throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
            lastIndex = t.EndOffset;
        }

        if (lastIndex > segment.EndOffset) throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
        return array;
    }

    #endregion

    #region IndentationStrategy property

    #endregion

    #region OnKeyDown/OnKeyUp

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        foreach (var h in StackedInputHandlers)
        {
            if (e.Handled) break;
            h.OnPreviewKeyDown(e);
        }
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        base.OnPreviewKeyUp(e);
        foreach (var h in StackedInputHandlers)
        {
            if (e.Handled) break;
            h.OnPreviewKeyUp(e);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        TextView.InvalidateCursorIfMouseWithinTextView();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        TextView.InvalidateCursorIfMouseWithinTextView();
    }

    #endregion

    #region Hide Mouse Cursor While Typing

    private bool isMouseCursorHidden;

    private void AttachTypingEvents()
    {
        MouseEnter       += delegate { ShowMouseCursor(); };
        MouseLeave       += delegate { ShowMouseCursor(); };
        PreviewMouseMove += delegate { ShowMouseCursor(); };
        TouchEnter       += delegate { ShowMouseCursor(); };
        TouchLeave       += delegate { ShowMouseCursor(); };
        PreviewTouchMove += delegate { ShowMouseCursor(); };
    }

    private void ShowMouseCursor()
    {
        if (!isMouseCursorHidden) return;
        System.Windows.Forms.Cursor.Show();
        isMouseCursorHidden = false;
    }

    private void HideMouseCursor()
    {
        if (!Options.HideCursorWhileTyping || isMouseCursorHidden || !IsMouseOver) return;
        isMouseCursorHidden = true;
        System.Windows.Forms.Cursor.Hide();
    }

    #endregion

    #region Overstrike mode

    public static readonly DependencyProperty OverstrikeModeProperty = DependencyProperty.Register(nameof(OverstrikeMode), typeof(bool), typeof(TextArea), new FrameworkPropertyMetadata(Boxes.False));

    [DefaultValue(false)]
    public bool OverstrikeMode
    {
        get => (bool)GetValue(OverstrikeModeProperty);
        set => SetValue(OverstrikeModeProperty, value);
    }

    #endregion

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TextAreaAutomationPeer(this);
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == SelectionBrushProperty || e.Property == SelectionBorderProperty || e.Property == SelectionForegroundProperty || e.Property == SelectionCornerRadiusProperty) TextView.Redraw();
        else if (e.Property == OverstrikeModeProperty) Caret.UpdateIfVisible();
    }

    public virtual object GetService(Type serviceType)
    {
        return TextView.GetService(serviceType);
    }

    public event EventHandler<TextEventArgs> TextCopied;

    internal void OnTextCopied(TextEventArgs e)
    {
        TextCopied?.Invoke(this, e);
    }
}

[Serializable]
public class TextEventArgs : EventArgs
{
    public string Text { get; }

    public TextEventArgs(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}
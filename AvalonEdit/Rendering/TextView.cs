using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using AvalonEdit.Document;
using AvalonEdit.Editing;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

[SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "The user usually doesn't work with TextView but with TextEditor; and nulling the Document property is sufficient to dispose everything.")]
public class TextView : FrameworkElement, IScrollInfo, IWeakEventListener, ITextEditorComponent
{
    #region Constructor

    static TextView()
    {
        ClipToBoundsProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(Boxes.True));
        FocusableProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(Boxes.False));
    }

    private ColumnRulerRenderer          columnRulerRenderer;
    private CurrentLineHighlightRenderer currentLineHighlighRenderer;

    public TextView()
    {
        Services.AddService(typeof(TextView), this);
        textLayer                   = new TextLayer(this);
        elementGenerators           = new ObserveAddRemoveCollection<VisualLineElementGenerator>(ElementGenerator_Added, ElementGenerator_Removed);
        lineTransformers            = new ObserveAddRemoveCollection<IVisualLineTransformer>(LineTransformer_Added, LineTransformer_Removed);
        backgroundRenderers         = new ObserveAddRemoveCollection<IBackgroundRenderer>(BackgroundRenderer_Added, BackgroundRenderer_Removed);
        columnRulerRenderer         = new ColumnRulerRenderer(this);
        currentLineHighlighRenderer = new CurrentLineHighlightRenderer(this);
        Options                     = new TextEditorOptions();

        Debug.Assert(singleCharacterElementGenerator != null);

        layers = new LayerCollection(this);
        InsertLayer(textLayer, KnownLayer.Text, LayerInsertionPosition.Replace);

        var hoverLogic = new MouseHoverLogic(this);
        hoverLogic.MouseHover        += (_, e) => RaiseHoverEventPair(e, PreviewMouseHoverEvent, MouseHoverEvent);
        hoverLogic.MouseHoverStopped += (_, e) => RaiseHoverEventPair(e, PreviewMouseHoverStoppedEvent, MouseHoverStoppedEvent);
    }

    #endregion

    #region Document Property

    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(Document), typeof(TextDocument), typeof(TextView), new FrameworkPropertyMetadata(OnDocumentChanged));

    private TextDocument document;
    private HeightTree   heightTree;

    public TextDocument Document { get => (TextDocument)GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }

    private static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        ((TextView)dp).OnDocumentChanged((TextDocument)e.OldValue, (TextDocument)e.NewValue);
    }

    internal double FontSize => (double)GetValue(TextBlock.FontSizeProperty);

    public event EventHandler DocumentChanged;

    private void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
    {
        if (oldValue != null)
        {
            heightTree.Dispose();
            heightTree = null;
            formatter.Dispose();
            formatter = null;
            cachedElements.Dispose();
            cachedElements = null;
            TextDocumentWeakEventManager.Changing.RemoveListener(oldValue, this);
        }

        document = newValue;
        ClearScrollData();
        ClearVisualLines();
        if (newValue != null)
        {
            TextDocumentWeakEventManager.Changing.AddListener(newValue, this);
            formatter = TextFormatterFactory.Create(this);
            InvalidateDefaultTextMetrics();
            heightTree     = new HeightTree(newValue, DefaultLineHeight);
            cachedElements = new TextViewCachedElements();
        }

        InvalidateMeasure(DispatcherPriority.Normal);
        DocumentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RecreateTextFormatter()
    {
        if (formatter == null) return;
        formatter.Dispose();
        formatter = TextFormatterFactory.Create(this);
        Redraw();
    }

    private void RecreateCachedElements()
    {
        if (cachedElements == null) return;
        cachedElements.Dispose();
        cachedElements = new TextViewCachedElements();
    }

    protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        if (managerType == typeof(TextDocumentWeakEventManager.Changing))
        {
            var change = (DocumentChangeEventArgs)e;
            Redraw(change.Offset, change.RemovalLength);
            return true;
        }

        if (managerType != typeof(PropertyChangedWeakEventManager)) return false;
        OnOptionChanged((PropertyChangedEventArgs)e);
        return true;
    }

    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        return ReceiveWeakEvent(managerType, sender, e);
    }

    #endregion

    #region Options property

    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(TextEditorOptions), typeof(TextView), new FrameworkPropertyMetadata(OnOptionsChanged));

    public TextEditorOptions Options { get => (TextEditorOptions)GetValue(OptionsProperty); set => SetValue(OptionsProperty, value); }

    public event PropertyChangedEventHandler OptionChanged;

    protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
    {
        OptionChanged?.Invoke(this, e);

        if (Options.ShowColumnRuler) columnRulerRenderer.SetRuler(Options.ColumnRulerPosition, ColumnRulerPen);
        else columnRulerRenderer.SetRuler(-1, ColumnRulerPen);

        UpdateBuiltinElementGeneratorsFromOptions();
        Redraw();
    }

    private static void OnOptionsChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        ((TextView)dp).OnOptionsChanged((TextEditorOptions)e.OldValue, (TextEditorOptions)e.NewValue);
    }

    private void OnOptionsChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
    {
        if (oldValue != null) PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
        if (newValue != null) PropertyChangedWeakEventManager.AddListener(newValue, this);
        OnOptionChanged(new PropertyChangedEventArgs(null));
    }

    #endregion

    #region ElementGenerators+LineTransformers Properties

    private readonly ObserveAddRemoveCollection<VisualLineElementGenerator> elementGenerators;

    public IList<VisualLineElementGenerator> ElementGenerators => elementGenerators;

    private void ElementGenerator_Added(VisualLineElementGenerator generator)
    {
        ConnectToTextView(generator);
        Redraw();
    }

    private void ElementGenerator_Removed(VisualLineElementGenerator generator)
    {
        DisconnectFromTextView(generator);
        Redraw();
    }

    private readonly ObserveAddRemoveCollection<IVisualLineTransformer> lineTransformers;

    public IList<IVisualLineTransformer> LineTransformers => lineTransformers;

    private void LineTransformer_Added(IVisualLineTransformer lineTransformer)
    {
        ConnectToTextView(lineTransformer);
        Redraw();
    }

    private void LineTransformer_Removed(IVisualLineTransformer lineTransformer)
    {
        DisconnectFromTextView(lineTransformer);
        Redraw();
    }

    #endregion

    #region Builtin ElementGenerators

    private SingleCharacterElementGenerator singleCharacterElementGenerator;


    private void UpdateBuiltinElementGeneratorsFromOptions()
    {
        var options = Options;

        AddRemoveDefaultElementGeneratorOnDemand(ref singleCharacterElementGenerator, options.ShowBoxForControlCharacters || options.ShowSpaces || options.ShowTabs);
    }

    private void AddRemoveDefaultElementGeneratorOnDemand<T>(ref T generator, bool demand) where T : VisualLineElementGenerator, IBuiltinElementGenerator, new()
    {
        var hasGenerator = generator != null;
        if (hasGenerator != demand)
        {
            if (demand)
            {
                generator = new T();
                ElementGenerators.Add(generator);
            }
            else
            {
                ElementGenerators.Remove(generator);
                generator = null;
            }
        }

        generator?.FetchOptions(Options);
    }

    #endregion

    #region Layers

    internal readonly TextLayer       textLayer;
    private readonly  LayerCollection layers;

    public UIElementCollection Layers => layers;

    private sealed class LayerCollection : UIElementCollection
    {
        private readonly TextView textView;

        public LayerCollection(TextView textView) : base(textView, textView)
        {
            this.textView = textView;
        }

        public override void Clear()
        {
            base.Clear();
            textView.LayersChanged();
        }

        public override int Add(UIElement element)
        {
            var r = base.Add(element);
            textView.LayersChanged();
            return r;
        }

        public override void RemoveAt(int index)
        {
            base.RemoveAt(index);
            textView.LayersChanged();
        }

        public override void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
            textView.LayersChanged();
        }
    }

    private void LayersChanged()
    {
        textLayer.index = layers.IndexOf(textLayer);
    }

    public void InsertLayer(UIElement layer, KnownLayer referencedLayer, LayerInsertionPosition position)
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        if (!Enum.IsDefined(typeof(KnownLayer), referencedLayer)) throw new InvalidEnumArgumentException(nameof(referencedLayer), (int)referencedLayer, typeof(KnownLayer));
        if (!Enum.IsDefined(typeof(LayerInsertionPosition), position)) throw new InvalidEnumArgumentException(nameof(position), (int)position, typeof(LayerInsertionPosition));
        if (referencedLayer == KnownLayer.Background && position != LayerInsertionPosition.Above) throw new InvalidOperationException("Cannot replace or insert below the background layer.");

        var newPosition = new LayerPosition(referencedLayer, position);
        LayerPosition.SetLayerPosition(layer, newPosition);
        for (var i = 0; i < layers.Count; i++)
        {
            var p = LayerPosition.GetLayerPosition(layers[i]);
            if (p != null)
            {
                if (p.KnownLayer == referencedLayer && p.Position == LayerInsertionPosition.Replace)
                {
                    switch (position)
                    {
                        case LayerInsertionPosition.Below:
                            layers.Insert(i, layer);
                            return;
                        case LayerInsertionPosition.Above:
                            layers.Insert(i + 1, layer);
                            return;
                        case LayerInsertionPosition.Replace:
                            layers[i] = layer;
                            return;
                    }
                }
                else if ((p.KnownLayer == referencedLayer && p.Position == LayerInsertionPosition.Above) || p.KnownLayer > referencedLayer)
                {
                    layers.Insert(i, layer);
                    return;
                }
            }
        }

        layers.Add(layer);
    }

    protected override int VisualChildrenCount => layers.Count + inlineObjects.Count;

    protected override Visual GetVisualChild(int index)
    {
        var cut = textLayer.index + 1;
        if (index < cut) return layers[index];
        return index < cut + inlineObjects.Count ? inlineObjects[index - cut].Element : layers[index - inlineObjects.Count];
    }

    protected override IEnumerator LogicalChildren { get { return inlineObjects.Select(io => io.Element).Concat(layers.Cast<UIElement>()).GetEnumerator(); } }

    #endregion

    #region Inline object handling

    private List<InlineObjectRun> inlineObjects = new();

    internal void AddInlineObject(InlineObjectRun inlineObject)
    {
        Debug.Assert(inlineObject.VisualLine != null);

        var alreadyAdded = false;
        for (var i = 0; i < inlineObjects.Count; i++)
            if (inlineObjects[i].Element == inlineObject.Element)
            {
                RemoveInlineObjectRun(inlineObjects[i], true);
                inlineObjects.RemoveAt(i);
                alreadyAdded = true;
                break;
            }

        inlineObjects.Add(inlineObject);
        if (!alreadyAdded) AddVisualChild(inlineObject.Element);
        inlineObject.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        inlineObject.desiredSize = inlineObject.Element.DesiredSize;
    }

    private void MeasureInlineObjects()
    {
        foreach (var inlineObject in inlineObjects.Where(inlineObject => !inlineObject.VisualLine.IsDisposed))
        {
            inlineObject.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (inlineObject.Element.DesiredSize.IsClose(inlineObject.desiredSize)) continue;
            inlineObject.desiredSize = inlineObject.Element.DesiredSize;
            if (allVisualLines.Remove(inlineObject.VisualLine)) DisposeVisualLine(inlineObject.VisualLine);
        }
    }

    private List<VisualLine> visualLinesWithOutstandingInlineObjects = new();

    private void RemoveInlineObjects(VisualLine visualLine)
    {
        if (visualLine.hasInlineObjects) visualLinesWithOutstandingInlineObjects.Add(visualLine);
    }

    private void RemoveInlineObjectsNow()
    {
        if (visualLinesWithOutstandingInlineObjects.Count == 0) return;
        inlineObjects.RemoveAll(ior =>
        {
            if (!visualLinesWithOutstandingInlineObjects.Contains(ior.VisualLine)) return false;
            RemoveInlineObjectRun(ior, false);
            return true;
        });
        visualLinesWithOutstandingInlineObjects.Clear();
    }

    private void RemoveInlineObjectRun(InlineObjectRun ior, bool keepElement)
    {
        if (!keepElement && ior.Element.IsKeyboardFocusWithin)
        {
            UIElement element                               = this;
            while (element is { Focusable: false }) element = VisualTreeHelper.GetParent(element) as UIElement;
            if (element != null) Keyboard.Focus(element);
        }

        ior.VisualLine = null;
        if (!keepElement) RemoveVisualChild(ior.Element);
    }

    #endregion

    #region Brushes

    public static readonly DependencyProperty NonPrintableCharacterBrushProperty = DependencyProperty.Register(nameof(NonPrintableCharacterBrush), typeof(Brush), typeof(TextView), new FrameworkPropertyMetadata(Brushes.LightGray));

    public Brush NonPrintableCharacterBrush { get => (Brush)GetValue(NonPrintableCharacterBrushProperty); set => SetValue(NonPrintableCharacterBrushProperty, value); }

    public static readonly DependencyProperty LinkTextForegroundBrushProperty = DependencyProperty.Register(nameof(LinkTextForegroundBrush), typeof(Brush), typeof(TextView), new FrameworkPropertyMetadata(Brushes.Blue));

    public Brush LinkTextForegroundBrush { get => (Brush)GetValue(LinkTextForegroundBrushProperty); set => SetValue(LinkTextForegroundBrushProperty, value); }

    public static readonly DependencyProperty LinkTextBackgroundBrushProperty = DependencyProperty.Register(nameof(LinkTextBackgroundBrush), typeof(Brush), typeof(TextView), new FrameworkPropertyMetadata(Brushes.Transparent));

    public Brush LinkTextBackgroundBrush { get => (Brush)GetValue(LinkTextBackgroundBrushProperty); set => SetValue(LinkTextBackgroundBrushProperty, value); }

    #endregion

    public static readonly DependencyProperty LinkTextUnderlineProperty = DependencyProperty.Register(nameof(LinkTextUnderline), typeof(bool), typeof(TextView), new FrameworkPropertyMetadata(true));

    public bool LinkTextUnderline { get => (bool)GetValue(LinkTextUnderlineProperty); set => SetValue(LinkTextUnderlineProperty, value); }

    #region Redraw methods / VisualLine invalidation

    public void Redraw(DispatcherPriority redrawPriority = DispatcherPriority.Normal)
    {
        VerifyAccess();
        ClearVisualLines();
        InvalidateMeasure(redrawPriority);
    }

    public void Redraw(VisualLine visualLine, DispatcherPriority redrawPriority = DispatcherPriority.Normal)
    {
        VerifyAccess();
        if (!allVisualLines.Remove(visualLine)) return;
        DisposeVisualLine(visualLine);
        InvalidateMeasure(redrawPriority);
    }

    public void Redraw(int offset, int length, DispatcherPriority redrawPriority = DispatcherPriority.Normal)
    {
        VerifyAccess();
        var changedSomethingBeforeOrInLine = false;
        for (var i = 0; i < allVisualLines.Count; i++)
        {
            var visualLine = allVisualLines[i];
            var lineStart  = visualLine.FirstDocumentLine.Offset;
            var lineEnd    = visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength;
            if (offset > lineEnd) continue;
            changedSomethingBeforeOrInLine = true;
            if (offset + length < lineStart) continue;
            allVisualLines.RemoveAt(i--);
            DisposeVisualLine(visualLine);
        }

        if (changedSomethingBeforeOrInLine) InvalidateMeasure(redrawPriority);
    }

    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "knownLayer", Justification = "This method is meant to invalidate only a specific layer - I just haven't figured out how to do that, yet.")]
    public void InvalidateLayer(KnownLayer knownLayer)
    {
        InvalidateMeasure(DispatcherPriority.Normal);
    }

    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "knownLayer", Justification = "This method is meant to invalidate only a specific layer - I just haven't figured out how to do that, yet.")]
    public void InvalidateLayer(KnownLayer knownLayer, DispatcherPriority priority)
    {
        InvalidateMeasure(priority);
    }

    public void Redraw(ISegment segment, DispatcherPriority redrawPriority = DispatcherPriority.Normal)
    {
        if (segment != null) Redraw(segment.Offset, segment.Length, redrawPriority);
    }

    private void ClearVisualLines()
    {
        visibleVisualLines = null;
        if (allVisualLines.Count == 0) return;
        foreach (var visualLine in allVisualLines) DisposeVisualLine(visualLine);
        allVisualLines.Clear();
    }

    private void DisposeVisualLine(VisualLine visualLine)
    {
        if (newVisualLines != null && newVisualLines.Contains(visualLine)) throw new ArgumentException("Cannot dispose visual line because it is in construction!");
        visibleVisualLines = null;
        visualLine.Dispose();
        RemoveInlineObjects(visualLine);
    }

    #endregion

    #region InvalidateMeasure(DispatcherPriority)

    private DispatcherOperation invalidateMeasureOperation;

    private void InvalidateMeasure(DispatcherPriority priority)
    {
        if (priority >= DispatcherPriority.Render)
        {
            if (invalidateMeasureOperation != null)
            {
                invalidateMeasureOperation.Abort();
                invalidateMeasureOperation = null;
            }

            base.InvalidateMeasure();
        }
        else
        {
            if (invalidateMeasureOperation != null) invalidateMeasureOperation.Priority = priority;
            else
                invalidateMeasureOperation = Dispatcher.BeginInvoke(priority, new Action(delegate
                {
                    invalidateMeasureOperation = null;
                    base.InvalidateMeasure();
                }));
        }
    }

    #endregion

    #region Get(OrConstruct)VisualLine

    public VisualLine GetVisualLine(int documentLineNumber)
    {
        foreach (var visualLine in allVisualLines)
        {
            Debug.Assert(visualLine.IsDisposed == false);
            var start = visualLine.FirstDocumentLine.LineNumber;
            var end   = visualLine.LastDocumentLine.LineNumber;
            if (documentLineNumber >= start && documentLineNumber <= end) return visualLine;
        }

        return null;
    }

    public VisualLine GetOrConstructVisualLine(DocumentLine documentLine)
    {
        if (documentLine == null) throw new ArgumentNullException(nameof(documentLine));
        if (!Document.Lines.Contains(documentLine)) throw new InvalidOperationException("Line belongs to wrong document");
        VerifyAccess();

        var l = GetVisualLine(documentLine.LineNumber);
        if (l != null) return l;
        var globalTextRunProperties = CreateGlobalTextRunProperties();
        var paragraphProperties     = CreateParagraphProperties(globalTextRunProperties);

        while (heightTree.GetIsCollapsed(documentLine.LineNumber)) documentLine = documentLine.PreviousLine;

        l = BuildVisualLine(documentLine, globalTextRunProperties, paragraphProperties, elementGenerators.ToArray(), lineTransformers.ToArray(), lastAvailableSize);
        allVisualLines.Add(l);
        foreach (var line in allVisualLines) line.VisualTop = heightTree.GetVisualPosition(line.FirstDocumentLine);

        return l;
    }

    #endregion

    #region Visual Lines (fields and properties)

    private List<VisualLine>               allVisualLines = new();
    private ReadOnlyCollection<VisualLine> visibleVisualLines;
    private double                         clippedPixelsOnTop;
    private List<VisualLine>               newVisualLines;

    [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
    public ReadOnlyCollection<VisualLine> VisualLines
    {
        get
        {
            if (visibleVisualLines == null) throw new VisualLinesInvalidException();
            return visibleVisualLines;
        }
    }

    public bool VisualLinesValid => visibleVisualLines != null;

    public event EventHandler<VisualLineConstructionStartEventArgs> VisualLineConstructionStarting;

    public event EventHandler VisualLinesChanged;

    public void EnsureVisualLines()
    {
        Dispatcher.VerifyAccess();
        if (inMeasure) throw new InvalidOperationException("The visual line build process is already running! Cannot EnsureVisualLines() during Measure!");
        if (!VisualLinesValid)
        {
            InvalidateMeasure(DispatcherPriority.Normal);
            UpdateLayout();
        }

        if (!VisualLinesValid)
        {
            Debug.WriteLine("UpdateLayout() failed in EnsureVisualLines");
            MeasureOverride(lastAvailableSize);
        }

        if (!VisualLinesValid) throw new VisualLinesInvalidException("Internal error: visual lines invalid after EnsureVisualLines call");
    }

    #endregion

    #region Measure

    private const double AdditionalHorizontalScrollAmount = 3;

    private Size lastAvailableSize;
    private bool inMeasure;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (availableSize.Width > 32000) availableSize.Width = 32000;

        if (!canHorizontallyScroll && !availableSize.Width.IsClose(lastAvailableSize.Width)) ClearVisualLines();
        lastAvailableSize = availableSize;

        foreach (UIElement layer in layers) layer.Measure(availableSize);
        MeasureInlineObjects();

        InvalidateVisual();

        double maxWidth;
        if (document == null)
        {
            allVisualLines     = new List<VisualLine>();
            visibleVisualLines = allVisualLines.AsReadOnly();
            maxWidth           = 0;
        }
        else
        {
            inMeasure = true;
            try
            {
                maxWidth = CreateAndMeasureVisualLines(availableSize);
            }
            finally
            {
                inMeasure = false;
            }
        }

        RemoveInlineObjectsNow();

        maxWidth += AdditionalHorizontalScrollAmount;
        var heightTreeHeight = DocumentHeight;
        var options          = Options;
        if (options.AllowScrollBelowDocument)
            if (!double.IsInfinity(scrollViewport.Height))
            {
                var minVisibleDocumentHeight = Math.Max(DefaultLineHeight, Caret.MinimumDistanceToViewBorder);
                var scrollViewportBottom     = Math.Min(heightTreeHeight - minVisibleDocumentHeight, scrollOffset.Y) + scrollViewport.Height;
                heightTreeHeight = Math.Max(heightTreeHeight, scrollViewportBottom);
            }

        textLayer.SetVisualLines(visibleVisualLines);

        SetScrollData(availableSize, new Size(maxWidth, heightTreeHeight), scrollOffset);
        if (VisualLinesChanged != null) VisualLinesChanged(this, EventArgs.Empty);

        return new Size(Math.Min(availableSize.Width, maxWidth), Math.Min(availableSize.Height, heightTreeHeight));
    }

    private double CreateAndMeasureVisualLines(Size availableSize)
    {
        var globalTextRunProperties = CreateGlobalTextRunProperties();
        var paragraphProperties     = CreateParagraphProperties(globalTextRunProperties);

        Debug.WriteLine("Measure availableSize=" + availableSize + ", scrollOffset=" + scrollOffset);
        var firstLineInView = heightTree.GetLineByVisualPosition(scrollOffset.Y);

        clippedPixelsOnTop = scrollOffset.Y - heightTree.GetVisualPosition(firstLineInView);
        Debug.Assert(clippedPixelsOnTop >= -ExtensionMethods.Epsilon);

        newVisualLines = new List<VisualLine>();

        if (VisualLineConstructionStarting != null) VisualLineConstructionStarting(this, new VisualLineConstructionStartEventArgs(firstLineInView));

        var    elementGeneratorsArray = elementGenerators.ToArray();
        var    lineTransformersArray  = lineTransformers.ToArray();
        var    nextLine               = firstLineInView;
        double maxWidth               = 0;
        var    yPos                   = -clippedPixelsOnTop;
        while (yPos < availableSize.Height && nextLine != null)
        {
            var visualLine = GetVisualLine(nextLine.LineNumber) ?? BuildVisualLine(nextLine, globalTextRunProperties, paragraphProperties, elementGeneratorsArray, lineTransformersArray, availableSize);

            visualLine.VisualTop = scrollOffset.Y + yPos;

            nextLine = visualLine.LastDocumentLine.NextLine;

            yPos += visualLine.Height;

            maxWidth = visualLine.TextLines.Select(textLine => textLine.WidthIncludingTrailingWhitespace).Prepend(maxWidth).Max();

            newVisualLines.Add(visualLine);
        }

        foreach (var line in allVisualLines)
        {
            Debug.Assert(line.IsDisposed == false);
            if (!newVisualLines.Contains(line)) DisposeVisualLine(line);
        }

        allVisualLines     = newVisualLines;
        visibleVisualLines = new ReadOnlyCollection<VisualLine>(newVisualLines.ToArray());
        newVisualLines     = null;

        if (allVisualLines.Any(line => line.IsDisposed)) throw new InvalidOperationException("A visual line was disposed even though it is still in use.\n" + "This can happen when Redraw() is called during measure for lines " + "that are already constructed.");
        return maxWidth;
    }

    #endregion

    #region BuildVisualLine

    private  TextFormatter          formatter;
    internal TextViewCachedElements cachedElements;

    private TextRunProperties CreateGlobalTextRunProperties()
    {
        var p = new GlobalTextRunProperties { typeface = this.CreateTypeface(), fontRenderingEmSize = FontSize, foregroundBrush = (Brush)GetValue(Control.ForegroundProperty) };
        ExtensionMethods.CheckIsFrozen(p.foregroundBrush);
        p.cultureInfo = CultureInfo.CurrentCulture;
        return p;
    }

    private VisualLineTextParagraphProperties CreateParagraphProperties(TextRunProperties defaultTextRunProperties)
    {
        return new VisualLineTextParagraphProperties { defaultTextRunProperties = defaultTextRunProperties, textWrapping = canHorizontallyScroll ? TextWrapping.NoWrap : TextWrapping.Wrap, tabSize = Options.IndentationSize * WideSpaceWidth, flowDirection = FlowDirection };
    }

    private VisualLine BuildVisualLine(DocumentLine documentLine, TextRunProperties globalTextRunProperties, VisualLineTextParagraphProperties paragraphProperties, VisualLineElementGenerator[] elementGeneratorsArray, IVisualLineTransformer[] lineTransformersArray, Size availableSize)
    {
        if (heightTree.GetIsCollapsed(documentLine.LineNumber)) throw new InvalidOperationException("Trying to build visual line from collapsed line");

        var visualLine = new VisualLine(this, documentLine);
        var textSource = new VisualLineTextSource(visualLine) { Document = document, GlobalTextRunProperties = globalTextRunProperties, TextView = this };

        visualLine.ConstructVisualElements(textSource, elementGeneratorsArray);

        if (visualLine.FirstDocumentLine != visualLine.LastDocumentLine)
        {
            var firstLinePos = heightTree.GetVisualPosition(visualLine.FirstDocumentLine.NextLine);
            var lastLinePos  = heightTree.GetVisualPosition(visualLine.LastDocumentLine.NextLine ?? visualLine.LastDocumentLine);
            if (!firstLinePos.IsClose(lastLinePos))
            {
                for (var i = visualLine.FirstDocumentLine.LineNumber + 1; i <= visualLine.LastDocumentLine.LineNumber; i++)
                    if (!heightTree.GetIsCollapsed(i))
                        throw new InvalidOperationException("Line " + i + " was skipped by a VisualLineElementGenerator, but it is not collapsed.");
                throw new InvalidOperationException("All lines collapsed but visual pos different - height tree inconsistency?");
            }
        }

        visualLine.RunTransformers(textSource, lineTransformersArray);

        var           textOffset    = 0;
        TextLineBreak lastLineBreak = null;
        var           textLines     = new List<TextLine>();
        paragraphProperties.indent               = 0;
        paragraphProperties.firstLineInParagraph = true;
        while (textOffset <= visualLine.VisualLengthWithEndOfLineMarker)
        {
            var textLine = formatter.FormatLine(textSource, textOffset, availableSize.Width, paragraphProperties, lastLineBreak);
            textLines.Add(textLine);
            textOffset += textLine.Length;

            if (textOffset >= visualLine.VisualLengthWithEndOfLineMarker) break;

            if (paragraphProperties.firstLineInParagraph)
            {
                paragraphProperties.firstLineInParagraph = false;

                var    options     = Options;
                double indentation = 0;
                if (options.InheritWordWrapIndentation)
                {
                    var indentVisualColumn                                                     = GetIndentationVisualColumn(visualLine);
                    if (indentVisualColumn > 0 && indentVisualColumn < textOffset) indentation = textLine.GetDistanceFromCharacterHit(new CharacterHit(indentVisualColumn, 0));
                }

                indentation += options.WordWrapIndentation;
                if (indentation > 0 && indentation * 2 < availableSize.Width) paragraphProperties.indent = indentation;
            }

            lastLineBreak = textLine.GetTextLineBreak();
        }

        visualLine.SetTextLines(textLines);
        heightTree.SetHeight(visualLine.FirstDocumentLine, visualLine.Height);
        return visualLine;
    }

    private static int GetIndentationVisualColumn(VisualLine visualLine)
    {
        if (visualLine.Elements.Count == 0) return 0;
        var column       = 0;
        var elementIndex = 0;
        var element      = visualLine.Elements[elementIndex];
        while (element.IsWhitespace(column))
        {
            column++;
            if (column == element.VisualColumn + element.VisualLength)
            {
                elementIndex++;
                if (elementIndex == visualLine.Elements.Count) break;
                element = visualLine.Elements[elementIndex];
            }
        }

        return column;
    }

    #endregion

    #region Arrange

    protected override Size ArrangeOverride(Size finalSize)
    {
        EnsureVisualLines();

        foreach (UIElement layer in layers) layer.Arrange(new Rect(new Point(0, 0), finalSize));

        if (document == null || allVisualLines.Count == 0) return finalSize;

        var newScrollOffset                                                            = scrollOffset;
        if (scrollOffset.X + finalSize.Width > scrollExtent.Width) newScrollOffset.X   = Math.Max(0, scrollExtent.Width - finalSize.Width);
        if (scrollOffset.Y + finalSize.Height > scrollExtent.Height) newScrollOffset.Y = Math.Max(0, scrollExtent.Height - finalSize.Height);
        if (SetScrollData(scrollViewport, scrollExtent, newScrollOffset)) InvalidateMeasure(DispatcherPriority.Normal);

        if (visibleVisualLines != null)
        {
            var pos = new Point(-scrollOffset.X, -clippedPixelsOnTop);
            foreach (var visualLine in visibleVisualLines)
            {
                var offset = 0;
                foreach (var textLine in visualLine.TextLines)
                {
                    foreach (var span in textLine.GetTextRunSpans())
                    {
                        if (span.Value is InlineObjectRun { VisualLine: { } } inline)
                        {
                            Debug.Assert(inlineObjects.Contains(inline));
                            var distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(offset, 0));
                            inline.Element.Arrange(new Rect(new Point(pos.X + distance, pos.Y), inline.Element.DesiredSize));
                        }

                        offset += span.Length;
                    }

                    pos.Y += textLine.Height;
                }
            }
        }

        InvalidateCursorIfMouseWithinTextView();

        return finalSize;
    }

    #endregion

    #region Render

    private readonly ObserveAddRemoveCollection<IBackgroundRenderer> backgroundRenderers;

    public IList<IBackgroundRenderer> BackgroundRenderers => backgroundRenderers;

    private void BackgroundRenderer_Added(IBackgroundRenderer renderer)
    {
        ConnectToTextView(renderer);
        InvalidateLayer(renderer.Layer);
    }

    private void BackgroundRenderer_Removed(IBackgroundRenderer renderer)
    {
        DisconnectFromTextView(renderer);
        InvalidateLayer(renderer.Layer);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateLayer(KnownLayer.Selection);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        RenderBackground(drawingContext, KnownLayer.Background);
        foreach (var line in visibleVisualLines)
        {
            Brush currentBrush = null;
            var   startVC      = 0;
            var   length       = 0;
            foreach (var element in line.Elements)
                if (currentBrush == null || !currentBrush.Equals(element.BackgroundBrush))
                {
                    if (currentBrush != null)
                    {
                        var builder = new BackgroundGeometryBuilder { AlignToWholePixels = true, CornerRadius = 3 };
                        foreach (var rect in BackgroundGeometryBuilder.GetRectsFromVisualSegment(this, line, startVC, startVC + length)) builder.AddRectangle(this, rect);
                        var geometry = builder.CreateGeometry();
                        if (geometry != null) drawingContext.DrawGeometry(currentBrush, null, geometry);
                    }

                    startVC      = element.VisualColumn;
                    length       = element.VisualLength;
                    currentBrush = element.BackgroundBrush;
                }
                else
                {
                    length += element.VisualLength;
                }

            if (currentBrush != null)
            {
                var builder = new BackgroundGeometryBuilder { AlignToWholePixels = true, CornerRadius = 3 };
                foreach (var rect in BackgroundGeometryBuilder.GetRectsFromVisualSegment(this, line, startVC, startVC + length)) builder.AddRectangle(this, rect);
                var geometry = builder.CreateGeometry();
                if (geometry != null) drawingContext.DrawGeometry(currentBrush, null, geometry);
            }
        }
    }

    internal void RenderBackground(DrawingContext drawingContext, KnownLayer layer)
    {
        foreach (var bg in backgroundRenderers)
            if (bg.Layer == layer)
                bg.Draw(this, drawingContext);
    }

    internal void ArrangeTextLayer(IEnumerable<VisualLineDrawingVisual> visuals)
    {
        var pos = new Point(-scrollOffset.X, -clippedPixelsOnTop);
        foreach (var visual in visuals)
        {
            if (visual.Transform is not TranslateTransform t || !t.X.Equals(pos.X) || !t.Y.Equals(pos.Y))
            {
                visual.Transform = new TranslateTransform(pos.X, pos.Y);
                visual.Transform.Freeze();
            }

            pos.Y += visual.Height;
        }
    }

    #endregion

    #region IScrollInfo implementation

    private Size scrollExtent;

    private Vector scrollOffset;

    private Size scrollViewport;

    private void ClearScrollData()
    {
        SetScrollData(new Size(), new Size(), new Vector());
    }

    private bool SetScrollData(Size viewport, Size extent, Vector offset)
    {
        if (!(viewport.IsClose(scrollViewport) && extent.IsClose(scrollExtent) && offset.IsClose(scrollOffset)))
        {
            scrollViewport = viewport;
            scrollExtent   = extent;
            SetScrollOffset(offset);
            OnScrollChange();
            return true;
        }

        return false;
    }

    private void OnScrollChange()
    {
        var scrollOwner = ((IScrollInfo)this).ScrollOwner;
        if (scrollOwner != null) scrollOwner.InvalidateScrollInfo();
    }

    private bool canVerticallyScroll;
    bool IScrollInfo.CanVerticallyScroll
    {
        get => canVerticallyScroll;
        set
        {
            if (canVerticallyScroll != value)
            {
                canVerticallyScroll = value;
                InvalidateMeasure(DispatcherPriority.Normal);
            }
        }
    }
    private bool canHorizontallyScroll;
    bool IScrollInfo.CanHorizontallyScroll
    {
        get => canHorizontallyScroll;
        set
        {
            if (canHorizontallyScroll != value)
            {
                canHorizontallyScroll = value;
                ClearVisualLines();
                InvalidateMeasure(DispatcherPriority.Normal);
            }
        }
    }

    double IScrollInfo.ExtentWidth => scrollExtent.Width;

    double IScrollInfo.ExtentHeight => scrollExtent.Height;

    double IScrollInfo.ViewportWidth => scrollViewport.Width;

    double IScrollInfo.ViewportHeight => scrollViewport.Height;

    public double HorizontalOffset => scrollOffset.X;

    public double VerticalOffset => scrollOffset.Y;

    public Vector ScrollOffset => scrollOffset;

    public event EventHandler ScrollOffsetChanged;

    private void SetScrollOffset(Vector vector)
    {
        if (!canHorizontallyScroll) vector.X = 0;
        if (!canVerticallyScroll) vector.Y   = 0;

        if (scrollOffset.IsClose(vector)) return;
        scrollOffset = vector;
        ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    ScrollViewer IScrollInfo.ScrollOwner { get; set; }

    void IScrollInfo.LineUp()
    {
        ((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y - DefaultLineHeight);
    }

    void IScrollInfo.LineDown()
    {
        ((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y + DefaultLineHeight);
    }

    void IScrollInfo.LineLeft()
    {
        ((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X - WideSpaceWidth);
    }

    void IScrollInfo.LineRight()
    {
        ((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X + WideSpaceWidth);
    }

    void IScrollInfo.PageUp()
    {
        ((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y - scrollViewport.Height);
    }

    void IScrollInfo.PageDown()
    {
        ((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y + scrollViewport.Height);
    }

    void IScrollInfo.PageLeft()
    {
        ((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X - scrollViewport.Width);
    }

    void IScrollInfo.PageRight()
    {
        ((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X + scrollViewport.Width);
    }

    void IScrollInfo.MouseWheelUp()
    {
        ((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y - SystemParameters.WheelScrollLines * DefaultLineHeight);
        OnScrollChange();
    }

    void IScrollInfo.MouseWheelDown()
    {
        ((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y + SystemParameters.WheelScrollLines * DefaultLineHeight);
        OnScrollChange();
    }

    void IScrollInfo.MouseWheelLeft()
    {
        ((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X - SystemParameters.WheelScrollLines * WideSpaceWidth);
        OnScrollChange();
    }

    void IScrollInfo.MouseWheelRight()
    {
        ((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X + SystemParameters.WheelScrollLines * WideSpaceWidth);
        OnScrollChange();
    }

    private bool   defaultTextMetricsValid;
    private double wideSpaceWidth;
    private double defaultLineHeight;
    private double defaultBaseline;

    public double WideSpaceWidth
    {
        get
        {
            CalculateDefaultTextMetrics();
            return wideSpaceWidth;
        }
    }

    public double DefaultLineHeight
    {
        get
        {
            CalculateDefaultTextMetrics();
            return defaultLineHeight;
        }
    }

    public double DefaultBaseline
    {
        get
        {
            CalculateDefaultTextMetrics();
            return defaultBaseline;
        }
    }

    private void InvalidateDefaultTextMetrics()
    {
        defaultTextMetricsValid = false;
        if (heightTree != null) CalculateDefaultTextMetrics();
    }

    private void CalculateDefaultTextMetrics()
    {
        if (defaultTextMetricsValid) return;
        defaultTextMetricsValid = true;
        if (formatter != null)
        {
            var       textRunProperties = CreateGlobalTextRunProperties();
            using var line              = formatter.FormatLine(new SimpleTextSource("x", textRunProperties), 0, 32000, new VisualLineTextParagraphProperties { defaultTextRunProperties = textRunProperties, flowDirection = FlowDirection }, null);
            wideSpaceWidth    = Math.Max(1, line.WidthIncludingTrailingWhitespace);
            defaultBaseline   = Math.Max(1, line.Baseline);
            defaultLineHeight = Math.Max(1, line.Height);
        }
        else
        {
            wideSpaceWidth    = FontSize / 2;
            defaultBaseline   = FontSize;
            defaultLineHeight = FontSize + 3;
        }

        if (heightTree != null) heightTree.DefaultLineHeight = defaultLineHeight;
    }

    private static double ValidateVisualOffset(double offset)
    {
        if (double.IsNaN(offset)) throw new ArgumentException("offset must not be NaN");
        if (offset < 0) return 0;
        return offset;
    }

    void IScrollInfo.SetHorizontalOffset(double offset)
    {
        offset = ValidateVisualOffset(offset);
        if (scrollOffset.X.IsClose(offset)) return;
        SetScrollOffset(new Vector(offset, scrollOffset.Y));
        InvalidateVisual();
        textLayer.InvalidateVisual();
    }

    void IScrollInfo.SetVerticalOffset(double offset)
    {
        offset = ValidateVisualOffset(offset);
        if (scrollOffset.Y.IsClose(offset)) return;
        SetScrollOffset(new Vector(scrollOffset.X, offset));
        InvalidateMeasure(DispatcherPriority.Normal);
    }

    Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle)
    {
        if (rectangle.IsEmpty || visual == null || visual == this || !IsAncestorOf(visual)) return Rect.Empty;
        var childTransform = visual.TransformToAncestor(this);
        rectangle = childTransform.TransformBounds(rectangle);

        MakeVisible(Rect.Offset(rectangle, scrollOffset));

        return rectangle;
    }

    public virtual void MakeVisible(Rect rectangle)
    {
        var visibleRectangle = new Rect(scrollOffset.X, scrollOffset.Y, scrollViewport.Width, scrollViewport.Height);
        var newScrollOffset  = scrollOffset;
        if (rectangle.Left < visibleRectangle.Left)
        {
            if (rectangle.Right > visibleRectangle.Right) newScrollOffset.X = rectangle.Left + rectangle.Width / 2;
            else newScrollOffset.X                                          = rectangle.Left;
        }
        else if (rectangle.Right > visibleRectangle.Right)
        {
            newScrollOffset.X = rectangle.Right - scrollViewport.Width;
        }

        if (rectangle.Top < visibleRectangle.Top)
        {
            if (rectangle.Bottom > visibleRectangle.Bottom) newScrollOffset.Y = rectangle.Top + rectangle.Height / 2;
            else newScrollOffset.Y                                            = rectangle.Top;
        }
        else if (rectangle.Bottom > visibleRectangle.Bottom)
        {
            newScrollOffset.Y = rectangle.Bottom - scrollViewport.Height;
        }

        newScrollOffset.X = ValidateVisualOffset(newScrollOffset.X);
        newScrollOffset.Y = ValidateVisualOffset(newScrollOffset.Y);
        if (scrollOffset.IsClose(newScrollOffset)) return;
        SetScrollOffset(newScrollOffset);
        OnScrollChange();
        InvalidateMeasure(DispatcherPriority.Normal);
    }

    #endregion

    #region Visual element mouse handling

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }

    [ThreadStatic]
    private static bool invalidCursor;

    public static void InvalidateCursor()
    {
        if (invalidCursor) return;
        invalidCursor = true;
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
        {
            invalidCursor = false;
            Mouse.UpdateCursor();
        }));
    }

    internal void InvalidateCursorIfMouseWithinTextView()
    {
        if (IsMouseOver) InvalidateCursor();
    }

    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        var element = GetVisualLineElementFromPosition(e.GetPosition(this) + scrollOffset);
        element?.OnQueryCursor(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Handled) return;
        EnsureVisualLines();
        var element = GetVisualLineElementFromPosition(e.GetPosition(this) + scrollOffset);
        element?.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Handled) return;
        EnsureVisualLines();
        var element = GetVisualLineElementFromPosition(e.GetPosition(this) + scrollOffset);
        element?.OnMouseUp(e);
    }

    #endregion

    #region Getting elements from Visual Position

    public VisualLine GetVisualLineFromVisualTop(double visualTop)
    {
        EnsureVisualLines();
        return VisualLines.Where(vl => !(visualTop < vl.VisualTop)).FirstOrDefault(vl => visualTop < vl.VisualTop + vl.Height);
    }

    public double GetVisualTopByDocumentLine(int line)
    {
        VerifyAccess();
        if (heightTree == null) throw ThrowUtil.NoDocumentAssigned();
        return heightTree.GetVisualPosition(heightTree.GetLineByNumber(line));
    }

    private VisualLineElement GetVisualLineElementFromPosition(Point visualPosition)
    {
        var vl     = GetVisualLineFromVisualTop(visualPosition.Y);
        var column = vl?.GetVisualColumnFloor(visualPosition);
        return vl?.Elements.FirstOrDefault(element => element.VisualColumn + element.VisualLength > column);
    }

    #endregion

    #region Visual Position <-> TextViewPosition

    public Point GetVisualPosition(TextViewPosition position, VisualYPosition yPositionMode)
    {
        VerifyAccess();
        if (Document == null) throw ThrowUtil.NoDocumentAssigned();
        var documentLine = Document.GetLineByNumber(position.Line);
        var visualLine   = GetOrConstructVisualLine(documentLine);
        var visualColumn = position.VisualColumn;
        if (visualColumn >= 0) return visualLine.GetVisualPosition(visualColumn, position.IsAtEndOfLine, yPositionMode);
        var offset = documentLine.Offset + position.Column - 1;
        visualColumn = visualLine.GetVisualColumn(offset - visualLine.FirstDocumentLine.Offset);

        return visualLine.GetVisualPosition(visualColumn, position.IsAtEndOfLine, yPositionMode);
    }

    public TextViewPosition? GetPosition(Point visualPosition)
    {
        VerifyAccess();
        if (Document == null) throw ThrowUtil.NoDocumentAssigned();
        var line = GetVisualLineFromVisualTop(visualPosition.Y);
        return line?.GetTextViewPosition(visualPosition, Options.EnableVirtualSpace);
    }

    public TextViewPosition? GetPositionFloor(Point visualPosition)
    {
        VerifyAccess();
        if (Document == null) throw ThrowUtil.NoDocumentAssigned();
        var line = GetVisualLineFromVisualTop(visualPosition.Y);
        return line?.GetTextViewPositionFloor(visualPosition, Options.EnableVirtualSpace);
    }

    #endregion

    #region Service Provider

    public ServiceContainer Services { get; } = new();

    public virtual object GetService(Type serviceType)
    {
        var instance                                       = Services.GetService(serviceType);
        if (instance == null && document != null) instance = document.ServiceProvider.GetService(serviceType);
        return instance;
    }

    private void ConnectToTextView(object obj)
    {
        if (obj is ITextViewConnect c) c.AddToTextView(this);
    }

    private void DisconnectFromTextView(object obj)
    {
        if (obj is ITextViewConnect c) c.RemoveFromTextView(this);
    }

    #endregion

    #region MouseHover

    public static readonly RoutedEvent PreviewMouseHoverEvent = EventManager.RegisterRoutedEvent("PreviewMouseHover", RoutingStrategy.Tunnel, typeof(MouseEventHandler), typeof(TextView));
    public static readonly RoutedEvent MouseHoverEvent        = EventManager.RegisterRoutedEvent("MouseHover", RoutingStrategy.Bubble, typeof(MouseEventHandler), typeof(TextView));

    public static readonly RoutedEvent PreviewMouseHoverStoppedEvent = EventManager.RegisterRoutedEvent("PreviewMouseHoverStopped", RoutingStrategy.Tunnel, typeof(MouseEventHandler), typeof(TextView));
    public static readonly RoutedEvent MouseHoverStoppedEvent        = EventManager.RegisterRoutedEvent("MouseHoverStopped", RoutingStrategy.Bubble, typeof(MouseEventHandler), typeof(TextView));


    public event MouseEventHandler PreviewMouseHover { add => AddHandler(PreviewMouseHoverEvent, value); remove => RemoveHandler(PreviewMouseHoverEvent, value); }

    public event MouseEventHandler MouseHover { add => AddHandler(MouseHoverEvent, value); remove => RemoveHandler(MouseHoverEvent, value); }

    public event MouseEventHandler PreviewMouseHoverStopped { add => AddHandler(PreviewMouseHoverStoppedEvent, value); remove => RemoveHandler(PreviewMouseHoverStoppedEvent, value); }

    public event MouseEventHandler MouseHoverStopped { add => AddHandler(MouseHoverStoppedEvent, value); remove => RemoveHandler(MouseHoverStoppedEvent, value); }

    private void RaiseHoverEventPair(MouseEventArgs e, RoutedEvent tunnelingEvent, RoutedEvent bubblingEvent)
    {
        var mouseDevice  = e.MouseDevice;
        var stylusDevice = e.StylusDevice;
        var inputTime    = Environment.TickCount;
        var args1        = new MouseEventArgs(mouseDevice, inputTime, stylusDevice) { RoutedEvent = tunnelingEvent, Source = this };
        RaiseEvent(args1);
        var args2 = new MouseEventArgs(mouseDevice, inputTime, stylusDevice) { RoutedEvent = bubblingEvent, Source = this, Handled = args1.Handled };
        RaiseEvent(args2);
    }

    #endregion

    public CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end)
    {
        VerifyAccess();
        if (heightTree == null) throw ThrowUtil.NoDocumentAssigned();
        return heightTree.CollapseText(start, end);
    }

    public double DocumentHeight => heightTree?.TotalHeight ?? 0;

    public DocumentLine GetDocumentLineByVisualTop(double visualTop)
    {
        VerifyAccess();
        if (heightTree == null) throw ThrowUtil.NoDocumentAssigned();
        return heightTree.GetLineByVisualPosition(visualTop);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (TextFormatterFactory.PropertyChangeAffectsTextFormatter(e.Property))
        {
            RecreateTextFormatter();
            RecreateCachedElements();
            InvalidateDefaultTextMetrics();
        }
        else if (e.Property == Control.ForegroundProperty || e.Property == NonPrintableCharacterBrushProperty || e.Property == LinkTextBackgroundBrushProperty || e.Property == LinkTextForegroundBrushProperty || e.Property == LinkTextUnderlineProperty)
        {
            RecreateCachedElements();
            Redraw();
        }

        if (e.Property == Control.FontFamilyProperty || e.Property == Control.FontSizeProperty || e.Property == Control.FontStretchProperty || e.Property == Control.FontStyleProperty || e.Property == Control.FontWeightProperty || e.Property == FlowDirectionProperty)
        {
            RecreateCachedElements();
            InvalidateDefaultTextMetrics();
            Redraw();
        }

        if (e.Property == ColumnRulerPenProperty) columnRulerRenderer.SetRuler(Options.ColumnRulerPosition, ColumnRulerPen);
        if (e.Property == CurrentLineBorderProperty) currentLineHighlighRenderer.BorderPen           = CurrentLineBorder;
        if (e.Property == CurrentLineBackgroundProperty) currentLineHighlighRenderer.BackgroundBrush = CurrentLineBackground;
    }

    public static readonly DependencyProperty ColumnRulerPenProperty = DependencyProperty.Register(nameof(ColumnRulerPen), typeof(Pen), typeof(TextView), new FrameworkPropertyMetadata(CreateFrozenPen(Brushes.LightGray)));

    private static Pen CreateFrozenPen(Brush brush)
    {
        var pen = new Pen(brush, 1);
        pen.Freeze();
        return pen;
    }

    public Pen ColumnRulerPen { get => (Pen)GetValue(ColumnRulerPenProperty); set => SetValue(ColumnRulerPenProperty, value); }

    public static readonly DependencyProperty CurrentLineBackgroundProperty = DependencyProperty.Register(nameof(CurrentLineBackground), typeof(Brush), typeof(TextView));

    public Brush CurrentLineBackground { get => (Brush)GetValue(CurrentLineBackgroundProperty); set => SetValue(CurrentLineBackgroundProperty, value); }

    public static readonly DependencyProperty CurrentLineBorderProperty = DependencyProperty.Register(nameof(CurrentLineBorder), typeof(Pen), typeof(TextView));

    public Pen CurrentLineBorder { get => (Pen)GetValue(CurrentLineBorderProperty); set => SetValue(CurrentLineBorderProperty, value); }

    public int HighlightedLine { get => currentLineHighlighRenderer.Line; set => currentLineHighlighRenderer.Line = value; }

    public virtual double EmptyLineSelectionWidth => 1;
}

internal class TextViewImpl : TextView
{
}
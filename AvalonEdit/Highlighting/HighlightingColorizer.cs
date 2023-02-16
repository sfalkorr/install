using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using AvalonEdit.Document;
using AvalonEdit.Rendering;

namespace AvalonEdit.Highlighting;

public class LineColorizer : DocumentColorizingTransformer

{
    private int   lineNumber;
    public  Brush LineColor { get; set; }

    public LineColorizer(int lineNumber)
    {
        this.lineNumber = lineNumber;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (!line.IsDeleted && line.LineNumber == lineNumber) ChangeLinePart(line.Offset, line.EndOffset, ApplyChanges);
    }

    private void ApplyChanges(VisualLineElement element)
    {
        element.TextRunProperties.SetForegroundBrush(LineColor);
    }
}

public class HighlightingColorizer : DocumentColorizingTransformer
{
    private readonly IHighlightingDefinition definition;
    private          TextView                textView;
    private          IHighlighter            highlighter;
    private          bool                    isFixedHighlighter;

    public HighlightingColorizer(IHighlightingDefinition definition)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }


    public HighlightingColorizer(IHighlighter highlighter)
    {
        this.highlighter   = highlighter ?? throw new ArgumentNullException(nameof(highlighter));
        isFixedHighlighter = true;
    }

    protected HighlightingColorizer()
    {
    }

    private void textView_DocumentChanged(object sender, EventArgs e)
    {
        var view = (TextView)sender;
        DeregisterServices(view);
        RegisterServices(view);
    }

    protected virtual void DeregisterServices(TextView textView_)
    {
        if (highlighter == null) return;
        if (isInHighlightingGroup)
        {
            highlighter.EndHighlighting();
            isInHighlightingGroup = false;
        }

        highlighter.HighlightingStateChanged -= OnHighlightStateChanged;
        if (textView_.Services.GetService(typeof(IHighlighter)) == highlighter) textView_.Services.RemoveService(typeof(IHighlighter));
        if (isFixedHighlighter) return;
        highlighter?.Dispose();
        highlighter = null;
    }

    protected virtual void RegisterServices(TextView textView_)
    {
        if (textView_.Document == null) return;
        if (!isFixedHighlighter) highlighter = textView_.Document != null ? CreateHighlighter(textView_, textView_.Document) : null;
        if (highlighter == null || highlighter.Document != textView_.Document) return;
        if (textView_.Services.GetService(typeof(IHighlighter)) == null) textView_.Services.AddService(typeof(IHighlighter), highlighter);
        highlighter.HighlightingStateChanged += OnHighlightStateChanged;
    }

    protected virtual IHighlighter CreateHighlighter(TextView textView_, TextDocument document)
    {
        if (definition != null) return new DocumentHighlighter(document, definition);
        throw new NotSupportedException("Cannot create a highlighter because no IHighlightingDefinition was specified, and the CreateHighlighter() method was not overridden.");
    }

    protected override void OnAddToTextView(TextView textView)
    {
        if (this.textView != null) throw new InvalidOperationException("Cannot use a HighlightingColorizer instance in multiple text views. Please create a separate instance for each text view.");
        base.OnAddToTextView(textView);
        this.textView                           =  textView;
        textView.DocumentChanged                += textView_DocumentChanged;
        textView.VisualLineConstructionStarting += textView_VisualLineConstructionStarting;
        textView.VisualLinesChanged             += textView_VisualLinesChanged;
        RegisterServices(textView);
    }

    protected override void OnRemoveFromTextView(TextView textView)
    {
        DeregisterServices(textView);
        textView.DocumentChanged                -= textView_DocumentChanged;
        textView.VisualLineConstructionStarting -= textView_VisualLineConstructionStarting;
        textView.VisualLinesChanged             -= textView_VisualLinesChanged;
        base.OnRemoveFromTextView(textView);
        this.textView = null;
    }

    private bool isInHighlightingGroup;

    private void textView_VisualLineConstructionStarting(object sender, VisualLineConstructionStartEventArgs e)
    {
        if (highlighter != null)
        {
            lineNumberBeingColorized = e.FirstLineInView.LineNumber - 1;
            if (!isInHighlightingGroup)
            {
                highlighter.BeginHighlighting();
                isInHighlightingGroup = true;
            }

            highlighter.UpdateHighlightingState(lineNumberBeingColorized);
            lineNumberBeingColorized = 0;
        }
    }

    private void textView_VisualLinesChanged(object sender, EventArgs e)
    {
        if (highlighter != null && isInHighlightingGroup)
        {
            highlighter.EndHighlighting();
            isInHighlightingGroup = false;
        }
    }

    private DocumentLine lastColorizedLine;

    protected override void Colorize(ITextRunConstructionContext context)
    {
        lastColorizedLine = null;
        base.Colorize(context);
        if (lastColorizedLine != context.VisualLine.LastDocumentLine)
            if (highlighter != null)
            {
                lineNumberBeingColorized = context.VisualLine.LastDocumentLine.LineNumber;
                highlighter.UpdateHighlightingState(lineNumberBeingColorized);
                lineNumberBeingColorized = 0;
            }

        lastColorizedLine = null;
    }

    private int lineNumberBeingColorized;

    protected override void ColorizeLine(DocumentLine line)
    {
        if (highlighter != null)
        {
            lineNumberBeingColorized = line.LineNumber;
            var hl = highlighter.HighlightLine(lineNumberBeingColorized);
            lineNumberBeingColorized = 0;
            foreach (var section in hl.Sections)
            {
                if (IsEmptyColor(section.Color)) continue;
                ChangeLinePart(section.Offset, section.Offset + section.Length, visualLineElement => ApplyColorToElement(visualLineElement, section.Color));
            }
        }

        lastColorizedLine = line;
    }

    internal static bool IsEmptyColor(HighlightingColor color)
    {
        if (color == null) return true;
        return color.Background == null && color.Foreground == null && color.FontStyle == null && color.FontWeight == null && color.Underline == null && color.Strikethrough == null;
    }

    protected virtual void ApplyColorToElement(VisualLineElement element, HighlightingColor color)
    {
        ApplyColorToElement(element, color, CurrentContext);
    }

    internal static void ApplyColorToElement(VisualLineElement element, HighlightingColor color, ITextRunConstructionContext context)
    {
        if (color.Foreground != null)
        {
            var b = color.Foreground.GetBrush(context);
            if (b != null) element.TextRunProperties.SetForegroundBrush(b);
        }

        if (color.Background != null)
        {
            var b                                  = color.Background.GetBrush(context);
            if (b != null) element.BackgroundBrush = b;
        }

        if (color.FontStyle != null || color.FontWeight != null || color.FontFamily != null)
        {
            var tf = element.TextRunProperties.Typeface;
            element.TextRunProperties.SetTypeface(new Typeface(color.FontFamily ?? tf.FontFamily, color.FontStyle ?? tf.Style, color.FontWeight ?? tf.Weight, tf.Stretch));
        }

        if (color.Underline ?? false) element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
        if (color.Strikethrough ?? false) element.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
        if (color.FontSize.HasValue) element.TextRunProperties.SetFontRenderingEmSize(color.FontSize.Value);
    }

    private void OnHighlightStateChanged(int fromLineNumber, int toLineNumber)
    {
        if (lineNumberBeingColorized != 0)
            if (toLineNumber <= lineNumberBeingColorized)
                return;

        Debug.WriteLine("OnHighlightStateChanged forces redraw of lines {0} to {1}", fromLineNumber, toLineNumber);

        if (fromLineNumber == toLineNumber)
        {
            textView.Redraw(textView.Document.GetLineByNumber(fromLineNumber));
        }
        else
        {
            var fromLine    = textView.Document.GetLineByNumber(fromLineNumber);
            var toLine      = textView.Document.GetLineByNumber(toLineNumber);
            var startOffset = fromLine.Offset;
            textView.Redraw(startOffset, toLine.EndOffset - startOffset);
        }
    }
}
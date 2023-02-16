using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using AvalonEdit.Document;
using AvalonEdit.Rendering;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

public sealed class Caret
{
    private readonly TextArea   textArea;
    private readonly TextView   textView;
    private readonly CaretLayer caretAdorner;
    private          bool       visible;

    internal Caret(TextArea textArea)
    {
        this.textArea = textArea;
        textView      = textArea.TextView;
        position      = new TextViewPosition(1, 1, 0);

        caretAdorner = new CaretLayer(textArea);
        textView.InsertLayer(caretAdorner, KnownLayer.Caret, LayerInsertionPosition.Replace);
        textView.VisualLinesChanged  += TextView_VisualLinesChanged;
        textView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
    }

    internal void UpdateIfVisible()
    {
        if (visible) Show();
    }

    private void TextView_VisualLinesChanged(object sender, EventArgs e)
    {
        if (visible) Show();
        InvalidateVisualColumn();
    }

    private void TextView_ScrollOffsetChanged(object sender, EventArgs e)
    {
        caretAdorner?.InvalidateVisual();
    }

    private TextViewPosition position;

    public TextViewPosition Position
    {
        get
        {
            ValidateVisualColumn();
            return position;
        }
        set
        {
            if (position == value) return;
            position = value;

            storedCaretOffset = -1;

            ValidatePosition();
            InvalidateVisualColumn();
            RaisePositionChanged();
            Log("Caret position changed to " + value);
            if (visible) Show();
        }
    }

    internal TextViewPosition NonValidatedPosition => position;

    public TextLocation Location { get => position.Location; set => Position = new TextViewPosition(value); }

    public int Line { get => position.Line; set => Position = new TextViewPosition(value, position.Column); }

    public int Column { get => position.Column; set => Position = new TextViewPosition(position.Line, value); }

    public int VisualColumn
    {
        get
        {
            ValidateVisualColumn();
            return position.VisualColumn;
        }
        set => Position = new TextViewPosition(position.Line, position.Column, value);
    }

    private bool isInVirtualSpace;

    public bool IsInVirtualSpace
    {
        get
        {
            ValidateVisualColumn();
            return isInVirtualSpace;
        }
    }

    private int storedCaretOffset;

    internal void OnDocumentChanging()
    {
        storedCaretOffset = Offset;
        InvalidateVisualColumn();
    }

    internal void OnDocumentChanged(DocumentChangeEventArgs e)
    {
        InvalidateVisualColumn();
        if (storedCaretOffset >= 0)
        {
            AnchorMovementType caretMovementType;
            if (!textArea.Selection.IsEmpty && storedCaretOffset == textArea.Selection.SurroundingSegment.EndOffset) caretMovementType = AnchorMovementType.BeforeInsertion;
            else caretMovementType                                                                                                     = AnchorMovementType.Default;
            var newCaretOffset             = e.GetNewOffset(storedCaretOffset, caretMovementType);
            var document                   = textArea.Document;
            if (document != null) Position = new TextViewPosition(document.GetLocation(newCaretOffset), position.VisualColumn);
        }

        storedCaretOffset = -1;
    }

    public int Offset
    {
        get
        {
            var document = textArea.Document;
            return document?.GetOffset(position.Location) ?? 0;
        }
        set
        {
            var document = textArea.Document;
            if (document == null) return;
            Position    = new TextViewPosition(document.GetLocation(value));
            DesiredXPos = double.NaN;
        }
    }

    public double DesiredXPos { get; set; } = double.NaN;

    private void ValidatePosition()
    {
        if (position.Line < 1) position.Line                  = 1;
        if (position.Column < 1) position.Column              = 1;
        if (position.VisualColumn < -1) position.VisualColumn = -1;
        var document                                          = textArea.Document;
        if (document != null)
        {
            if (position.Line > document.LineCount)
            {
                position.Line         = document.LineCount;
                position.Column       = document.GetLineByNumber(position.Line).Length + 1;
                position.VisualColumn = -1;
            }
            else
            {
                var line = document.GetLineByNumber(position.Line);
                if (position.Column > line.Length + 1)
                {
                    position.Column       = line.Length + 1;
                    position.VisualColumn = -1;
                }
            }
        }
    }

    public event EventHandler PositionChanged;

    private bool raisePositionChangedOnUpdateFinished;

    private void RaisePositionChanged()
    {
        if (textArea.Document is { IsInUpdate: true }) raisePositionChangedOnUpdateFinished = true;
        else PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void OnDocumentUpdateFinished()
    {
        if (!raisePositionChangedOnUpdateFinished) return;
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool visualColumnValid;

    private void ValidateVisualColumn()
    {
        if (visualColumnValid) return;
        var document = textArea.Document;
        if (document == null) return;
        Debug.WriteLine("Explicit validation of caret column");
        var documentLine = document.GetLineByNumber(position.Line);
        RevalidateVisualColumn(textView.GetOrConstructVisualLine(documentLine));
    }

    private void InvalidateVisualColumn()
    {
        visualColumnValid = false;
    }

    private void RevalidateVisualColumn(VisualLine visualLine)
    {
        if (visualLine == null) throw new ArgumentNullException(nameof(visualLine));

        visualColumnValid = true;

        var caretOffset             = textView.Document.GetOffset(position.Location);
        var firstDocumentLineOffset = visualLine.FirstDocumentLine.Offset;
        position.VisualColumn = visualLine.ValidateVisualColumn(position, textArea.Selection.EnableVirtualSpace);

        var newVisualColumnForwards = visualLine.GetNextCaretPosition(position.VisualColumn - 1, LogicalDirection.Forward, CaretPositioningMode.Normal, textArea.Selection.EnableVirtualSpace);
        if (newVisualColumnForwards != position.VisualColumn)
        {
            var newVisualColumnBackwards = visualLine.GetNextCaretPosition(position.VisualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.Normal, textArea.Selection.EnableVirtualSpace);

            if (newVisualColumnForwards < 0 && newVisualColumnBackwards < 0) throw ThrowUtil.NoValidCaretPosition();

            int newOffsetForwards;
            if (newVisualColumnForwards >= 0) newOffsetForwards = visualLine.GetRelativeOffset(newVisualColumnForwards) + firstDocumentLineOffset;
            else newOffsetForwards                              = -1;
            int newOffsetBackwards;
            if (newVisualColumnBackwards >= 0) newOffsetBackwards = visualLine.GetRelativeOffset(newVisualColumnBackwards) + firstDocumentLineOffset;
            else newOffsetBackwards                               = -1;

            int newVisualColumn, newOffset;
            if (newVisualColumnForwards < 0)
            {
                newVisualColumn = newVisualColumnBackwards;
                newOffset       = newOffsetBackwards;
            }
            else if (newVisualColumnBackwards < 0)
            {
                newVisualColumn = newVisualColumnForwards;
                newOffset       = newOffsetForwards;
            }
            else
            {
                if (Math.Abs(newOffsetBackwards - caretOffset) < Math.Abs(newOffsetForwards - caretOffset))
                {
                    newVisualColumn = newVisualColumnBackwards;
                    newOffset       = newOffsetBackwards;
                }
                else
                {
                    newVisualColumn = newVisualColumnForwards;
                    newOffset       = newOffsetForwards;
                }
            }

            Position = new TextViewPosition(textView.Document.GetLocation(newOffset), newVisualColumn);
        }

        isInVirtualSpace = position.VisualColumn > visualLine.VisualLength;
    }

    private Rect CalcCaretRectangle(VisualLine visualLine)
    {
        if (!visualColumnValid) RevalidateVisualColumn(visualLine);

        var textLine   = visualLine.GetTextLine(position.VisualColumn, position.IsAtEndOfLine);
        var xPos       = visualLine.GetTextLineVisualXPosition(textLine, position.VisualColumn);
        var lineTop    = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.TextTop);
        var lineBottom = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.TextBottom);

        return new Rect(xPos, lineTop, SystemParameters.CaretWidth, lineBottom - lineTop);
    }

    private Rect CalcCaretOverstrikeRectangle(VisualLine visualLine)
    {
        if (!visualColumnValid) RevalidateVisualColumn(visualLine);

        var currentPos = position.VisualColumn;
        var nextPos    = visualLine.GetNextCaretPosition(currentPos, LogicalDirection.Forward, CaretPositioningMode.Normal, true);
        var textLine   = visualLine.GetTextLine(currentPos);

        Rect r;
        if (currentPos < visualLine.VisualLength)
        {
            var textBounds = textLine.GetTextBounds(currentPos, nextPos - currentPos)[0];
            r   =  textBounds.Rectangle;
            r.Y += visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.LineTop);
        }
        else
        {
            var xPos       = visualLine.GetTextLineVisualXPosition(textLine, currentPos);
            var xPos2      = visualLine.GetTextLineVisualXPosition(textLine, nextPos);
            var lineTop    = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.TextTop);
            var lineBottom = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.TextBottom);
            r = new Rect(xPos, lineTop, xPos2 - xPos, lineBottom - lineTop);
        }

        if (r.Width < SystemParameters.CaretWidth) r.Width = SystemParameters.CaretWidth;
        return r;
    }

    public Rect CalculateCaretRectangle()
    {
        if (textView is not { Document: { } }) return Rect.Empty;
        var visualLine = textView.GetOrConstructVisualLine(textView.Document.GetLineByNumber(position.Line));
        return textArea.OverstrikeMode ? CalcCaretOverstrikeRectangle(visualLine) : CalcCaretRectangle(visualLine);
    }

    internal const double MinimumDistanceToViewBorder = 30;

    public void BringCaretToView()
    {
        BringCaretToView(MinimumDistanceToViewBorder);
    }

    internal void BringCaretToView(double border)
    {
        var caretRectangle = CalculateCaretRectangle();
        if (caretRectangle.IsEmpty) return;
        caretRectangle.Inflate(border, border);
        textView.MakeVisible(caretRectangle);
    }

    public void Show()
    {
        Log("Caret.Show()");
        visible = true;
        if (showScheduled) return;
        showScheduled = true;
        textArea.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(ShowInternal));
    }

    private bool showScheduled;
    private bool hasWin32Caret;

    private void ShowInternal()
    {
        showScheduled = false;

        if (!visible) return;

        if (caretAdorner == null || textView == null) return;
        var visualLine = textView.GetVisualLine(position.Line);
        if (visualLine != null)
        {
            var caretRect                     = textArea.OverstrikeMode ? CalcCaretOverstrikeRectangle(visualLine) : CalcCaretRectangle(visualLine);
            if (!hasWin32Caret) hasWin32Caret = Win32.CreateCaret(textView, caretRect.Size);
            if (hasWin32Caret) Win32.SetCaretPosition(textView, caretRect.Location - textView.ScrollOffset);
            caretAdorner.Show(caretRect);
        }
        else
        {
            caretAdorner.Hide();
        }
    }

    public void Hide()
    {
        Log("Caret.Hide()");
        visible = false;
        if (hasWin32Caret)
        {
            Win32.DestroyCaret();
            hasWin32Caret = false;
        }

        caretAdorner?.Hide();
    }

    [Conditional("DEBUG")]
    private static void Log(string text)
    {
    }

    public Brush CaretBrush { get => caretAdorner.CaretBrush; set => caretAdorner.CaretBrush = value; }
}
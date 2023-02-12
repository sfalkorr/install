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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing;

/// <summary>
///     Margin showing line numbers.
/// </summary>
public class LineNumberMargin : AbstractMargin, IWeakEventListener
{
    /// <summary>
    ///     Creates a new instance of a LineNumberMargin
    /// </summary>
    public LineNumberMargin()
    {
        // override Property Value Inheritance, and always render
        // the line number margin left-to-right
        FlowDirection = FlowDirection.LeftToRight;
    }

    static LineNumberMargin()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LineNumberMargin), new FrameworkPropertyMetadata(typeof(LineNumberMargin)));
    }

    private TextArea textArea;

    /// <summary>
    ///     The typeface used for rendering the line number margin.
    ///     This field is calculated in MeasureOverride() based on the FontFamily etc. properties.
    /// </summary>
    protected Typeface typeface;

    /// <summary>
    ///     The font size used for rendering the line number margin.
    ///     This field is calculated in MeasureOverride() based on the FontFamily etc. properties.
    /// </summary>
    protected double emSize;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        typeface = this.CreateTypeface();
        emSize   = (double)GetValue(TextBlock.FontSizeProperty);

        var text = TextFormatterFactory.CreateFormattedText(this, new string('9', maxLineNumberLength), typeface, emSize, (Brush)GetValue(Control.ForegroundProperty));
        return new Size(text.Width, 0);
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        var textView   = TextView;
        var renderSize = RenderSize;
        if (textView != null && textView.VisualLinesValid)
        {
            var foreground = (Brush)GetValue(Control.ForegroundProperty);
            foreach (var line in textView.VisualLines)
            {
                var lineNumber = line.FirstDocumentLine.LineNumber;
                var text       = TextFormatterFactory.CreateFormattedText(this, lineNumber.ToString(CultureInfo.CurrentCulture), typeface, emSize, foreground);
                var y          = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                drawingContext.DrawText(text, new Point(renderSize.Width - text.Width, y - textView.VerticalOffset));
            }
        }
    }

    /// <inheritdoc />
    protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
    {
        if (oldTextView != null) oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
        base.OnTextViewChanged(oldTextView, newTextView);
        if (newTextView != null)
        {
            newTextView.VisualLinesChanged += TextViewVisualLinesChanged;

            // find the text area belonging to the new text view
            textArea = newTextView.GetService(typeof(TextArea)) as TextArea;
        }
        else
        {
            textArea = null;
        }

        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnDocumentChanged(TextDocument oldDocument, TextDocument newDocument)
    {
        if (oldDocument != null) PropertyChangedEventManager.RemoveListener(oldDocument, this, "LineCount");
        base.OnDocumentChanged(oldDocument, newDocument);
        if (newDocument != null) PropertyChangedEventManager.AddListener(newDocument, this, "LineCount");
        OnDocumentLineCountChanged();
    }

    /// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent" />
    protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        if (managerType == typeof(PropertyChangedEventManager))
        {
            OnDocumentLineCountChanged();
            return true;
        }

        return false;
    }

    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        return ReceiveWeakEvent(managerType, sender, e);
    }

    /// <summary>
    ///     Maximum length of a line number, in characters
    /// </summary>
    protected int maxLineNumberLength = 1;

    private void OnDocumentLineCountChanged()
    {
        var documentLineCount = Document != null ? Document.LineCount : 1;
        var newLength         = documentLineCount.ToString(CultureInfo.CurrentCulture).Length;

        // The margin looks too small when there is only one digit, so always reserve space for
        // at least two digits
        if (newLength < 2) newLength = 2;

        if (newLength != maxLineNumberLength)
        {
            maxLineNumberLength = newLength;
            InvalidateMeasure();
        }
    }

    private void TextViewVisualLinesChanged(object sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private AnchorSegment selectionStart;
    private bool          selecting;

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!e.Handled && TextView != null && textArea != null)
        {
            e.Handled = true;
            textArea.Focus();

            var currentSeg = GetTextLineSegment(e);
            if (currentSeg == SimpleSegment.Invalid) return;
            textArea.Caret.Offset = currentSeg.Offset + currentSeg.Length;
            if (CaptureMouse())
            {
                selecting      = true;
                selectionStart = new AnchorSegment(Document, currentSeg.Offset, currentSeg.Length);
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    if (textArea.Selection is SimpleSelection simpleSelection)
                        selectionStart = new AnchorSegment(Document, simpleSelection.SurroundingSegment);
                textArea.Selection = Selection.Create(textArea, selectionStart);
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) ExtendSelection(currentSeg);
                textArea.Caret.BringCaretToView(5.0);
            }
        }
    }

    private SimpleSegment GetTextLineSegment(MouseEventArgs e)
    {
        var pos = e.GetPosition(TextView);
        pos.X =  0;
        pos.Y =  pos.Y.CoerceValue(0, TextView.ActualHeight);
        pos.Y += TextView.VerticalOffset;
        var vl = TextView.GetVisualLineFromVisualTop(pos.Y);
        if (vl == null) return SimpleSegment.Invalid;
        var tl                                                                              = vl.GetTextLineByVisualYPosition(pos.Y);
        var visualStartColumn                                                               = vl.GetTextLineVisualStartColumn(tl);
        var visualEndColumn                                                                 = visualStartColumn + tl.Length;
        var relStart                                                                        = vl.FirstDocumentLine.Offset;
        var startOffset                                                                     = vl.GetRelativeOffset(visualStartColumn) + relStart;
        var endOffset                                                                       = vl.GetRelativeOffset(visualEndColumn) + relStart;
        if (endOffset == vl.LastDocumentLine.Offset + vl.LastDocumentLine.Length) endOffset += vl.LastDocumentLine.DelimiterLength;
        return new SimpleSegment(startOffset, endOffset - startOffset);
    }

    private void ExtendSelection(SimpleSegment currentSeg)
    {
        if (currentSeg.Offset < selectionStart.Offset)
        {
            textArea.Caret.Offset = currentSeg.Offset;
            textArea.Selection    = Selection.Create(textArea, currentSeg.Offset, selectionStart.Offset + selectionStart.Length);
        }
        else
        {
            textArea.Caret.Offset = currentSeg.Offset + currentSeg.Length;
            textArea.Selection    = Selection.Create(textArea, selectionStart.Offset, currentSeg.Offset + currentSeg.Length);
        }
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (selecting && textArea != null && TextView != null)
        {
            e.Handled = true;
            var currentSeg = GetTextLineSegment(e);
            if (currentSeg == SimpleSegment.Invalid) return;
            ExtendSelection(currentSeg);
            textArea.Caret.BringCaretToView(5.0);
        }

        base.OnMouseMove(e);
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (selecting)
        {
            selecting      = false;
            selectionStart = null;
            ReleaseMouseCapture();
            e.Handled = true;
        }

        base.OnMouseLeftButtonUp(e);
    }

    /// <inheritdoc />
    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        // accept clicks even when clicking on the background
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }
}
﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

internal sealed class SelectionMouseHandler : ITextAreaInputHandler
{
    private readonly TextArea textArea;

    private MouseSelectionMode mode;
    private AnchorSegment      startWord;
    private Point              possibleDragStartMousePos;

    #region Constructor + Attach + Detach

    internal SelectionMouseHandler(TextArea textArea)
    {
        this.textArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
    }

    static SelectionMouseHandler()
    {
        EventManager.RegisterClassHandler(typeof(TextArea), Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCapture));
    }

    private static void OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        var textArea = (TextArea)sender;
        if (Equals(Mouse.Captured, textArea)) return;
        if (textArea.DefaultInputHandler.MouseSelection is SelectionMouseHandler handler) handler.mode = MouseSelectionMode.None;
    }

    TextArea ITextAreaInputHandler.TextArea => textArea;

    void ITextAreaInputHandler.Attach()
    {
        textArea.MouseLeftButtonDown += textArea_MouseLeftButtonDown;
        textArea.MouseMove           += textArea_MouseMove;
        textArea.MouseLeftButtonUp   += textArea_MouseLeftButtonUp;
        textArea.QueryCursor         += textArea_QueryCursor;
        textArea.DocumentChanged     += textArea_DocumentChanged;
        textArea.OptionChanged       += textArea_OptionChanged;

        enableTextDragDrop = textArea.Options.EnableTextDragDrop;
        //if (enableTextDragDrop) AttachDragDrop();
    }

    void ITextAreaInputHandler.Detach()
    {
        mode                         =  MouseSelectionMode.None;
        textArea.MouseLeftButtonDown -= textArea_MouseLeftButtonDown;
        textArea.MouseMove           -= textArea_MouseMove;
        textArea.MouseLeftButtonUp   -= textArea_MouseLeftButtonUp;
        textArea.QueryCursor         -= textArea_QueryCursor;
        textArea.DocumentChanged     -= textArea_DocumentChanged;
        textArea.OptionChanged       -= textArea_OptionChanged;
        //if (enableTextDragDrop) DetachDragDrop();
    }

    // private void AttachDragDrop()
    // {
    //     textArea.AllowDrop         =  true;
    //     textArea.GiveFeedback      += textArea_GiveFeedback;
    //     textArea.QueryContinueDrag += textArea_QueryContinueDrag;
    //     textArea.DragEnter         += textArea_DragEnter;
    //     textArea.DragOver          += textArea_DragOver;
    //     textArea.DragLeave         += textArea_DragLeave;
    //     textArea.Drop              += textArea_Drop;
    // }
    //
    // private void DetachDragDrop()
    // {
    //     textArea.AllowDrop         =  false;
    //     textArea.GiveFeedback      -= textArea_GiveFeedback;
    //     textArea.QueryContinueDrag -= textArea_QueryContinueDrag;
    //     textArea.DragEnter         -= textArea_DragEnter;
    //     textArea.DragOver          -= textArea_DragOver;
    //     textArea.DragLeave         -= textArea_DragLeave;
    //     textArea.Drop              -= textArea_Drop;
    // }

    private bool enableTextDragDrop;

    private void textArea_OptionChanged(object sender, PropertyChangedEventArgs e)
    {
        var newEnableTextDragDrop = textArea.Options.EnableTextDragDrop;
        if (newEnableTextDragDrop == enableTextDragDrop) return;
        enableTextDragDrop = newEnableTextDragDrop;
        //if (newEnableTextDragDrop) AttachDragDrop();
        //else DetachDragDrop();
    }

    private void textArea_DocumentChanged(object sender, EventArgs e)
    {
        if (mode != MouseSelectionMode.None)
        {
            mode = MouseSelectionMode.None;
            textArea.ReleaseMouseCapture();
        }

        startWord = null;
    }

    #endregion

    #region Dropping text

    // [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    // private void textArea_DragEnter(object sender, DragEventArgs e)
    // {
    //     try
    //     {
    //         e.Effects = GetEffect(e);
    //         textArea.Caret.Show();
    //     }
    //     catch (Exception ex)
    //     {
    //         OnDragException(ex);
    //     }
    // }
    //
    // [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    // private void textArea_DragOver(object sender, DragEventArgs e)
    // {
    //     try
    //     {
    //         e.Effects = GetEffect(e);
    //     }
    //     catch (Exception ex)
    //     {
    //         OnDragException(ex);
    //     }
    // }

    // private DragDropEffects GetEffect(DragEventArgs e)
    // {
    //     if (!e.Data.GetDataPresent(DataFormats.UnicodeText, true)) return DragDropEffects.None;
    //     e.Handled = true;
    //     var offset = GetOffsetFromMousePosition(e.GetPosition(textArea.TextView), out var visualColumn, out var isAtEndOfLine);
    //     if (offset < 0) return DragDropEffects.None;
    //     textArea.Caret.Position    = new TextViewPosition(textArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
    //     textArea.Caret.DesiredXPos = double.NaN;
    //     if (!textArea.ReadOnlySectionProvider.CanInsert(offset)) return DragDropEffects.None;
    //     if ((e.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move && (e.KeyStates & DragDropKeyStates.ControlKey) != DragDropKeyStates.ControlKey) return DragDropEffects.Move;
    //     return e.AllowedEffects & DragDropEffects.Copy;
    // }
    //
    // [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    // private void textArea_DragLeave(object sender, DragEventArgs e)
    // {
    //     try
    //     {
    //         e.Handled = true;
    //         if (!textArea.IsKeyboardFocusWithin) textArea.Caret.Hide();
    //     }
    //     catch (Exception ex)
    //     {
    //         OnDragException(ex);
    //     }
    // }

    // [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    // private void textArea_Drop(object sender, DragEventArgs e)
    // {
    //     try
    //     {
    //         var effect = GetEffect(e);
    //         e.Effects = effect;
    //         if (effect != DragDropEffects.None)
    //         {
    //             var start = textArea.Caret.Offset;
    //             if (mode == MouseSelectionMode.Drag && textArea.Selection.Contains(start))
    //             {
    //                 Debug.WriteLine("Drop: did not drop: drop target is inside selection");
    //                 e.Effects = DragDropEffects.None;
    //             }
    //             else
    //             {
    //                 Debug.WriteLine("Drop: insert at " + start);
    //
    //                 var pastingEventArgs = new DataObjectPastingEventArgs(e.Data, true, DataFormats.UnicodeText);
    //                 textArea.RaiseEvent(pastingEventArgs);
    //                 if (pastingEventArgs.CommandCancelled) return;
    //
    //                 var rectangular = pastingEventArgs.DataObject.GetDataPresent(RectangleSelection.RectangularSelectionDataType);
    //             }
    //
    //             e.Handled = true;
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         OnDragException(ex);
    //     }
    // }

    // private void OnDragException(Exception ex)
    // {
    // }

    // [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    // private void textArea_GiveFeedback(object sender, GiveFeedbackEventArgs e)
    // {
    //     try
    //     {
    //         e.UseDefaultCursors = true;
    //         e.Handled           = true;
    //     }
    //     catch (Exception ex)
    //     {
    //         OnDragException(ex);
    //     }
    // }

    // [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
    // private void textArea_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
    // {
    //     try
    //     {
    //         if (e.EscapePressed) e.Action                                                                             = DragAction.Cancel;
    //         else if ((e.KeyStates & DragDropKeyStates.LeftMouseButton) != DragDropKeyStates.LeftMouseButton) e.Action = DragAction.Drop;
    //         else e.Action                                                                                             = DragAction.Continue;
    //         e.Handled = true;
    //     }
    //     catch (Exception ex)
    //     {
    //         OnDragException(ex);
    //     }
    // }

    #endregion

    #region Start Drag

    private void StartDrag()
    {
        textArea.ReleaseMouseCapture();

        mode = MouseSelectionMode.Drag;

        var dataObject = textArea.Selection.CreateDataObject(textArea);

        var deleteOnMove   = textArea.Selection.Segments.Select(s => new AnchorSegment(textArea.Document, s)).ToList();
        var allowedEffects = (from s in deleteOnMove let result = textArea.GetDeletableSegments(s) where result.Length != 1 || result[0].Offset != s.Offset || result[0].EndOffset != s.EndOffset select s).Aggregate(DragDropEffects.All, (current, s) => current & ~DragDropEffects.Move);

        var copyingEventArgs = new DataObjectCopyingEventArgs(dataObject, true);
        textArea.RaiseEvent(copyingEventArgs);
        if (copyingEventArgs.CommandCancelled) return;

        DragDropEffects resultEffect;
        using (textArea.AllowCaretOutsideSelection())
        {
            var oldCaretPosition = textArea.Caret.Position;
            try
            {
                Debug.WriteLine("DoDragDrop with allowedEffects=" + allowedEffects);
                resultEffect = DragDrop.DoDragDrop(textArea, dataObject, allowedEffects);
                Debug.WriteLine("DoDragDrop done, resultEffect=" + resultEffect);
            }
            catch (COMException ex)
            {
                Debug.WriteLine("DoDragDrop failed: " + ex);
                return;
            }

            if (resultEffect == DragDropEffects.None) textArea.Caret.Position = oldCaretPosition;
        }

        if (resultEffect != DragDropEffects.Move || (allowedEffects & DragDropEffects.Move) != DragDropEffects.Move) return;
        {
            textArea.Document.BeginUpdate();
            try
            {
                foreach (var s in deleteOnMove) textArea.Document.Remove(s.Offset, s.Length);
            }
            finally
            {
                textArea.Document.EndUpdate();
            }
        }
    }

    #endregion

    #region QueryCursor

    private void textArea_QueryCursor(object sender, QueryCursorEventArgs e)
    {
        if (e.Handled) return;
        if (mode != MouseSelectionMode.None)
        {
            e.Cursor  = Cursors.IBeam;
            e.Handled = true;
        }
        else if (textArea.TextView.VisualLinesValid)
        {
            var p = e.GetPosition(textArea.TextView);
            if (p is not { X: >= 0, Y: >= 0 } || !(p.X <= textArea.TextView.ActualWidth) || !(p.Y <= textArea.TextView.ActualHeight)) return;
            var offset = GetOffsetFromMousePosition(e, out _, out _);
            if (enableTextDragDrop && textArea.Selection.Contains(offset)) e.Cursor = Cursors.Arrow;
            else e.Cursor                                                           = Cursors.IBeam;
            e.Handled = true;
        }
    }

    #endregion

    #region LeftButtonDown

    private void textArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        mode = MouseSelectionMode.None;
        if (textArea.Document == null) return;
        if (!e.Handled && e.ChangedButton == MouseButton.Left)
        {
            var modifiers = Keyboard.Modifiers;
            var shift     = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            if (enableTextDragDrop && e.ClickCount == 1 && !shift)
            {
                var offset = GetOffsetFromMousePosition(e, out _, out _);
                if (textArea.Selection.Contains(offset))
                {
                    if (textArea.CaptureMouse())
                    {
                        mode                      = MouseSelectionMode.PossibleDragStart;
                        possibleDragStartMousePos = e.GetPosition(textArea);
                    }

                    e.Handled = true;
                    return;
                }
            }

            var oldPosition = textArea.Caret.Position;
            SetCaretOffsetToMousePosition(e);

            if (!shift) textArea.ClearSelection();

            if (textArea.CaptureMouse())
            {
                if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && textArea.Options.EnableRectangularSelection)
                {
                }
                else if (e.ClickCount == 1 && (modifiers & ModifierKeys.Control) == 0)
                {
                    mode = MouseSelectionMode.Normal;
                    if (shift && textArea.Selection is RectangleSelection) textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
                }
                else
                {
                    SimpleSegment startWord;
                    if (e.ClickCount == 3)
                    {
                        mode      = MouseSelectionMode.WholeLine;
                        startWord = GetLineAtMousePosition(e);
                    }
                    else
                    {
                        mode      = MouseSelectionMode.WholeWord;
                        startWord = GetWordAtMousePosition(e);
                    }

                    if (startWord == SimpleSegment.Invalid)
                    {
                        mode = MouseSelectionMode.None;
                        textArea.ReleaseMouseCapture();
                        return;
                    }

                    if (shift && !textArea.Selection.IsEmpty)
                    {
                        if (startWord.Offset < textArea.Selection.SurroundingSegment.Offset) textArea.Selection            = textArea.Selection.SetEndpoint(new TextViewPosition(textArea.Document.GetLocation(startWord.Offset)));
                        else if (startWord.EndOffset > textArea.Selection.SurroundingSegment.EndOffset) textArea.Selection = textArea.Selection.SetEndpoint(new TextViewPosition(textArea.Document.GetLocation(startWord.EndOffset)));
                        this.startWord = new AnchorSegment(textArea.Document, textArea.Selection.SurroundingSegment);
                    }
                }
            }
        }

        e.Handled = true;
    }

    public MouseSelectionMode MouseSelectionMode
    {
        get => mode;
        set
        {
            if (mode == value) return;
            if (value == MouseSelectionMode.None)
            {
                mode = MouseSelectionMode.None;
                textArea.ReleaseMouseCapture();
            }
            else if (textArea.CaptureMouse())
            {
                switch (value)
                {
                    case MouseSelectionMode.Normal:
                    case MouseSelectionMode.Rectangular:
                        mode = value;
                        break;
                    default:
                        throw new NotImplementedException("Programmatically starting mouse selection is only supported for normal and rectangular selections.");
                }
            }
        }
    }

    #endregion

    #region Mouse Position <-> Text coordinates

    private SimpleSegment GetWordAtMousePosition(MouseEventArgs e)
    {
        var textView = textArea.TextView;
        if (textView == null) return SimpleSegment.Invalid;
        var pos                                  = e.GetPosition(textView);
        if (pos.Y < 0) pos.Y                     = 0;
        if (pos.Y > textView.ActualHeight) pos.Y = textView.ActualHeight;
        pos += textView.ScrollOffset;
        var line = textView.GetVisualLineFromVisualTop(pos.Y);
        if (line == null) return SimpleSegment.Invalid;
        var visualColumn                   = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace);
        var wordStartVC                    = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
        if (wordStartVC == -1) wordStartVC = 0;
        var wordEndVC                      = line.GetNextCaretPosition(wordStartVC, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, textArea.Selection.EnableVirtualSpace);
        if (wordEndVC == -1) wordEndVC     = line.VisualLength;
        var relOffset                      = line.FirstDocumentLine.Offset;
        var wordStartOffset                = line.GetRelativeOffset(wordStartVC) + relOffset;
        var wordEndOffset                  = line.GetRelativeOffset(wordEndVC) + relOffset;
        return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
    }

    private SimpleSegment GetLineAtMousePosition(MouseEventArgs e)
    {
        var textView = textArea.TextView;
        if (textView == null) return SimpleSegment.Invalid;
        var pos                                  = e.GetPosition(textView);
        if (pos.Y < 0) pos.Y                     = 0;
        if (pos.Y > textView.ActualHeight) pos.Y = textView.ActualHeight;
        pos += textView.ScrollOffset;
        var line = textView.GetVisualLineFromVisualTop(pos.Y);
        if (line != null) return new SimpleSegment(line.StartOffset, line.LastDocumentLine.EndOffset - line.StartOffset);
        return SimpleSegment.Invalid;
    }

    private int GetOffsetFromMousePosition(MouseEventArgs e, out int visualColumn, out bool isAtEndOfLine)
    {
        return GetOffsetFromMousePosition(e.GetPosition(textArea.TextView), out visualColumn, out isAtEndOfLine);
    }

    private int GetOffsetFromMousePosition(Point positionRelativeToTextView, out int visualColumn, out bool isAtEndOfLine)
    {
        visualColumn = 0;
        var textView                             = textArea.TextView;
        var pos                                  = positionRelativeToTextView;
        if (pos.Y < 0) pos.Y                     = 0;
        if (pos.Y > textView.ActualHeight) pos.Y = textView.ActualHeight;
        pos += textView.ScrollOffset;
        if (pos.Y >= textView.DocumentHeight) pos.Y = textView.DocumentHeight - ExtensionMethods.Epsilon;
        var line                                    = textView.GetVisualLineFromVisualTop(pos.Y);
        if (line != null)
        {
            visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace, out isAtEndOfLine);
            return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
        }

        isAtEndOfLine = false;
        return -1;
    }

    private int GetOffsetFromMousePositionFirstTextLineOnly(Point positionRelativeToTextView, out int visualColumn)
    {
        visualColumn = 0;
        var textView                             = textArea.TextView;
        var pos                                  = positionRelativeToTextView;
        if (pos.Y < 0) pos.Y                     = 0;
        if (pos.Y > textView.ActualHeight) pos.Y = textView.ActualHeight;
        pos += textView.ScrollOffset;
        if (pos.Y >= textView.DocumentHeight) pos.Y = textView.DocumentHeight - ExtensionMethods.Epsilon;
        var line                                    = textView.GetVisualLineFromVisualTop(pos.Y);
        if (line != null)
        {
            visualColumn = line.GetVisualColumn(line.TextLines.First(), pos.X, textArea.Selection.EnableVirtualSpace);
            return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
        }

        return -1;
    }

    #endregion

    #region MouseMove

    private void textArea_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Handled) return;
        if (mode == MouseSelectionMode.Normal || mode == MouseSelectionMode.WholeWord || mode == MouseSelectionMode.WholeLine || mode == MouseSelectionMode.Rectangular)
        {
            e.Handled = true;
            if (textArea.TextView.VisualLinesValid) ExtendSelectionToMouse(e);
        }
        else if (mode == MouseSelectionMode.PossibleDragStart)
        {
            e.Handled = true;
            var mouseMovement = e.GetPosition(textArea) - possibleDragStartMousePos;
            if (Math.Abs(mouseMovement.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(mouseMovement.Y) > SystemParameters.MinimumVerticalDragDistance) StartDrag();
        }
    }

    #endregion

    #region ExtendSelection

    private void SetCaretOffsetToMousePosition(MouseEventArgs e, ISegment allowedSegment = null)
    {
        int  visualColumn;
        bool isAtEndOfLine;
        int  offset;
        if (mode == MouseSelectionMode.Rectangular)
        {
            offset        = GetOffsetFromMousePositionFirstTextLineOnly(e.GetPosition(textArea.TextView), out visualColumn);
            isAtEndOfLine = true;
        }
        else
        {
            offset = GetOffsetFromMousePosition(e, out visualColumn, out isAtEndOfLine);
        }

        if (allowedSegment != null) offset = offset.CoerceValue(allowedSegment.Offset, allowedSegment.EndOffset);
        if (offset < 0) return;
        textArea.Caret.Position    = new TextViewPosition(textArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
        textArea.Caret.DesiredXPos = double.NaN;
    }

    private void ExtendSelectionToMouse(MouseEventArgs e)
    {
        var oldPosition = textArea.Caret.Position;
        switch (mode)
        {
            case MouseSelectionMode.Normal:
            case MouseSelectionMode.Rectangular:
                SetCaretOffsetToMousePosition(e);
                textArea.Selection = mode switch
                                     {
                                         MouseSelectionMode.Normal when textArea.Selection is RectangleSelection          => new SimpleSelection(textArea, oldPosition, textArea.Caret.Position),
                                         MouseSelectionMode.Rectangular when textArea.Selection is not RectangleSelection => new RectangleSelection(textArea, oldPosition, textArea.Caret.Position),
                                         _                                                                                => textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position)
                                     };
                break;
            case MouseSelectionMode.WholeWord:
            case MouseSelectionMode.WholeLine:
            {
                var newWord = mode == MouseSelectionMode.WholeLine ? GetLineAtMousePosition(e) : GetWordAtMousePosition(e);
                if (newWord != SimpleSegment.Invalid)
                {
                }

                break;
            }
            case MouseSelectionMode.None:
                break;
            case MouseSelectionMode.PossibleDragStart:
                break;
            case MouseSelectionMode.Drag:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        textArea.Caret.BringCaretToView(5.0);
    }

    #endregion

    #region MouseLeftButtonUp

    private void textArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (mode == MouseSelectionMode.None || e.Handled) return;
        e.Handled = true;
        if (mode == MouseSelectionMode.PossibleDragStart)
        {
            SetCaretOffsetToMousePosition(e);
            textArea.ClearSelection();
        }
        else if (mode is MouseSelectionMode.Normal or MouseSelectionMode.WholeWord or MouseSelectionMode.WholeLine or MouseSelectionMode.Rectangular)
        {
            ExtendSelectionToMouse(e);
        }

        mode = MouseSelectionMode.None;
        textArea.ReleaseMouseCapture();
    }

    #endregion
}
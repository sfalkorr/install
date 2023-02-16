using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using AvalonEdit.Utils;

namespace AvalonEdit.Document;

public sealed class TextDocument : IDocument, INotifyPropertyChanged
{
    #region Thread ownership

    private readonly object lockObject = new();
    private          Thread owner      = Thread.CurrentThread;

    public void VerifyAccess()
    {
        if (Thread.CurrentThread != owner) throw new InvalidOperationException("TextDocument can be accessed only from the thread that owns it.");
    }

    // public void SetOwnerThread(Thread newOwner)
    // {
    //     lock (lockObject)
    //     {
    //         if (owner != null) VerifyAccess();
    //         owner = newOwner;
    //     }
    // }

    #endregion

    #region Fields + Constructor

    private readonly Rope<char>       rope;
    private readonly DocumentLineTree lineTree;
    private readonly LineManager      lineManager;

    private readonly TextSourceVersionProvider versionProvider = new();

    public TextDocument() : this(string.Empty)
    {
    }

    public TextDocument(IEnumerable<char> initialText)
    {
        if (initialText == null) throw new ArgumentNullException(nameof(initialText));
        rope        = new Rope<char>(initialText);
        lineTree    = new DocumentLineTree(this);
        lineManager = new LineManager(lineTree, this);
        lineTrackers.CollectionChanged += delegate
        {
            lineManager.UpdateListOfLineTrackers();
        };

        FireChangeEvents();
    }

    // public TextDocument(ITextSource initialText) : this(GetTextFromTextSource(initialText))
    // {
    // }

    // private static IEnumerable<char> GetTextFromTextSource(ITextSource textSource)
    // {
    //     if (textSource == null) throw new ArgumentNullException(nameof(textSource));
    //
    //     if (textSource is RopeTextSource rts) return rts.GetRope();
    //
    //     if (textSource is TextDocument doc) return doc.rope;
    //
    //     return textSource.Text;
    // }

    #endregion

    #region Text

    private void ThrowIfRangeInvalid(int offset, int length)
    {
        if (offset < 0 || offset > rope.Length) throw new ArgumentOutOfRangeException(nameof(offset), offset, "0 <= offset <= " + rope.Length.ToString(CultureInfo.InvariantCulture));
        if (length < 0 || offset + length > rope.Length) throw new ArgumentOutOfRangeException(nameof(length), length, "0 <= length, offset(" + offset + ")+length <= " + rope.Length.ToString(CultureInfo.InvariantCulture));
    }

    public string GetText(int offset, int length)
    {
        VerifyAccess();
        return rope.ToString(offset, length);
    }

    public string GetText(ISegment segment)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        return GetText(segment.Offset, segment.Length);
    }

    public int IndexOf(char c, int startIndex, int count)
    {
        DebugVerifyAccess();
        return rope.IndexOf(c, startIndex, count);
    }

    public int LastIndexOf(char c, int startIndex, int count)
    {
        DebugVerifyAccess();
        return rope.LastIndexOf(c, startIndex, count);
    }

    public int IndexOfAny(char[] anyOf, int startIndex, int count)
    {
        DebugVerifyAccess();
        return rope.IndexOfAny(anyOf, startIndex, count);
    }

    public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
    {
        DebugVerifyAccess();
        return rope.IndexOf(searchText, startIndex, count, comparisonType);
    }

    public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
    {
        DebugVerifyAccess();
        return rope.LastIndexOf(searchText, startIndex, count, comparisonType);
    }

    public char GetCharAt(int offset)
    {
        DebugVerifyAccess();
        return rope[offset];
    }

    private WeakReference cachedText;

    public string Text
    {
        get
        {
            VerifyAccess();
            var completeText = cachedText?.Target as string;
            if (completeText == null)
            {
                completeText = rope.ToString();
                cachedText   = new WeakReference(completeText);
            }

            return completeText;
        }
        set
        {
            VerifyAccess();
            if (value == null) throw new ArgumentNullException(nameof(value));
            Replace(0, rope.Length, value);
        }
    }

    public event EventHandler TextChanged;

    event EventHandler IDocument.ChangeCompleted { add => TextChanged += value; remove => TextChanged -= value; }

    public int TextLength
    {
        get
        {
            VerifyAccess();
            return rope.Length;
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;

    public event EventHandler<DocumentChangeEventArgs> Changing;

    private event EventHandler<TextChangeEventArgs> textChanging;

    event EventHandler<TextChangeEventArgs> IDocument.TextChanging { add => textChanging += value; remove => textChanging -= value; }

    public event EventHandler<DocumentChangeEventArgs> Changed;

    private event EventHandler<TextChangeEventArgs> textChanged;

    event EventHandler<TextChangeEventArgs> IDocument.TextChanged { add => textChanged += value; remove => textChanged -= value; }

    public ITextSource CreateSnapshot()
    {
        lock (lockObject)
        {
            return new RopeTextSource(rope, versionProvider.CurrentVersion);
        }
    }

    public ITextSource CreateSnapshot(int offset, int length)
    {
        lock (lockObject)
        {
            return new RopeTextSource(rope.GetRange(offset, length));
        }
    }

    public ITextSourceVersion Version => versionProvider.CurrentVersion;

    public TextReader CreateReader()
    {
        lock (lockObject)
        {
            return new RopeTextReader(rope);
        }
    }

    public TextReader CreateReader(int offset, int length)
    {
        lock (lockObject)
        {
            return new RopeTextReader(rope.GetRange(offset, length));
        }
    }

    public void WriteTextTo(TextWriter writer)
    {
        VerifyAccess();
        rope.WriteTo(writer, 0, rope.Length);
    }

    public void WriteTextTo(TextWriter writer, int offset, int length)
    {
        VerifyAccess();
        rope.WriteTo(writer, offset, length);
    }

    #endregion

    #region BeginUpdate / EndUpdate

    private int beginUpdateCount;

    public bool IsInUpdate
    {
        get
        {
            VerifyAccess();
            return beginUpdateCount > 0;
        }
    }

    public IDisposable RunUpdate()
    {
        BeginUpdate();
        return new CallbackOnDispose(EndUpdate);
    }

    public void BeginUpdate()
    {
        VerifyAccess();
        if (inDocumentChanging) throw new InvalidOperationException("Cannot change document within another document change.");
        beginUpdateCount++;
        if (beginUpdateCount != 1) return;
        UpdateStarted?.Invoke(this, EventArgs.Empty);
    }

    public void EndUpdate()
    {
        VerifyAccess();
        if (inDocumentChanging) throw new InvalidOperationException("Cannot end update within document change.");
        if (beginUpdateCount == 0) throw new InvalidOperationException("No update is active.");
        if (beginUpdateCount == 1)
        {
            FireChangeEvents();

            beginUpdateCount = 0;
            UpdateFinished?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            beginUpdateCount -= 1;
        }
    }

    public event EventHandler UpdateStarted;

    public event EventHandler UpdateFinished;

    void IDocument.StartUndoableAction()
    {
        BeginUpdate();
    }

    void IDocument.EndUndoableAction()
    {
        EndUpdate();
    }

    IDisposable IDocument.OpenUndoGroup()
    {
        return RunUpdate();
    }

    #endregion

    #region Fire events after update

    private int  oldTextLength;
    private int  oldLineCount;
    private bool fireTextChanged;

    internal void FireChangeEvents()
    {
        while (fireTextChanged)
        {
            fireTextChanged = false;
            TextChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged("Text");

            var textLength = rope.Length;
            if (textLength != oldTextLength)
            {
                oldTextLength = textLength;
                OnPropertyChanged("TextLength");
            }

            var lineCount = lineTree.LineCount;
            if (lineCount == oldLineCount) continue;
            oldLineCount = lineCount;
            OnPropertyChanged("LineCount");
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Insert / Remove  / Replace

    public void Insert(int offset, string text)
    {
        Replace(offset, 0, new StringTextSource(text), null);
    }

    public void Insert(int offset, ITextSource text)
    {
        Replace(offset, 0, text, null);
    }

    public void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType)
    {
        if (defaultAnchorMovementType == AnchorMovementType.BeforeInsertion) Replace(offset, 0, new StringTextSource(text), OffsetChangeMappingType.KeepAnchorBeforeInsertion);
        else Replace(offset, 0, new StringTextSource(text), null);
    }

    public void Insert(int offset, ITextSource text, AnchorMovementType defaultAnchorMovementType)
    {
        if (defaultAnchorMovementType == AnchorMovementType.BeforeInsertion) Replace(offset, 0, text, OffsetChangeMappingType.KeepAnchorBeforeInsertion);
        else Replace(offset, 0, text, null);
    }

    public void Remove(ISegment segment)
    {
        Replace(segment, string.Empty);
    }

    public void Remove(int offset, int length)
    {
        Replace(offset, length, StringTextSource.Empty);
    }

    internal bool inDocumentChanging;

    public void Replace(ISegment segment, string text)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        Replace(segment.Offset, segment.Length, new StringTextSource(text), null);
    }

    public void Replace(ISegment segment, ITextSource text)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        Replace(segment.Offset, segment.Length, text, null);
    }

    public void Replace(int offset, int length, string text)
    {
        Replace(offset, length, new StringTextSource(text), null);
    }

    public void Replace(int offset, int length, ITextSource text)
    {
        Replace(offset, length, text, null);
    }

    public void Replace(int offset, int length, string text, OffsetChangeMappingType offsetChangeMappingType)
    {
        Replace(offset, length, new StringTextSource(text), offsetChangeMappingType);
    }

    public void Replace(int offset, int length, ITextSource text, OffsetChangeMappingType offsetChangeMappingType)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        switch (offsetChangeMappingType)
        {
            case OffsetChangeMappingType.Normal:
                Replace(offset, length, text, null);
                break;
            case OffsetChangeMappingType.KeepAnchorBeforeInsertion:
                Replace(offset, length, text, OffsetChangeMap.FromSingleElement(new OffsetChangeMapEntry(offset, length, text.TextLength, false, true)));
                break;
            case OffsetChangeMappingType.RemoveAndInsert:
                if (length == 0 || text.TextLength == 0)
                {
                    Replace(offset, length, text, null);
                }
                else
                {
                    var map = new OffsetChangeMap(2) { new(offset, length, 0), new(offset, 0, text.TextLength) };
                    map.Freeze();
                    Replace(offset, length, text, map);
                }

                break;
            case OffsetChangeMappingType.CharacterReplace:
                if (length == 0 || text.TextLength == 0)
                {
                    Replace(offset, length, text, null);
                }
                else if (text.TextLength > length)
                {
                    var entry = new OffsetChangeMapEntry(offset + length - 1, 1, 1 + text.TextLength - length);
                    Replace(offset, length, text, OffsetChangeMap.FromSingleElement(entry));
                }
                else if (text.TextLength < length)
                {
                    var entry = new OffsetChangeMapEntry(offset + text.TextLength, length - text.TextLength, 0, true, false);
                    Replace(offset, length, text, OffsetChangeMap.FromSingleElement(entry));
                }
                else
                {
                    Replace(offset, length, text, OffsetChangeMap.Empty);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(offsetChangeMappingType), offsetChangeMappingType, "Invalid enum value");
        }
    }

    public void Replace(int offset, int length, string text, OffsetChangeMap offsetChangeMap)
    {
        Replace(offset, length, new StringTextSource(text), offsetChangeMap);
    }

    public void Replace(int offset, int length, ITextSource text, OffsetChangeMap offsetChangeMap)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        text = text.CreateSnapshot();
        offsetChangeMap?.Freeze();

        BeginUpdate();
        try
        {
            inDocumentChanging = true;
            try
            {
                ThrowIfRangeInvalid(offset, length);

                DoReplace(offset, length, text, offsetChangeMap);
            }
            finally
            {
                inDocumentChanging = false;
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    private void DoReplace(int offset, int length, ITextSource newText, OffsetChangeMap offsetChangeMap)
    {
        if (length == 0 && newText.TextLength == 0) return;

        if (length == 1 && newText.TextLength == 1 && offsetChangeMap == null) offsetChangeMap = OffsetChangeMap.Empty;

        ITextSource removedText;
        if (length == 0) removedText       = StringTextSource.Empty;
        else if (length < 100) removedText = new StringTextSource(rope.ToString(offset, length));
        else removedText                   = new RopeTextSource(rope.GetRange(offset, length));
        var args = new DocumentChangeEventArgs(offset, removedText, newText, offsetChangeMap);

        Changing?.Invoke(this, args);
        textChanging?.Invoke(this, args);

        cachedText      = null;
        fireTextChanged = true;
        lock (lockObject)
        {
            versionProvider.AppendChange(args);

            if (offset == 0 && length == rope.Length)
            {
                rope.Clear();
                if (newText is RopeTextSource newRopeTextSource) rope.InsertRange(0, newRopeTextSource.GetRope());
                else rope.InsertText(0, newText.Text);
                lineManager.Rebuild();
            }
            else
            {
                rope.RemoveRange(offset, length);
                //lineManager.Remove(offset, length);
#if DEBUG
                //lineTree.CheckProperties();
#endif
                if (newText is RopeTextSource newRopeTextSource) rope.InsertRange(offset, newRopeTextSource.GetRope());
                else rope.InsertText(offset, newText.Text);
                lineManager.Insert(offset, newText);
#if DEBUG
                //lineTree.CheckProperties();
#endif
            }
        }

        lineManager.ChangeComplete(args);

        Changed?.Invoke(this, args);
        textChanged?.Invoke(this, args);
    }

    #endregion

    #region GetLineBy...

    public IList<DocumentLine> Lines => lineTree;

    public DocumentLine GetLineByNumber(int number)
    {
        VerifyAccess();
        if (number < 1 || number > lineTree.LineCount) throw new ArgumentOutOfRangeException(nameof(number), number, "Value must be between 1 and " + lineTree.LineCount);
        return lineTree.GetByNumber(number);
    }

    IDocumentLine IDocument.GetLineByNumber(int lineNumber)
    {
        return GetLineByNumber(lineNumber);
    }

    [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
    public DocumentLine GetLineByOffset(int offset)
    {
        VerifyAccess();
        if (offset < 0 || offset > rope.Length) throw new ArgumentOutOfRangeException(nameof(offset), offset, "0 <= offset <= " + rope.Length);
        return lineTree.GetByOffset(offset);
    }

    IDocumentLine IDocument.GetLineByOffset(int offset)
    {
        return GetLineByOffset(offset);
    }

    #endregion

    #region GetOffset / GetLocation

    public int GetOffset(TextLocation location)
    {
        return GetOffset(location.Line, location.Column);
    }

    public int GetOffset(int line, int column)
    {
        var docLine = GetLineByNumber(line);
        if (column <= 0) return docLine.Offset;
        if (column > docLine.Length) return docLine.EndOffset;
        return docLine.Offset + column - 1;
    }

    public TextLocation GetLocation(int offset)
    {
        var line = GetLineByOffset(offset);
        return new TextLocation(line.LineNumber, offset - line.Offset + 1);
    }

    #endregion

    #region Line Trackers

    private readonly ObservableCollection<ILineTracker> lineTrackers = new();

    public IList<ILineTracker> LineTrackers
    {
        get
        {
            VerifyAccess();
            return lineTrackers;
        }
    }

    #endregion

    #region UndoStack

    #endregion

    #region CreateAnchor

    public TextAnchor CreateAnchor(int offset)
    {
        return null;
    }

    ITextAnchor IDocument.CreateAnchor(int offset)
    {
        return CreateAnchor(offset);
    }

    #endregion

    #region LineCount

    public int LineCount
    {
        get
        {
            VerifyAccess();
            return lineTree.LineCount;
        }
    }

    #endregion

    #region Debugging

    [Conditional("DEBUG")]
    internal void DebugVerifyAccess()
    {
        VerifyAccess();
    }

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
//     internal string GetLineTreeAsString()
//     {
// #if DEBUG
//         return lineTree.GetTreeAsString();
// #else
//         return "Not available in release build.";
// #endif
//     }

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
    internal string GetTextAnchorTreeAsString()
    {
#if DEBUG
        return null;
#else
        return "Not available in release build.";
#endif
    }

    #endregion

    #region Service Provider

    private IServiceProvider serviceProvider;

    public IServiceProvider ServiceProvider
    {
        get
        {
            VerifyAccess();
            if (serviceProvider == null)
            {
                var container = new ServiceContainer();
                container.AddService(typeof(IDocument), this);
                container.AddService(typeof(TextDocument), this);
                serviceProvider = container;
            }

            return serviceProvider;
        }
        set
        {
            VerifyAccess();
            serviceProvider = value ?? throw new ArgumentNullException();
        }
    }

    object IServiceProvider.GetService(Type serviceType)
    {
        return ServiceProvider.GetService(serviceType);
    }

    #endregion

    #region FileName

    private string fileName;

    public event EventHandler FileNameChanged;

    private void OnFileNameChanged(EventArgs e)
    {
        var handler = FileNameChanged;
        handler?.Invoke(this, e);
    }

    public string FileName
    {
        get => fileName;
        set
        {
            if (fileName == value) return;
            fileName = value;
            OnFileNameChanged(EventArgs.Empty);
        }
    }

    #endregion
}
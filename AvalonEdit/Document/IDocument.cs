using System;

namespace AvalonEdit.Document;

public interface IDocument : ITextSource, IServiceProvider
{
    new string Text { get; set; }

    event EventHandler<TextChangeEventArgs> TextChanging;

    event EventHandler<TextChangeEventArgs> TextChanged;

    event EventHandler ChangeCompleted;

    int LineCount { get; }

    IDocumentLine GetLineByNumber(int lineNumber);

    IDocumentLine GetLineByOffset(int offset);

    int GetOffset(int line, int column);

    int GetOffset(TextLocation location);

    TextLocation GetLocation(int offset);

    void Insert(int offset, string text);

    void Insert(int offset, ITextSource text);

    void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType);

    void Insert(int offset, ITextSource text, AnchorMovementType defaultAnchorMovementType);

    void Remove(int offset, int length);

    void Replace(int offset, int length, string newText);

    void Replace(int offset, int length, ITextSource newText);

    void StartUndoableAction();

    void EndUndoableAction();

    IDisposable OpenUndoGroup();

    ITextAnchor CreateAnchor(int offset);

    string FileName { get; }

    event EventHandler FileNameChanged;
}

public interface IDocumentLine : ISegment
{
    int TotalLength { get; }

    int DelimiterLength { get; }

    int LineNumber { get; }

    IDocumentLine PreviousLine { get; }

    IDocumentLine NextLine { get; }

    bool IsDeleted { get; }
}

[Serializable]
public class TextChangeEventArgs : EventArgs
{
    public int Offset { get; }

    public ITextSource RemovedText { get; }

    public int RemovalLength => RemovedText.TextLength;

    public ITextSource InsertedText { get; }

    public int InsertionLength => InsertedText.TextLength;

    public TextChangeEventArgs(int offset, string removedText, string insertedText)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset must not be negative");
        Offset       = offset;
        RemovedText  = removedText != null ? new StringTextSource(removedText) : StringTextSource.Empty;
        InsertedText = insertedText != null ? new StringTextSource(insertedText) : StringTextSource.Empty;
    }

    public TextChangeEventArgs(int offset, ITextSource removedText, ITextSource insertedText)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset must not be negative");
        Offset       = offset;
        RemovedText  = removedText ?? StringTextSource.Empty;
        InsertedText = insertedText ?? StringTextSource.Empty;
    }

    public virtual int GetNewOffset(int offset, AnchorMovementType movementType = AnchorMovementType.Default)
    {
        if (offset >= Offset && offset <= Offset + RemovalLength)
        {
            if (movementType == AnchorMovementType.BeforeInsertion) return Offset;
            return Offset + InsertionLength;
        }

        if (offset > Offset) return offset + InsertionLength - RemovalLength;
        return offset;
    }

    public virtual TextChangeEventArgs Invert()
    {
        return new TextChangeEventArgs(Offset, InsertedText, RemovedText);
    }
}
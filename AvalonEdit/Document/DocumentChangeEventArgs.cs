using System;

namespace AvalonEdit.Document;

[Serializable]
public class DocumentChangeEventArgs : TextChangeEventArgs
{
    private volatile OffsetChangeMap offsetChangeMap;

    internal OffsetChangeMapEntry CreateSingleChangeMapEntry()
    {
        return new OffsetChangeMapEntry(Offset, RemovalLength, InsertionLength);
    }

    internal OffsetChangeMap OffsetChangeMapOrNull => offsetChangeMap;

    public override int GetNewOffset(int offset, AnchorMovementType movementType = AnchorMovementType.Default)
    {
        return offsetChangeMap?.GetNewOffset(offset, movementType) ?? CreateSingleChangeMapEntry().GetNewOffset(offset, movementType);
    }

    public DocumentChangeEventArgs(int offset, string removedText, string insertedText, OffsetChangeMap offsetChangeMap = null) : base(offset, removedText, insertedText)
    {
        SetOffsetChangeMap(offsetChangeMap);
    }

    public DocumentChangeEventArgs(int offset, ITextSource removedText, ITextSource insertedText, OffsetChangeMap offsetChangeMap) : base(offset, removedText, insertedText)
    {
        SetOffsetChangeMap(offsetChangeMap);
    }

    private void SetOffsetChangeMap(OffsetChangeMap _offsetChangeMap)
    {
        if (_offsetChangeMap == null) return;
        if (!_offsetChangeMap.IsFrozen) throw new ArgumentException("The OffsetChangeMap must be frozen before it can be used in DocumentChangeEventArgs");
        if (!_offsetChangeMap.IsValidForDocumentChange(Offset, RemovalLength, InsertionLength)) throw new ArgumentException("OffsetChangeMap is not valid for this document change", nameof(_offsetChangeMap));
        this.offsetChangeMap = _offsetChangeMap;
    }

    public override TextChangeEventArgs Invert()
    {
        var map = OffsetChangeMapOrNull;
        if (map == null) return new DocumentChangeEventArgs(Offset, InsertedText, RemovedText, null);
        map = map.Invert();
        map.Freeze();

        return new DocumentChangeEventArgs(Offset, InsertedText, RemovedText, map);
    }
}
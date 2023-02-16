using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using AvalonEdit.Utils;

namespace AvalonEdit.Document;

public enum OffsetChangeMappingType
{
    Normal,
    RemoveAndInsert,
    CharacterReplace,
    KeepAnchorBeforeInsertion
}

[Serializable]
[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "It's a mapping old offsets -> new offsets")]
public sealed class OffsetChangeMap : Collection<OffsetChangeMapEntry>
{
    [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The Empty instance is immutable")]
    public static readonly OffsetChangeMap Empty = new(Empty<OffsetChangeMapEntry>.Array, true);

    public static OffsetChangeMap FromSingleElement(OffsetChangeMapEntry entry)
    {
        return new OffsetChangeMap(new[] { entry }, true);
    }

    public OffsetChangeMap()
    {
    }

    internal OffsetChangeMap(int capacity) : base(new List<OffsetChangeMapEntry>(capacity))
    {
    }

    private OffsetChangeMap(IList<OffsetChangeMapEntry> entries, bool isFrozen) : base(entries)
    {
        IsFrozen = isFrozen;
    }

    public int GetNewOffset(int offset, AnchorMovementType movementType = AnchorMovementType.Default)
    {
        var items                              = Items;
        var count                              = items.Count;
        for (var i = 0; i < count; i++) offset = items[i].GetNewOffset(offset, movementType);
        return offset;
    }

    public bool IsValidForDocumentChange(int offset, int removalLength, int insertionLength)
    {
        var endOffset = offset + removalLength;
        foreach (var entry in this)
        {
            if (entry.Offset < offset || entry.Offset + entry.RemovalLength > endOffset) return false;
            endOffset += entry.InsertionLength - entry.RemovalLength;
        }

        return endOffset == offset + insertionLength;
    }

    public OffsetChangeMap Invert()
    {
        if (this == Empty) return this;
        var newMap = new OffsetChangeMap(Count);
        for (var i = Count - 1; i >= 0; i--)
        {
            var entry = this[i];
            newMap.Add(new OffsetChangeMapEntry(entry.Offset, entry.InsertionLength, entry.RemovalLength));
        }

        return newMap;
    }

    protected override void ClearItems()
    {
        CheckFrozen();
        base.ClearItems();
    }

    protected override void InsertItem(int index, OffsetChangeMapEntry item)
    {
        CheckFrozen();
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        CheckFrozen();
        base.RemoveItem(index);
    }

    protected override void SetItem(int index, OffsetChangeMapEntry item)
    {
        CheckFrozen();
        base.SetItem(index, item);
    }

    private void CheckFrozen()
    {
        if (IsFrozen) throw new InvalidOperationException("This instance is frozen and cannot be modified.");
    }

    public bool IsFrozen { get; private set; }

    public void Freeze()
    {
        IsFrozen = true;
    }
}

[Serializable]
public struct OffsetChangeMapEntry : IEquatable<OffsetChangeMapEntry>
{
    private readonly uint insertionLengthWithMovementFlag;

    private readonly uint removalLengthWithDeletionFlag;

    public int Offset { get; }

    public int InsertionLength => (int)(insertionLengthWithMovementFlag & 0x7fffffff);

    public int RemovalLength => (int)(removalLengthWithDeletionFlag & 0x7fffffff);

    public bool RemovalNeverCausesAnchorDeletion => (removalLengthWithDeletionFlag & 0x80000000) != 0;

    public bool DefaultAnchorMovementIsBeforeInsertion => (insertionLengthWithMovementFlag & 0x80000000) != 0;

    public int GetNewOffset(int oldOffset, AnchorMovementType movementType = AnchorMovementType.Default)
    {
        var insertionLength = InsertionLength;
        var removalLength   = RemovalLength;
        if (!(removalLength == 0 && oldOffset == Offset))
        {
            if (oldOffset <= Offset) return oldOffset;
            if (oldOffset >= Offset + removalLength) return oldOffset + insertionLength - removalLength;
        }

        if (movementType == AnchorMovementType.AfterInsertion) return Offset + insertionLength;
        if (movementType == AnchorMovementType.BeforeInsertion) return Offset;
        return DefaultAnchorMovementIsBeforeInsertion ? Offset : Offset + insertionLength;
    }

    public OffsetChangeMapEntry(int offset, int removalLength, int insertionLength)
    {
        ThrowUtil.CheckNotNegative(offset, "offset");
        ThrowUtil.CheckNotNegative(removalLength, "removalLength");
        ThrowUtil.CheckNotNegative(insertionLength, "insertionLength");

        Offset                          = offset;
        removalLengthWithDeletionFlag   = (uint)removalLength;
        insertionLengthWithMovementFlag = (uint)insertionLength;
    }

    public OffsetChangeMapEntry(int offset, int removalLength, int insertionLength, bool removalNeverCausesAnchorDeletion, bool defaultAnchorMovementIsBeforeInsertion) : this(offset, removalLength, insertionLength)
    {
        if (removalNeverCausesAnchorDeletion) removalLengthWithDeletionFlag         |= 0x80000000;
        if (defaultAnchorMovementIsBeforeInsertion) insertionLengthWithMovementFlag |= 0x80000000;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Offset + 3559 * (int)insertionLengthWithMovementFlag + 3571 * (int)removalLengthWithDeletionFlag;
        }
    }

    public override bool Equals(object obj)
    {
        return obj is OffsetChangeMapEntry && Equals((OffsetChangeMapEntry)obj);
    }

    public bool Equals(OffsetChangeMapEntry other)
    {
        return Offset == other.Offset && insertionLengthWithMovementFlag == other.insertionLengthWithMovementFlag && removalLengthWithDeletionFlag == other.removalLengthWithDeletionFlag;
    }

    public static bool operator ==(OffsetChangeMapEntry left, OffsetChangeMapEntry right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OffsetChangeMapEntry left, OffsetChangeMapEntry right)
    {
        return !left.Equals(right);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using AvalonEdit.Document;
using AvalonEdit.Utils;

namespace AvalonEdit.Editing;

internal sealed class NoReadOnlySections : IReadOnlySectionProvider
{
    public static readonly NoReadOnlySections Instance = new();

    public bool CanInsert(int offset)
    {
        return true;
    }

    public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        return ExtensionMethods.Sequence(segment);
    }
}

internal sealed class ReadOnlySectionDocument : IReadOnlySectionProvider
{
    public static readonly ReadOnlySectionDocument Instance = new();

    public bool CanInsert(int offset)
    {
        return false;
    }

    public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
    {
        return Enumerable.Empty<ISegment>();
    }
}
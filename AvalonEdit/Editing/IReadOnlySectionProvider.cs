using System.Collections.Generic;
using AvalonEdit.Document;

namespace AvalonEdit.Editing;

public interface IReadOnlySectionProvider
{
    bool CanInsert(int offset);

    IEnumerable<ISegment> GetDeletableSegments(ISegment segment);
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AvalonEdit.Utils;

namespace AvalonEdit.Document;

public class TextSourceVersionProvider
{
    private Version currentVersion;

    public TextSourceVersionProvider()
    {
        currentVersion = new Version(this);
    }

    public ITextSourceVersion CurrentVersion => currentVersion;

    public void AppendChange(TextChangeEventArgs change)
    {
        if (change == null) throw new ArgumentNullException(nameof(change));
        currentVersion.change = change;
        currentVersion.next   = new Version(currentVersion);
        currentVersion        = currentVersion.next;
    }

    [DebuggerDisplay("Version #{id}")]
    private sealed class Version : ITextSourceVersion
    {
        private readonly TextSourceVersionProvider provider;
        private readonly int                       id;

        internal TextChangeEventArgs change;
        internal Version             next;

        internal Version(TextSourceVersionProvider provider)
        {
            this.provider = provider;
        }

        internal Version(Version prev)
        {
            provider = prev.provider;
            id       = unchecked(prev.id + 1);
        }

        public bool BelongsToSameDocumentAs(ITextSourceVersion other)
        {
            var o = other as Version;
            return o != null && provider == o.provider;
        }

        public int CompareAge(ITextSourceVersion other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (other is not Version o || provider != o.provider) throw new ArgumentException("Versions do not belong to the same document.");
            return Math.Sign(unchecked(id - o.id));
        }

        public IEnumerable<TextChangeEventArgs> GetChangesTo(ITextSourceVersion other)
        {
            var result = CompareAge(other);
            var o      = (Version)other;
            if (result < 0) return GetForwardChanges(o);
            if (result > 0) return o.GetForwardChanges(this).Reverse().Select(textChangeEventArgs => textChangeEventArgs.Invert());
            return Empty<TextChangeEventArgs>.Array;
        }

        private IEnumerable<TextChangeEventArgs> GetForwardChanges(Version other)
        {
            for (var node = this; node != other; node = node.next) yield return node.change;
        }

        public int MoveOffsetTo(ITextSourceVersion other, int oldOffset, AnchorMovementType movement)
        {
            return GetChangesTo(other).Aggregate(oldOffset, (current, e) => e.GetNewOffset(current, movement));
        }
    }
}
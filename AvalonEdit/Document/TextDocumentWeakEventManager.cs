using System.Diagnostics.CodeAnalysis;
using AvalonEdit.Utils;

namespace AvalonEdit.Document;

public static class TextDocumentWeakEventManager
{
    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public sealed class UpdateStarted : WeakEventManagerBase<UpdateStarted, TextDocument>
    {
        protected override void StartListening(TextDocument source)
        {
            source.UpdateStarted += DeliverEvent;
        }

        protected override void StopListening(TextDocument source)
        {
            source.UpdateStarted -= DeliverEvent;
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public sealed class UpdateFinished : WeakEventManagerBase<UpdateFinished, TextDocument>
    {
        protected override void StartListening(TextDocument source)
        {
            source.UpdateFinished += DeliverEvent;
        }

        protected override void StopListening(TextDocument source)
        {
            source.UpdateFinished -= DeliverEvent;
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public sealed class Changing : WeakEventManagerBase<Changing, TextDocument>
    {
        protected override void StartListening(TextDocument source)
        {
            source.Changing += DeliverEvent;
        }

        protected override void StopListening(TextDocument source)
        {
            source.Changing -= DeliverEvent;
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public sealed class Changed : WeakEventManagerBase<Changed, TextDocument>
    {
        protected override void StartListening(TextDocument source)
        {
            source.Changed += DeliverEvent;
        }

        protected override void StopListening(TextDocument source)
        {
            source.Changed -= DeliverEvent;
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public sealed class TextChanged : WeakEventManagerBase<TextChanged, TextDocument>
    {
        protected override void StartListening(TextDocument source)
        {
            source.TextChanged += DeliverEvent;
        }

        protected override void StopListening(TextDocument source)
        {
            source.TextChanged -= DeliverEvent;
        }
    }
}
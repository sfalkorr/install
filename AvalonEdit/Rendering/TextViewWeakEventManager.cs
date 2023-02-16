using System.Diagnostics.CodeAnalysis;
using AvalonEdit.Utils;

namespace AvalonEdit.Rendering;

public static class TextViewWeakEventManager
{
    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public sealed class DocumentChanged : WeakEventManagerBase<DocumentChanged, TextView>
    {
        protected override void StartListening(TextView source)
        {
            source.DocumentChanged += DeliverEvent;
        }

        protected override void StopListening(TextView source)
        {
            source.DocumentChanged -= DeliverEvent;
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public sealed class VisualLinesChanged : WeakEventManagerBase<VisualLinesChanged, TextView>
    {
        protected override void StartListening(TextView source)
        {
            source.VisualLinesChanged += DeliverEvent;
        }

        protected override void StopListening(TextView source)
        {
            source.VisualLinesChanged -= DeliverEvent;
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public sealed class ScrollOffsetChanged : WeakEventManagerBase<ScrollOffsetChanged, TextView>
    {
        protected override void StartListening(TextView source)
        {
            source.ScrollOffsetChanged += DeliverEvent;
        }

        protected override void StopListening(TextView source)
        {
            source.ScrollOffsetChanged -= DeliverEvent;
        }
    }
}
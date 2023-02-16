using System;
using System.Collections.Generic;

namespace AvalonEdit.Rendering;

public abstract class ColorizingTransformer : IVisualLineTransformer, ITextViewConnect
{
    protected IList<VisualLineElement> CurrentElements { get; private set; }

    public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
    {
        if (CurrentElements != null) throw new InvalidOperationException("Recursive Transform() call");
        CurrentElements = elements ?? throw new ArgumentNullException(nameof(elements));
        try
        {
            Colorize(context);
        }
        finally
        {
            CurrentElements = null;
        }
    }

    protected abstract void Colorize(ITextRunConstructionContext context);

    protected void ChangeVisualElements(int visualStartColumn, int visualEndColumn, Action<VisualLineElement> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        for (var i = 0; i < CurrentElements.Count; i++)
        {
            var e = CurrentElements[i];
            if (e.VisualColumn > visualEndColumn) break;
            if (e.VisualColumn < visualStartColumn && e.VisualColumn + e.VisualLength > visualStartColumn)
                if (e.CanSplit)
                {
                    e.Split(visualStartColumn, CurrentElements, i--);
                    continue;
                }

            if (e.VisualColumn >= visualStartColumn && e.VisualColumn < visualEndColumn)
            {
                if (e.VisualColumn + e.VisualLength > visualEndColumn)
                {
                    if (e.CanSplit) e.Split(visualEndColumn, CurrentElements, i--);
                }
                else
                {
                    action(e);
                }
            }
        }
    }

    protected virtual void OnAddToTextView(TextView textView)
    {
    }

    protected virtual void OnRemoveFromTextView(TextView textView)
    {
    }

    void ITextViewConnect.AddToTextView(TextView textView)
    {
        OnAddToTextView(textView);
    }

    void ITextViewConnect.RemoveFromTextView(TextView textView)
    {
        OnRemoveFromTextView(textView);
    }
}
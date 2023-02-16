using System;

namespace AvalonEdit.Rendering;

public abstract class VisualLineElementGenerator
{
    protected ITextRunConstructionContext CurrentContext { get; private set; }

    public virtual void StartGeneration(ITextRunConstructionContext context)
    {
        CurrentContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public virtual void FinishGeneration()
    {
        CurrentContext = null;
    }

    internal int cachedInterest;

    public abstract int GetFirstInterestedOffset(int startOffset);

    public abstract VisualLineElement ConstructElement(int offset);
}

internal interface IBuiltinElementGenerator
{
    void FetchOptions(TextEditorOptions options);
}
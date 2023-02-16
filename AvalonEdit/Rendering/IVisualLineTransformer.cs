using System.Collections.Generic;

namespace AvalonEdit.Rendering;

public interface IVisualLineTransformer
{
    void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements);
}
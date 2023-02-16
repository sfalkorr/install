using System.Collections.Generic;
using System.Diagnostics;

namespace AvalonEdit.Rendering;

internal struct HeightTreeLineNode
{
    internal HeightTreeLineNode(double height)
    {
        collapsedSections = null;
        this.height       = height;
    }

    internal double                     height;
    internal List<CollapsedLineSection> collapsedSections;

    internal bool IsDirectlyCollapsed => collapsedSections != null;

    internal void AddDirectlyCollapsed(CollapsedLineSection section)
    {
        collapsedSections ??= new List<CollapsedLineSection>();
        collapsedSections.Add(section);
    }

    internal void RemoveDirectlyCollapsed(CollapsedLineSection section)
    {
        Debug.Assert(collapsedSections.Contains(section));
        collapsedSections.Remove(section);
        if (collapsedSections.Count == 0) collapsedSections = null;
    }

    internal double TotalHeight => IsDirectlyCollapsed ? 0 : height;
}
using System;

namespace AvalonEdit.Document;

public interface ITextAnchor
{
    TextLocation Location { get; }

    int Offset { get; }

    AnchorMovementType MovementType { get; set; }

    bool SurviveDeletion { get; set; }

    bool IsDeleted { get; }

    event EventHandler Deleted;

    int Line { get; }

    int Column { get; }
}

public enum AnchorMovementType
{
    Default,
    BeforeInsertion,
    AfterInsertion
}
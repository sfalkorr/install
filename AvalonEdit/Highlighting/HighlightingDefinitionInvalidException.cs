﻿using System;
using System.Runtime.Serialization;

namespace AvalonEdit.Highlighting;

[Serializable]
public class HighlightingDefinitionInvalidException : Exception
{
    public HighlightingDefinitionInvalidException()
    {
    }

    public HighlightingDefinitionInvalidException(string message) : base(message)
    {
    }

    public HighlightingDefinitionInvalidException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected HighlightingDefinitionInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
﻿namespace AvalonEdit.Utils;

internal static class Empty<T>
{
    public static readonly T[] Array = System.Array.Empty<T>();
}
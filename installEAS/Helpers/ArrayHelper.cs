using System;

namespace installEAS.Helpers;

public static class ArrayHelper
{
    public static bool InArray(string[] array, string ElementToCHeck)
    {
        return Array.FindIndex(array, t => t.IndexOf(ElementToCHeck, StringComparison.InvariantCultureIgnoreCase) >= 0) >= 0;
    }
}
using System.IO;

namespace AvalonEdit.Highlighting;

internal static class Resources
{
    private static readonly string Prefix = typeof(Resources).FullName + ".";

    public static Stream OpenStream(string name)
    {
        var s = typeof(Resources).Assembly.GetManifestResourceStream(Prefix + name);
        if (s == null) throw new FileNotFoundException("The resource file '" + name + "' was not found.");
        return s;
    }

    internal static void RegisterBuiltInHighlightings(HighlightingManager.DefaultHighlightingManager hlm)
    {
    }
}
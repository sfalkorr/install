namespace installEAS.Helpers;

internal abstract class Files
{
    public static string GetFilesEx(string path, string Partname, SearchOption Option = SearchOption.AllDirectories)
    {
        var returnarray = Directory.GetFiles(path, Partname, Option);
        return returnarray.Length switch
        {
            1 => returnarray[0],
            > 1 => Join("\n", returnarray.Select(p => p.ToString()).ToArray()),
            _ => null
        };
    }

    public static string ReadFilesToString(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    public static void GetAllFiles(string rootDirectory, string fileExtension, List<string> files)
    {
        var directories = Directory.GetDirectories(rootDirectory);
        files.AddRange(Directory.GetFiles(rootDirectory, fileExtension));
        foreach (var path in directories) GetAllFiles(path, fileExtension, files);
    }

    public static void CopyFiles(string Source, string Destination, string Extension)
    {
        if (!Directory.Exists(Destination)) Directory.CreateDirectory(Destination);
        foreach (var newPath in Directory.GetFiles(Source, Extension, SearchOption.AllDirectories)) File.Copy(newPath, newPath.Replace(Source, Destination), true);
    }

    public static string RtfToPlainText(string rtf)
    {
        var flowDocument = new FlowDocument();
        var textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(rtf ?? Empty));
        {
            textRange.Load(stream, DataFormats.Rtf);
        }
        return textRange.Text;
    }

    public static string GetSubstringByString(string a, string b, string c)
    {
        return c.Substring(c.IndexOf(a, StringComparison.Ordinal) + a.Length, c.IndexOf(b, StringComparison.Ordinal) - c.IndexOf(a, StringComparison.Ordinal) - a.Length);
    }

    public static void CreateBackupFile(string path)
    {
        if (IsNullOrEmpty(path) || !File.Exists(path)) return;
        File.Copy(path, path + ".bak", true);
    }

    public static void DeleteBackupFile(string path)
    {
        var path1 = path + ".bak";
        if (!File.Exists(path1)) return;
        File.Delete(path1);
    }

    public static bool TestFilePath(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }
}
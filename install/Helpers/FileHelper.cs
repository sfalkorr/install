using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace installEAS.Helpers;

internal abstract class FileHelper
{
    public static string GetFilesEx(string path, string Partname, SearchOption Option = SearchOption.AllDirectories)
    {
        var returnarray = Directory.GetFiles(path, Partname, Option);
        return returnarray.Length switch
               {
                   1   => returnarray[0],
                   > 1 => string.Join("\n", returnarray.Select(p => p.ToString()).ToArray()),
                   _   => null
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
        var textRange    = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
        var stream       = new MemoryStream(Encoding.UTF8.GetBytes(rtf ?? string.Empty));
        {
            textRange.Load(stream, DataFormats.Rtf);
        }
        return textRange.Text;
    }

    public static string GetSubstringByString(string a, string b, string c)
    {
        return c.Substring(c.IndexOf(a, StringComparison.Ordinal) + a.Length, c.IndexOf(b, StringComparison.Ordinal) - c.IndexOf(a, StringComparison.Ordinal) - a.Length);
    }
}
using System;
using System.Windows.Media;
using Microsoft.Win32;
using static installEAS.Helpers.Log;

namespace installEAS.Helpers;

public static class Reg
{
    private static RegistryKey registry;
    private static string      _regHive, _regPath;

    public static object Read(string inpPath, string inpKey = null)
    {
        _regHive = inpPath.Split(':')[0];
        _regPath = inpPath.Split(':')[1].Trim('\\');
        registry = _regHive switch
                   {
                       "HKLM" => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64),
                       "HKCU" => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64),
                       _      => registry
                   };

        try
        {
            return inpKey == null ? registry.OpenSubKey(_regPath, true) : registry.OpenSubKey(_regPath, true)?.GetValue(inpKey);
        }
        catch (Exception e)
        {
            log(inpPath + " " + "Key: " + inpKey + " " + e.Message, Brushes.Red);
            return null;
        }
        finally { registry.Close(); }
    }

    public static void Write(string inpPath, string inpKey = null, object inpValue = default, RegistryValueKind Type = default)
    {
        _regHive = inpPath.Split(':')[0];
        _regPath = inpPath.Split(':')[1].Trim('\\');
        registry = _regHive switch
                   {
                       "HKLM" => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64),
                       "HKCU" => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64),
                       _      => registry
                   };
        try
        {
            if (inpKey == null) registry.CreateSubKey(_regPath, true);
            else if (inpValue != null) registry.CreateSubKey(_regPath, true).SetValue(inpKey, inpValue, Type);
        }
        catch (Exception e) { log(inpPath + " " + "Key: " + inpKey + " " + e.Message, Brushes.Red); }
        finally { registry.Close(); }
    }

    public static void WriteMultistring(string inpPath, string inpKey = null, object[] inpValue = default, RegistryValueKind Type = RegistryValueKind.MultiString)
    {
        _regHive = inpPath.Split(':')[0];
        _regPath = inpPath.Split(':')[1].Trim('\\');
        registry = _regHive switch
                   {
                       "HKLM" => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64),
                       "HKCU" => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64),
                       _      => registry
                   };
        try
        {
            if (inpKey == null) registry.CreateSubKey(_regPath, true);
            else if (inpValue != null) registry.CreateSubKey(_regPath, RegistryKeyPermissionCheck.ReadWriteSubTree)?.SetValue(inpKey, inpValue, Type);
        }
        catch (Exception e) { log(inpPath + " " + "Key: " + inpKey + " " + e.Message, Brushes.Red); }
        finally { registry.Close(); }
    }

    public static void Remove(string inpPath, string inpKey = null)
    {
        _regHive = inpPath.Split(':')[0];
        _regPath = inpPath.Split(':')[1].Trim('\\');
        registry = _regHive switch
                   {
                       "HKLM" => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64),
                       "HKCU" => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64),
                       _      => registry
                   };
        try
        {
            if (inpKey == null) registry.DeleteSubKeyTree(_regPath);
            else registry.OpenSubKey(_regPath, true)?.DeleteValue(inpKey, true);
        }
        catch (Exception e) { log(inpPath + " " + "Key: " + inpKey + " " + e.Message, Brushes.Red); }
        finally { registry.Close(); }
    }


    public static bool TestRegPath(string inpPath, string inpKey = null)
    {
        _regHive = inpPath.Split(':')[0];
        _regPath = inpPath.Split(':')[1].Trim('\\');
        registry = _regHive switch
                   {
                       "HKLM" => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64),
                       "HKCU" => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64),
                       _      => registry
                   };

        try
        {
            var testpath = inpKey == null ? registry.OpenSubKey(_regPath, true) : registry.OpenSubKey(_regPath, true)?.GetValue(inpKey);
            if (testpath != null) return true;
        }
        catch
        {
            return false;
        }
        finally { registry.Close(); }

        return false;
    }
}
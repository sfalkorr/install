namespace installEAS.Helpers;

public static class RegistryTools
{
    public static bool ValueExists(RegistryKey RegistryKeyPath, string ValueName)
    {
        var flag = false;
        try
        {
            flag = ArrayHelper.InArray(RegistryKeyPath.GetValueNames(), ValueName);
        }
        catch
        {
            // ignored
        }

        return flag;
    }

    public static bool ValueExists(RegistryKey RootKey, string RegPath, string ValueName)
    {
        var flag = false;
        try
        {
            var registryKey = RootKey.OpenSubKey(RegPath);
            if (registryKey != null)
            {
                var valueNames = registryKey.GetValueNames();
                registryKey.Close();
                flag = ArrayHelper.InArray(valueNames, ValueName);
            }
        }
        catch
        {
            // ignored
        }

        return flag;
    }

    public static bool KeyExists(RegistryKey RootKey, string RegistryPath)
    {
        var flag = false;
        try
        {
            var registryKey = RootKey.OpenSubKey(RegistryPath);
            if (registryKey != null)
            {
                registryKey.Close();
                flag = true;
            }
        }
        catch
        {
            // ignored
        }

        return flag;
    }

    public static bool KeyExists(string FullPath)
    {
        var flag = false;
        ParseKey(FullPath, out var RootKey, out var SubKey);
        if (RootKey.Length <= 0) return false;
        try
        {
            var registryKey = StringToRegistryKey(ExpandRooKeyName(RootKey), RegistryAccessMode.Read);
            if (registryKey != null)
            {
                if (SubKey.Length == 0)
                {
                    registryKey.Close();
                    return true;
                }

                flag = KeyExists(registryKey, SubKey);
                registryKey.Close();
            }
        }
        catch
        {
            // ignored
        }

        return flag;
    }

    public static string ExpandRooKeyName(string RootKeyString)
    {
        var str = "";
        switch (RootKeyString.ToUpper())
        {
            case "HKEY_CLASSES_ROOT":
            case "HKCR":
                str = "HKEY_CLASSES_ROOT";
                break;
            case "HKEY_CURRENT_USER":
            case "HKCU":
                str = "HKEY_CURRENT_USER";
                break;
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                str = "HKEY_LOCAL_MACHINE";
                break;
            case "HKEY_USERS":
            case "HKU":
                str = "HKEY_USERS";
                break;
        }

        return str;
    }

    public static void ParseKey(string FullPath, out string RootKey, out string SubKey)
    {
        RootKey = "";
        SubKey = "";
        var strArray = FullPath.Split('\\');
        RootKey = ExpandRooKeyName(strArray[0]);
        if (RootKey.Length <= 0 || strArray.Length <= 1) return;
        for (var index = 1; index < strArray.Length && strArray[index].Length != 0; ++index) SubKey += SubKey.Length == 0 ? strArray[index] : "\\" + strArray[index];
    }

    public static RegistryKey StringToRegistryKey(string FullPath, RegistryAccessMode registryAccessMode)
    {
        ParseKey(FullPath, out var RootKey, out var SubKey);
        if (RootKey.Length <= 0) return null;
        var registryKey2 = RootKey switch
        {
            "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKEY_USERS" => Registry.Users,
            _ => null
        };
        var registryKey1 = registryKey2;
        if (SubKey.Length > 0)
            if (registryKey1 != null)
                try
                {
                    registryKey1 = registryAccessMode != RegistryAccessMode.Create ? registryKey2.OpenSubKey(SubKey, registryAccessMode == RegistryAccessMode.Write) : registryKey2.CreateSubKey(SubKey);
                }
                catch
                {
                    registryKey2.Close();
                    registryKey1 = null;
                }

        registryKey2?.Close();
        return registryKey1;
    }

    public static void ParseKeyToExistedPart(string FullPath, out string RootKey, out string SubKey)
    {
        RootKey = "";
        SubKey = "";
        var strArray = FullPath.Split('\\');
        RootKey = ExpandRooKeyName(strArray[0]);
        if (RootKey.Length <= 0) return;
        var registryKey = StringToRegistryKey(RootKey, RegistryAccessMode.Read);
        if (registryKey == null) return;
        if (strArray.Length > 1)
            for (var index = 1; index < strArray.Length && strArray[index].Length != 0; ++index)
            {
                var RegistryPath = SubKey + (SubKey.Length == 0 ? strArray[index] : "\\" + strArray[index]);
                if (KeyExists(registryKey, RegistryPath)) SubKey = RegistryPath;
                else break;
            }

        registryKey.Close();
    }

    public static void DeleteSubKeyTree(ref RegistryKey reg, string SubKeyToDelete)
    {
        try
        {
            reg.DeleteSubKeyTree(SubKeyToDelete);
        }
        catch
        {
            // ignored
        }
    }

    public static void AddSeparatorAbove(ref RegistryKey reg)
    {
        reg.SetValue("CommandFlags", 32, RegistryValueKind.DWord);
    }

    public enum RegistryAccessMode
    {
        Read,
        Write,
        Create
    }
}
﻿namespace installEAS;

public static class Variables
{
    public static void CreateVariablesInstance()
    {
        var variablesInstance = CreateVariablesInstance;
        Console.WriteLine(variablesInstance.GetMethodInfo());
    }

    public static object GetRegValue(string path, string key)
    {
        var value = Reg.Read(path, key);
        if (value != null) return value;
        CustomMessageBox.ShowOK($"Не найдено значение {key} в\n{path}\nПродолжение работы невозможно", "Ошибка инициализации", "Выход", MessageBoxImage.Error);
        Process.GetCurrentProcess().Kill();

        return "";
    }

    public static string GetVarPath(string path)
    {
        if (TestFilePath(path)) return path;
        CustomMessageBox.ShowOK($"Не найден путь {path}\nПродолжение работы невозможно", "Ошибка инициализации", "Выход", MessageBoxImage.Error);
        Process.GetCurrentProcess().Kill();

        return null;
    }

    public static bool IsServer()
    {
        return SQLRegParameters != null;
    }

    public static bool IsComputernameCorrect()
    {
        return Regex.Match(Computername, "(?<B>[RC])(\\d{2})-(\\d{6})-(W)(\\d{2}$)").Success || Regex.Match(Computername, "(?<B>[RC])(\\d{2})-(\\d{6})-(N$)").Success;
    }

    public static string AppVersion       => "7.0";
    public static string Username         => UserName;
    public static string Domainname       => UserDomainName;
    public static string Computername     => MachineName;
    public static object SQLRegParameters => Reg.Read(@"HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQLServer\Parameters");
    public static string OPSNum           => IsComputernameCorrect() ? Regex.Match(Computername, "\\d{6}").ToString() : null;
    public static string DBOPSName        => IsComputernameCorrect() ? "DB" + Regex.Match(Computername, "\\d{6}") : null;
    public static object SevenZReg        => GetRegValue(@"HKLM:\SOFTWARE\7-Zip", "Path");
    public static string SevenZgPath      => GetVarPath(SevenZReg + "7zG.exe");
    public static string SevenZPath       => GetVarPath(SevenZReg + "7z.exe");
    public static string AppExe           => Process.GetCurrentProcess().MainModule?.FileName;
    public static string AppPath          => CurrentDirectory;
    public static string TempPath         => Path.GetTempPath();

    public static string AppRegPath => @"HKLM:\SOFTWARE\Microsoft\Sharp";
    public static string CertsPath => GetVarPath(AppPath + @"\Certs");
    //public static string SqlPackPath      => GetVarPath(AppPath + @"\SqlPackage");
    //public static string SqlPackLib       => GetVarPath(AppPath + @"\SqlPackageLib");
    //public static string SqlPackTemp      => TempPath + "sqlpackage";
    //public static object POSRegPath       => GetRegValue(@"HKLM:\SOFTWARE\Wow6432Node\GMCS\POS", "InstallDir");
    //public static object POSVer           => GetRegValue(@"HKLM:\SOFTWARE\Wow6432Node\GMCS\POS", "Version");
    //public static string POSPath          => GetVarPath(POSRegPath.ToString());

    //public static string POSConfig => GetVarPath(POSRegPath + "POS.exe.config");
    public static string POSConfig => SpecialFolder.ProgramFilesX86 + "POS.exe.config";

    //public static string DBOPSName   { get; } = "DB" + Computername.Split('-')[1];
    //public static string DBOPSName      { get; }      = "DB160024";
    
    public static object SqlPassReg      => GetRegValue(@"HKLM:\SOFTWARE\7-Zip", "Path");
    
    
    
    public static string SqlInitcatalog { get; set; } = "master";

    //public static string SqlPass { get; set; } = "JHertg76#$%g8";
    public static string SqlPass       { get; set; } = "QWEasd123*";
    public static string SQLServername { get; set; } = "DESKTOP-BVIBBQO";
    public static string SQLUsername   { get; set; } = "sa";
    public static string SQLInstance   { get; set; } = "MSSQLSERVER";
    public static int    SQLTimeout    { get; set; } = 5;
    

    public static string meta_num { get; set; }
    public static string meta_sec { get; set; }
    public static string meta_ver { get; set; }
    public static string archive  { get; set; }
    public static string arcmeta  { get; set; }
    public static string package  { get; set; }
}
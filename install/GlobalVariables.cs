using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;

namespace installEAS;

public static class Variables
{
    public static void CreateVariablesInstance()
    {
        var variablesInstance = CreateVariablesInstance;
        Console.WriteLine( variablesInstance.GetMethodInfo() );
    }

    public static bool TestPath( string path )
    {
        return File.Exists( path ) || Directory.Exists( path );
    }

    public static object GetRegValue( string path, string key )
    {
        var value = Reg.Read( path, key );
        if ( value != null ) return value;
        //Log.log( $"Не найден ключ реестра {key} в {path}", Brushes.OrangeRed );
        CustomMessageBox.ShowOK( $"Не найдено значение {key} в {path}\nПродолжение работы невозможно", "Ошибка инициализации", "Выход", MessageBoxImage.Error );
        Process.GetCurrentProcess().Kill();

        return "";
    }

    public static string GetVarPath( string path )
    {
        if ( TestPath( path ) ) return path;
        //Log.log( $"Не найден путь {path}", Brushes.OrangeRed );
        CustomMessageBox.ShowOK( $"Не найден путь {path}\nПродолжение работы невозможно", "Ошибка инициализации", "Выход", MessageBoxImage.Error );
        Process.GetCurrentProcess().Kill();

        return null;
    }

    public static string AppVersion     { get; }      = "7.01";
    public static string Username       { get; }      = Environment.UserName;
    public static string Domainname     { get; }      = Environment.UserDomainName;
    public static string Computername   { get; }      = "C01-160024-N";
    public static object SevenZReg      { get; }      = GetRegValue( @"HKLM:\SOFTWARE\7-Zip", "Path" );
    public static string SevenZgPath    { get; }      = GetVarPath( SevenZReg + "7zG.exe" );
    public static string SevenZPath     { get; }      = GetVarPath( SevenZReg + "7z.exe" );
    public static string AppExe         { get; }      = Process.GetCurrentProcess().MainModule?.FileName;
    public static string AppPath        { get; }      = Environment.CurrentDirectory;
    public static string TempPath       { get; }      = Path.GetTempPath();
    public static string ImportPath     { get; }      = GetVarPath( AppPath + @"\import" );
    public static string EsppPath       { get; }      = GetVarPath( AppPath + @"\Espp" );
    public static string CertsPath      { get; }      = GetVarPath( AppPath + @"\Certs" );
    public static string SqlPackPath    { get; }      = GetVarPath( AppPath + @"\SqlPackage" );
    public static string SqlPackLib     { get; }      = GetVarPath( AppPath + @"\SqlPackageLib" );
    public static string SqlPackTemp    { get; }      = TempPath + "sqlpackage";
    public static string DBOPSName      { get; set; } = "DB" + Computername.Split( '-' )[1];
    public static string SqlInitcatalog { get; set; } = "master";
    public static string SqlPass        { get; set; } = "QWEasd123*";
    public static string SQLServername  { get; set; } = "C01-160024-N";
    public static string SQLUsername    { get; set; } = "sa";
    public static string SQLInstance    { get; set; } = "MSSQLSERVER";
    public static int    SQLTimeout     { get; set; } = 5;
    public static object POSRegPath     { get; }      = GetRegValue( @"HKLM:\SOFTWARE\Wow6432Node\GMCS\POS", "InstallDir" );
    public static object POSVer         { get; }      = GetRegValue( @"HKLM:\SOFTWARE\Wow6432Node\GMCS\POS", "Version" );
    public static string POSPath        { get; }      = GetVarPath( POSRegPath.ToString() );
    public static string POSConfig      { get; }      = GetVarPath( POSRegPath + "POS.exe.config" );
    public static string meta_num, meta_sec, meta_ver, archive, arcmeta, package;
}
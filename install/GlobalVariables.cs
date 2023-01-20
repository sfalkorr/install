using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static installEAS.LogHelper;
#pragma warning disable CS0414
#pragma warning disable CS0649

namespace installEAS
{
    internal abstract class GlobalVariables
    {
        public static RegistryHelper reg = new();
        public static bool TestPath( string path ) { return File.Exists( path ) || Directory.Exists( path ) ? true : false; }
        public static string SetVarPath( string path )
        {
            if (!TestPath( path ))
            {
                clog( "Не найден путь " + path, ConsoleColor.Red);
            }
            return path;
        }

        public static string ObjToString( object obj ) { return obj?.ToString(); }
        public static string path_temp = Path.GetTempPath().TrimEnd( '\\' );
        public static string AppRegPath = @"HKLM:\SOFTWARE\Microsoft\Sharp";

        public static string path_appexe = Assembly.GetExecutingAssembly().GetName().CodeBase.Replace( "/", "\\" ).Replace( "file:\\\\\\", "" );
        public static string path_apppath = Path.GetDirectoryName( path_appexe );
        public static string path_import = SetVarPath( path_apppath + "\\import" );
        public static string path_certs = SetVarPath( path_apppath + "\\espp" );

        public static string path_package = SetVarPath( path_apppath + "\\package" );
        public static string path_packagelib = SetVarPath( path_apppath + "\\lib" );
        public static string path_temp_sqllib = (path_temp + "\\sqlpackage");

        public static string zgPath = SetVarPath( @"C:\Program Files\7-Zip\7zG.exe" );
        public static string zPath = SetVarPath( @"C:\Program Files\7-Zip\7z.exe" );

        public static string sver = "7.0";
        public static string OSVer = Environment.OSVersion.VersionString;
        public static object OSName = reg.Read( @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName" );
        public static object OSBuild = reg.Read( @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild" );
        public static object OSRelease = reg.Read( @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId" );

        public static string Username = Environment.UserName;
        public static string Domainname = Environment.UserDomainName;
        //public static string Computername = Environment.MachineName;
        public const string Computername = "C01-160024-N";
        public static string DBOPSName = "DB" + Computername.Split( '-' )[1].ToString();
        public static string SQLInitcatalog = "master";
        public static string SQLPass = "JHertg76#$%g8";
        public static string SQLServername = "C01-160024-N";
        public static string SQLUsername = "sa";
        public static string SQLInstance = "MSSQLSERVER";
        public static int SQLTimeout = 5;
        public static object SQLDir_RegPath = reg.Read( @"HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQLServer\Parameters" );

        public static object POSInstalledVer = reg.Read( @"HKLM:\SOFTWARE\Wow6432Node\GMCS\POS", "Version" );

        public static string meta_filepath, meta_num = "", meta_sec = "", meta_ver = "", archive, arcmeta, package;
        public static string espp_num, espp_user, espp_pass, espp_zcx_pass;
        public static string sp = " ";

        public static string path_zcs = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + @"\zcs";
        public static string path_plugins = Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ) + @"\Pos\Plugins";


    }
}

using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static installEAS.GlobalVariables;
using static installEAS.FileHelper;
using static installEAS.LogHelper;
using static installEAS.ArchiveHelper;
using static installEAS.ProgressBarExtensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Threading;
using InstallEAS;
using System.Text.RegularExpressions;

namespace installEAS
{

    public abstract class Replica
    {
        public static RounderProgressBarControl round = new();

        public static void ReplicaCheck()
        {
            var checkimport = Directory.GetFiles( path_import, "*", SearchOption.AllDirectories );
            switch (checkimport.Length)
            {
                case 0:
                    mLog( path_import + " не содержит файлов", Brushes.Yellow );
                    break;

                case 1:
                    foreach (var file in checkimport)
                    {
                        var ext = Path.GetExtension( file );
                        if (ext != ".rar" && ext != ".zip" && ext != ".7z") continue;
                        arcmeta = ArchView( file, "meta.xml" );
                        if (arcmeta == "meta.xml")
                        {
                            var pathTemp = Path.GetTempPath().TrimEnd( '\\' );
                            ArchExtract( file, pathTemp, "*meta.xml*" );
                            if (GetFilesEx( pathTemp, "meta.xml" ) != null)
                            {
                                ReplicaMetaParse( pathTemp + @"\meta.xml" );
                                archive = file;
                                mLog( archive );
                            }
                            else { mLog( "Ошибка извлечения файла meta.xml из " + file, Brushes.Red ); }
                        }
                        else { mLog( "Архив " + file + " не содержит файла meta.xml с данными реплики\n", Brushes.Yellow ); }
                    }
                    break;

                case > 1:
                    foreach (var file in checkimport) { if (file.Contains( "meta.xml" )) { ReplicaMetaParse( file ); } }
                    break;
            }
        }


        public static void ReplicaMetaParse( string path )
        {
            var readedmeta = ReadFilesToString( path );
            var dstid = Regex.Match( readedmeta, "dstid(.*)", RegexOptions.IgnorePatternWhitespace );
            var dbVer = Regex.Match( readedmeta, "dbVersion(.*)", RegexOptions.IgnorePatternWhitespace );
            var secw2 = Regex.Match( readedmeta, "securityword(.*)", RegexOptions.IgnorePatternWhitespace );
            if (dstid.Success) meta_num = dstid.Value.Split( '"' )[1]; else { mLog( "Ошибка получения dstid из " + path, Brushes.Red );}
            if (dbVer.Success) meta_ver = dbVer.Value.Split( '"' )[1]; else { mLog( "Ошибка получения dbVer из " + path, Brushes.Red );}
            if (secw2.Success) meta_sec = secw2.Value.Split( '"' )[1]; else { mLog( "Ошибка получения secw2 из " + path, Brushes.Red ); }
        }


        public static string ReplicaGetSqlPackage()
        {
            var packages = Directory.GetDirectories( path_package );
            foreach (var item in packages) if (item.Split( '\\' ).Last().ToString() == POSInstalledVer.ToString()) package = item;
            if (package != null)
            {
                mLog( "Применение SQLPackage " + package );
                CopyFiles( package, path_temp_sqllib, "*.*" );
                CopyFiles( path_packagelib, path_temp_sqllib, "*.*" );
                var dacfile = GetFilesEx( path_temp_sqllib, "POSDatabase.dacpac" );
                return dacfile;
            }
            else return null;
        }

        public static Task ReplicaSqlPackageStartAsync()
        {
            return Task.Run( () =>
            {
                try
                {
                    double num = 0; string str; double perc = 0; double perc2 = 1; var startTime = DateTime.Now;
                    //ProcessKill( "SqlPackage" );
                    var dacpath =  ReplicaGetSqlPackage();
                    mLog( "Публикации структуры базы данных " + DBOPSName + "... ", false );
                    MainWindow.MainFrame.Dispatcher.InvokeOrExecute( () => { MainWindow.MainFrame.pbLabel.Foreground = Brushes.White; } );
                    ProgressBarSet( perc2, TimeSpan.FromMilliseconds( 1000 ) );
                    MainWindow.MainFrame.roundw.Start();
                    var process = new Process();
                    process.StartInfo.FileName = GetFilesEx( path_temp_sqllib, "SqlPackage.exe" );
                    process.StartInfo.Arguments = "/Action:Publish /SourceFile:" + dacpath + " /TargetServerName:" + Computername + " /TargetDatabaseName:" + DBOPSName + " /TargetUser:sa /TargetPassword:" + SQLPass + " /p:BlockOnPossibleDataLoss=False /p:DropObjectsNotInSource=False /p:IncludeTransactionalScripts=True /p:RegisterDataTierApplication=true /p:ScriptRefreshModule=False /p:BlockWhenDriftDetected=False /p:IgnoreWhitespace=False /p:BackupDatabaseBeforeChanges=True /p:IgnorePermissions=True /p:IncludeCompositeObjects=True /p:IgnoreRoleMembership=True /p:DeployDatabaseInSingleUserMode=True";
                    process.StartInfo.ErrorDialog = false;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.OutputDataReceived += async ( sender, args ) =>
                    {
                        if (args.Data is not { Length: > 0 }) return;
                        str = args.Data.ToString();
                        perc = Math.Round( num / 68354 * 100, 0 );
                        num++;
                        perc2 = Math.Round( num++ / 68354 * 100, 0 );
                        if (perc2 != perc)
                        {
                            var currentTime = (DateTime.Now - startTime);
                            var ct = Math.Round( currentTime.TotalSeconds );
                            ProgressBarSet( perc2, TimeSpan.FromMilliseconds( 1000 ) );
                        }

                        if (str != "Successfully published database.") return;
                        perc2 = 100;
                        ProgressBarSet( perc2, TimeSpan.FromMilliseconds( 1000 ) );
                        mLog("Successfully published database", Brushes.GreenYellow )  ;
                        await Task.Delay(1000);
                        MainWindow.MainFrame.Dispatcher.InvokeOrExecute( () => { MainWindow.MainFrame.pbLabel.Foreground = Brushes.Transparent; } );
                        ProgressBarSet( 0, TimeSpan.FromMilliseconds( 1 ) );
                        MainWindow.MainFrame.roundw.Stop();
                    };

                    process.ErrorDataReceived += ( sender, e ) =>
                    {
                        MainWindow.MainFrame.roundw.Stop();
                        MainWindow.MainFrame.Dispatcher.InvokeOrExecute( () => { MainWindow.MainFrame.pbLabel.Foreground = Brushes.Transparent; } );
                        Console.WriteLine( e.Data );
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    ProgressBarSet( 0, TimeSpan.FromMilliseconds( 1 ) );
                    MainWindow.MainFrame.roundw.Stop();
                    MainWindow.MainFrame.Dispatcher.InvokeOrExecute( () => { MainWindow.MainFrame.pbLabel.Foreground = Brushes.Transparent; } );
                    Console.WriteLine(ex);
                }
            } );
        }


    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using installEAS.Controls;
using installEAS.Helpers;
using static installEAS.Variables;
using static installEAS.MainWindow;
using static installEAS.Helpers.FileHelper;
using static installEAS.Helpers.Log;
using static installEAS.Helpers.Functions;
using static installEAS.Helpers.ArchiveHelper;

namespace installEAS.Common;

public abstract class Replica
{
    public static void ReplicaCheck()
    {
        var checkimport = Directory.GetFiles( ImportPath, "*", SearchOption.AllDirectories );
        switch ( checkimport.Length )
        {
            case 0:
                log( $"{ImportPath} не содержит файлов", Brushes.Yellow );
                break;

            case 1:
                foreach ( var file in checkimport )
                {
                    var ext = Path.GetExtension( file );
                    if ( ext != ".rar" && ext != ".zip" && ext != ".7z" ) continue;
                    var arcmeta = ArchView( file, "meta.xml" );
                    if ( arcmeta == "meta.xml" )
                    {
                        var pathTemp = Path.GetTempPath().TrimEnd( '\\' );
                        ArchExtract( file, pathTemp, "*meta.xml*" );
                        if ( GetFilesEx( pathTemp, "meta.xml" ) != null )
                        {
                            ReplicaMetaParse( $@"{pathTemp}\meta.xml" );
                            log( file );
                        }
                        else { log( $"Ошибка извлечения файла meta.xml из {file}", Brushes.Red ); }
                    }
                    else { log( $"Архив {file} не содержит файла meta.xml с данными реплики\n", Brushes.Yellow ); }
                }

                break;

            case > 1:
                foreach ( var file in checkimport )
                    if ( file.Contains( "meta.xml" ) )
                        ReplicaMetaParse( file );
                break;
        }
    }


    public static void ReplicaMetaParse( string path )
    {
        var readedmeta = ReadFilesToString( path );
        var dstid      = Regex.Match( readedmeta, "dstid(.*)", RegexOptions.IgnorePatternWhitespace );
        var dbVer      = Regex.Match( readedmeta, "dbVersion(.*)", RegexOptions.IgnorePatternWhitespace );
        var secw2      = Regex.Match( readedmeta, "securityword(.*)", RegexOptions.IgnorePatternWhitespace );
        if ( dstid.Success ) meta_num = dstid.Value.Split( '"' )[1];
        else log( $"Ошибка получения dstid из {path}", Brushes.Red );
        if ( dbVer.Success ) meta_ver = dbVer.Value.Split( '"' )[1];
        else log( $"Ошибка получения dbVer из {path}", Brushes.Red );
        if ( secw2.Success ) meta_sec = secw2.Value.Split( '"' )[1];
        else log( $"Ошибка получения secw2 из {path}", Brushes.Red );
    }


    public static string ReplicaGetSqlPackage()
    {
        var packages = Directory.GetDirectories( SqlPackPath );
        foreach ( var item in packages )
            if ( item.Split( '\\' ).Last() == ( string ) POSVer )
            {
                package = item;
            }
            else
            {
                log( $"В {ImportPath} не найден SQL Package, соответствующий установленной версии POS {POSVer}", Brushes.OrangeRed );
                return null;
            }

        if ( package != null )
        {
            log( "Применен SQL Package " + package );
            CopyFiles( package, SqlPackTemp, "*.*" );
            CopyFiles( SqlPackLib, SqlPackTemp, "*.*" );
            var dacfile = GetFilesEx( SqlPackTemp, "POSDatabase.dacpac" );
            return dacfile;
        }

        log( $"В {ImportPath} не найден SQL Package, соответствующий установленной версии POS {POSVer}", Brushes.Red );
        return null;
    }

    [STAThread]
    public static Task ReplicaSqlPackageStartAsync()
    {
        return Task.Run( () =>
        {
            try
            {
                double num = 0;
                string str;
                double perc2 = 1;
                ProcessKill( "SqlPackage" );
                var dacpath = ReplicaGetSqlPackage();
                log( $"Публикации структуры базы данных {DBOPSName}... ", false );
                UIControlRoundedProgressBar.UiControlRounded.Dispatcher.InvokeOrExecute( UIControlRoundedProgressBar.Start );
                MainFrame.pb.Dispatcher.InvokeOrExecute( () => { MainFrame.pb.progressBar.SetPercentDuration( perc2, 1000 ); } );

                var process = new Process();
                process.StartInfo.FileName = GetFilesEx( SqlPackTemp, "SqlPackage.exe" );
                process.StartInfo.Arguments =
                    $"/Action:Publish /SourceFile:{dacpath} /TargetServerName:{Computername} /TargetDatabaseName:{DBOPSName} /TargetUser:sa /TargetPassword:{SqlPass} /p:BlockOnPossibleDataLoss=False /p:DropObjectsNotInSource=False /p:IncludeTransactionalScripts=True /p:RegisterDataTierApplication=true /p:ScriptRefreshModule=False /p:BlockWhenDriftDetected=False /p:IgnoreWhitespace=False /p:BackupDatabaseBeforeChanges=True /p:IgnorePermissions=True /p:IncludeCompositeObjects=True /p:IgnoreRoleMembership=True /p:DeployDatabaseInSingleUserMode=True";
                process.StartInfo.ErrorDialog            = false;
                process.StartInfo.UseShellExecute        = false;
                process.StartInfo.CreateNoWindow         = true;
                process.StartInfo.RedirectStandardError  = true;
                process.StartInfo.RedirectStandardInput  = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.OutputDataReceived += async ( _, args ) =>
                {
                    if ( args.Data is not {Length: > 0} ) return;
                    str = args.Data.ToString();
                    num++;
                    perc2 = Math.Round( num++ / 68354 * 100, 0 );
                    if ( str != "Successfully published database." ) return;
                    perc2 = 100;
                    MainFrame.pb.Dispatcher.InvokeOrExecute( () => { MainFrame.pb.progressBar.SetPercentDuration( perc2, 1000 ); } );
                    log( "Successfully published database", Brushes.LightSteelBlue );
                    await Task.Delay( 1000 ).ConfigureAwait( false );
                    UIControlRoundedProgressBar.UiControlRounded.Dispatcher.InvokeOrExecute( UIControlRoundedProgressBar.Stop );
                };

                process.ErrorDataReceived += ( _, e ) =>
                {
                    UIControlRoundedProgressBar.UiControlRounded.Dispatcher.InvokeOrExecute( UIControlRoundedProgressBar.Stop );
                    MainFrame.pb.Dispatcher.InvokeOrExecute( () => { MainFrame.pb.progressBar.SetPercentDuration( 0, 1 ); } );
                    log( e.Data, Brushes.Red );
                    Console.WriteLine( e.Data );
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch ( Exception ex )
            {
                UIControlRoundedProgressBar.UiControlRounded.Dispatcher.InvokeOrExecute( UIControlRoundedProgressBar.Stop );
                MainFrame.pb.Dispatcher.InvokeOrExecute( () => { MainFrame.pb.progressBar.SetPercentDuration( 0, 1 ); } );
                log( ex.Message, Brushes.Red );
                Console.WriteLine( ex );
            }
        } );
    }
}
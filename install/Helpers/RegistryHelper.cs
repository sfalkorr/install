using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using static installEAS.LogHelper;

namespace installEAS
{
    public class RegistryHelper
    {
        public static RegistryKey registry;
        private string _regHive, _regPath;
        public object Read( string inpPath, string inpKey = null )
        {
            _regHive = inpPath.Split( ':' )[0]; _regPath = inpPath.Split( ':' )[1].Trim( '\\' );
            registry = _regHive switch
            {
                "HKLM" => RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry64 ),
                "HKCU" => RegistryKey.OpenBaseKey( RegistryHive.CurrentUser, RegistryView.Registry64 ),
                _ => registry
            };
            try { return (inpKey == null) ? registry.OpenSubKey( _regPath, true ) : registry.OpenSubKey( _regPath, true )?.GetValue( inpKey ); }
            catch (Exception e) { clog( "\"" + inpPath + "\"" + " Key: \"" + inpKey + "\" " + e.Message, ConsoleColor.Red ); return null; }
            finally { registry.Close(); }
        }

        public void Write( string inpPath, string inpKey = null, object inpValue = default, RegistryValueKind Type = default )
        {
            _regHive = inpPath.Split( ':' )[0]; _regPath = inpPath.Split( ':' )[1].Trim( '\\' );
            registry = _regHive switch
            {
                "HKLM" => RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry64 ),
                "HKCU" => RegistryKey.OpenBaseKey( RegistryHive.CurrentUser, RegistryView.Registry64 ),
                _ => registry
            };
            try
            {
                if (inpKey == null) registry.CreateSubKey( _regPath, true );
                else registry.CreateSubKey( _regPath, true ).SetValue( inpKey, inpValue, Type );
            }
            catch (Exception e) { clog( "\"" + inpPath + "\"" + " Key: \"" + inpKey + "\" " + e.Message, ConsoleColor.Red ); }
            finally { registry.Close(); }
        }

        public void WriteMultistring( string inpPath, string inpKey = null, object[] inpValue = default, RegistryValueKind Type = RegistryValueKind.MultiString )
        {
            _regHive = inpPath.Split( ':' )[0]; _regPath = inpPath.Split( ':' )[1].Trim( '\\' );
            registry = _regHive switch
            {
                "HKLM" => RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry64 ),
                "HKCU" => RegistryKey.OpenBaseKey( RegistryHive.CurrentUser, RegistryView.Registry64 ),
                _ => registry
            };
            try
            {
                if (inpKey == null) registry.CreateSubKey( _regPath, true );
                else registry.CreateSubKey( _regPath, RegistryKeyPermissionCheck.ReadWriteSubTree )?.SetValue( inpKey, inpValue, Type );
            }
            catch (Exception e) { clog( "\"" + inpPath + "\"" + " Key: \"" + inpKey + "\" " + e.Message, ConsoleColor.Red ); }
            finally { registry.Close(); }
        }

        public void Remove( string inpPath, string inpKey = null )
        {
            _regHive = inpPath.Split( ':' )[0]; _regPath = inpPath.Split( ':' )[1].Trim( '\\' );
            registry = _regHive switch
            {
                "HKLM" => RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry64 ),
                "HKCU" => RegistryKey.OpenBaseKey( RegistryHive.CurrentUser, RegistryView.Registry64 ),
                _ => registry
            };
            try
            {
                if (inpKey == null) registry.DeleteSubKeyTree( _regPath );
                else registry.OpenSubKey( _regPath, true )?.DeleteValue( inpKey, true );
            }
            catch (Exception e) { clog( "\"" + inpPath + "\"" + " Key: \"" + inpKey + "\" " + e.Message, ConsoleColor.Red ); }
            finally { registry.Close(); }
        }
    }
}

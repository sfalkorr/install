using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Media;
using installEAS.Helpers;
using static installEAS.Variables;
using static installEAS.MainWindow;
using static installEAS.Helpers.Files;
using static installEAS.Helpers.Log;
using static installEAS.Helpers.Functions;
using static installEAS.Helpers.Archive;

namespace installEAS.Common;

public abstract class EnvCheck
{
    public static bool NameCheck(int Server, string Testname)
    {
        return Server switch
               {
                   0 => Regex.Match(Testname, "(?<B>R|C)(\\d{2})-(\\d{6})-([W])(\\d{2}$)").Success,
                   1 => Regex.Match(Testname, "(?<B>R|C)(\\d{2})-(\\d{6})-([N]$)").Success,
                   _ => false
               };
    }

    public static void StartUp()
    {
    }
}
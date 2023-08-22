namespace installEAS.Common;

public abstract class EnvCheck
{
    public static bool NameCheck(int Server, string Testname)
    {
        return Server switch
        {
            0 => Regex.Match(Testname, "(?<B>[RC])(\\d{2})-(\\d{6})-(W)(\\d{2}$)").Success,
            1 => Regex.Match(Testname, "(?<B>[RC])(\\d{2})-(\\d{6})-(N$)").Success,
            _ => false
        };
    }

    public static void StartUp()
    {
    }
}
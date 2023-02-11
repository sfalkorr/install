using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using installEAS.Helpers;
using static installEAS.Helpers.Log;
using static installEAS.Variables;

namespace installEAS.Helpers;

internal static class Password
{
    internal static readonly Random random = new();

    public static string GeneratePass(int Cap, int Sml, int Num, int Spe)
    {
        {
            string rand;
            var    str = RandomStringCap(Cap) + RandomStringNum(Num) + RandomStringSpe(Spe) + RandomStringSml(Sml);
            var    r   = new Random();
            do
            {
                rand = new string(str.ToCharArray().OrderBy(_ => r.Next(2) % 2 == 0).ToArray());
            }
            while (ValidateGenPass(rand));

            return rand;
        }

        string RandomStringCap(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        string RandomStringSml(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        string RandomStringNum(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        string RandomStringSpe(int length)
        {
            const string chars = "!@#$%^&*_-~?";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public static bool ValidatePass(string password)
    {
        var hasNumberChar = new Regex(@"[0-9]");
        var hasUpperChar  = new Regex(@"[A-Z]");
        var hasMinMaxChar = new Regex(@".{10,25}");
        var hasLowerChar  = new Regex(@"[a-z]");
        var hasSymbChar   = new Regex(@"[!@#$%^&*()_+=?-]");
        var hasSpaceChars = new Regex(@"[\s]");
        var hasRepeatChar = new Regex($"([a-zA-Z0-9!@#$%^&*()_+=?-])\\1{{{3 - 1}}}");
        var hasNonEnChar  = new Regex("([а-яА-Я])");

        if (!hasMinMaxChar.IsMatch(password))
        {
            log($"{password} не соответствует политике. Пароль должен быть не менее 10 символов");
            return false;
        }

        if (hasSpaceChars.IsMatch(password))
        {
            log($"{password} не соответствует политике. Пароль не должен содержать пробелы или быть пустым");
            return false;
        }

        if (!hasLowerChar.IsMatch(password))
        {
            log($"{password} не соответствует политике. Пароль должен содержать как минимум один прописной символ");
            return false;
        }

        if (!hasUpperChar.IsMatch(password))
        {
            log($"{password} не соответствует политике. Пароль должен содержать как минимум один заглавный символ");
            return false;
        }

        if (!hasNumberChar.IsMatch(password))
        {
            log($"{password} не соответствует политике. Пароль должен содержать как минимум одну цифру");
            return false;
        }

        if (!hasSymbChar.IsMatch(password))
        {
            log($"{password} не соответствует политике. Пароль должен содержать как минимум один специальный символ из перечисленных: !@#$%^&*()_+=?-");
            return false;
        }

        if (hasRepeatChar.IsMatch(password))
        {
            log($"{password} не соответствует политике. Пароль не должен содержать более двух повторений одинаковых символов подряд");
            return false;
        }

        if (hasNonEnChar.IsMatch(password))
        {
            log($"{password} не соответствует политике. Пароль не должен содержать кирилических символов");
            return false;
        }

        return true;
    }

    public static bool ValidateGenPass(string password)
    {
        var validateChars = new Regex("^(?=.*?[A-Z]).{3,}(?=.*?[a-z])(?=.*?[0-9])(?!.*?[\\s])(?!.*?[а-яА-Я])(?=.*?[!@#$%^&*()_+=?-]).{10,}$");
        var hasRepeatChar = new Regex("([a-zA-Z0-9!@#$%^&*()_+=?-])\\1{" + (3 - 1) + "}");
        return validateChars.IsMatch(password) && !hasRepeatChar.IsMatch(password);
    }

    public static void SaveSqlPassToReg(string pass)
    {
        if (pass == null) return;
        Reg.Write(AppRegPath, "CRC", Crypt.EncryptString(pass));
    }

    public static string ReadSqlPassFromReg()
    {
        var regpass = Reg.Read(AppRegPath, "CRC");
        if (regpass != null) return Crypt.DecryptString(regpass.ToString());
        log("Не найден сохраненный пароль для sa", Brushes.OrangeRed);
        return null;
    }
}
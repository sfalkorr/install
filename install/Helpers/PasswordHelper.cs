using System;
using System.Linq;
using System.Text.RegularExpressions;
using static installEAS.Helpers.Log;

namespace installEAS.Helpers;

internal class Password
{
    internal static readonly Random random = new();

    public string Generate(int Cap, int Sml, int Num, int Spe)
    {
        {
            string rand;
            var    str = RandomStringCap(Cap) + RandomStringNum(Num) + RandomStringSpe(Spe) + RandomStringSml(Sml);
            var    r   = new Random();
            do
            {
                rand = new string(str.ToCharArray().OrderBy(_ => r.Next(2) % 2 == 0).ToArray());
            }
            while (ValidateGen(rand));

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

    public bool Validate(string password)
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

    public static bool ValidateGen(string password)
    {
        var validateChars = new Regex("^(?=.*?[A-Z]).{3,}(?=.*?[a-z])(?=.*?[0-9])(?!.*?[\\s])(?!.*?[а-яА-Я])(?=.*?[!@#$%^&*()_+=?-]).{10,}$");
        var hasRepeatChar = new Regex("([a-zA-Z0-9!@#$%^&*()_+=?-])\\1{" + (3 - 1) + "}");
        return validateChars.IsMatch(password) && !hasRepeatChar.IsMatch(password);
    }
}
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

    private static string shuffle(string input)
    {
        var q = from c in input.ToCharArray() orderby Guid.NewGuid() select c;
        return q.Aggregate(string.Empty, (current, r) => current + r);
    }


    public static string GeneratePass(int Cap, int Sml, int Num, int Spe)
    {
        {
            string rand;
            var    str = RandomStringCap(Cap) + RandomStringNum(Num) + RandomStringSpe(Spe) + RandomStringSml(Sml);
            do { rand = new string(str.ToCharArray().OrderBy(_ => new Random().Next(2) % 2 == 0).ToArray()); }
            while (ValidateGenPass(rand));

            return shuffle(rand);
        }

        string RandomStringCap(int length) { return new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", length).Select(s => s[random.Next(s.Length)]).ToArray()); }
        string RandomStringSml(int length) { return new string(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz", length).Select(s => s[random.Next(s.Length)]).ToArray()); }
        string RandomStringNum(int length) { return new string(Enumerable.Repeat("0123456789", length).Select(s => s[random.Next(s.Length)]).ToArray()); }
        string RandomStringSpe(int length) { return new string(Enumerable.Repeat("!@#$%^&*()_+=?-", length).Select(s => s[random.Next(s.Length)]).ToArray()); }
    }


    public static string ValidatePass(string password)
    {
        if (password == "") return "Пароль не может быть пустым";
        if (new Regex("[а-яА-Я]").IsMatch(password)) return "Пароль не должен содержать кирилических символов";
        if (new Regex("[\\s]").IsMatch(password)) return "Пароль не должен содержать пробелы";
        if (!new Regex("[a-z]").IsMatch(password)) return "Пароль должен содержать как минимум один прописной символ";
        if (!new Regex("[A-Z]").IsMatch(password)) return "Пароль должен содержать как минимум один заглавный символ";
        if (!new Regex("[0-9]").IsMatch(password)) return "Пароль должен содержать как минимум одну цифру";
        if (!new Regex("[!@#$%^&*()_+=?-]").IsMatch(password)) return "Пароль должен содержать минимум один специальный символ: !@#$%^&*()_+=?-";
        if (new Regex($"([a-zA-Z0-9!@#$%^&*()_+=?-])\\1{{{3 - 1}}}").IsMatch(password)) return "Пароль не должен содержать более двух одинаковых символов подряд";
        return !new Regex(".{10,25}").IsMatch(password) ? "Пароль должен быть не менее 10 символов" : "Пароль корректен";
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static installEAS.LogHelper;

namespace installEAS
{
    internal class Password
    {
        internal static Random random = new();

        public string Generate( int Cap, int Sml, int Num, int Spe )
        {
            {
                string rand;
                var str = RandomStringCap( Cap ) + RandomStringNum( Num ) + RandomStringSpe( Spe ) + RandomStringSml( Sml );
                var r = new Random();
                do { rand = new string( str.ToCharArray().OrderBy( s => (r.Next( 2 ) % 2) == 0 ).ToArray() ); } while (ValidateGen( rand ));
                return rand;
            }
            string RandomStringCap( int length )
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                return new string( Enumerable.Repeat( chars, length ).Select( s => s[random.Next( s.Length )] ).ToArray() );
            }
            string RandomStringSml( int length )
            {
                const string chars = "abcdefghijklmnopqrstuvwxyz";
                return new string( Enumerable.Repeat( chars, length ).Select( s => s[random.Next( s.Length )] ).ToArray() );
            }
            string RandomStringNum( int length )
            {
                const string chars = "0123456789";
                return new string( Enumerable.Repeat( chars, length ).Select( s => s[random.Next( s.Length )] ).ToArray() );
            }
            string RandomStringSpe( int length )
            {
                const string chars = "!@#$%^&*_-~?";
                return new string( Enumerable.Repeat( chars, length ).Select( s => s[random.Next( s.Length )] ).ToArray() );
            }
        }
        public bool Validate( string password )
        {

            var hasNumbersCh = new Regex( @"[0-9]" );
            var hasUpperChar = new Regex( @"[A-Z]" );
            var hasMinimMaxiChars = new Regex( @".{10,25}" );
            var hasLowerChar = new Regex( @"[a-z]" );
            var hasSymbolsCh = new Regex( @"[!@#$%^&*()_+=?-]" );
            var hasSpaceChars = new Regex( @"[\s]" );
            var hasRepeatChar = new Regex( "([a-zA-Z0-9!@#$%^&*()_+=?-])\\1{" + (3 - 1) + "}" );
            var hasNonLatinCh = new Regex( "([а-яА-Я])" );
            if (!hasMinimMaxiChars.IsMatch( password )) { mLog( password + " не соответствует политике." + " Пароль должен быть не менее 10 символов" ); return false; }
            else if (hasSpaceChars.IsMatch( password )) { mLog( password + " не соответствует политике." + " Пароль не должен содержать пробелы или быть пустым" ); return false; }
            else if (!hasLowerChar.IsMatch( password )) { mLog( password + " не соответствует политике." + " Пароль должен содержать как минимум один прописной символ" ); return false; }
            else if (!hasUpperChar.IsMatch( password )) { mLog( password + " не соответствует политике." + " Пароль должен содержать как минимум один заглавный символ" ); return false; }
            else if (!hasNumbersCh.IsMatch( password )) { mLog( password + " не соответствует политике." + " Пароль должен содержать как минимум одну цифру" ); return false; }
            else if (!hasSymbolsCh.IsMatch( password )) { mLog( password + " не соответствует политике." + " Пароль должен содержать как минимум один специальный символ из перечисленных: !@#$%^&*()_+=?-" ); return false; }
            else if (hasRepeatChar.IsMatch( password )) { mLog( password + " не соответствует политике." + " Пароль не должен содержать более двух повторений одинаковых символов подряд" ); return false; }
            else if (hasNonLatinCh.IsMatch( password )) { mLog( password + " не соответствует политике." + " Пароль не должен содержать кирилических символов" ); return false; }
            else { return true; }
        }

        public static bool ValidateGen( string password )
        {
            var validateChars = new Regex( "^(?=.*?[A-Z]).{3,}(?=.*?[a-z])(?=.*?[0-9])(?!.*?[\\s])(?!.*?[а-яА-Я])(?=.*?[!@#$%^&*()_+=?-]).{10,}$" );
            var hasRepeatChar = new Regex( "([a-zA-Z0-9!@#$%^&*()_+=?-])\\1{" + (3 - 1) + "}" );
            return validateChars.IsMatch( password ) && !hasRepeatChar.IsMatch( password );
        }


    }
}

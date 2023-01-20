using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace installEAS
{
    public static class ThemesController
    {
        public enum ThemeTypes
        {
            ColorBlue, ColorDark

        }

        public static ThemeTypes CurrentTheme { get; set; }

        private static ResourceDictionary ThemeDictionary
        {
            get { return Application.Current.Resources.MergedDictionaries[0]; }
            set { Application.Current.Resources.MergedDictionaries[0] = value; }
        }

        private static void ChangeTheme( Uri uri )
        {
            ThemeDictionary = new ResourceDictionary() { Source = uri };
        }

        public static void SetTheme( ThemeTypes theme )
        {
            string themeName = null;
            CurrentTheme = theme;
            switch (theme)
            {
                case ThemeTypes.ColorBlue:
                    themeName = "ColorBlue";
                    CurrentTheme = theme;
                    break;
                case ThemeTypes.ColorDark:
                    themeName = "ColorDark";
                    CurrentTheme = theme;
                    break;
            }

            try
            {
                if (!string.IsNullOrEmpty( themeName ))
                {
                    ChangeTheme( new Uri( $"{themeName}.xaml", UriKind.Relative ) );

                }
            }
            catch
            {
            }
        }
    }
}

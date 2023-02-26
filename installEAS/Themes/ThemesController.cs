using System;
using System.Drawing;
using System.Windows;
using static installEAS.MainWindow;
using static installEAS.Helpers.Animate;

namespace installEAS.Themes;

public static class ThemesController
{
    public enum ThemeTypes
    {
        ColorBlue,
        ColorDark,
        ColorGray
    }

    public static ThemeTypes CurrentTheme { get; set; }

    
    private static ResourceDictionary ThemeDictionary { set => Application.Current.Resources.MergedDictionaries[0] = value; }

    private static void ChangeTheme(Uri uri)
    {
        ThemeDictionary = new ResourceDictionary() { Source = uri };
    }

    public static void SetTheme(ThemeTypes theme)
    {
        string themeName;
        CurrentTheme = theme;
        switch (theme)
        {
            case ThemeTypes.ColorBlue:
                themeName    = "ColorBlue";
                CurrentTheme = theme;
                break;
            case ThemeTypes.ColorDark:
                themeName    = "ColorDark";
                CurrentTheme = theme;
                break;
            case ThemeTypes.ColorGray:
                themeName    = "ColorGray";
                CurrentTheme = theme;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
        }

        if (!string.IsNullOrEmpty(themeName)) ChangeTheme(new Uri($"/Themes/{themeName}.xaml", UriKind.Relative));
    }

    public static void ChangeTheme()
    {
        switch (CurrentTheme)
        {
            case ThemeTypes.ColorGray:
                SetTheme(ThemeTypes.ColorDark);
                controlFrom = "#FF343a41";
                controlTo   = "#00343a41";
                closeFrom   = "#FF902020";
                closeTo     = "#00902020";
                if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
                break;
            case ThemeTypes.ColorDark:
                SetTheme(ThemeTypes.ColorBlue);
                controlFrom = "#FF496785";
                controlTo   = "#00496785";
                closeFrom   = "#FF902020";
                closeTo     = "#00496785";
                if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
                break;
            case ThemeTypes.ColorBlue:
                SetTheme(ThemeTypes.ColorGray);
                controlFrom = "#FA5A5F64";
                controlTo   = "#005A5F64";
                closeFrom   = "#FF902020";
                closeTo     = "#005A5F64";
                if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
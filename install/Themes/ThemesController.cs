using System;
using System.Windows;
using static installEAS.MainWindow;
using static installEAS.Helpers.Animate;

namespace installEAS.Themes;

public static class ThemesController
{
    public enum ThemeTypes
    {
        ColorBlue,
        ColorDark
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
            default:
                throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
        }

        try
        {
            if (!string.IsNullOrEmpty(themeName)) ChangeTheme(new Uri($"/Themes/{themeName}.xaml", UriKind.Relative));
        }
        catch
        {
            // ignored
        }
    }

    public static void ChangeTheme()
    {
        switch (CurrentTheme)
        {
            case ThemeTypes.ColorBlue:
                SetTheme(ThemeTypes.ColorDark);
                controlFrom = "#FF242A31";
                controlTo   = "#00242A31";
                closeFrom   = "#FF902020";
                closeTo     = "#00902020";
                if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
                break;
            case ThemeTypes.ColorDark:
                SetTheme(ThemeTypes.ColorBlue);
                controlFrom = "#FF32506E";
                controlTo   = "#0032506E";
                closeFrom   = "#FF902020";
                closeTo     = "#00902020";
                if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
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

    private static ResourceDictionary ThemeDictionary
    {
        set => Application.Current.Resources.MergedDictionaries[0] = value;
    }

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
                themeName = "ColorBlue";
                CurrentTheme = theme;
                break;
            case ThemeTypes.ColorDark:
                themeName = "ColorDark";
                CurrentTheme = theme;
                break;
            case ThemeTypes.ColorGray:
                themeName = "ColorGray";
                CurrentTheme = theme;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
        }

        if (!IsNullOrEmpty(themeName)) ChangeTheme(new Uri($"/Themes/{themeName}.xaml", UriKind.Relative));
    }

    public static void ChangeTheme()
    {
        switch (CurrentTheme)
        {
            case ThemeTypes.ColorGray:
                SetTheme(ThemeTypes.ColorDark);
                if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
                break;
            case ThemeTypes.ColorDark:
                SetTheme(ThemeTypes.ColorBlue);
                if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
                break;
            case ThemeTypes.ColorBlue:
                SetTheme(ThemeTypes.ColorGray);
                if (MainFrame.textBox.IsEnabled) MainFrame.textBox.Focus();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
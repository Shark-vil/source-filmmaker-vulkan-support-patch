using System;
using System.Globalization;

// Interface language: "auto" means Russian when the Windows UI language is Russian, English otherwise.
static class L
{
    public static readonly string[] Ids = { "auto", "en", "ru" };
    public static string Setting = "auto";
    public static bool Ru;

    public static void Load()
    {
        Setting = Settings.Get("language", "auto");
        if (Array.IndexOf(Ids, Setting) < 0) Setting = "auto";
        Ru = Setting == "ru" || (Setting == "auto" && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru");
    }

    public static string T(string en, string ru) { return Ru ? ru : en; }
}

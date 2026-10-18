using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

// Finds the SourceFilmmaker\game folder.
static class GameFolder
{
    public static bool IsValid(string d)
    {
        return d != null && File.Exists(Path.Combine(d, "sfm.exe")) && File.Exists(Path.Combine(d, "bin", "shaderapidx9.dll"));
    }

    // Steam library roots from the registry and steamapps\libraryfolders.vdf.
    static List<string> SteamLibraries()
    {
        List<string> roots = new List<string>();
        foreach (string[] key in new[] {
            new[] { @"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath" },
            new[] { @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath" },
            new[] { @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath" } })
        {
            try
            {
                string steam = Registry.GetValue(key[0], key[1], null) as string;
                if (string.IsNullOrEmpty(steam)) continue;
                steam = steam.Replace('/', '\\');
                if (!roots.Contains(steam)) roots.Add(steam);
                string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                {
                    string p = m.Groups[1].Value.Replace("\\\\", "\\");
                    if (!roots.Contains(p)) roots.Add(p);
                }
            }
            catch { }
        }
        return roots;
    }

    // Next to the exe first, then every Steam library.
    public static string Find()
    {
        string here = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        if (IsValid(here)) return here;
        if (IsValid(Path.Combine(here, "game"))) return Path.Combine(here, "game");
        foreach (string lib in SteamLibraries())
        {
            string d = Path.Combine(lib, @"steamapps\common\SourceFilmmaker\game");
            if (IsValid(d)) return d;
        }
        return null;
    }

    // Accepts either the game folder itself or the SourceFilmmaker folder above it; null if neither.
    public static string Normalize(string d)
    {
        if (IsValid(d)) return d;
        if (IsValid(Path.Combine(d, "game"))) return Path.Combine(d, "game");
        return null;
    }
}

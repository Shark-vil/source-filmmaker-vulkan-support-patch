using System;
using System.Collections.Generic;
using System.IO;

// key=value settings next to the exe.
static class Settings
{
    static string FilePath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SFM_Vulkan_Patcher.ini"); } }

    static List<string> Lines()
    {
        try { return new List<string>(File.ReadAllLines(FilePath)); }
        catch { return new List<string>(); }
    }

    public static string Get(string key, string def)
    {
        foreach (string line in Lines())
            if (line.StartsWith(key + "=")) return line.Substring(key.Length + 1).Trim();
        return def;
    }

    public static void Set(string key, string value)
    {
        List<string> lines = Lines();
        lines.RemoveAll(delegate(string line) { return line.StartsWith(key + "="); });
        lines.Add(key + "=" + value);
        try { File.WriteAllLines(FilePath, lines.ToArray()); }
        catch { } // read-only folder: the choice just won't be remembered
    }
}

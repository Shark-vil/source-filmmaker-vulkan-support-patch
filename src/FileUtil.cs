using System;
using System.IO;
using System.Text;

static class FileUtil
{
    // Writes text to a file; an existing file with different content is first copied to .backup (or .backup-<time>).
    public static void WriteWithBackup(string path, string text)
    {
        if (File.Exists(path))
        {
            if (File.ReadAllText(path) == text) return;
            string bak = path + ".backup";
            if (File.Exists(bak)) bak = path + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(path, bak);
        }
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
}

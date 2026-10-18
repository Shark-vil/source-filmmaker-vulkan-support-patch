using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

enum DxvkState { Off, On, Foreign }

// Installs and removes the embedded DXVK d3d9.dll in SFM's bin folder.
class Dxvk
{
    public const string Version = "3.1.1";

    const string Conf =
        "# DXVK config for Source Filmmaker (32-bit). Used only while bin\\d3d9.dll is DXVK.\r\n" +
        "\r\n" +
        "# Keep managed D3D9 textures outside the 32-bit address space, mapping only this many MiB at once.\r\n" +
        "d3d9.textureMemory = 64\r\n" +
        "\r\n" +
        "# VRAM reported to the game.\r\n" +
        "d3d9.maxAvailableMemory = 4096\r\n" +
        "\r\n" +
        "# Do not let DXVK fake out-of-memory errors.\r\n" +
        "d3d9.memoryTrackTest = False\r\n";

    readonly byte[] dll;
    readonly string dllHash;

    public Dxvk()
    {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("dxvk_d3d9.dll"))
        using (MemoryStream ms = new MemoryStream())
        {
            s.CopyTo(ms);
            dll = ms.ToArray();
        }
        dllHash = Hash(dll);
    }

    static string Hash(byte[] data)
    {
        using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data));
    }

    static string D3d9Path(string gameDir) { return Path.Combine(gameDir, "bin", "d3d9.dll"); }
    static string BackupPath(string gameDir) { return D3d9Path(gameDir) + ".backup"; }

    bool IsOurs(string path)
    {
        return File.Exists(path) && Hash(File.ReadAllBytes(path)) == dllHash;
    }

    static bool SfmRunning()
    {
        return Process.GetProcessesByName("sfm").Length > 0;
    }

    public DxvkState GetState(string gameDir)
    {
        string d3d9 = D3d9Path(gameDir);
        if (IsOurs(d3d9)) return DxvkState.On;
        return File.Exists(d3d9) ? DxvkState.Foreign : DxvkState.Off;
    }

    // Returns the message to show; throws when nothing could be done.
    public string Enable(string gameDir)
    {
        if (SfmRunning()) throw new Exception(L.T("Close SFM first.", "Сначала закройте SFM."));
        string d3d9 = D3d9Path(gameDir), backup = BackupPath(gameDir);
        if (IsOurs(d3d9)) return L.T("Vulkan is already on.", "Vulkan уже включён.");
        if (File.Exists(d3d9))
        {
            if (File.Exists(backup))
                throw new Exception(L.T("bin already has both d3d9.dll and d3d9.dll.backup. Nothing was changed; sort them out manually.",
                                        "В bin уже есть и d3d9.dll, и d3d9.dll.backup. Ничего не изменено — разберитесь с ними вручную."));
            File.Move(d3d9, backup);
        }
        File.WriteAllBytes(d3d9, dll);
        FileUtil.WriteWithBackup(Path.Combine(gameDir, "dxvk.conf"), Conf);
        return L.T("Vulkan enabled.", "Vulkan включён.");
    }

    public string Disable(string gameDir)
    {
        if (SfmRunning()) throw new Exception(L.T("Close SFM first.", "Сначала закройте SFM."));
        string d3d9 = D3d9Path(gameDir), backup = BackupPath(gameDir);
        if (!File.Exists(d3d9)) return L.T("Vulkan is already off.", "Vulkan уже выключен.");
        if (!IsOurs(d3d9))
            throw new Exception(L.T("bin\\d3d9.dll is not the DXVK from this patcher. The file was left alone.",
                                    "bin\\d3d9.dll — не DXVK из этого патчера. Файл не тронут."));
        File.Delete(d3d9);
        string msg = L.T("Vulkan disabled.", "Vulkan выключен.");
        if (File.Exists(backup))
        {
            File.Move(backup, d3d9);
            msg += L.T("\nThe previous d3d9.dll was restored from its backup.", "\nПрежняя d3d9.dll восстановлена из резервной копии.");
        }
        return msg;
    }
}

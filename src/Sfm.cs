using System;
using System.Diagnostics;
using System.IO;

class Preset
{
    public string Name, Id, Description, Args;
    public Preset(string name, string id, string description, string args)
    {
        Name = name; Id = id; Description = description; Args = args;
    }
    public override string ToString() { return Name; }

    // Built on each call: names and descriptions depend on the current language.
    // Archived convars (mat_picmip, mat_forceaniso...) persist in SFM's config, so every preset sets them explicitly.
    public static Preset[] All()
    {
        return new[]
        {
            new Preset(L.T("Minimum", "Минимальный"), "min",
                L.T("Most free memory: half-resolution textures, 1024 shadows, no expensive water. For very heavy scenes.",
                    "Максимум свободной памяти: текстуры в половинном разрешении, тени 1024, без дорогой воды. Для очень тяжёлых сцен."),
                "-sfm_shadowmapres 1024 -monitortexturesize 256 -reflectiontexturesize 256 +mat_envmapsize 32 +mat_picmip 1 +mat_forceaniso 1 +mat_mipmaptextures 1 +r_waterforceexpensive 0 +r_waterforcereflectentities 0"),
            new Preset(L.T("Standard", "Стандартный"), "standard",
                L.T("Close to SFM defaults: 2048 shadows, full-resolution textures.",
                    "Близко к настройкам SFM по умолчанию: тени 2048, полные текстуры."),
                "-sfm_shadowmapres 2048 +mat_envmapsize 128 +mat_picmip 0 +mat_forceaniso 4 +mat_mipmaptextures 1 +r_waterforceexpensive 0 +r_waterforcereflectentities 0"),
            new Preset(L.T("Medium", "Средний"), "medium",
                L.T("Balance of quality and memory: 4096 shadows, 512 reflections, 8x anisotropy.",
                    "Баланс качества и памяти: тени 4096, отражения 512, анизотропия 8x."),
                "-sfm_shadowmapres 4096 -monitortexturesize 512 -reflectiontexturesize 512 +mat_envmapsize 128 +mat_picmip 0 +mat_forceaniso 8 +mat_mipmaptextures 1 +r_waterforceexpensive 1 +r_waterforcereflectentities 1"),
            new Preset(L.T("Maximum", "Максимальный"), "max",
                L.T("8192 shadows (~256 MB per shadowed light), 1024 reflections, 16x anisotropy. Uses the most memory.",
                    "Тени 8192 (~256 МБ на каждый источник с тенями), отражения 1024, анизотропия 16x. Больше всего памяти."),
                "-sfm_shadowmapres 8192 -monitortexturesize 1024 -reflectiontexturesize 1024 +mat_envmapsize 256 +mat_picmip 0 +mat_forceaniso 16 +r_waterforceexpensive 1 +r_waterforcereflectentities 1 +mat_wateroverlaysize 1024 +r_hunkalloclightmaps 0 +mat_mipmaptextures 0"),
        };
    }
}

// Launch options, starting SFM and .bat launchers.
static class Sfm
{
    const string BaseArgs = "-nosteam -nop4 -dxlevel 95 -num_edicts 8192 -sfm_resolution {H} -w {W} -h {H} -sfm_width {W} -sfm_height {H}";

    public static readonly int[][] Resolutions = { new[] { 1280, 720 }, new[] { 1920, 1080 }, new[] { 2560, 1440 }, new[] { 3840, 2160 } };

    public static string BuildArgs(Preset p, int[] res)
    {
        return (BaseArgs + " " + p.Args).Replace("{W}", res[0].ToString()).Replace("{H}", res[1].ToString());
    }

    public static void Launch(string gameDir, string args, bool hud)
    {
        ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(gameDir, "sfm.exe"), args);
        psi.WorkingDirectory = gameDir;
        psi.UseShellExecute = false;
        if (hud) psi.EnvironmentVariables["DXVK_HUD"] = "devinfo,fps,memory";
        Process.Start(psi);
    }

    // Returns the path of the saved file.
    public static string SaveBat(string gameDir, Preset p, int[] res, string args)
    {
        string path = Path.Combine(gameDir, "sfm_" + p.Id + "_" + res[1] + "p.bat");
        string bat = "@echo off\r\nrem SFM preset: " + p.Id + " (made by SFM Vulkan Patcher)\r\ncd /d \"%~dp0\"\r\nstart \"\" sfm.exe " + args + "\r\n";
        FileUtil.WriteWithBackup(path, bat);
        return path;
    }
}

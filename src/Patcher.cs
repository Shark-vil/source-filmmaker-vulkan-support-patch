// SFM Vulkan Patcher: enables/disables DXVK (Vulkan) for Source Filmmaker and launches SFM with quality presets.
// Built with the C# 5 compiler from .NET Framework 4 (no newer syntax).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Changing the language closes the window and opens a new one carrying over its state.
        MainForm form = null;
        do
        {
            L.Load();
            form = new MainForm(form);
            Application.Run(form);
        } while (form.RestartRequested);
    }
}

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

class Preset
{
    public string Name, Id, Description, Args;
    public Preset(string name, string id, string description, string args)
    {
        Name = name; Id = id; Description = description; Args = args;
    }
    public override string ToString() { return Name; }
}

class MainForm : Form
{
    const string DxvkVersion = "3.1.1";
    const string BaseArgs = "-nosteam -nop4 -dxlevel 95 -num_edicts 8192 -sfm_resolution {H} -w {W} -h {H} -sfm_width {W} -sfm_height {H}";
    const string DxvkConf =
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

    // Archived convars (mat_picmip, mat_forceaniso...) persist in SFM's config, so every preset sets them explicitly.
    readonly Preset[] Presets = // per instance: names depend on the current language
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

    static readonly int[][] Resolutions = { new[] { 1280, 720 }, new[] { 1920, 1080 }, new[] { 2560, 1440 }, new[] { 3840, 2160 } };

    string gameDir;
    byte[] dxvkDll;

    Label lblStatus, lblPath, lblDesc;
    Button btnOn, btnOff;
    ComboBox cbPreset, cbRes;
    CheckBox chkHud;
    TextBox txtArgs;
    ComboBox cbLang;

    public bool RestartRequested;

    static string AppVersion
    {
        get
        {
            object[] a = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            return a.Length > 0 ? ((AssemblyInformationalVersionAttribute)a[0]).InformationalVersion : "dev";
        }
    }

    public MainForm(MainForm prev)
    {
        Text = "SFM Vulkan Patcher " + AppVersion + " (DXVK " + DxvkVersion + ")";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = new Font("Segoe UI", 9f);
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 500);

        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("dxvk_d3d9.dll"))
        using (MemoryStream ms = new MemoryStream())
        {
            s.CopyTo(ms);
            dxvkDll = ms.ToArray();
        }

        // --- Vulkan group
        GroupBox gVk = new GroupBox { Text = L.T("Vulkan for SFM (DXVK)", "Vulkan для SFM (DXVK)"), Left = 12, Top = 10, Width = 536, Height = 100 };
        lblStatus = new Label { Left = 14, Top = 26, Width = 508, Height = 22, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
        btnOn = new Button { Text = L.T("Enable Vulkan", "Включить Vulkan"), Left = 14, Top = 56, Width = 160, Height = 30 };
        btnOff = new Button { Text = L.T("Disable Vulkan", "Выключить Vulkan"), Left = 184, Top = 56, Width = 160, Height = 30 };
        btnOn.Click += delegate { Guard(EnableDxvk); };
        btnOff.Click += delegate { Guard(DisableDxvk); };
        gVk.Controls.AddRange(new Control[] { lblStatus, btnOn, btnOff });

        // --- Launch group
        GroupBox gRun = new GroupBox { Text = L.T("Launch SFM", "Запуск SFM"), Left = 12, Top = 118, Width = 536, Height = 296 };
        Label l1 = new Label { Text = L.T("Preset:", "Пресет:"), Left = 14, Top = 28, Width = 60 };
        cbPreset = new ComboBox { Left = 80, Top = 24, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        cbPreset.Items.AddRange(Presets);
        Label l2 = new Label { Text = L.T("Resolution:", "Разрешение:"), Left = 266, Top = 28, Width = 80 };
        cbRes = new ComboBox { Left = 350, Top = 24, Width = 172, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (int[] r in Resolutions) cbRes.Items.Add(r[0] + " x " + r[1]);
        lblDesc = new Label { Left = 14, Top = 56, Width = 508, Height = 36, Tag = "muted" };
        Label l3 = new Label { Text = L.T("Launch options (you can edit them):", "Параметры запуска (можно править вручную):"), Left = 14, Top = 96, Width = 400 };
        txtArgs = new TextBox { Left = 14, Top = 116, Width = 508, Height = 96, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 8.5f) };
        chkHud = new CheckBox { Text = L.T("DXVK overlay (FPS, memory), only when launched from here", "Оверлей DXVK (FPS, память) — только при запуске отсюда"), Left = 14, Top = 218, Width = 508 };
        Button btnRun = new Button { Text = L.T("Launch SFM", "Запустить SFM"), Left = 14, Top = 248, Width = 160, Height = 32 };
        Button btnBat = new Button { Text = L.T("Save as .bat", "Сохранить как .bat"), Left = 184, Top = 248, Width = 160, Height = 32 };
        btnRun.Click += delegate { Guard(LaunchSfm); };
        btnBat.Click += delegate { Guard(SaveBat); };
        cbPreset.SelectedIndexChanged += delegate { UpdateArgs(); };
        cbRes.SelectedIndexChanged += delegate { UpdateArgs(); };
        gRun.Controls.AddRange(new Control[] { l1, cbPreset, l2, cbRes, lblDesc, l3, txtArgs, chkHud, btnRun, btnBat });

        // --- Folder
        lblPath = new Label { Left = 12, Top = 428, Width = 430, Height = 32, Tag = "muted" };
        Button btnDir = new Button { Text = L.T("SFM folder...", "Папка SFM..."), Left = 448, Top = 426, Width = 100, Height = 28 };
        btnDir.Click += delegate { PickDir(); };

        // --- Theme
        Label l4 = new Label { Text = L.T("Theme:", "Тема интерфейса:"), Left = 12, Top = 468, Width = 110 };
        cbTheme = new ComboBox { Left = 124, Top = 464, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        cbTheme.Items.AddRange(new object[] { L.T("System", "Как в системе"), L.T("Light", "Светлая"), L.T("Dark", "Тёмная") });

        // --- Language (language names are not translated)
        Label l5 = new Label { Text = L.T("Language:", "Язык:"), Left = 290, Top = 468, Width = 80 };
        cbLang = new ComboBox { Left = 374, Top = 464, Width = 174, DropDownStyle = ComboBoxStyle.DropDownList };
        cbLang.Items.AddRange(new object[] { L.T("System", "Как в системе"), "English", "Русский" });

        Controls.AddRange(new Control[] { gVk, gRun, lblPath, btnDir, l4, cbTheme, l5, cbLang });

        if (prev != null)
        {
            StartPosition = FormStartPosition.Manual;
            Location = prev.Location;
            gameDir = prev.gameDir;
            cbPreset.SelectedIndex = prev.cbPreset.SelectedIndex;
            cbRes.SelectedIndex = prev.cbRes.SelectedIndex;
            txtArgs.Text = prev.txtArgs.Text; // keep manual edits
            chkHud.Checked = prev.chkHud.Checked;
        }
        else
        {
            cbPreset.SelectedIndex = 2;
            cbRes.SelectedIndex = 1;
            gameDir = FindGameDir();
        }

        cbTheme.SelectedIndex = Array.IndexOf(ThemeIds, Settings.Get("theme", "auto"));
        if (cbTheme.SelectedIndex < 0) cbTheme.SelectedIndex = 0;
        cbTheme.SelectedIndexChanged += delegate { Settings.Set("theme", ThemeIds[cbTheme.SelectedIndex]); ApplyTheme(); };
        cbLang.SelectedIndex = Array.IndexOf(L.Ids, L.Setting);
        cbLang.SelectedIndexChanged += delegate
        {
            Settings.Set("language", L.Ids[cbLang.SelectedIndex]);
            RestartRequested = true;
            Close();
        };
        SystemEvents.UserPreferenceChanged += OnSystemPrefsChanged;
        ApplyTheme();
    }

    // ---------- theme

    static readonly string[] ThemeIds = { "auto", "light", "dark" };
    ComboBox cbTheme;
    bool dark;

    static readonly Color DarkBg = Color.FromArgb(32, 32, 32);
    static readonly Color DarkInput = Color.FromArgb(45, 45, 48);
    static readonly Color DarkButton = Color.FromArgb(58, 58, 62);
    static readonly Color DarkBorder = Color.FromArgb(90, 90, 96);
    static readonly Color DarkText = Color.FromArgb(230, 230, 230);
    static readonly Color DarkMuted = Color.FromArgb(155, 155, 160);

    static bool SystemUsesDark()
    {
        try
        {
            object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
            return v is int && (int)v == 0;
        }
        catch { return false; }
    }

    void OnSystemPrefsChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (cbTheme.SelectedIndex == 0 && IsHandleCreated) BeginInvoke(new Action(ApplyTheme));
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnSystemPrefsChanged; // static event: must unsubscribe
        base.OnFormClosed(e);
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    void SetDarkTitleBar(bool on)
    {
        int v = on ? 1 : 0;
        // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Win10 2004+), 19 = same on older Win10 builds.
        if (DwmSetWindowAttribute(Handle, 20, ref v, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref v, 4);
    }

    void ApplyTheme()
    {
        int sel = cbTheme.SelectedIndex;
        dark = sel == 2 || (sel == 0 && SystemUsesDark());
        SuspendLayout();
        Style(this);
        ResumeLayout();
        if (IsHandleCreated) SetDarkTitleBar(dark);
        RefreshStatus();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SetDarkTitleBar(dark);
    }

    void Style(Control c)
    {
        bool muted = "muted".Equals(c.Tag);
        if (c is Button)
        {
            Button b = (Button)c;
            if (dark)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.BackColor = DarkButton;
                b.FlatAppearance.BorderColor = DarkBorder;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(74, 74, 80);
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 90, 98);
                b.ForeColor = DarkText;
            }
            else
            {
                b.FlatStyle = FlatStyle.Standard;
                b.BackColor = SystemColors.Control;
                b.UseVisualStyleBackColor = true;
                b.ForeColor = SystemColors.ControlText;
            }
        }
        else if (c is TextBox || c is ComboBox)
        {
            c.BackColor = dark ? DarkInput : SystemColors.Window;
            c.ForeColor = dark ? DarkText : SystemColors.WindowText;
            if (c is TextBox) ((TextBox)c).BorderStyle = dark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
            // Undocumented but stable Windows 10/11 themes: dark scrollbars and dark combo drop-down button.
            SetWindowTheme(c.Handle, dark ? (c is ComboBox ? "DarkMode_CFD" : "DarkMode_Explorer") : null, null);
        }
        else
        {
            c.BackColor = dark ? DarkBg : SystemColors.Control;
            c.ForeColor = muted ? (dark ? DarkMuted : SystemColors.GrayText) : (dark ? DarkText : SystemColors.ControlText);
            if (c is CheckBox) ((CheckBox)c).FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard;
        }
        foreach (Control child in c.Controls) Style(child);
    }

    // ---------- helpers

    static bool IsGameDir(string d)
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

    static string FindGameDir()
    {
        string here = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        if (IsGameDir(here)) return here;
        if (IsGameDir(Path.Combine(here, "game"))) return Path.Combine(here, "game");
        foreach (string lib in SteamLibraries())
        {
            string d = Path.Combine(lib, @"steamapps\common\SourceFilmmaker\game");
            if (IsGameDir(d)) return d;
        }
        return null;
    }

    void PickDir()
    {
        using (FolderBrowserDialog dlg = new FolderBrowserDialog())
        {
            dlg.Description = L.T("Select the SourceFilmmaker\\game folder (the one with sfm.exe)", "Выберите папку SourceFilmmaker\\game (где лежит sfm.exe)");
            if (gameDir != null) dlg.SelectedPath = gameDir;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            string d = dlg.SelectedPath;
            if (!IsGameDir(d) && IsGameDir(Path.Combine(d, "game"))) d = Path.Combine(d, "game");
            if (!IsGameDir(d))
            {
                MessageBox.Show(this, L.T("This folder has no sfm.exe and bin\\shaderapidx9.dll.", "В этой папке нет sfm.exe и bin\\shaderapidx9.dll."),
                    L.T("Wrong folder", "Не та папка"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            gameDir = d;
            RefreshStatus();
        }
    }

    string D3d9Path { get { return Path.Combine(gameDir, "bin", "d3d9.dll"); } }
    string BackupPath { get { return D3d9Path + ".backup"; } }

    static string Hash(byte[] data)
    {
        using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data));
    }

    bool IsOurDll(string path)
    {
        return File.Exists(path) && Hash(File.ReadAllBytes(path)) == Hash(dxvkDll);
    }

    static bool SfmRunning()
    {
        return Process.GetProcessesByName("sfm").Length > 0;
    }

    void Guard(Action a)
    {
        try { a(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, L.T("Error", "Ошибка"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        RefreshStatus();
    }

    void Info(string text)
    {
        MessageBox.Show(this, text, L.T("Done", "Готово"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void RefreshStatus()
    {
        bool ok = gameDir != null;
        lblPath.Text = ok ? L.T("SFM folder: ", "Папка SFM: ") + gameDir
                          : L.T("SFM folder not found, click \"SFM folder...\"", "Папка SFM не найдена — нажмите «Папка SFM...»");
        btnOn.Enabled = btnOff.Enabled = ok;
        if (!ok) { lblStatus.Text = "—"; lblStatus.ForeColor = dark ? DarkText : SystemColors.ControlText; return; }

        if (IsOurDll(D3d9Path))
        {
            lblStatus.Text = L.T("Vulkan is ON (DXVK ", "Vulkan ВКЛЮЧЁН (DXVK ") + DxvkVersion + ")";
            lblStatus.ForeColor = dark ? Color.FromArgb(87, 204, 110) : Color.ForestGreen;
        }
        else if (File.Exists(D3d9Path))
        {
            lblStatus.Text = L.T("bin has a different d3d9.dll (not this DXVK build)", "В bin лежит другая d3d9.dll (не эта версия DXVK)");
            lblStatus.ForeColor = dark ? Color.FromArgb(240, 170, 70) : Color.DarkOrange;
        }
        else
        {
            lblStatus.Text = L.T("Vulkan is off (stock DirectX 9)", "Vulkan выключен (стандартный DirectX 9)");
            lblStatus.ForeColor = dark ? Color.FromArgb(240, 110, 110) : Color.Firebrick;
        }
    }

    void UpdateArgs()
    {
        Preset p = cbPreset.SelectedItem as Preset;
        if (p == null || cbRes.SelectedIndex < 0) return;
        int[] r = Resolutions[cbRes.SelectedIndex];
        txtArgs.Text = (BaseArgs + " " + p.Args).Replace("{W}", r[0].ToString()).Replace("{H}", r[1].ToString());
        lblDesc.Text = p.Description;
    }

    // Writes text to a file; an existing file with different content is first copied to .backup (or .backup-<time>).
    static void WriteWithBackup(string path, string text)
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

    // ---------- actions

    void EnableDxvk()
    {
        if (SfmRunning()) throw new Exception(L.T("Close SFM first.", "Сначала закройте SFM."));
        if (IsOurDll(D3d9Path)) { MessageBox.Show(this, L.T("Vulkan is already on.", "Vulkan уже включён.")); return; }
        if (File.Exists(D3d9Path))
        {
            if (File.Exists(BackupPath))
                throw new Exception(L.T("bin already has both d3d9.dll and d3d9.dll.backup. Nothing was changed; sort them out manually.",
                                        "В bin уже есть и d3d9.dll, и d3d9.dll.backup. Ничего не изменено — разберитесь с ними вручную."));
            File.Move(D3d9Path, BackupPath);
        }
        File.WriteAllBytes(D3d9Path, dxvkDll);
        WriteWithBackup(Path.Combine(gameDir, "dxvk.conf"), DxvkConf);
        Info(L.T("Vulkan enabled.", "Vulkan включён."));
    }

    void DisableDxvk()
    {
        if (SfmRunning()) throw new Exception(L.T("Close SFM first.", "Сначала закройте SFM."));
        if (!File.Exists(D3d9Path)) { MessageBox.Show(this, L.T("Vulkan is already off.", "Vulkan уже выключен.")); return; }
        if (!IsOurDll(D3d9Path))
            throw new Exception(L.T("bin\\d3d9.dll is not the DXVK from this patcher. The file was left alone.",
                                    "bin\\d3d9.dll — не DXVK из этого патчера. Файл не тронут."));
        File.Delete(D3d9Path);
        string msg = L.T("Vulkan disabled.", "Vulkan выключен.");
        if (File.Exists(BackupPath))
        {
            File.Move(BackupPath, D3d9Path);
            msg += L.T("\nThe previous d3d9.dll was restored from its backup.", "\nПрежняя d3d9.dll восстановлена из резервной копии.");
        }
        Info(msg);
    }

    string Args { get { return txtArgs.Text.Replace("\r", " ").Replace("\n", " ").Trim(); } }

    void LaunchSfm()
    {
        if (gameDir == null) throw new Exception(L.T("No SFM folder selected.", "Не выбрана папка SFM."));
        ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(gameDir, "sfm.exe"), Args);
        psi.WorkingDirectory = gameDir;
        psi.UseShellExecute = false;
        if (chkHud.Checked) psi.EnvironmentVariables["DXVK_HUD"] = "devinfo,fps,memory";
        Process.Start(psi);
    }

    void SaveBat()
    {
        if (gameDir == null) throw new Exception(L.T("No SFM folder selected.", "Не выбрана папка SFM."));
        Preset p = (Preset)cbPreset.SelectedItem;
        string name = "sfm_" + p.Id + "_" + Resolutions[cbRes.SelectedIndex][1] + "p.bat";
        string path = Path.Combine(gameDir, name);
        string bat = "@echo off\r\nrem SFM preset: " + p.Id + " (made by SFM Vulkan Patcher)\r\ncd /d \"%~dp0\"\r\nstart \"\" sfm.exe " + Args + "\r\n";
        WriteWithBackup(path, bat);
        Info(L.T("Saved: ", "Сохранено: ") + path);
    }
}

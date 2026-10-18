using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

class MainForm : Form
{
    readonly Dxvk dxvk = new Dxvk();
    string gameDir;
    bool dark;

    Label lblStatus, lblPath, lblDesc;
    Button btnOn, btnOff;
    ComboBox cbPreset, cbRes, cbTheme, cbLang;
    CheckBox chkHud;
    TextBox txtArgs;

    public FormState RestartState; // set when the window closes to reopen in another language

    static string AppVersion
    {
        get
        {
            object[] a = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            return a.Length > 0 ? ((AssemblyInformationalVersionAttribute)a[0]).InformationalVersion : "dev";
        }
    }

    public MainForm(FormState prev)
    {
        Text = "SFM Vulkan Patcher " + AppVersion + " (DXVK " + Dxvk.Version + ")";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = new Font("Segoe UI", 9f);
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 500);

        // --- Vulkan group
        GroupBox gVk = new GroupBox { Text = L.T("Vulkan for SFM (DXVK)", "Vulkan для SFM (DXVK)"), Left = 12, Top = 10, Width = 536, Height = 100 };
        lblStatus = new Label { Left = 14, Top = 26, Width = 508, Height = 22, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
        btnOn = new Button { Text = L.T("Enable Vulkan", "Включить Vulkan"), Left = 14, Top = 56, Width = 160, Height = 30 };
        btnOff = new Button { Text = L.T("Disable Vulkan", "Выключить Vulkan"), Left = 184, Top = 56, Width = 160, Height = 30 };
        btnOn.Click += delegate { Guard(delegate { Info(dxvk.Enable(gameDir)); }); };
        btnOff.Click += delegate { Guard(delegate { Info(dxvk.Disable(gameDir)); }); };
        gVk.Controls.AddRange(new Control[] { lblStatus, btnOn, btnOff });

        // --- Launch group
        GroupBox gRun = new GroupBox { Text = L.T("Launch SFM", "Запуск SFM"), Left = 12, Top = 118, Width = 536, Height = 296 };
        Label l1 = new Label { Text = L.T("Preset:", "Пресет:"), Left = 14, Top = 28, Width = 60 };
        cbPreset = new ComboBox { Left = 80, Top = 24, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        cbPreset.Items.AddRange(Preset.All());
        Label l2 = new Label { Text = L.T("Resolution:", "Разрешение:"), Left = 266, Top = 28, Width = 80 };
        cbRes = new ComboBox { Left = 350, Top = 24, Width = 172, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (int[] r in Sfm.Resolutions) cbRes.Items.Add(r[0] + " x " + r[1]);
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
            gameDir = prev.GameDir;
            cbPreset.SelectedIndex = prev.Preset;
            cbRes.SelectedIndex = prev.Resolution;
            txtArgs.Text = prev.Args; // keep manual edits
            chkHud.Checked = prev.Hud;
        }
        else
        {
            cbPreset.SelectedIndex = 2;
            cbRes.SelectedIndex = 1;
            gameDir = GameFolder.Find();
        }

        cbTheme.SelectedIndex = Array.IndexOf(Theme.Ids, Settings.Get("theme", "auto"));
        if (cbTheme.SelectedIndex < 0) cbTheme.SelectedIndex = 0;
        cbTheme.SelectedIndexChanged += delegate { Settings.Set("theme", Theme.Ids[cbTheme.SelectedIndex]); ApplyTheme(); };
        cbLang.SelectedIndex = Array.IndexOf(L.Ids, L.Setting);
        cbLang.SelectedIndexChanged += delegate
        {
            Settings.Set("language", L.Ids[cbLang.SelectedIndex]);
            // Captured before Close(): the controls are disposed once the window closes.
            RestartState = new FormState
            {
                Location = Location, GameDir = gameDir, Preset = cbPreset.SelectedIndex,
                Resolution = cbRes.SelectedIndex, Args = txtArgs.Text, Hud = chkHud.Checked
            };
            Close();
        };
        SystemEvents.UserPreferenceChanged += OnSystemPrefsChanged;
        ApplyTheme();
    }

    // ---------- theme

    void ApplyTheme()
    {
        int sel = cbTheme.SelectedIndex;
        dark = sel == 2 || (sel == 0 && Theme.SystemUsesDark());
        SuspendLayout();
        Theme.Apply(this, dark);
        ResumeLayout();
        if (IsHandleCreated) Theme.SetDarkTitleBar(Handle, dark);
        RefreshStatus();
    }

    void OnSystemPrefsChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (cbTheme.SelectedIndex == 0 && IsHandleCreated) BeginInvoke(new Action(ApplyTheme));
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.SetDarkTitleBar(Handle, dark);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnSystemPrefsChanged; // static event: must unsubscribe
        base.OnFormClosed(e);
    }

    // ---------- helpers

    void PickDir()
    {
        using (FolderBrowserDialog dlg = new FolderBrowserDialog())
        {
            dlg.Description = L.T("Select the SourceFilmmaker\\game folder (the one with sfm.exe)", "Выберите папку SourceFilmmaker\\game (где лежит sfm.exe)");
            if (gameDir != null) dlg.SelectedPath = gameDir;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            string d = GameFolder.Normalize(dlg.SelectedPath);
            if (d == null)
            {
                MessageBox.Show(this, L.T("This folder has no sfm.exe and bin\\shaderapidx9.dll.", "В этой папке нет sfm.exe и bin\\shaderapidx9.dll."),
                    L.T("Wrong folder", "Не та папка"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            gameDir = d;
            RefreshStatus();
        }
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
        if (!ok) { lblStatus.Text = "—"; lblStatus.ForeColor = dark ? Theme.DarkText : SystemColors.ControlText; return; }

        switch (dxvk.GetState(gameDir))
        {
            case DxvkState.On:
                lblStatus.Text = L.T("Vulkan is ON (DXVK ", "Vulkan ВКЛЮЧЁН (DXVK ") + Dxvk.Version + ")";
                lblStatus.ForeColor = dark ? Color.FromArgb(87, 204, 110) : Color.ForestGreen;
                break;
            case DxvkState.Foreign:
                lblStatus.Text = L.T("bin has a different d3d9.dll (not this DXVK build)", "В bin лежит другая d3d9.dll (не эта версия DXVK)");
                lblStatus.ForeColor = dark ? Color.FromArgb(240, 170, 70) : Color.DarkOrange;
                break;
            default:
                lblStatus.Text = L.T("Vulkan is off (stock DirectX 9)", "Vulkan выключен (стандартный DirectX 9)");
                lblStatus.ForeColor = dark ? Color.FromArgb(240, 110, 110) : Color.Firebrick;
                break;
        }
    }

    void UpdateArgs()
    {
        Preset p = cbPreset.SelectedItem as Preset;
        if (p == null || cbRes.SelectedIndex < 0) return;
        txtArgs.Text = Sfm.BuildArgs(p, Sfm.Resolutions[cbRes.SelectedIndex]);
        lblDesc.Text = p.Description;
    }

    // ---------- actions

    string Args { get { return txtArgs.Text.Replace("\r", " ").Replace("\n", " ").Trim(); } }

    void LaunchSfm()
    {
        if (gameDir == null) throw new Exception(L.T("No SFM folder selected.", "Не выбрана папка SFM."));
        Sfm.Launch(gameDir, Args, chkHud.Checked);
    }

    void SaveBat()
    {
        if (gameDir == null) throw new Exception(L.T("No SFM folder selected.", "Не выбрана папка SFM."));
        string path = Sfm.SaveBat(gameDir, (Preset)cbPreset.SelectedItem, Sfm.Resolutions[cbRes.SelectedIndex], Args);
        Info(L.T("Saved: ", "Сохранено: ") + path);
    }
}

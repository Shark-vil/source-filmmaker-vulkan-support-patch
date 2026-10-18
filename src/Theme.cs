using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

// Light/dark styling of WinForms controls.
static class Theme
{
    public static readonly string[] Ids = { "auto", "light", "dark" };

    public static readonly Color DarkBg = Color.FromArgb(32, 32, 32);
    public static readonly Color DarkInput = Color.FromArgb(45, 45, 48);
    public static readonly Color DarkButton = Color.FromArgb(58, 58, 62);
    public static readonly Color DarkBorder = Color.FromArgb(90, 90, 96);
    public static readonly Color DarkText = Color.FromArgb(230, 230, 230);
    public static readonly Color DarkMuted = Color.FromArgb(155, 155, 160);

    public static bool SystemUsesDark()
    {
        try
        {
            object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
            return v is int && (int)v == 0;
        }
        catch { return false; }
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void SetDarkTitleBar(IntPtr hwnd, bool on)
    {
        int v = on ? 1 : 0;
        // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Win10 2004+), 19 = same on older Win10 builds.
        if (DwmSetWindowAttribute(hwnd, 20, ref v, 4) != 0) DwmSetWindowAttribute(hwnd, 19, ref v, 4);
    }

    // Styles c and its children. Controls with Tag = "muted" get secondary text color.
    public static void Apply(Control c, bool dark)
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
        foreach (Control child in c.Controls) Apply(child, dark);
    }
}

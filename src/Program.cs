// SFM Vulkan Patcher: enables/disables DXVK (Vulkan) for Source Filmmaker and launches SFM with quality presets.
// Built with the C# 5 compiler from .NET Framework 4 (no newer syntax).
using System;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Changing the language closes the window and opens a new one carrying over its state.
        FormState state = null;
        do
        {
            L.Load();
            MainForm form = new MainForm(state);
            Application.Run(form);
            state = form.RestartState;
        } while (state != null);
    }
}

using System.Drawing;

// What the window carries over when it is reopened after a language change.
class FormState
{
    public Point Location;
    public string GameDir, Args;
    public int Preset, Resolution;
    public bool Hud;
}

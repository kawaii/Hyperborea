using Dalamud.Interface.Components;
using ECommons.SimpleGui;

namespace Hyperborea.Gui;
public class SettingsWindow : Window
{
    public SettingsWindow() : base("Hyperborea settings")
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        if (ImGuiGroup.BeginGroupBox("Danger Zone", EColor.RedBright.ToUint()))
        {
            if (P.Enabled) ImGui.BeginDisabled();
            ImGui.Checkbox("Disable Zone Lock", ref C.DisableInnCheck);
            ImGuiComponents.HelpMarker("Removes the inn room requirement for Hyperborea to function. Potentially dangerous if the packet filter fails for any reason while operating in a public area.");
            if (P.Enabled)
            {
                ImGui.EndDisabled();
                ImGuiEx.TextWrapped(EColor.RedBright, "You can not change settings while plugin is enabled");
            }
            ImGuiGroup.EndGroupBox();
        }
    }
}

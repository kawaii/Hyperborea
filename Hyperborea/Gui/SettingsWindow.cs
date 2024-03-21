using Dalamud.Interface.Components;
using ECommons.SimpleGui;
using Lumina.Excel.GeneratedSheets;

namespace Hyperborea.Gui;
public class SettingsWindow : Window
{
    public SettingsWindow() : base("Hyperborea settings")
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        if (ImGuiGroup.BeginGroupBox("General settings"))
        {
            ImGuiEx.Text($"Mount:");
            ImGuiEx.SetNextItemFullWidth(-10);
            if (ImGui.BeginCombo($"##mount", Utils.GetMountName(C.CurrentMount) ?? "Select a mount..."))
            {
                ImGui.SetNextItemWidth(150f);
                ImGui.InputTextWithHint("##search", "Filter", ref UI.MountFilter, 50);
                if (ImGui.Selectable("No mount"))
                {
                    C.CurrentMount = 0;
                }
                foreach (var x in Svc.Data.GetExcelSheet<Mount>())
                {
                    var name = Utils.GetMountName(x.RowId);
                    if (!name.IsNullOrEmpty())
                    {
                        if (UI.MountFilter.IsNullOrEmpty() || name.Contains(UI.MountFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            if (ImGui.Selectable(name))
                            {
                                C.CurrentMount = x.RowId;
                            }
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGuiGroup.EndGroupBox();
        }

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

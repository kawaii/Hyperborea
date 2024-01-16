using ECommons.GameHelpers;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea.Gui;
public unsafe class CompassWindow : Window
{
    Point3 Position = new();
    public CompassWindow() : base("Hyperborea Compass", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.IsOpen = true;
        this.RespectCloseHotkey = false;
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override bool DrawConditions()
    {
        if (!P.Enabled) return false;
        var layout = Utils.GetLayout();
        Utils.TryGetZoneData(layout, out var info);
        if (P.Enabled && layout != null) return true;
        return false;
    }

    public override void Draw()
    {
        var layout = Utils.GetLayout();
        Utils.TryGetZoneData(layout, out var info, out var isOverriden);
        if (P.Enabled && layout != null)
        {
            var array = info?.Phases ?? [];
            var phase = Utils.GetPhase(Svc.ClientState.TerritoryType);
            var index = array.IndexOf(phase);

            ImGuiEx.SetNextItemWidthScaled(250f);
            if(ImGui.BeginCombo("##selphase", $"{phase?.Name.NullWhenEmpty() ?? "Select phase"}"))
            {
                foreach(var x in array)
                {
                    if (ImGui.Selectable(x.Name + $"##{x.GUID}"))
                    {
                        x.SwitchTo();
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();

            if (array.Count < 2) ImGui.BeginDisabled(); 

            if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                if (index > 0)
                {
                    array[index - 1].SwitchTo();
                }
            }

            ImGui.SameLine();

            if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowRight))
            {
                if (index < array.Count - 1)
                {
                    array[index + 1].SwitchTo();
                }
            }
            if (array.Count < 2) ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGuiEx.IconButton("\uf303"))
            {
                P.EditorWindow.IsOpen = true;
                P.EditorWindow.SelectedTerritory = Svc.ClientState.TerritoryType;
            }


            UI.CoordBlock("X:", ref Position.X);
            ImGui.SameLine();
            UI.CoordBlock("Y:", ref Position.Y);
            ImGui.SameLine();
            UI.CoordBlock("Z:", ref Position.Z);
            ImGui.SameLine();
            if (ImGuiEx.IconButton("\uf3c5"))
            {
                Player.GameObject->SetPosition(Position.X, Position.Y, Position.Z);
            }
            ImGuiEx.Tooltip("Teleport to the configured coordinates.");
            ImGui.SameLine();
            if (ImGuiEx.IconButton("\uf030"))
            {
                var cam = (CameraEx*)CameraManager.Instance()->GetActiveCamera();
                Player.GameObject->SetPosition(cam->x, cam->y, cam->z);
            }
            ImGuiEx.Tooltip("Teleport to the location of the camera.");
        }
    }
}

using ECommons.GameHelpers;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea.Gui;
public unsafe class CompassWindow : Window
{
    public Point3 PlayerPosition = new();
    string FestFilter = "";

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
        Utils.TryGetZoneInfo(layout, out var info);
        if (P.Enabled && layout != null) return true;
        return false;
    }

    public override void Draw()
    {
        var layout = Utils.GetLayout();
        Utils.TryGetZoneInfo(layout, out var info, out var isOverriden);
        if (P.Enabled && layout != null)
        {
            var array = info?.Phases ?? [];
            var phase = Utils.GetPhase(Svc.ClientState.TerritoryType);
            var index = array.IndexOf(phase);

            ImGui.SetNextItemWidth(250f);
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


            UI.CoordBlock("X:", ref PlayerPosition.X);
            ImGui.SameLine();
            UI.CoordBlock("Y:", ref PlayerPosition.Y);
            ImGui.SameLine();
            UI.CoordBlock("Z:", ref PlayerPosition.Z);
            ImGui.SameLine();
            if (ImGuiEx.IconButton("\uf3c5"))
            {
                Player.GameObject->SetPosition(PlayerPosition.X, PlayerPosition.Y, PlayerPosition.Z);
            }
            ImGuiEx.Tooltip("Teleport to the configured coordinates.");
            ImGui.SameLine();
            if (ImGuiEx.IconButton("\uf030"))
            {
                var cam = (CameraEx*)CameraManager.Instance()->GetActiveCamera();
                Player.GameObject->SetPosition(cam->x, cam->y, cam->z);
            }
            ImGuiEx.Tooltip("Teleport to the location of the camera.");
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.ButtonCheckbox("\uf05b", ref C.FastTeleport);
            ImGui.PopFont();
            ImGuiEx.Tooltip("Enables CTRL + click to teleport to mouse cursor location."); 

            ImGui.SetNextItemWidth(200f);
            if(ImGui.BeginCombo($"##mount", Utils.GetMountName(C.CurrentMount) ?? "Select a mount..."))
            {
                ImGui.SetNextItemWidth(150f);
                ImGui.InputTextWithHint("##search", "Filter", ref UI.MountFilter, 50);
                if (ImGui.Selectable("No mount"))
                {
                    C.CurrentMount = 0;
                }
                foreach(var x in Svc.Data.GetExcelSheet<Mount>())
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
            ImGui.SameLine();
            if (ImGuiEx.IconButton("\uf206"))
            {
                Player.Character->Mount.CreateAndSetupMount((short)(Svc.Condition[ConditionFlag.Mounted] ? 0 : C.CurrentMount), 0, 0, 0, 0, 0, 0);
            }

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.ButtonCheckbox("\uf072", ref C.ForcedFlight);
            ImGui.PopFont();
            ImGuiEx.Tooltip("Enable mount flight. (Also allows mount-like flight while unmounted). Incompatiable with noclip.");
            if (C.ForcedFlight) P.Noclip = false;
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.ButtonCheckbox("\uf6e2", ref P.Noclip);
            ImGui.PopFont();
            ImGuiEx.Tooltip("Enables noclip. WASD - move, space - up, left shift - down.");
            if (P.Noclip)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f);
                ImGuiEx.SliderFloat("##speed", ref C.NoclipSpeed, 0.05f, 0.5f);
                C.ForcedFlight = false;
            }

            ImGui.SetNextItemWidth(250f);
            var fests = P.SelectedFestivals.Select(x => P.FestivalDatas.FirstOrDefault(z => z.Id == x).Name).Join(", ");
            if (ImGui.BeginCombo("##fest", fests.IsNullOrEmpty()? "Select festivals...":fests))
            {
                ImGui.SetNextItemWidth(150f);
                ImGui.InputTextWithHint($"##fltr1", "Search", ref FestFilter, 50);
                ImGui.SameLine();
                if(ImGui.Button("Deselect All"))
                {
                    P.SelectedFestivals.Clear();
                    P.ApplyFestivals();
                }
                foreach(var x in P.FestivalDatas)
                {
                    if (x.Unsafe) continue;
                    if (FestFilter != "" && !x.Name.Contains(FestFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    var disabled = !P.SelectedFestivals.Contains(x.Id) && P.SelectedFestivals.Count >= 4;
                    if (disabled) ImGui.BeginDisabled();
                    ImGuiEx.CollectionCheckbox($"{x.Name}##{x.Id}", x.Id, P.SelectedFestivals);
                    if (disabled) ImGui.EndDisabled();
                }
                ImGui.EndCombo();
            }
            var disabled2 = !EzThrottler.Check("ApplyFestival");
            if (disabled2) ImGui.BeginDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Apply"))
            {
                P.ApplyFestivals();
                EzThrottler.Throttle("ApplyFestival", 2000, true);
            }
            if (disabled2) ImGui.EndDisabled();
        }
    }
}

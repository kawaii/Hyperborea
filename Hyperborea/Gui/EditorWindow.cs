using Dalamud.Interface.Components;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea.Gui;
public unsafe class EditorWindow : Window
{
    Dictionary<string, HashSet<uint>> BgToTerritoryType = [];
    internal uint SelectedTerritory = 0;
    uint TerrID => SelectedTerritory == 0 ? Svc.ClientState.TerritoryType : SelectedTerritory;
    public EditorWindow() : base("Hyperborea Zone Editor")
    {
        EzConfigGui.WindowSystem.AddWindow(this);
        foreach(var x in Svc.Data.GetExcelSheet<TerritoryType>())
        {
            var bg = x.GetBG();
            if (!bg.IsNullOrEmpty())
            {
                if(!BgToTerritoryType.TryGetValue(bg, out var list))
                {
                    list = [];
                    BgToTerritoryType[bg] = list;
                }
                list.Add(x.RowId);
            }
        }
    }

    public override void Draw()
    {
        var cur = ImGui.GetCursorPos();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.SetCursorPosX(ImGuiEx.GetWindowContentRegionWidth() - ImGui.CalcTextSize("\uf0c7").X);
        if(Utils.TryGetZoneInfo(ExcelTerritoryHelper.GetBG(TerrID), out _, out var isOverriden1))
        {
            if (isOverriden1)
            {
                ImGuiEx.Text(EColor.YellowBright, $"\uf0c7");
                ImGui.PopFont();
                ImGuiEx.Tooltip("Zone data is being loaded from your data overrides file.");
            }
            else
            {
                ImGuiEx.Text(EColor.GreenBright, $"\uf0c7");
                ImGui.PopFont();
                ImGuiEx.Tooltip("Zone data loaded from master data file.");
            }
        }
        else
        {
            ImGuiEx.Text(EColor.RedBright, $"\uf0c7");
            ImGui.PopFont();
            ImGuiEx.Tooltip("No configuration found for this zone in either the master or override data file(s).");
        }
        ImGui.SetCursorPos(cur);
        var shares = BgToTerritoryType.TryGetValue(ExcelTerritoryHelper.GetBG(TerrID), out var set) ? set : [];
        ImGuiEx.TextWrapped($"Currently editing: {ExcelTerritoryHelper.GetName(TerrID, true)}");
        if(shares.Count > 1)
        {
            ImGuiComponents.HelpMarker($"Shares data with \n{shares.Where(z => z != TerrID).Select(z => ExcelTerritoryHelper.GetName(z, true)).Print("\n")}");
        }
        if (ImGuiComponents.IconButtonWithText((FontAwesomeIcon)0xf002, "Browse"))
        {
            new TerritorySelector(SelectedTerritory, (_, x) =>
            {
                SelectedTerritory = x;
            });
        }
        ImGui.SameLine();
        if(ImGuiComponents.IconButtonWithText((FontAwesomeIcon)0xf276, "Current Zone"))
        {
            SelectedTerritory = 0;
        }

        var bg = ExcelTerritoryHelper.GetBG(TerrID);
        if (bg.IsNullOrEmpty())
        {
            ImGuiEx.Text($"This zone is unsupported");
        }
        else
        {
            if (Utils.TryGetZoneInfo(bg, out var info, out var isOverriden))
            {
                var overrideSpawn = info.Spawn != null;
                if (ImGui.Checkbox("Custom Spawn Point", ref overrideSpawn))
                {
                    info.Spawn = overrideSpawn ? new() : null;
                }
                if (overrideSpawn)
                {
                    UI.CoordBlock("X:", ref info.Spawn.X);
                    ImGui.SameLine();
                    UI.CoordBlock("Y:", ref info.Spawn.Y);
                    ImGui.SameLine();
                    UI.CoordBlock("Z:", ref info.Spawn.Z);
                    ImGui.SameLine();
                    if (ImGuiEx.IconButton("\uf3c5")) info.Spawn = Player.Object.Position.ToPoint3();
                    ImGuiEx.Tooltip("Set the zone spawn point to your character's current location.");
                }
                ImGui.Separator();
                ImGuiEx.TextV("Phases:");
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesome.Plus))
                {
                    info.Phases.Add(new());
                }
                ImGuiEx.Tooltip($"Create a new phase.");
                foreach (var p in info.Phases)
                {
                    ImGui.PushID(p.GUID);
                    if (ImGui.CollapsingHeader($"{p.Name}###phase"))
                    {
                        ImGuiEx.TextV($"Name:");
                        ImGui.SameLine();
                        ImGuiEx.SetNextItemWidthScaled(150f);
                        ImGui.InputText($"##Name", ref p.Name, 20);
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesome.Trash) && ImGuiEx.Ctrl)
                        {
                            new TickScheduler(() => info.Phases.RemoveAll(z => z.GUID == p.GUID));
                        }
                        ImGuiEx.Tooltip("Hold CTRL to delete this phase.");
                        ImGuiEx.TextV($"Weather:");
                        ImGui.SameLine();
                        if (ImGui.BeginCombo("##Weather", $"{Utils.GetWeatherName(p.Weather)}"))
                        {
                            foreach (var x in (uint[])[0, .. P.Weathers[TerrID]])
                            {
                                if (ImGui.Selectable($"{x} - {Utils.GetWeatherName(x)}"))
                                {
                                    if (P.Enabled && Svc.ClientState.TerritoryType == TerrID && Utils.GetPhase(Svc.ClientState.TerritoryType) == p)
                                    {
                                        EnvManager.Instance()->ActiveWeather = (byte)x;
                                        EnvManager.Instance()->TransitionTime = 0.5f;
                                    }
                                    p.Weather = x;
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGuiEx.TextV($"MapEffects:");
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesome.Plus))
                        {
                            p.MapEffects.Add(new());
                        }
                        ImGuiEx.Tooltip("Add a new MapEffect.");
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                        {
                            Copy(P.YamlFactory.Serialize(p.MapEffects, true));
                        }
                        ImGuiEx.Tooltip("Copy the configured MapEffects for this phase.");
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Paste))
                        {
                            Safe(() => p.MapEffects = P.YamlFactory.Deserialize<List<MapEffectInfo>>(Paste()));
                        }
                        ImGuiEx.Tooltip("Paste and override MapEffects into this phase.");
                        foreach (var x in p.MapEffects)
                        {
                            ImGui.PushID(x.GUID);
                            ImGuiEx.SetNextItemWidthScaled(100f);
                            ImGui.InputInt($"##a1", ref x.a1);
                            ImGui.SameLine();
                            ImGuiEx.SetNextItemWidthScaled(100f);
                            ImGui.InputInt($"##a2", ref x.a2);
                            ImGui.SameLine();
                            ImGuiEx.SetNextItemWidthScaled(100f);
                            ImGui.InputInt($"##a3", ref x.a3);
                            ImGui.SameLine();
                            if (ImGui.Button("Delete"))
                            {
                                new TickScheduler(() => p.MapEffects.RemoveAll(z => z.GUID == x.GUID));
                            }
                            ImGui.PopID();
                        }
                    }
                    ImGui.PopID();
                }
                if (ImGui.Button("Save"))
                {
                    Utils.CreateZoneInfoOverride(bg, info.JSONClone(), true);
                    P.SaveZoneData();
                }
                if(isOverriden)
                {
                    if(ImGui.Button("Reset"))
                    {
                        Utils.LoadBuiltInZoneData();
                        new TickScheduler(() =>
                        {
                            P.ZoneData.Data.Remove(bg);
                            P.SaveZoneData();
                        });
                    }
                }
            }
            else
            {
                ImGuiEx.Text($"No data found");
                if (ImGui.Button("Create override"))
                {
                    Utils.CreateZoneInfoOverride(bg, new()
                    {
                        Name = ExcelTerritoryHelper.GetName(TerrID),
                    });
                }
            }
        }
    }
}

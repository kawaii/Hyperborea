using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Lumina.Excel.Sheets;
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
            var bg = ((TerritoryType?)x).GetBG();
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
                        ImGui.SetNextItemWidth(150f);
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
                        var slots = MapEffectResolver.GetZoneSlots(TerrID);
                        foreach (var x in p.MapEffects)
                        {
                            ImGui.PushID(x.GUID);

                            var isDupe = p.MapEffects.Count(y => y.Slot == x.Slot) > 1;
                            if (isDupe)
                            {
                                ImGuiEx.Text(EColor.RedBright, "[DUPLICATE SLOT]");
                                ImGuiEx.Tooltip("Another entry in this phase already targets this slot. Only the last one in the list will actually apply.");
                                ImGui.SameLine();
                            }

                            var curSlot = slots.FirstOrDefault(s => s.Slot == x.Slot);
                            ImGui.SetNextItemWidth(180f);
                            if (ImGui.BeginCombo("##slot", curSlot != null ? $"{x.Slot}: {curSlot.SgbName}" : $"Slot {x.Slot} (unknown)"))
                            {
                                foreach (var s in slots)
                                {
                                    if (s.Slot != x.Slot && p.MapEffects.Any(y => y.GUID != x.GUID && y.Slot == s.Slot)) continue;

                                    if (ImGui.Selectable($"{s.Slot}: {s.SgbName}##slot{s.Slot}", x.Slot == s.Slot))
                                    {
                                        x.Slot = s.Slot;
                                        x.State = 0;
                                        x.TimelineOverride = 0;
                                    }
                                }
                                ImGui.EndCombo();
                            }
                            ImGuiEx.Tooltip("Which MapEffect slot to control.");
                            ImGui.SameLine();

                            var states = curSlot?.States ?? [];
                            var curStateName = states.FirstOrDefault(st => st.State == (ushort)x.State).Name;
                            ImGui.SetNextItemWidth(180f);
                            if (ImGui.BeginCombo("##state", curStateName.NullWhenEmpty() != null ? $"{x.State}: {curStateName}" : $"State {x.State}"))
                            {
                                if (ImGui.Selectable("0 (no-op)", x.State == 0)) x.State = 0;
                                foreach (var (state, name) in states)
                                {
                                    if (ImGui.Selectable($"{state}: {name}##st{state}", x.State == state)) x.State = state;
                                }
                                ImGui.EndCombo();
                            }
                            ImGuiEx.Tooltip("Which state to set the slot to. By default this also drives which timeline actively plays.");
                            ImGui.SameLine();

                            if (ImGuiEx.IconButton(FontAwesomeIcon.Cog))
                            {
                                ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y));
                                ImGui.OpenPopup("##advTimeline");
                            }
                            ImGuiEx.Tooltip("Advanced: override which timelines actively play, independent of State.\nLeave at default (0) unless you need to combine multiple independent effects on this slot (e.g. torches + gate) or trigger a transition bit that isn't a resting State value.");
                            ImGui.SetNextWindowSize(new Vector2(280f, 0f), ImGuiCond.Appearing);
                            if (ImGui.BeginPopup("##advTimeline"))
                            {
                                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 260f);
                                ImGuiEx.TextWrapped("Which timelines actively play/stop right now. Leave everything unchecked to just follow State above (default, safest).");
                                ImGui.PopTextWrapPos();
                                if (ImGui.Selectable("Reset to default (follow State)")) x.TimelineOverride = 0;
                                ImGui.Separator();
                                foreach (var (state, name) in states)
                                {
                                    var on = ((ushort)x.TimelineOverride & state) != 0;
                                    if (ImGui.Checkbox($"{state}: {name}##tl{state}", ref on))
                                    {
                                        x.TimelineOverride = on ? (x.TimelineOverride | state) : (x.TimelineOverride & ~state);
                                    }
                                }
                                ImGui.EndPopup();
                            }
                            if (x.TimelineOverride != 0)
                            {
                                ImGui.SameLine();
                                var curTimelineNames = states.Where(st => ((ushort)x.TimelineOverride & st.State) != 0).Select(st => st.Name).ToList();
                                ImGuiEx.Text(EColor.YellowBright, curTimelineNames.Count > 0 ? string.Join(", ", curTimelineNames) : $"{x.TimelineOverride}");
                                ImGuiEx.Tooltip("Timeline override active (not following State).");
                            }
                            ImGui.SameLine();

                            if (ImGui.Button("Delete"))
                            {
                                new TickScheduler(() => p.MapEffects.RemoveAll(z => z.GUID == x.GUID));
                            }
                            ImGui.PopID();
                        }
                        if (slots.Count == 0)
                        {
                            ImGui.TextDisabled(" No MapEffect slots found for this zone.");
                        }
                        ImGuiEx.TextV($"Festivals:");
                        var zoneFests = Utils.GetZoneFestivals(bg);
                        if (zoneFests.Count == 0)
                        {
                            ImGui.SameLine();
                            ImGui.TextDisabled("(none in this zone)");
                        }
                        foreach (var (festivalId, festivalPhases) in zoneFests)
                        {
                            DrawFestivalRow(p, festivalId, festivalPhases);
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

    void DrawFestivalRow(PhaseInfo p, int festivalId, List<int> festivalPhaseIds)
    {
        using var id = ImRaii.PushId(festivalId);

        var data = P.FestivalDatas.FirstOrDefault(z => z.Id == festivalId);
        var selected = p.Festivals.FirstOrDefault(z => z.Id == festivalId);
        var IsMulti = festivalPhaseIds.Any(x => x != 0);

        ImGui.SetNextItemWidth(160f);
        if (ImGui.BeginCombo($"##Festival", PhaseLabel(data, selected?.PhaseId, IsMulti)))
        {
            foreach (var phaseId in (int[])[-1, .. festivalPhaseIds])
            {
                if (ImGui.Selectable(PhaseLabel(data, phaseId, IsMulti), selected?.PhaseId == phaseId))
                {
                    if (phaseId < 0)
                    {
                        p.Festivals.RemoveAll(z => z.Id == festivalId);
                    }
                    else if (selected == null)
                    {
                        selected = new() { Id = festivalId, PhaseId = phaseId };
                        p.Festivals.Add(selected);
                    }
                    else
                    {
                        selected.PhaseId = phaseId;
                    }
                    if (P.Enabled && Svc.ClientState.TerritoryType == TerrID && Utils.GetPhase(Svc.ClientState.TerritoryType) == p)
                    {
                        P.ApplyFestivals(p.Festivals);
                    }
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGuiEx.Text(data?.Name.NullWhenEmpty() ?? $"#{festivalId}");
    }

    static string PhaseLabel(FestivalData data, int? phaseId, bool isMulti)
    {
        if (phaseId < 0 || phaseId == null) return "Off";
        if (!isMulti) return "On";
        var pname = data?.Phases.FirstOrDefault(z => z.Id == phaseId)?.Name;
        if (!pname.IsNullOrEmpty()) return $"{phaseId} - {pname}";
        return $"Phase {phaseId}";
    }
}

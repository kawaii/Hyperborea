using Dalamud.Interface.Components;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel.GeneratedSheets;

namespace Hyperborea.Gui;

public unsafe static class UI
{
    public static SavedZoneState SavedZoneState = null;
    public static Vector3? SavedPos = null;
    static int a2 = 0;
    static int a3 = 0;
    static int a4 = 0;
    static int a5 = 1;
    internal static int a6 = 1;
    static Point3 Position = new(0,0,0);

    public static void DrawNeo()
    {
        var l = LayoutWorld.Instance()->ActiveLayout;
        var disableCheckbox = !(Utils.IsInInn() || C.DisableInnCheck) && !P.Enabled;
        if (disableCheckbox) ImGui.BeginDisabled();
        if (ImGui.Checkbox("Enable Hyperborea", ref P.Enabled))
        {
            if (P.Enabled)
            {
                SavedPos = Player.Object.Position;
                P.Memory.EnableFirewall();
                P.Memory.TargetSystem_InteractWithObjectHook.Enable();
            }
            else
            {
                Utils.Revert();
                SavedPos = null;
                SavedZoneState = null;
                P.Memory.DisableFirewall();
                P.Memory.TargetSystem_InteractWithObjectHook.Disable();
            }
        }
        if (disableCheckbox) ImGui.EndDisabled();
        ImGuiEx.Text("Packet Filter:");
        ImGui.SameLine();
        if (P.Memory.PacketDispatcher_OnSendPacketHook.IsEnabled && P.Memory.PacketDispatcher_OnReceivePacketHook.IsEnabled)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
            ImGui.PopFont();
        }
        else
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.RedBright, "\uf00d");
            ImGui.PopFont();
        }
        ImGuiEx.Tooltip("When Hyperborea's packet filter is enabled, your packets to and from the game server are filtered to only prevent the client kicking you out to the lobby.");
        ImGui.SameLine();

        ImGuiEx.Text("Interact Hook:");
        ImGui.SameLine();
        if (P.Memory.TargetSystem_InteractWithObjectHook.IsEnabled)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
            ImGui.PopFont();
        }
        else
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.RedBright, "\uf00d");
            ImGui.PopFont();
        }
        ImGuiEx.Tooltip("When Hyperborea's interact hook is enabled, you will be unable to interact with EventNpcs/EventObjs.");

        ImGuiEx.Text("Free Trial:");
        ImGui.SameLine();
        if (Svc.Condition[ConditionFlag.OnFreeTrial])
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
            ImGui.PopFont();
        }
        else
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.RedBright, "\uf00d");
            ImGui.PopFont();
        }
        ImGuiEx.Tooltip("While Hyperborea attempts to implement safety as much as possible by preventing sending data to server while using it, no guarantees is given and it's always recommended to use it with free trial account.");

        ImGui.SameLine();
        ImGuiEx.Text("In The Inn:");
        ImGui.SameLine();
        if (Utils.IsInInnInternal() || (SavedZoneState != null && Svc.Data.GetExcelSheet<TerritoryType>().GetRow(SavedZoneState.ZoneId).TerritoryIntendedUse == (uint)TerritoryIntendedUseEnum.Inn))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
            ImGui.PopFont();
        }
        else
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.RedBright, "\uf00d");
            ImGui.PopFont();
        }
        ImGuiEx.Tooltip("Hyperborea can only be used in the inn.");


        if (ImGuiGroup.BeginGroupBox())
        {
            var cur = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(ImGuiEx.GetWindowContentRegionWidth() - ImGuiHelpers.GetButtonSize("Browse").X - 20f.Scale());
            if (ImGuiComponents.IconButtonWithText((FontAwesomeIcon)0xf002, "Browse"))
            {
                new TerritorySelector((uint)a2, (sel, x) =>
                {
                    a2 = (int)x;
                    /*if (Utils.CanUse())
                    {
                        SavedZoneState ??= new SavedZoneState(l->TerritoryTypeId, Player.Object.Position);
                        Utils.LoadZone(x, inp2, inp3, inp4, inp5);
                    }*/
                });
            }
            ImGui.SetCursorPos(cur);
            ImGuiEx.TextV("Zone Data:");
            ImGuiEx.SetNextItemWidthScaled(150);
            var dis = TerritorySelector.Selectors.Any(x => x.IsOpen);
            if (dis) ImGui.BeginDisabled();
            ImGui.InputInt("Territory Type ID", ref a2);
            if (dis) ImGui.EndDisabled();
            if (ExcelTerritoryHelper.NameExists((uint)a2))
            {
                ImGuiEx.Text(ExcelTerritoryHelper.GetName((uint)a2));
            }
            ImGuiEx.Text($"Additional Data:");
            ImGuiEx.SetNextItemWidthScaled(150);
            ImGui.InputInt("Argument 3", ref a3);
            ImGuiEx.SetNextItemWidthScaled(150);
            ImGui.InputInt("Argument 4", ref a4);
            ImGuiEx.SetNextItemWidthScaled(150);
            ImGui.InputInt("Argument 5", ref a5);

            ImGuiEx.Text($"Spawn Override:");
            CoordBlock("X:", ref Position.X);
            ImGui.SameLine();
            CoordBlock("Y:", ref Position.Y);
            ImGui.SameLine();
            CoordBlock("Z:", ref Position.Z);

            ImGuiHelpers.ScaledDummy(3f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(3f);

            {
                var size = ImGuiEx.CalcIconSize("\uf3c5", true);
                size += ImGuiEx.CalcIconSize("\uf15c", true);
                size += ImGuiEx.CalcIconSize(FontAwesomeIcon.Cog, true);
                size.X += ImGui.GetStyle().ItemSpacing.X * 2;

                var cur2 = ImGui.GetCursorPos();
                ImGui.SetCursorPosX(ImGuiEx.GetWindowContentRegionWidth() - size.X);
                var disabled = !Utils.CanUse();
                if (disabled) ImGui.BeginDisabled();
                if (ImGuiEx.IconButton("\uf3c5"))
                {
                    Player.GameObject->SetPosition(Position.X, Position.Y, Position.Z);
                }
                ImGuiEx.Tooltip("Teleports to the coordinates defined in the spawn override setting. Only functional while Hyperborea is enabled.");
                if (disabled) ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGuiEx.IconButton("\uf15c"))
                {
                    P.LogWindow.IsOpen = true;
                }
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Cog))
                {
                    P.SettingsWindow.IsOpen = true;
                }
                ImGui.SetCursorPos(cur2);
            }

            {
                var disabled = !Utils.CanUse();
                if (disabled) ImGui.BeginDisabled();
                if (ImGui.Button("Load Zone"))
                {
                    SavedZoneState ??= new SavedZoneState(l->TerritoryTypeId, Player.Object.Position);
                    Utils.LoadZone((uint)a2, a3, a4, a5, a6);
                    Player.GameObject->SetPosition(Position.X, Position.Y, Position.Z);
                }
                if (disabled) ImGui.EndDisabled();
            }
            ImGui.SameLine();
            {
                var disabled = !P.Enabled;
                if (disabled) ImGui.BeginDisabled();
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Undo, "Revert"))
                {
                    Utils.Revert();
                }
                if (disabled) ImGui.EndDisabled();
            }
            ImGuiGroup.EndGroupBox();
        }
    }
    static void CoordBlock(string t, ref float p)
    {
        ImGuiEx.TextV(t);
        ImGui.SameLine();
        ImGuiEx.SetNextItemWidthScaled(60f);
        ImGui.DragFloat("##" + t, ref p, 0.1f);
    }
}

using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Hyperborea.Gui;
using Lumina.Excel.GeneratedSheets;
using System.Globalization;

namespace Hyperborea;
public unsafe static class Utils
{
    public static bool CanEnablePlugin(out List<string> reasons)
    {
        reasons = [];
        if (P.Enabled) return true;
        var ret = true;
        if (!Player.Available)
        {
            reasons.Add("LocalPlayer Missing (Not logged in");
            ret = false;
        }
        else if(Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType)?.TerritoryIntendedUse != (byte)TerritoryIntendedUseEnum.Inn && !C.DisableInnCheck)
        {
            reasons.Add("Zone Restriction Active (Must be in an inn room)");
            ret = false;
        }
        foreach (var cond in Enum.GetValues<ConditionFlag>())
        {
            if (cond.EqualsAny(ConditionFlag.NormalConditions, ConditionFlag.OnFreeTrial, ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance, ConditionFlag.DutyRecorderPlayback)) continue;
            if (Svc.Condition[cond])
            {
                reasons.Add($"{cond}");
                ret = false;
            }
        }
        return ret;
    }

    public static Vector3 CameraPos => *(Vector3*)((nint)CameraManager.Instance()->GetActiveCamera() + 0x60);

    public static bool TryFindBytes(this byte[] haystack, byte[] needle, out int pos)
    {
        var len = needle.Length;
        var limit = haystack.Length - len;
        for (var i = 0; i <= limit; i++)
        {
            var k = 0;
            for (; k < len; k++)
            {
                if (needle[k] != haystack[i + k]) break;
            }
            if (k == len)
            {
                pos = i;
                return true;
            }
        }
        pos = default;
        return false;
    }

    public static (List<byte> WeatherList, string EnvbFile) ParseLvb(ushort id)
    {
        var weathers = new List<byte>();
        var territoryType = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(id);
        if (territoryType == null) return default;
        try
        {
            var file = Svc.Data.GetFile<LvbFile>($"bg/{territoryType.Bg}.lvb");
            if (file?.weatherIds == null || file.weatherIds.Length == 0)
                return (null, null);
            foreach (var weather in file.weatherIds)
                if (weather > 0 && weather < 255)
                    weathers.Add((byte)weather);
            weathers.Sort();
            return (weathers, file.envbFile);
        }
        catch (Exception e)
        {
            PluginLog.Error($"Failed to load lvb for {territoryType}\n{e}");
        }
        return default;
    }
    public static bool TryFindBytes(this byte[] haystack, string needle, out int pos)
    {
        return TryFindBytes(haystack, needle.Split(" ").Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray(), out pos);
    }

    public static bool IsInInn()
    {
        if (Svc.ClientState.LocalPlayer == null) return false;
        if (P.Enabled) return true;
        return IsInInnInternal();
    }
    
    public static bool IsInInnInternal() => Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType)?.TerritoryIntendedUse == (uint) TerritoryIntendedUseEnum.Inn;


    public static void LoadZone(uint territory, int a3 = 0, int a4 = 0, int a5 = 1, int a6 = 1)
    {
        foreach (var x in Svc.Objects)
        {
            x.Struct()->DisableDraw();
        }
        P.Memory.LoadZoneDetour((nint)GameMain.Instance(), territory, a3, (byte)a4, (byte)a5, (byte)a6);
        P.Memory.SetupTerritoryType(EventFramework.Instance(), (ushort)territory);
    }

    public static bool CanUse()
    {
        var hooks = (P.Memory.PacketDispatcher_OnReceivePacketHook.IsEnabled && P.Memory.PacketDispatcher_OnSendPacketHook.IsEnabled);
        var inn = Utils.IsInInn() || C.DisableInnCheck;
        return hooks && inn;
    }

    public static void Revert()
    {
        if (UI.SavedPos != null)
        {
            Player.GameObject->SetPosition(UI.SavedPos.Value.X, UI.SavedPos.Value.Y, UI.SavedPos.Value.Z);
        }
        if (UI.SavedZoneState != null)
        {
            if (LayoutWorld.Instance()->ActiveLayout == null || LayoutWorld.Instance()->ActiveLayout->TerritoryTypeId != UI.SavedZoneState.ZoneId)
            {
                Utils.LoadZone(UI.SavedZoneState.ZoneId);
            }
        }
    }
}

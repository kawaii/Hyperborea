using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Hyperborea.Gui;
using Lumina.Excel.Sheets;
using System.Globalization;
using System.IO;

namespace Hyperborea;
public unsafe static class Utils
{
    public static Vector3 RotatePoint(float cx, float cy, float angle, Vector3 p)
    {
        if (angle == 0f) return p;
        var s = (float)Math.Sin(angle);
        var c = (float)Math.Cos(angle);

        // translate point back to origin:
        p.X -= cx;
        p.Z -= cy;

        // rotate point
        float xnew = p.X * c - p.Z * s;
        float ynew = p.X * s + p.Z * c;

        // translate point back:
        p.X = xnew + cx;
        p.Z = ynew + cy;
        return p;
    }

    public static string GetMountName(uint id)
    {
        if (id == 0) return null;
        return Svc.Data.GetExcelSheet<Mount>().GetRowOrDefault(id)?.Singular.ExtractText();
    }

    public static void LoadBuiltInZoneData()
    {
        P.BuiltInZoneData = EzConfig.LoadConfiguration<ZoneData>(Path.Combine(Svc.PluginInterface.AssemblyLocation.Directory.FullName, "data.yaml"));
    }

    public static bool TryGetZoneInfo(string bg, out ZoneInfo info) => TryGetZoneInfo(bg, out info, out _);

    public static bool TryGetZoneInfo(string bg, out ZoneInfo info, out bool isOverriden)
    {
        info = GetZoneInfo(bg, out isOverriden);
        return info != null;
    }

    public static ZoneInfo GetZoneInfo(string bg, out bool isOverriden)
    {
        isOverriden = false;
        {
            if (P.ZoneData.Data.TryGetValue(bg, out var info))
            {
                isOverriden = true;
                return info;
            }
        }
        {
            if (P.BuiltInZoneData.Data.TryGetValue(bg, out var info))
            {
                return info;
            }
        }
        return null;
    }

    public static void CreateZoneInfoOverride(string bg, ZoneInfo newData, bool forceNew = false)
    {
        if (P.BuiltInZoneData.Data.TryGetValue(bg, out var info) && !forceNew)
        {
            P.ZoneData.Data[bg] = info.JSONClone();
        }
        else
        {
            P.ZoneData.Data[bg] = newData;
        }
    }

    static Dictionary<uint, HashSet<uint>> StoryValues = [];
    public static HashSet<uint> GetStoryValues(uint territoryType)
    {
        if(!StoryValues.TryGetValue(territoryType, out var value))
        {
            value = [0];
            /*foreach (var x in Svc.Data.GetExcelSheet<Story>())
            {
                if (x.LayerSetTerritoryType0.Row == territoryType)
                {
                    for (int i = 0; i < x.LayerSet1.Length; i++)
                    {
                        value.Add(x.LayerSet0[i]);
                    }
                }
            }*/
            StoryValues[territoryType] = value;
        }
        return StoryValues[territoryType];
    }

    public static string GetWeatherName(uint id)
    {
        if (id == 0) return "Not defined";
        return Svc.Data.GetExcelSheet<Weather>().GetRowOrDefault((uint)id)?.Name.ToString() ?? $"#{id}";
    }

    public static nint GetMapEffectModule()
    {
        return *(nint*)(((nint)EventFramework.Instance()) + 344);
    }

    public static bool CanEnablePlugin(out List<string> reasons)
    {
        reasons = [];
        if (P.Bypass) return true;
        if (P.Enabled) return true;
        var ret = true;
        if (!Player.Available)
        {
            reasons.Add("LocalPlayer Missing (Not logged in");
            ret = false;
        }
        else if(Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(Svc.ClientState.TerritoryType)?.TerritoryIntendedUse.RowId != (byte)TerritoryIntendedUseEnum.Inn && !C.DisableInnCheck)
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

    public static string GetLayout(uint territoryType)
    {
        var bg = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(territoryType)?.Bg.ExtractText();
        return bg;
    }
    public static string GetLayout() => GetLayout(Svc.ClientState.TerritoryType);

    public static void SwitchTo(this PhaseInfo phase)
    {
        var e = EnvManager.Instance();
        e->ActiveWeather = (byte)phase.Weather;
        e->TransitionTime = 0.5f;
        if(phase.MapEffects.Count > 0)
        {
            foreach(var x in phase.MapEffects)
            {
                MapEffect.Delegate(Utils.GetMapEffectModule(), (uint)x.a1, (ushort)x.a2, (ushort)x.a3);
            }
        }
    }

    public static PhaseInfo GetPhase(uint territoryType)
    {
        if (TryGetZoneInfo(ExcelTerritoryHelper.GetBG(territoryType), out var zone))
        {
            var e = EnvManager.Instance();
            if (zone.Phases.TryGetFirst(x => x.Weather == e->ActiveWeather, out var v)) return v;
        }
        return null;
    }

    public static bool IsNear(this Vector3 v, Vector3 o)
    {
        return Vector3.DistanceSquared(v, o) < 1;
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
        var territoryType = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(id);
        if (territoryType == null) return default;
        try
        {
            var file = Svc.Data.GetFile<LvbFile>($"bg/{territoryType.Value.Bg}.lvb");
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
    
    public static bool IsInInnInternal() => Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(Svc.ClientState.TerritoryType)?.TerritoryIntendedUse.RowId == (uint) TerritoryIntendedUseEnum.Inn;

    internal static uint? InstanceContentWasLoaded = null;
    public static void LoadZone(uint territory, bool setPosition, bool setPhase, int a3 = 0, int a4 = 0, int a5 = 1, int a6 = 1, int cfcOverride = 0)
    {
        if(InstanceContentWasLoaded != null)
        {
            P.Memory.FinalizeInstanceContentHook.Original((nint)EventFramework.Instance(), 0x80030000 + InstanceContentWasLoaded.Value);
            InstanceContentWasLoaded = null;
        }
        foreach (var x in Svc.Objects)
        {
            x.Struct()->DisableDraw();
        }
        var content = ExcelTerritoryHelper.Get((uint)territory)?.ContentFinderCondition.ValueNullable?.Content.RowId;
        if(cfcOverride != 0) content = (ushort?)cfcOverride;
        if (content != null && content != 0)
        {
            P.Memory.SetupInstanceContentHook.Original((nint)EventFramework.Instance(), 0x80030000 + content.Value, content.Value, 0);
            InstanceContentWasLoaded = content.Value;
        }

        P.Memory.LoadZoneDetour((nint)GameMain.Instance(), territory, a3, (byte)a4, (byte)a5, (byte)a6);
        P.Memory.SetupTerritoryType(EventFramework.Instance(), (ushort)territory);
        P.TaskManager.Enqueue(P.ApplyFestivals);
        var level = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(territory)?.Bg.ExtractText();
        if(!level.IsNullOrEmpty() && Utils.TryGetZoneInfo(level, out var value))
        {
            if (setPosition && value.Spawn != null)
            {
                Player.GameObject->SetPosition(value.Spawn.X, value.Spawn.Y, value.Spawn.Z);
            }
            if (setPhase && value.Phases.Count > 0)
            {
                P.TaskManager.DelayNext(1000);
                P.TaskManager.Enqueue(() =>
                {
                    var e = EnvManager.Instance();
                    e->ActiveWeather = (byte)value.Phases.First().Weather;
                    e->TransitionTime = 0.5f;
                });
            }
        }
    }

    public static bool CanUse()
    {
        var hooks = (P.Memory.PacketDispatcher_OnReceivePacketHook.IsEnabled && P.Memory.PacketDispatcher_OnSendPacketHook.IsEnabled);
        var inn = Utils.IsInInn() || C.DisableInnCheck;
        return hooks && inn;
    }

    public static void Revert()
    {
        if (Svc.Condition[ConditionFlag.Mounted]) Player.Character->Mount.CreateAndSetupMount(0, 0, 0, 0, 0, 0, 0);
        if (UI.SavedPos != null)
        {
            Player.GameObject->SetPosition(UI.SavedPos.Value.X, UI.SavedPos.Value.Y, UI.SavedPos.Value.Z);
        }
        if (UI.SavedZoneState != null)
        {
            if (LayoutWorld.Instance()->ActiveLayout == null || LayoutWorld.Instance()->ActiveLayout->TerritoryTypeId != UI.SavedZoneState.ZoneId)
            {
                Utils.LoadZone(UI.SavedZoneState.ZoneId, false, false);
            }
        }
    }
}

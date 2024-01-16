using ECommons.Automation;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using Hyperborea.Gui;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using System.IO;

namespace Hyperborea;

public unsafe class Hyperborea : IDalamudPlugin
{
    public static Hyperborea P;
    public Memory Memory;
    public SettingsWindow SettingsWindow;
    public LogWindow LogWindow;
    public DebugWindow DebugWindow;
    public bool Enabled = false;
    Config Config;
    public Dictionary<uint, List<byte>> Weathers;
    public static Config C => P.Config;
    public ZoneData ZoneData;
    public TaskManager TaskManager;
    public Random Random = new();
    public const string DataFileName = "DefaultZoneData.yaml";
    public bool Bypass = false;
    public YamlFactory YamlFactory = new();
    public EditorWindow EditorWindow;
    public CompassWindow CompassWindow;
    public ZoneData BuiltInZoneData;

    public Hyperborea(DalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, P);

        new TickScheduler(() =>
        {
            EzConfig.DefaultSerializationFactory = YamlFactory;
            var scale = ImGui.GetIO().FontGlobalScale;
            var constraint = new Window.WindowSizeConstraints() { MinimumSize = new(300f * scale, 100f), MaximumSize = new(300f * scale, 1000f) };
            constraint.MaximumSize /= ImGuiHelpers.GlobalScale ;
            constraint.MinimumSize /= ImGuiHelpers.GlobalScale ;

            Config = EzConfig.Init<Config>();
            EzConfigGui.Init(UI.DrawNeo);
            EzConfigGui.Window.SizeConstraints = constraint;
            EzConfigGui.Window.Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar;
            EzCmd.Add("/hyper", OnCommand);
            Memory = new();
            SettingsWindow = new();
            LogWindow = new();
            DebugWindow = new();
            Weathers = Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.Bg != "").ToDictionary(x => x.RowId, x => Utils.ParseLvb((ushort)x.RowId).WeatherList);
            new EzLogout(OnLogout);
            new EzTerritoryChanged(OnTerritoryChanged);
            TaskManager = new();
            ZoneData = EzConfig.LoadConfiguration<ZoneData>(DataFileName);
            MapEffect.Init(OnMapEffect);
            EditorWindow = new();
            CompassWindow = new();
            Utils.LoadBuiltInZoneData();
        });
    }

    private void OnMapEffect(long arg1, uint arg2, ushort arg3, ushort arg4)
    {
        InternalLog.Debug($"Map effect: {arg2}, {arg3}, {arg4}");
    }

    private void OnTerritoryChanged(ushort obj)
    {
        /*if (P.Enabled) return;
        TaskManager.Abort();
        TaskManager.Enqueue(() =>
        {
            if (Player.Interactable)
            {
                if (P.Enabled) return true;
                var level = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(obj)?.Bg?.ExtractText();
                if (level.IsNullOrEmpty()) return true;
                if(ZoneData.Data.TryGetValue(level, out var info))
                {
                    info.Name = ExcelTerritoryHelper.GetName(Svc.ClientState.TerritoryType);
                    SaveZoneData();
                }
                else
                {
                    info = new()
                    {
                        Spawn = Player.Object.Position.ToPoint3(),
                        Name = ExcelTerritoryHelper.GetName(Svc.ClientState.TerritoryType)
                    };
                    ZoneData.Data[level] = info;
                    SaveZoneData();
                }
                return true;
            }
            return false;
        });*/
    }

    public void SaveZoneData() => EzConfig.SaveConfiguration(ZoneData, DataFileName);

    private void OnLogout()
    {
        P.Enabled = false;
        UI.SavedPos = null;
        UI.SavedZoneState = null;
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.EqualsIgnoreCaseAny("log", "l"))
        {
            LogWindow.IsOpen = true;
        }
        else if (arguments.EqualsIgnoreCaseAny("settings", "s"))
        {
            LogWindow.IsOpen = true;
        }
        else if (arguments.EqualsIgnoreCaseAny("debug", "d"))
        {
            DebugWindow.IsOpen = true;
        }
        else if (arguments.EqualsIgnoreCaseAny("editor", "e"))
        {
            EditorWindow.IsOpen = true;
        }
        else
        {
            EzConfigGui.Open();
        }
    }

    public void Dispose()
    {
        if(Svc.ClientState.IsLoggedIn && Enabled)
        {
            Utils.Revert();
        }
        ECommonsMain.Dispose();
        P = null;
    }
}

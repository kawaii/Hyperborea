using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.Automation.LegacyTaskManager;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Interop;
using ECommons.SimpleGui;
using ECommons.Singletons;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.UI;
using Hyperborea.Gui;
using Hyperborea.Services;
using Lumina.Data;
using Lumina.Excel.Sheets;
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
    public Overlay Overlay;
    public bool Noclip = false;
    public FestivalData[] FestivalDatas;
    public List<int> SelectedFestivals = [];
    public bool AllowedOperation = false;

    public Hyperborea(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, P);

        new TickScheduler(() =>
        {
            EzConfig.DefaultSerializationFactory = YamlFactory;
            var scale = ImGui.GetIO().FontGlobalScale;
            var constraint = new Window.WindowSizeConstraints() { MinimumSize = new(300f * scale, 100f), MaximumSize = new(300f * scale, 1000f) };
            constraint.MaximumSize /= ImGuiHelpers.GlobalScaleSafe;
            constraint.MinimumSize /= ImGuiHelpers.GlobalScaleSafe;
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
            new EzFrameworkUpdate(Tick);
            Overlay = new();
            FestivalDatas = EzConfig.DefaultSerializationFactory.Deserialize<FestivalData[]>(File.ReadAllText(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "festivals.yaml")));
            SingletonServiceManager.Initialize(typeof(S));
        });
    }

    bool IsLButtonPressed = false;
    private void Tick()
    {
        if(Enabled)
        {
            if (C.FastTeleport)
            {
                if (!CSFramework.Instance()->WindowInactive && IsKeyPressed([LimitedKeys.LeftControlKey, LimitedKeys.RightControlKey]))
                {
                    var pos = ImGui.GetMousePos();
                    if (Svc.GameGui.ScreenToWorld(pos, out var res))
                    {
                        CompassWindow.PlayerPosition = res.ToPoint3();
                        if (IsKeyPressed(LimitedKeys.LeftMouseButton))
                        {
                            if (!IsLButtonPressed)
                            {
                                Player.GameObject->SetPosition(res.X, res.Y, res.Z);
                            }
                            IsLButtonPressed = true;
                        }
                        else
                        {
                            IsLButtonPressed = false;
                        }
                    }
                }
            }
            if (Noclip && !CSFramework.Instance()->WindowInactive)
            {
                if (Svc.KeyState.GetRawValue(VirtualKey.SPACE) != 0 || IsKeyPressed(LimitedKeys.Space))
                {
                    Svc.KeyState.SetRawValue(VirtualKey.SPACE, 0);
                    Player.GameObject->SetPosition(Player.Object.Position.X, Player.Object.Position.Y + C.NoclipSpeed, Player.Object.Position.Z);
                }
                if (Svc.KeyState.GetRawValue(VirtualKey.LSHIFT) != 0 || IsKeyPressed(LimitedKeys.LeftShiftKey))
                {
                    Svc.KeyState.SetRawValue(VirtualKey.LSHIFT, 0);
                    Player.GameObject->SetPosition(Player.Object.Position.X, Player.Object.Position.Y - C.NoclipSpeed, Player.Object.Position.Z);
                }
                if (Svc.KeyState.GetRawValue(VirtualKey.W) != 0 || IsKeyPressed(LimitedKeys.W))
                {
                    var newPoint = Utils.RotatePoint(Player.Object.Position.X, Player.Object.Position.Z, MathF.PI-((CameraEx*)CameraManager.Instance()->GetActiveCamera())->currentHRotation, Player.Object.Position + new Vector3(0, 0, C.NoclipSpeed));
                    Svc.KeyState.SetRawValue(VirtualKey.W, 0);
                    Player.GameObject->SetPosition(newPoint.X, newPoint.Y, newPoint.Z);
                }
                if (Svc.KeyState.GetRawValue(VirtualKey.S) != 0 || IsKeyPressed(LimitedKeys.S))
                {
                    var newPoint = Utils.RotatePoint(Player.Object.Position.X, Player.Object.Position.Z, MathF.PI - ((CameraEx*)CameraManager.Instance()->GetActiveCamera())->currentHRotation, Player.Object.Position + new Vector3(0, 0, -C.NoclipSpeed));
                    Svc.KeyState.SetRawValue(VirtualKey.S, 0);
                    Player.GameObject->SetPosition(newPoint.X, newPoint.Y, newPoint.Z);
                }
                if (Svc.KeyState.GetRawValue(VirtualKey.A) != 0 || IsKeyPressed(LimitedKeys.A))
                {
                    var newPoint = Utils.RotatePoint(Player.Object.Position.X, Player.Object.Position.Z, MathF.PI - ((CameraEx*)CameraManager.Instance()->GetActiveCamera())->currentHRotation, Player.Object.Position + new Vector3(C.NoclipSpeed, 0, 0));
                    Svc.KeyState.SetRawValue(VirtualKey.A, 0);
                    Player.GameObject->SetPosition(newPoint.X, newPoint.Y, newPoint.Z);
                }
                if (Svc.KeyState.GetRawValue(VirtualKey.D) != 0 || IsKeyPressed(LimitedKeys.D))
                {
                    var newPoint = Utils.RotatePoint(Player.Object.Position.X, Player.Object.Position.Z, MathF.PI - ((CameraEx*)CameraManager.Instance()->GetActiveCamera())->currentHRotation, Player.Object.Position + new Vector3(-C.NoclipSpeed, 0, 0));
                    Svc.KeyState.SetRawValue(VirtualKey.D, 0);
                    Player.GameObject->SetPosition(newPoint.X, newPoint.Y, newPoint.Z);
                }
            }
        }
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
        if(P.Enabled)
        {
            PluginLog.Warning($"Disconnect detected, opcode redownload scheduled.");
            C.GameVersion = "";
            C.OpcodesZoneDown = [];
            C.OpcodesZoneUp = [];
            AllowedOperation = false;
            EzConfig.Save();
        }
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

    public void ApplyFestivals()
    {
        var festivalArray = stackalloc uint[] { 0, 0, 0, 0 };
        for (int i = 0; i < Math.Min(4, SelectedFestivals.Count); i++)
        {
            festivalArray[i] = (uint)SelectedFestivals[i];
        }
        var l = LayoutWorld.Instance()->ActiveLayout;
        if(l != null)
        {
            l->SetActiveFestivals((GameMain.Festival*)festivalArray);
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

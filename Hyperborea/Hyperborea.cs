using ECommons.Automation;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using Hyperborea.Gui;
using Lumina.Excel.GeneratedSheets;
using System.IO;
using YamlDotNet.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

    public Hyperborea(DalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, P);

        new TickScheduler(() =>
        {
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
        });
    }

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

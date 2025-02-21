using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea.Services.OpcodeUpdaterService;
public unsafe class OpcodeUpdater : IDisposable
{
    volatile bool Disposed = false;
    public string CurrentVersion => $"{CSFramework.Instance()->GameVersionString}_{P.GetType().Assembly.GetName().Version}";

    private OpcodeUpdater()
    {
        if (CurrentVersion == C.GameVersion)
        {
            PluginLog.Information("No opcode update required");
            P.AllowedOperation = true;
        }
        else
        {
            PluginLog.Information("New game version detected, opcode update required");
            S.ThreadPool.Run(RunForCurrentVersion);
        }
    }

    public void RunForCurrentVersion()
    {
        var v = CSFramework.Instance()->GameVersionString;
        S.ThreadPool.Run(() => UpdateOpcodes(v, CurrentVersion));
    }

    private void UpdateOpcodes(string gameVersion, string fileVersion)
    {
        using var client = new HttpClient();
        try
        {
            var result = client.GetStringAsync($"https://github.com/kawaii/Hyperborea/raw/main/opcodes/{gameVersion}.txt").Result.ReplaceLineEndings().Split(Environment.NewLine);
            if (Disposed) throw new Exception("Opcode updater was disposed");
            foreach (var s in result)
            {
                /*if (s.StartsWith("ZoneUp="))
                {
                    var opcodes = s["ZoneUp=".Length..].Split(",").Select(uint.Parse);
                    PluginLog.Information($"Opcodes zone up: {opcodes}");
                    if (!opcodes.Any(x => x != 0)) throw new Exception("No opcodes were parsed");
                    Svc.Framework.RunOnFrameworkThread(() => C.OpcodesZoneUp = [.. opcodes]);
                }*/
                if (s.StartsWith("ZoneDown="))
                {
                    var opcodes = s["ZoneDown=".Length..].Split(",").Select(uint.Parse);
                    PluginLog.Information($"Opcodes zone down: {opcodes}");
                    if (!opcodes.Any(x => x != 0)) throw new Exception("No opcodes were parsed");
                    Svc.Framework.RunOnFrameworkThread(() => C.OpcodesZoneDown = [.. opcodes]);
                }
                Svc.Framework.RunOnFrameworkThread(Save);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Failed to download opcodes for new game version");
            ex.LogWarning();
        }
    }

    void Save()
    {
        C.GameVersion = CurrentVersion;
        P.AllowedOperation = true;
        EzConfig.Save();
        PluginLog.Information($"New opcodes received. Plugin operational.");
    }

    public void Dispose()
    {
        Disposed = true;
    }
}

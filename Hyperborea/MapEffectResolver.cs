using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel.Sheets;
using System.IO;

namespace Hyperborea;

public sealed class MapEffectSlotInfo
{
    public ushort Slot;
    public uint LayoutId;
    public bool DefaultOn;
    public string SgbPath = "";
    public (ushort State, string Name)[] States = [];

    public string SgbName => string.IsNullOrEmpty(SgbPath)
        ? $"#{LayoutId:X8}"
        : Path.GetFileNameWithoutExtension(SgbPath);
}

public static class MapEffectResolver
{
    private static readonly string[] LgbNames = ["bg", "planmap", "planevent", "planlive", "vfx", "sound", "planner"];

    private static Dictionary<uint, ushort>? _cfcToManagedSGRow;
    private static readonly Dictionary<uint, List<MapEffectSlotInfo>> _zoneCache = [];

    public static List<MapEffectSlotInfo> GetZoneSlots(uint territoryType)
    {
        if (_zoneCache.TryGetValue(territoryType, out var cached)) return cached;

        var result = new List<MapEffectSlotInfo>();
        _zoneCache[territoryType] = result;

        var territory = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(territoryType);
        if (territory == null) return result;

        var cfcId = territory.Value.ContentFinderCondition.RowId;
        if (cfcId == 0) return result;

        EnsureCfcMap();
        if (!_cfcToManagedSGRow!.TryGetValue(cfcId, out var sgRowId)) return result;

        var sgSheet = Svc.Data.GetSubrowExcelSheet<ContentDirectorManagedSG>();
        var subrows = sgSheet?.GetRowOrDefault(sgRowId);
        if (subrows == null) return result;

        ushort slotIdx = 0;
        foreach (var sub in subrows.Value)
        {
            var slot = slotIdx++;
            if (sub.LayoutId == 0) continue;
            result.Add(new MapEffectSlotInfo
            {
                Slot = slot,
                LayoutId = (uint)sub.LayoutId,
                DefaultOn = sub.Unknown1,
            });
        }
        if (result.Count == 0) return result;

        var bg = territory.Value.Bg.ExtractText();
        ResolveSgbPaths(bg, result);

        foreach (var s in result)
        {
            if (string.IsNullOrEmpty(s.SgbPath)) continue;
            s.States = ReadStateMappings(s.SgbPath);
        }

        return result;
    }

    public static void InvalidateCache(uint? territoryType = null)
    {
        if (territoryType.HasValue) _zoneCache.Remove(territoryType.Value);
        else _zoneCache.Clear();
    }

    private static void EnsureCfcMap()
    {
        if (_cfcToManagedSGRow != null) return;
        _cfcToManagedSGRow = [];
        var sheet = Svc.Data.GetExcelSheet<InstanceContent>();
        if (sheet == null) return;
        foreach (var row in sheet)
        {
            var cfc = row.ContentFinderCondition.RowId;
            var sg = row.ContentDirectorManagedSG.RowId;
            if (cfc == 0 || sg == 0) continue;
            _cfcToManagedSGRow.TryAdd(cfc, (ushort)sg);
        }
    }

    private static void ResolveSgbPaths(string bg, List<MapEffectSlotInfo> slots)
    {
        if (string.IsNullOrEmpty(bg)) return;
        var slash = bg.LastIndexOf('/');
        var dir = slash >= 0 ? bg[..slash] : bg;

        var wanted = new Dictionary<uint, MapEffectSlotInfo>();
        foreach (var s in slots) wanted.TryAdd(s.LayoutId, s);
        if (wanted.Count == 0) return;

        foreach (var name in LgbNames)
        {
            if (wanted.Count == 0) break;

            LgbFile? lgb;
            try { lgb = Svc.Data.GetFile<LgbFile>($"bg/{dir}/{name}.lgb"); }
            catch { continue; }
            if (lgb == null) continue;

            foreach (var layer in lgb.Layers)
            {
                if (wanted.Count == 0) break;
                foreach (var io in layer.InstanceObjects)
                {
                    if (io.AssetType != LayerEntryType.SharedGroup) continue;
                    if (!wanted.TryGetValue(io.InstanceId, out var slot)) continue;
                    if (io.Object is LayerCommon.SharedGroupInstanceObject sg)
                    {
                        slot.SgbPath = sg.AssetPath;
                        wanted.Remove(io.InstanceId);
                    }
                }
            }
        }
    }


	// TODO: This should be changed to the proper SGBFile in Lumina once it got merged!
	// return Svc.Data.GetFile<SgbFile>(sgbPath)?.StateMappings
    private static (ushort State, string Name)[] ReadStateMappings(string sgbPath)
    {
        byte[]? data;
        try { data = Svc.Data.GetFile<FileResource>(sgbPath)?.Data; }
        catch { return []; }
        if (data == null || data.Length < 60) return [];

        var offsetAnimHandlers = BitConverter.ToInt32(data, 52);
        if (offsetAnimHandlers <= 0) return []; 
        var configBase = 20 + offsetAnimHandlers;
        if (configBase + 21 > data.Length) return [];

        var offsetTimelines = BitConverter.ToInt32(data, 36);
        var fileDataBase = 20 + offsetTimelines;
        if (fileDataBase + 8 > data.Length) return [];

        var entryOffset = BitConverter.ToInt32(data, fileDataBase);
        var count = BitConverter.ToInt32(data, fileDataBase + 4);
        if (count <= 0 || count > 128) return [];

        var tmlbNames = ExtractTmlbNames(data, count);

        var subIdToName = new Dictionary<int, string>(count);
        for (var n = 0; n < count; n++)
        {
            var entryStart = fileDataBase + entryOffset + n * 44;
            if (entryStart + 4 > data.Length) break;
            var subId = BitConverter.ToInt32(data, entryStart);
            if (subId > 0 && n < tmlbNames.Length)
                subIdToName.TryAdd(subId, tmlbNames[n]);
        }

        var result = new List<(ushort, string)>(16);
        for (var i = 0; i < 16; i++)
        {
            int subId = data[configBase + 5 + i];
            if (subId == 0) continue;
            var state = (ushort)(1 << i);
            var name = subIdToName.TryGetValue(subId, out var n2) ? n2 : $"SubId={subId}";
            result.Add((state, name));
        }
        return [.. result];
    }

    private static string[] ExtractTmlbNames(byte[] data, int limit)
    {
        var markerPos = -1;
        for (var i = data.Length - 1; i >= 0; i--)
        {
            if (data[i] == 0xFF) { markerPos = i; break; }
        }
        if (markerPos < 0 || markerPos >= data.Length - 1) return [];

        var names = new List<string>(limit);
        var pos = markerPos + 1;
        while (pos < data.Length && names.Count < limit)
        {
            var start = pos;
            while (pos < data.Length && data[pos] != 0) pos++;
            if (pos > start)
                names.Add(Encoding.UTF8.GetString(data, start, pos - start));
            pos++;
        }
        return [.. names];
    }
}

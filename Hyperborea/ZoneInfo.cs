namespace Hyperborea;
[Serializable]
public class ZoneInfo
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public string Name = "";
    public Point3 Spawn;
    public List<PhaseInfo> Phases = [];
}

[Serializable]
public class PhaseInfo
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public string Name = "";
    public uint Weather = 0;
    public List<MapEffectInfo> MapEffects = [];
    public List<FestivalInfo> Festivals = [];
}

[Serializable]
public class MapEffectInfo
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public int a1;
    public int a2;
    public int a3;
}

[Serializable]
public class FestivalInfo
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public int Id;
    public int PhaseId = -1;
}

[Serializable]
public class DirectorInitInfo
{
    public uint a1;
    public uint a2;
}
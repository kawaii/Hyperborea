namespace Hyperborea;
public class SavedZoneState
{
    public uint ZoneId;
    public Vector3 Position;

    public SavedZoneState(uint zoneId, Vector3 position)
    {
        this.ZoneId = zoneId;
        this.Position = position;
    }
}

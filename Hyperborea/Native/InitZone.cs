namespace Hyperborea.Native;
[StructLayout(LayoutKind.Sequential)]
public unsafe struct InitZone
{
    public ushort ZoneId;
    public ushort TerritoryType;
    public ushort TerritoryIndex;
    public byte __padding1;
    public byte __padding2;
    public uint LayerSetId;
    public uint LayoutId;
    public byte WeatherId;
    public byte Flag;
    public ushort FestivalEid0;
    public ushort FestivalPid0;
    public ushort FestivalEid1;
    public ushort FestivalPid1;
    public byte __padding3;
    public byte __padding4;
    public fixed float Pos[3];
}

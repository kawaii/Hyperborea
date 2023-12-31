using Lumina.Data;

namespace Hyperborea;

unsafe public class LvbFile : FileResource
{
    public ushort[] weatherIds;
    public string envbFile;

    public override void LoadFile()
    {
        weatherIds = new ushort[32];

        int pos = 0xC;
        if (Data[pos] != 'S' || Data[pos + 1] != 'C' || Data[pos + 2] != 'N' || Data[pos + 3] != '1')
            pos += 0x14;
        int sceneChunkStart = pos;
        pos += 0x10;
        int settingsStart = sceneChunkStart + 8 + BitConverter.ToInt32(Data, pos);
        pos = settingsStart + 0x40;
        int weatherTableStart = settingsStart + BitConverter.ToInt32(Data, pos);
        pos = weatherTableStart;
        for (int i = 0; i < 32; i++)
            weatherIds[i] = BitConverter.ToUInt16(Data, pos + i * 2);

        if (Data.TryFindBytes("2E 65 6E 76 62 00", out pos))
        {
            var end = pos + 5;
            while (Data[pos - 1] != 0 && pos > 0)
            {
                pos--;
            }
            envbFile = Encoding.UTF8.GetString(Data.Skip(pos).Take(end - pos).ToArray());
        }
    }
}
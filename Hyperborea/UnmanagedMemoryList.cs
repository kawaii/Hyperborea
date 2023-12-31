using System.Collections;

namespace Hyperborea;
public class UnmanagedMemoryList : IDisposable, IEnumerable<nint>
{
    List<nint> Objects = [];

    public UnmanagedMemoryList()
    {

    }

    public nint Add(int size)
    {
        var ptr = Marshal.AllocHGlobal(size);
        Objects.Add(ptr);
        return ptr;
    }

    public IReadOnlyCollection<nint> Values => Objects.AsReadOnly();

    public nint this[int index] => Objects[index];

    public int Count => Objects.Count;

    public void Dispose()
    {
        foreach(var obj in Objects)
        {
            Marshal.FreeHGlobal(obj);
        }
        Objects.Clear();
    }

    public IEnumerator<nint> GetEnumerator()
    {
        return Objects.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Objects.GetEnumerator();
    }
}

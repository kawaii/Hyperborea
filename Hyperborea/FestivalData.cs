using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea;
[Serializable]
public record struct FestivalData
{
    public int Id;
    public string Name = "";
    public bool Unsafe;

    public FestivalData()
    {
    }
}

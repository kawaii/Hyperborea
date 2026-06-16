using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea;
[Serializable]
public class FestivalData
{
    public int Id;
    public string Name = "";
    public bool Unsafe;
    public List<FestivalPhaseData> Phases = [];
}

[Serializable]
public class FestivalPhaseData
{
    public int Id;
    public string Name = "";
}

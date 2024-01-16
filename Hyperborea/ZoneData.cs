using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea;
public class ZoneData : IEzConfig
{
    public Dictionary<string, ZoneInfo> Data = [];
}

using ECommons.Singletons;
using Hyperborea.Services.OpcodeUpdaterService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea.Services;
public static class S
{
    [Priority(int.MaxValue)] public static ThreadPool ThreadPool { get; private set; }
    public static OpcodeUpdater OpcodeUpdater { get; private set; }
}

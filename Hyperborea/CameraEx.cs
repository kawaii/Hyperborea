using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea;
[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CameraEx
{
    [FieldOffset(0x60)] public float x;
    [FieldOffset(0x64)] public float y;
    [FieldOffset(0x68)] public float z;
    [FieldOffset(0x130)] public float currentHRotation; 
}
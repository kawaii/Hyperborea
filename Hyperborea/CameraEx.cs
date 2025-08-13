using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperborea;
[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CameraEx
{
    //just copied it from hypostasis https://github.com/UnknownX7/Hypostasis/blob/master/Game/Structures/GameCamera.cs (c) UnknownX7
    [FieldOffset(0x0)] public nint* vtbl;
    [FieldOffset(0x60)] public float x;
    [FieldOffset(0x64)] public float y;
    [FieldOffset(0x68)] public float z;
    [FieldOffset(0x90)] public float lookAtX; // Position that the camera is focused on (Actual position when zoom is 0)
    [FieldOffset(0x94)] public float lookAtY;
    [FieldOffset(0x98)] public float lookAtZ;
    [FieldOffset(0x124)] public float currentZoom; // 6
    [FieldOffset(0x128)] public float minZoom; // 1.5
    [FieldOffset(0x12C)] public float maxZoom; // 20
    [FieldOffset(0x130)] public float currentFoV; // 0.78
    [FieldOffset(0x134)] public float minFoV; // 0.69
    [FieldOffset(0x138)] public float maxFoV; // 0.78
    [FieldOffset(0x13C)] public float addedFoV; // 0
    [FieldOffset(0x140)] public float currentHRotation; // -pi -> pi, default is pi
    [FieldOffset(0x144)] public float currentVRotation; // -0.349066
    [FieldOffset(0x148)] public float hRotationDelta;
    [FieldOffset(0x158)] public float minVRotation; // -1.483530, should be -+pi/2 for straight down/up but camera breaks so use -+1.569
    [FieldOffset(0x15C)] public float maxVRotation; // 0.785398 (pi/4)
    [FieldOffset(0x170)] public float tilt;
    [FieldOffset(0x180)] public int mode; // Camera mode? (0 = 1st person, 1 = 3rd person, 2+ = weird controller mode? cant look up/down)
    [FieldOffset(0x184)] public int controlType; // 0 first person, 1 legacy, 2 standard, 4 talking to npc in first person (with option enabled), 5 talking to npc (with option enabled), 3/6 ???
    [FieldOffset(0x18C)] public float interpolatedZoom;
    [FieldOffset(0x1A0)] public float transition; // Seems to be related to the 1st <-> 3rd camera transition
    [FieldOffset(0x1C0)] public float viewX;
    [FieldOffset(0x1C4)] public float viewY;
    [FieldOffset(0x1C8)] public float viewZ;
    [FieldOffset(0x1F4)] public byte isFlipped; // 1 while holding the keybind
    [FieldOffset(0x22C)] public float interpolatedY;
    [FieldOffset(0x234)] public float lookAtHeightOffset; // No idea what to call this (0x230 is the interpolated value)
    [FieldOffset(0x238)] public byte resetLookatHeightOffset; // No idea what to call this
    [FieldOffset(0x240)] public float interpolatedLookAtHeightOffset;
    [FieldOffset(0x2C0)] public byte lockPosition;
    [FieldOffset(0x2D4)] public float lookAtY2;

}
using ECommons.ExcelServices;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel.GeneratedSheets;

namespace Hyperborea.Gui;
public unsafe class DebugWindow: Window
{
    public DebugWindow() : base("Hyperborea Debug Window")
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        var l = (nint)LayoutWorld.Instance()->ActiveLayout;
        if(l != 0)
        {
            ImGuiEx.Text($"{*(int*)(l + 40)} | {*(int*)(l + 40):X16}");
            ImGuiEx.TextCopy($"{(l + 40):X16}");
        }

        ImGuiEx.Checkbox("Load Map", ref UI.a6);
    }
}

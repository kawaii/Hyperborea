using ECommons.Interop;
using ECommons.SimpleGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hyperborea.Gui;
public class Overlay : Window
{
    public Overlay() : base("Hyperborea Overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysUseWindowPadding, true)
    {
        this.Position = Vector2.Zero;
        this.PositionCondition = ImGuiCond.Always;
        this.Size = ImGuiHelpers.MainViewport.Size;
        this.SizeCondition = ImGuiCond.Always;
        EzConfigGui.WindowSystem.AddWindow(this);
        this.IsOpen = true;
        this.RespectCloseHotkey = false;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
    }

    public override bool DrawConditions()
    {
        return P.Enabled && C.FastTeleport && IsKeyPressed([LimitedKeys.LeftControlKey, LimitedKeys.RightControlKey]);
    }

    public override void Draw()
    {
        var pos = ImGui.GetMousePos();
        if (Svc.GameGui.ScreenToWorld(pos, out var res))
        {
            var col = GradientColor.Get(EColor.RedBright, EColor.YellowBright);
            DrawRingWorld(res, 0.5f, col.ToUint(), 2f);
            var l = MathF.Sqrt(2f)/2f * 0.5f;
            DrawLineWorld(res + new Vector3(-l, 0, -l), res + new Vector3(l, 0, l), col.ToUint(), 2f);
            DrawLineWorld(res + new Vector3(l, 0, -l), res + new Vector3(-l, 0, l), col.ToUint(), 2f);
        }
    }

    void DrawLineWorld(Vector3 a, Vector3 b, uint color, float thickness)
    {
        var result = GetAdjustedLine(a, b);
        if (result.posA == null) return;
        ImGui.GetWindowDrawList().PathLineTo(new Vector2(result.posA.Value.X, result.posA.Value.Y));
        ImGui.GetWindowDrawList().PathLineTo(new Vector2(result.posB.Value.X, result.posB.Value.Y));
        ImGui.GetWindowDrawList().PathStroke(color, ImDrawFlags.None, thickness);
    }

    (Vector2? posA, Vector2? posB) GetAdjustedLine(Vector3 pointA, Vector3 pointB)
    {
        var resultA = Svc.GameGui.WorldToScreen(pointA, out Vector2 posA);
        var resultB = Svc.GameGui.WorldToScreen(pointB, out Vector2 posB);
        //if (!resultA || !resultB) return default;
        return (posA, posB);
    }

    public void DrawRingWorld(Vector3 position, float radius, uint color, float thickness)
    {
        var segments = 50;
        int seg = segments / 2;
        Vector2?[] elements = new Vector2?[segments];
        for (int i = 0; i < segments; i++)
        {
            Svc.GameGui.WorldToScreen(
                new Vector3(position.X + radius * (float)Math.Sin(Math.PI / seg * i),
                position.Y,
                position.Z + radius * (float)Math.Cos(Math.PI / seg * i)
                ),
                out Vector2 pos);
            elements[i] = new Vector2(pos.X, pos.Y);
        }
        foreach (var pos in elements)
        {
            if (pos == null) continue;
            ImGui.GetWindowDrawList().PathLineTo(pos.Value);
        }
        ImGui.GetWindowDrawList().PathStroke(color, ImDrawFlags.Closed, thickness);
    }
}

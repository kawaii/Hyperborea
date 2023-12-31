namespace Hyperborea;

public static partial class ImGuiGroup
{
    public record GroupBoxOptions
    {
        public bool Collapsible { get; init; } = false;
        public uint HeaderTextColor { get; init; } = ImGui.GetColorU32(ImGuiCol.Text);
        public Action HeaderTextAction { get; init; } = null;
        public uint BorderColor { get; init; } = ImGui.GetColorU32(ImGuiCol.Border);
        public Vector2 BorderPadding { get; init; } = ImGui.GetStyle().WindowPadding;
        public float BorderRounding { get; init; } = ImGui.GetStyle().FrameRounding;
        public ImDrawFlags DrawFlags { get; init; } = ImDrawFlags.None;
        public float BorderThickness { get; init; } = 2f;
        public float Width { get; set; }
        public float MaxX { get; set; }
    }

    private static readonly Stack<GroupBoxOptions> groupBoxOptionsStack = new();
    public static bool BeginGroupBox(string id = null, float minimumWindowPercent = 1.0f, GroupBoxOptions options = null)
    {
        options ??= new GroupBoxOptions();
        ImGui.BeginGroup();

        var open = true;
        if (!string.IsNullOrEmpty(id))
        {
            if (!options.Collapsible)
            {
                ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(options.HeaderTextColor), id);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, options.HeaderTextColor);
                open = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.DefaultOpen);
                ImGui.PopStyleColor();
            }

            options.HeaderTextAction?.Invoke();

            // This prevents rounding issues caused by ImGui flooring the cursor position after items
            ImGui.Indent();
            ImGui.Unindent();
        }

        var style = ImGui.GetStyle();
        var spacing = style.ItemSpacing.X * (1 - minimumWindowPercent);
        var contentRegionWidth = groupBoxOptionsStack.TryPeek(out var parent) ? parent.Width - parent.BorderPadding.X * 2 : ImGui.GetWindowContentRegionMax().X - style.WindowPadding.X;
        var width = Math.Max(contentRegionWidth * minimumWindowPercent - spacing, 1);
        options.Width = minimumWindowPercent > 0 ? width : 0;

        ImGui.BeginGroup();
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        ImGui.Dummy(options.BorderPadding with { X = width });
        ImGui.PopStyleVar();

        var max = ImGui.GetItemRectMax();
        options.MaxX = max.X;

        if (options.Width > 0)
            ImGui.PushClipRect(ImGui.GetItemRectMin(), max with { Y = 10000 }, true);

        ImGui.Indent(Math.Max(options.BorderPadding.X, 0.01f));
        ImGui.PushItemWidth(MathF.Floor((width - options.BorderPadding.X * 2) * 0.65f));

        groupBoxOptionsStack.Push(options);
        if (open) return true;

        ImGui.TextDisabled(". . .");
        EndGroupBox();
        return false;
    }

    public static bool BeginGroupBox(string text, GroupBoxOptions options) => BeginGroupBox(text, 1.0f, options);

    public static bool BeginGroupBox(string label, uint borderColor, float minimumWindowPercent = 1.0f) => BeginGroupBox(label, minimumWindowPercent, new GroupBoxOptions { BorderColor = borderColor });

    public static unsafe void EndGroupBox()
    {
        var options = groupBoxOptionsStack.Pop();
        var autoAdjust = options.Width <= 0;
        ImGui.PopItemWidth();
        ImGui.Unindent(Math.Max(options.BorderPadding.X, 0.01f));

        if (!autoAdjust)
            ImGui.PopClipRect();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
        ImGui.Dummy(options.BorderPadding with { X = 0 });

        if (!autoAdjust)
        {
            var window = GetCurrentWindow();
            window->CursorMaxPos = window->CursorMaxPos with { X = options.MaxX };
        }

        ImGui.EndGroup();

        var min = ImGui.GetItemRectMin();
        var max = autoAdjust ? ImGui.GetItemRectMax() : ImGui.GetItemRectMax() with { X = options.MaxX };

        // Rect with text corner missing
        /*ImGui.PushClipRect(min with { Y = min.Y + options.BorderRounding }, max, true);
        ImGui.GetWindowDrawList().AddRect(min, max, options.BorderColor, options.BorderRounding, options.DrawFlags, options.BorderThickness);
        ImGui.PopClipRect();
        ImGui.PushClipRect(min with { X = (min.X + max.X) / 2 }, max with { Y = min.Y + options.BorderRounding }, true);
        ImGui.GetWindowDrawList().AddRect(min, max, options.BorderColor, options.BorderRounding, options.DrawFlags, options.BorderThickness);
        ImGui.PopClipRect();*/

        // [ ] Brackets
        /*ImGui.PushClipRect(min, max with { X = (min.X * 2 + max.X) / 3 }, true);
        ImGui.GetWindowDrawList().AddRect(min, max, options.BorderColor, options.BorderRounding, options.DrawFlags, options.BorderThickness);
        ImGui.PopClipRect();
        ImGui.PushClipRect(min with { X = (min.X + max.X * 2) / 3 }, max, true);
        ImGui.GetWindowDrawList().AddRect(min, max, options.BorderColor, options.BorderRounding, options.DrawFlags, options.BorderThickness);
        ImGui.PopClipRect();*/

        // Horizontal brackets
        /*ImGui.PushClipRect(min, max with { Y = (min.Y * 2 + max.Y) / 3 }, true);
        ImGui.GetWindowDrawList().AddRect(min, max, options.BorderColor, options.BorderRounding, options.DrawFlags, options.BorderThickness);
        ImGui.PopClipRect();
        ImGui.PushClipRect(min with { Y = (min.Y + max.Y * 2) / 3 }, max, true);
        ImGui.GetWindowDrawList().AddRect(min, max, options.BorderColor, options.BorderRounding, options.DrawFlags, options.BorderThickness);
        ImGui.PopClipRect();*/

        ImGui.GetWindowDrawList().AddRect(min, max, options.BorderColor, options.BorderRounding, options.DrawFlags, options.BorderThickness);

        ImGui.EndGroup();
    }

    [LibraryImport("cimgui")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial nint igGetCurrentWindow();
    public static unsafe ImGuiWindow* GetCurrentWindow() => (ImGuiWindow*)igGetCurrentWindow();
    public static unsafe ImGuiWindowFlags GetCurrentWindowFlags() => GetCurrentWindow()->Flags;
    public static unsafe bool CurrentWindowHasCloseButton() => GetCurrentWindow()->HasCloseButton != 0;
}

[StructLayout(LayoutKind.Explicit)]
public struct ImGuiWindow
{
    [FieldOffset(0xC)] public ImGuiWindowFlags Flags;

    [FieldOffset(0xD5)] public byte HasCloseButton;

    // 0x118 is the start of ImGuiWindowTempData
    [FieldOffset(0x130)] public Vector2 CursorMaxPos;
}
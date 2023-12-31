using ECommons.SimpleGui;

namespace Hyperborea.Gui;
public class LogWindow : Window
{
    public LogWindow() : base("Hyperborea log")
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        InternalLog.PrintImgui();
    }
}

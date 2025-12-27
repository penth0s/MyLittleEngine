using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Core;

public static class Input
{
    public static KeyboardState Keybord { internal set; get; }
    public static MouseState Mouse { internal set; get; }
}
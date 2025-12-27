using Engine.Scripts;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using PhysicsEngine;
using Project;
using Editor;
using Engine.Core;
using Engine.Utilities;
using PhysicsEngine.Core;

namespace ConsoleApp;

public static class Program
{
    internal static void Main()
    {
        var engine = StartEngineWindow();
        var editor = new Editor.Core.Editor();
        var physics = new PhysicsManager();
        
        engine.InitEngine();
        physics.Init(engine.GetEngineInfoProvider());
        editor.InitEditor(engine.GetEngineInfoProvider());
        engine.ImportEditorProvider(editor.EditorInfoProvider);
        
        engine.Run();
    }

    private static EngineWindow StartEngineWindow()
    {
        var nativeWindowSettings = new NativeWindowSettings()
        {
            WindowState = WindowState.Maximized,
            Title = "My Little Engine v1.0",
            Flags = ContextFlags.ForwardCompatible,
        };

        var window = new EngineWindow(GameWindowSettings.Default, nativeWindowSettings);
        return window;
    }
    
  
    
}
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Adapters;


public interface IGameEngineInfoProvider
{
    public delegate void GameFrameUpdateHandler(FrameEventArgs frameArgs, KeyboardState keyboardState, MouseState mouseState);
    public delegate void MouseWheelHandler(MouseWheelEventArgs wheelArgs);
    public delegate void TextInputHandler(TextInputEventArgs textArgs);
    public delegate void ResizeEditorHandler(Vector2i newSize);
    public delegate void DrawEditorHandler(GameWindow window, int framebufferId, FrameEventArgs frameArgs);
    public delegate Guid RayCastHandler(Vector3 origin, Vector3 direction);
        
    event GameFrameUpdateHandler GameFrameUpdate;
    event MouseWheelHandler MouseWheel;
    event TextInputHandler TextInput;
    event ResizeEditorHandler ResizeEditor;
    event DrawEditorHandler DrawEditor;
    event EventHandler<IPhysicsBodyListener> RigidBodyCreated;
    event EventHandler<IPhysicsBody> RigidBodyDestroyed;
    event EventHandler<Guid> SelectObject;
    event EventHandler<ConsoleMessage> ConsoleMessage;
    event EventHandler EngineInitialized;
    event EventHandler ResestInspector;
    event Action EngineShutdown; 
    event RayCastHandler RayCast;
    
    Vector2i GetResolution();
    
    bool GetIsGridEnabled();
    
    bool GetIsDebugViewEnabled();
    
    bool GetIsWireframeEnabled();
}
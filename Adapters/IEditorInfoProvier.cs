using OpenTK.Mathematics;

namespace Adapters;

public interface IEditorInfoProvider
{
    bool IsDebugView { get; }
    bool IsWireFrameActive { get; }
    float GetAspectRatio();
    Vector2i GetViewportSize();
    Vector2 GetViewportNdc();
    
    EventHandler RaycastRequest { get; set; }
}
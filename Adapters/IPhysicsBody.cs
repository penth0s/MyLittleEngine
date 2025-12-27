using OpenTK.Mathematics;

namespace Adapters;

public interface IPhysicsBody
{
    
    Guid Id { get; set;}
    Vector3 Position { get; set; }
    Vector3 Velocity { get; set; }
    
    Vector3 AngularVelocity { get; set; }
    
    Quaternion Orientation { get; set; }
    
    float Friction { get; set; }
    
    bool IsStatic { get; set; }

    EventHandler BodyUpdated { get; set; }

    void AddShape(List<Vector3> vertices);
    
    void AddCapsuleShape(float radius, float lenght);
    
    List<Vector3>?  GetDebugShape();
}
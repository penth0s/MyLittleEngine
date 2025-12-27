using OpenTK.Mathematics;

namespace Adapters;

public interface IPhysicsBodyListener
{
    Guid Id { get; }
    void SetPhysicsBody(IPhysicsBody body);
}
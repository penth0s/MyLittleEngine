namespace Engine.Systems;

internal interface IGameUpdateSystem : ISystem
{
    void FrameUpdate();
}
namespace Engine.Systems;

internal interface ISystem
{
    void Initialize();
}

internal interface ISystem<TConfig> where TConfig : ISystemConfig
{
    void Initialize(TConfig config);
}
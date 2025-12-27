namespace Engine.Database;

public interface IDatabaseProvider
{
    T Get<T>() where T : IDatabase;
}
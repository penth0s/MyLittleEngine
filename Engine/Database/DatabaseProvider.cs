namespace Engine.Database;

/// <summary>
/// Manages and provides access to all database instances in the engine.
/// Acts as a centralized registry for database implementations.
/// </summary>
public sealed class DatabaseProvider : IDatabaseProvider
{
    #region Fields

    private readonly HashSet<IDatabase> _registeredDatabases = new();

    #endregion

    #region Database Retrieval

    /// <summary>
    /// Retrieves a database instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of database to retrieve.</typeparam>
    /// <returns>The database instance if found; otherwise, the default value for the type.</returns>
    public T Get<T>() where T : IDatabase
    {
        var database = FindDatabase<T>();

        if (database == null) LogDatabaseNotFound<T>();

        return database;
    }

    private T FindDatabase<T>() where T : IDatabase
    {
        foreach (var database in _registeredDatabases)
            if (database is T matchingDatabase)
                return matchingDatabase;

        return default;
    }

    private void LogDatabaseNotFound<T>() where T : IDatabase
    {
        var databaseTypeName = typeof(T).Name;
        Console.WriteLine($"Failed to find Database of type '{databaseTypeName}'");
    }

    #endregion

    #region Database Registration

    /// <summary>
    /// Registers and initializes a database instance with the provider.
    /// </summary>
    /// <param name="database">The database instance to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if the database is null.</exception>
    public void Bind(IDatabase database)
    {
        ValidateDatabaseNotNull(database);
        RegisterDatabase(database);
        InitializeDatabase(database);
    }

    private void ValidateDatabaseNotNull(IDatabase database)
    {
        if (database == null)
            throw new ArgumentNullException(
                nameof(database),
                "Cannot bind a null database instance."
            );
    }

    private void RegisterDatabase(IDatabase database)
    {
        _registeredDatabases.Add(database);
    }

    private void InitializeDatabase(IDatabase database)
    {
        database.Initialize();
    }

    #endregion
}
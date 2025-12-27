using System.Reflection;
using Engine.Database;

namespace Engine.Systems;

/// <summary>
/// System responsible for discovering, initializing, and managing all database instances in the engine.
/// Automatically finds and registers all IDatabase implementations at startup.
/// </summary>
public sealed class DatabaseSystem : ISystem
{
    #region Constants

    private const string INITIALIZATION_START_MESSAGE = "=== DatabaseSystem Initialization Started ===\n";
    private const string INITIALIZATION_COMPLETE_MESSAGE = "=== DatabaseSystem Initialization Completed ===\n";
    private const string CHECKMARK = "✓";
    private const string WARNING_SYMBOL = "⚠";
    private const string ERROR_SYMBOL = "✗";

    #endregion

    #region Fields

    private DatabaseProvider _databaseProvider;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the database system by discovering and registering all database implementations.
    /// </summary>
    public void Initialize()
    {
        LogInitializationStart();

        _databaseProvider = new DatabaseProvider();

        var databaseTypes = DiscoverDatabaseTypes();
        LogDiscoveredDatabaseCount(databaseTypes);

        RegisterAllDatabases(databaseTypes);

        LogInitializationComplete();
    }

    private void LogInitializationStart()
    {
        Console.WriteLine(INITIALIZATION_START_MESSAGE);
    }

    private void LogInitializationComplete()
    {
        Console.WriteLine(INITIALIZATION_COMPLETE_MESSAGE);
    }

    #endregion

    #region Database Discovery

    /// <summary>
    /// Discovers all types in the current assembly that implement IDatabase.
    /// </summary>
    /// <returns>A list of database types found in the assembly.</returns>
    private List<Type> DiscoverDatabaseTypes()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(IsValidDatabaseType)
            .ToList();
    }

    /// <summary>
    /// Determines whether a type is a valid database implementation.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a concrete class implementing IDatabase; otherwise, false.</returns>
    private bool IsValidDatabaseType(Type type)
    {
        return !type.IsInterface
               && !type.IsAbstract
               && type.IsClass
               && typeof(IDatabase).IsAssignableFrom(type);
    }

    private void LogDiscoveredDatabaseCount(List<Type> databaseTypes)
    {
        Console.WriteLine($"Found {databaseTypes.Count} database types\n");
    }

    #endregion

    #region Database Registration

    /// <summary>
    /// Registers all discovered database types with the provider.
    /// </summary>
    /// <param name="databaseTypes">The list of database types to register.</param>
    private void RegisterAllDatabases(List<Type> databaseTypes)
    {
        foreach (var databaseType in databaseTypes) TryRegisterDatabase(databaseType);
    }

    /// <summary>
    /// Attempts to register a single database type with error handling.
    /// </summary>
    /// <param name="databaseType">The database type to register.</param>
    private void TryRegisterDatabase(Type databaseType)
    {
        try
        {
            RegisterDatabase(databaseType);
        }
        catch (Exception ex)
        {
            LogRegistrationError(databaseType, ex);
        }
    }

    /// <summary>
    /// Creates an instance of a database type and registers it with the provider.
    /// </summary>
    /// <param name="databaseType">The database type to instantiate and register.</param>
    private void RegisterDatabase(Type databaseType)
    {
        var database = CreateDatabaseInstance(databaseType);

        if (database == null)
        {
            LogInstanceCreationWarning(databaseType);
            return;
        }

        BindDatabaseToProvider(database);
        LogSuccessfulRegistration(databaseType);
    }

    /// <summary>
    /// Creates an instance of the specified database type.
    /// </summary>
    /// <param name="databaseType">The type of database to create.</param>
    /// <returns>An instance of the database, or null if creation fails.</returns>
    private IDatabase CreateDatabaseInstance(Type databaseType)
    {
        return Activator.CreateInstance(databaseType) as IDatabase;
    }

    /// <summary>
    /// Registers a database instance with the provider.
    /// </summary>
    /// <param name="database">The database instance to register.</param>
    private void BindDatabaseToProvider(IDatabase database)
    {
        _databaseProvider?.Bind(database);
    }

    #endregion

    #region Database Retrieval

    /// <summary>
    /// Retrieves a database instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of database to retrieve.</typeparam>
    /// <returns>The database instance if found; otherwise, the default value.</returns>
    public T GetDatabase<T>() where T : IDatabase
    {
        if (_databaseProvider == null)
            throw new InvalidOperationException(
                "DatabaseSystem has not been initialized. Call Initialize() first."
            );

        return _databaseProvider.Get<T>();
    }

    #endregion

    #region Logging

    private void LogInstanceCreationWarning(Type databaseType)
    {
        Console.WriteLine($"{WARNING_SYMBOL} Warning: Could not create instance of {databaseType.Name}");
    }

    private void LogSuccessfulRegistration(Type databaseType)
    {
        Console.WriteLine($"{CHECKMARK} {databaseType.Name} bound to DatabaseProvider");
    }

    private void LogRegistrationError(Type databaseType, Exception exception)
    {
        Console.WriteLine($"{ERROR_SYMBOL} Error binding {databaseType.Name}: {exception.Message}\n");
    }

    #endregion
}
using System.Reflection;

namespace Engine.Utilities;

/// <summary>
/// Provides utility methods for locating key project directories
/// such as the engine root and example project folders.
/// </summary>
public static class ProjectPathHelper
{
    private static readonly string BasePath;
    private static readonly string SolutionPath;

    private const string EngineProjectName = "Engine";
    private const string ExampleProjectName = "Examples";

    static ProjectPathHelper()
    {
        try
        {
            BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                       ?? throw new DirectoryNotFoundException("Unable to determine base assembly path.");

            // Try walking up until solution root (assuming 'Engine/bin/...').
            var dir = new DirectoryInfo(BasePath);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, EngineProjectName))) dir = dir.Parent;

            SolutionPath = dir?.FullName
                           ?? throw new DirectoryNotFoundException(
                               "Unable to locate solution root (Engine folder not found).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectPathHelper] Initialization failed: {ex.Message}");
            BasePath = AppContext.BaseDirectory;
            SolutionPath = BasePath;
        }
    }

    /// <summary>
    /// Gets the absolute path to the engine project directory.
    /// </summary>
    public static string GetEnginePath()
    {
        var path = Path.Combine(SolutionPath, EngineProjectName);
        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Gets the absolute path to the examples project directory.
    /// </summary>
    public static string GetExamplePath()
    {
        var path = Path.Combine(SolutionPath, ExampleProjectName);
        return Path.GetFullPath(path);
    }
}
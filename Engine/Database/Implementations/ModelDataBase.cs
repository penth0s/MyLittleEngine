using Engine.Utilities;

namespace Engine.Database.Implementations;

internal sealed class ModelDataBase : IDatabase
{
    private readonly Dictionary<string, Assimp.Scene> models = new();
    private readonly Dictionary<string, Assimp.Animation> animations = new();

    public void Initialize()
    {
        LoadAllModels();
    }

    private void LoadAllModels()
    {
        var modelNames = AssetLoader.GetModelNames();

        foreach (var modelName in modelNames)
            try
            {
                var modelData = AssetLoader.LoadFromFile(modelName);
                models.Add(modelName, modelData);

                foreach (var animation in modelData.Animations)
                {
                    var animName = Path.GetFileNameWithoutExtension(modelName);
                    animations.TryAdd(animName, animation);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load model {modelName}: {ex.Message}");
            }
    }

    public Assimp.Animation GetAnimation(string animationName)
    {
        if (string.IsNullOrEmpty(animationName))
            throw new ArgumentException("Animation name cannot be null or empty.", nameof(animationName));

        if (!animations.TryGetValue(animationName, out var animation))
            throw new KeyNotFoundException($"Animation '{animationName}' not found in the database.");

        return animation;
    }

    public Assimp.Scene GetModel(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
            throw new ArgumentException("Model name cannot be null or empty.", nameof(modelName));

        return !models.TryGetValue(modelName, out var model) ? throw new KeyNotFoundException($"Model '{modelName}' not found in the database.") : model;
    }

    public Assimp.Mesh GetMesh(string modelName, int meshIndex)
    {
        if (string.IsNullOrEmpty(modelName))
            throw new ArgumentException("Model name cannot be null or empty.", nameof(modelName));

        var scene = GetModel(modelName);

        if (meshIndex < 0 || meshIndex >= scene.MeshCount)
            throw new ArgumentOutOfRangeException(nameof(meshIndex), "Mesh index is out of range.");

        return scene.Meshes[meshIndex];
    }

    public Assimp.Mesh GetMeshFromSaveData(string modelName, int meshIndex)
    {
        var scene = GetModel(modelName);

        if (meshIndex < 0 || meshIndex >= scene.MeshCount)
            throw new ArgumentOutOfRangeException(nameof(meshIndex), "Mesh index is out of range.");

        return scene.Meshes[meshIndex];
    }
}
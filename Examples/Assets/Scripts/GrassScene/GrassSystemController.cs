using Engine.Components;
using Engine.Core;
using Engine.Systems;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project.Assets.Scripts.GrassScene;

public class GrassSystemController : BeroBehaviour
{
    public string InteactionGameObjectName;
    public float MoveSpeed;

    private GrassRenderer _grassRenderer;
    private SceneSystem _sceneSystem;

    private GameObject _interactionObject;

    protected override void Start()
    {
        base.Start();

        _grassRenderer = new GrassRenderer();
        _grassRenderer.Initialize();
        _sceneSystem = SystemManager.GetSystem<SceneSystem>();

        _interactionObject = _sceneSystem.CurrentScene.GetGameObjectByName(InteactionGameObjectName);

        if (_interactionObject != null) _grassRenderer.AddInteractionObject(_interactionObject.Transform);
    }

    public override void RenderUpdate()
    {
        base.RenderUpdate();

        _grassRenderer.RenderGrass(_sceneSystem.CurrentScene.Camera);
    }

    public override void ShadowPassUpdate()
    {
        base.ShadowPassUpdate();

        var spaceMatrix = _sceneSystem.CurrentScene.GeSceneLights[0].GetLightSpaceMatrix();
        var lightDir = _sceneSystem.CurrentScene.GeSceneLights[0].Transform.Forward;
        _grassRenderer.RenderShadow(spaceMatrix, lightDir, 2);
    }

    public override void Update()
    {
        base.Update();

        if (_interactionObject == null)
            return;

        if (Input.Keybord.IsKeyDown(Keys.D))
            _interactionObject.Transform.WorldPosition -= Transform.Forward * MoveSpeed * Time.DeltaTime;

        if (Input.Keybord.IsKeyDown(Keys.A))
            _interactionObject.Transform.WorldPosition += Transform.Forward * MoveSpeed * Time.DeltaTime;

        if (Input.Keybord.IsKeyDown(Keys.S))
            _interactionObject.Transform.WorldPosition -= Transform.Right * MoveSpeed * Time.DeltaTime;

        if (Input.Keybord.IsKeyDown(Keys.W))
            _interactionObject.Transform.WorldPosition += Transform.Right * MoveSpeed * Time.DeltaTime;
    }

    public override void Destroy()
    {
        base.Destroy();
        _grassRenderer.Cleanup();
    }
}
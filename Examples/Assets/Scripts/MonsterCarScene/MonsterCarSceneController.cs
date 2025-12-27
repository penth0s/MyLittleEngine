using Engine.Components;
using Engine.Core;
using Engine.Systems;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project.Assets.Scripts.MonsterCarScene;

public class MonsterCarSceneController : BeroBehaviour
{
    private SceneSystem _sceneSystem;

    protected override void Start()
    {
        base.Start();

        _sceneSystem = SystemManager.GetSystem<SceneSystem>();
        ClearScene();
    }

    public override void Update()
    {
        base.Update();

        if (Input.Keybord.IsKeyPressed(Keys.R))
        {
            ClearScene();

            var car = _sceneSystem.CurrentScene.AddAssetToScene("Monster_Truck_12A.fbx");
            var component = car.AddComponent<MonsterTruckController>();
            component.Enabled = true;
        }
    }

    private void ClearScene()
    {
        foreach (var oldCarComponent in _sceneSystem.CurrentScene.GetComponents<MonsterTruckController>())
            oldCarComponent.GameObject.Destroy();
    }
}
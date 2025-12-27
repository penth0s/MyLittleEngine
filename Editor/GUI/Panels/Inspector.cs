using Editor.Database;
using Editor.Drawers;
using Engine.Components;
using Engine.Core;
using ImGuiNET;

namespace Editor.GUI;

/// <summary>
/// Manages the inspector panel in the editor, displaying and editing properties of selected GameObjects.
/// Handles component management, property editing, and the add component popup.
/// </summary>
internal class Inspector
{
    #region Constants

    private const float WINDOW_POSITION_X_RATIO = 0.85f;
    private const float WINDOW_WIDTH_RATIO = 0.15f;
    private const float IMGUI_FONT_SCALE = 2.0f;
    private const float ADD_COMPONENT_BUTTON_WIDTH = 250f;
    private const float ADD_COMPONENT_SPACING = 50f;
    private const int NAME_INPUT_MAX_LENGTH = 100;
    private const string ADD_COMPONENT_POPUP_ID = "AddComponentPopup";

    #endregion

    #region Fields

    private GameObject _selectedGameObject;
    private ComponentDrawer _componentDrawer;
    private bool _isObjectActive;
    private string _objectName = string.Empty;
    private bool _showAddComponentPopup;

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the inspector to display the properties of the selected GameObject.
    /// </summary>
    /// <param name="selectedGameObject">The GameObject to inspect, or null to clear the inspector.</param>
    public void UpdateInspector(GameObject selectedGameObject)
    {
        _selectedGameObject = null;

        if (selectedGameObject == null)
            return;

        _selectedGameObject = selectedGameObject;
        _componentDrawer = new ComponentDrawer();

        UpdateCachedObjectProperties();
    }

    /// <summary>
    /// Draws the inspector window and its contents.
    /// </summary>
    public void Draw()
    {
        SetupInspectorWindow();

        if (ImGui.Begin("Inspector"))
        {
            ImGui.SetWindowFontScale(IMGUI_FONT_SCALE);

            if (_selectedGameObject != null) DrawInspectorContent();
        }

        ImGui.End();
    }

    #endregion

    #region Window Setup

    private void SetupInspectorWindow()
    {
        ImGui.SetNextWindowPos(CalculateWindowPosition(), ImGuiCond.Always);
        ImGui.SetNextWindowSize(CalculateWindowSize(), ImGuiCond.Always);
    }

    private System.Numerics.Vector2 CalculateWindowPosition()
    {
        var io = ImGui.GetIO();
        var screenSize = io.DisplaySize;

        return new System.Numerics.Vector2(screenSize.X * WINDOW_POSITION_X_RATIO, 0);
    }

    private System.Numerics.Vector2 CalculateWindowSize()
    {
        var io = ImGui.GetIO();
        var screenSize = io.DisplaySize;

        return screenSize with { X = screenSize.X * WINDOW_WIDTH_RATIO };
    }

    #endregion

    #region Inspector Content

    private void UpdateCachedObjectProperties()
    {
        if (_selectedGameObject == null)
            return;

        _isObjectActive = _selectedGameObject.IsActive;
        _objectName = _selectedGameObject.Name;
    }

    private void DrawInspectorContent()
    {
        var allComponents = _selectedGameObject!.GetAllComponents();

        if (allComponents.Count == 0)
            return;

        DrawObjectProperties();
        DrawAllComponents(allComponents);
        DrawAddComponentButton();
    }

    private void DrawObjectProperties()
    {
        DrawActiveToggle();
        DrawNameField();
    }

    private void DrawActiveToggle()
    {
        if (ImGui.Checkbox("Active", ref _isObjectActive)) _selectedGameObject!.SetActive(_isObjectActive);
    }

    private void DrawNameField()
    {
        ImGui.Text("Name");
        ImGui.SameLine();

        if (ImGui.InputText("##NameInput", ref _objectName, NAME_INPUT_MAX_LENGTH))
            _selectedGameObject!.Name = _objectName;
    }

    #endregion

    #region Component Drawing

    private void DrawAllComponents(List<Component> components)
    {
        var treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed;

        for (var i = 0; i < components.Count; i++)
        {
            var component = components[i];

            ImGui.PushID(component.GetHashCode());

            if (DrawComponentHeader(component))
            {
                // Component was removed, refresh and break
                UpdateInspector(_selectedGameObject);
                ImGui.PopID();
                break;
            }

            DrawComponentProperties(component, treeNodeFlags);

            ImGui.PopID();
        }
    }

    private bool DrawComponentHeader(Component component)
    {
        var componentTypeName = component.GetType().Name;
        var wasRemoved = false;

        ImGui.Columns(2);

        ImGui.Text(componentTypeName);
        ImGui.NextColumn();

        if (ImGui.Button("X"))
        {
            _selectedGameObject!.RemoveComponent(component);
            wasRemoved = true;
        }

        ImGui.Columns(1);

        return wasRemoved;
    }

    private void DrawComponentProperties(Component component, ImGuiTreeNodeFlags flags)
    {
        var componentTypeName = component.GetType().Name;

        if (ImGui.TreeNodeEx(componentTypeName, flags))
        {
            _componentDrawer.Draw(component);
            ImGui.TreePop();
        }
    }

    #endregion

    #region Add Component

    private void DrawAddComponentButton()
    {
        ImGui.Dummy(new System.Numerics.Vector2(0, ADD_COMPONENT_SPACING));

        CenterAddComponentButton();

        if (ImGui.Button("Add Component", new System.Numerics.Vector2(ADD_COMPONENT_BUTTON_WIDTH, 0)))
            OpenAddComponentPopup();

        if (_showAddComponentPopup) DrawAddComponentPopup();
    }

    private void CenterAddComponentButton()
    {
        var contentWidth = ImGui.GetContentRegionAvail().X;
        var buttonOffset = (contentWidth - ADD_COMPONENT_BUTTON_WIDTH) * 0.5f;
        ImGui.SetCursorPosX(buttonOffset);
    }

    private void OpenAddComponentPopup()
    {
        _showAddComponentPopup = true;
        ImGui.OpenPopup(ADD_COMPONENT_POPUP_ID);
    }

    private void DrawAddComponentPopup()
    {
        if (ImGui.BeginPopup(ADD_COMPONENT_POPUP_ID))
        {
            var availableComponentTypes = EditorDataBase.GetAllComponentTypes();

            foreach (var componentType in availableComponentTypes)
                if (DrawComponentOption(componentType))
                    break;

            ImGui.EndPopup();
        }
    }

    private bool DrawComponentOption(Type componentType)
    {
        var componentTypeName = componentType.Name;

        if (ImGui.Selectable(componentTypeName))
        {
            AddComponentToSelectedObject(componentType);
            CloseAddComponentPopup();
            return true;
        }

        return false;
    }

    private void AddComponentToSelectedObject(Type componentType)
    {
        _selectedGameObject!.AddComponent(componentType);
        UpdateInspector(_selectedGameObject);
    }

    private void CloseAddComponentPopup()
    {
        _showAddComponentPopup = false;
        ImGui.CloseCurrentPopup();
    }

    #endregion
}
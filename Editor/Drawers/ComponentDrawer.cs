using System.Numerics;
using System.Reflection;
using Engine.Components;
using Engine.Database.Implementations;
using Engine.Rendering;
using Engine.Shaders;
using Engine.Systems;
using ImGuiNET;

namespace Editor.Drawers;

/// <summary>
/// Draws component properties in the inspector using ImGui.
/// Handles various types including primitives, vectors, colors, textures, and nested objects.
/// </summary>
internal sealed class ComponentDrawer
{
    #region Constants

    private const int MAX_RECURSION_DEPTH = 3;
    private const float DRAG_SPEED = 0.01f;
    private const BindingFlags PUBLIC_INSTANCE_MEMBERS = BindingFlags.Public | BindingFlags.Instance;

    #endregion

    #region Fields

    private readonly HashSet<object> _visitedObjects = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// Draws all properties of the given component in the inspector.
    /// </summary>
    public void Draw(Component component)
    {
        if (component == null)
            return;

        _visitedObjects.Clear();
        DrawObjectProperties(component, 0);
    }

    #endregion

    #region Core Drawing Logic

    private void DrawObjectProperties(object obj, int currentDepth)
    {
        if (!ShouldDrawObject(obj, currentDepth))
            return;

        _visitedObjects.Add(obj);

        var objectType = obj.GetType();

        if (IsPrimitiveOrBuiltInType(objectType))
        {
            ImGui.Text(obj.ToString());
            return;
        }

        DrawFields(obj, objectType, currentDepth);
        DrawProperties(obj, objectType, currentDepth);
    }

    private bool ShouldDrawObject(object obj, int currentDepth)
    {
        if (obj == null)
            return false;

        if (currentDepth > MAX_RECURSION_DEPTH)
            return false;

        return !_visitedObjects.Contains(obj);
    }

    private void DrawFields(object obj, Type objectType, int currentDepth)
    {
        var fields = objectType.GetFields(PUBLIC_INSTANCE_MEMBERS);

        foreach (var field in fields)
        {
            if (ShouldSkipMember(field, field.FieldType))
                continue;

            DrawMember(
                field.Name,
                field.FieldType,
                () => field.GetValue(obj),
                val => field.SetValue(obj, val),
                currentDepth
            );
        }
    }

    private void DrawProperties(object obj, Type objectType, int currentDepth)
    {
        var properties = objectType.GetProperties(PUBLIC_INSTANCE_MEMBERS);

        foreach (var property in properties)
        {
            if (!property.CanRead)
                continue;

            if (ShouldSkipMember(property, property.PropertyType))
                continue;

            DrawMember(
                property.Name,
                property.PropertyType,
                () => property.GetValue(obj),
                val =>
                {
                    if (property.CanWrite)
                        property.SetValue(obj, val);
                },
                currentDepth
            );
        }
    }

    private bool ShouldSkipMember(MemberInfo member, Type memberType)
    {
        // Skip Guid types
        if (memberType == typeof(Guid))
            return true;

        // Skip members with JsonIgnore attribute
        if (HasJsonIgnoreAttribute(member))
            return true;

        return false;
    }

    #endregion

    #region Member Drawing Router

    private void DrawMember(
        string memberName,
        Type memberType,
        Func<object> getValue,
        Action<object> setValue,
        int currentDepth)
    {
        var value = getValue();

        // Route to appropriate draw method based on type
        if (memberType.IsEnum)
            DrawEnum(memberName, memberType, value, setValue);
        else if (memberType == typeof(bool))
            DrawBool(memberName, value, setValue);
        else if (memberType == typeof(int))
            DrawInt(memberName, value, setValue);
        else if (memberType == typeof(float))
            DrawFloat(memberName, value, setValue);
        else if (memberType == typeof(double))
            DrawDouble(memberName, value, setValue);
        else if (memberType == typeof(string))
            DrawString(memberName, value, setValue);
        else if (memberType == typeof(Vector2))
            DrawVector2(memberName, value, setValue);
        else if (memberType == typeof(Vector3))
            DrawVector3(memberName, value, setValue);
        else if (memberType == typeof(Vector4))
            DrawVector4Numerics(memberName, value, setValue);
        else if (memberType == typeof(OpenTK.Mathematics.Vector4))
            DrawVector4OpenTK(memberName, value, setValue);
        else if (memberType == typeof(OpenTK.Mathematics.Color4))
            DrawColor4OpenTK(memberName, value, setValue);
        else if (memberType == typeof(Texture))
            DrawTexture(memberName, value, setValue);
        else if (memberType == typeof(ShaderSelector))
            DrawShaderSelector(memberName, value);
        else if (memberType.IsArray || IsGenericList(memberType))
            DrawCollection(memberName, value, currentDepth);
        else if (memberType.IsClass && !IsPrimitiveOrBuiltInType(memberType))
            DrawNestedObject(memberName, memberType, value, currentDepth);
        else
            DrawUnknownType(memberName, value);
    }

    #endregion

    #region Primitive Type Drawing

    private void DrawEnum(string name, Type enumType, object value, Action<object> setValue)
    {
        var enumNames = Enum.GetNames(enumType);
        var currentIndex = value != null ? (int)value : 0;

        ImGui.Text($"{name}: ");
        ImGui.SameLine();

        if (!ImGui.Combo($"##{name}", ref currentIndex, enumNames, enumNames.Length)) return;

        var enumValue = Enum.ToObject(enumType, currentIndex);
        setValue(enumValue);
    }

    private void DrawBool(string name, object value, Action<object> setValue)
    {
        var boolValue = value != null && (bool)value;

        if (ImGui.Checkbox(name, ref boolValue)) setValue(boolValue);
    }

    private void DrawInt(string name, object value, Action<object> setValue)
    {
        var intValue = value != null ? (int)value : 0;

        if (ImGui.DragInt(name, ref intValue)) setValue(intValue);
    }

    private void DrawFloat(string name, object value, Action<object> setValue)
    {
        var floatValue = value != null ? (float)value : 0f;

        if (ImGui.DragFloat(name, ref floatValue, DRAG_SPEED)) setValue(floatValue);
    }

    private void DrawDouble(string name, object value, Action<object> setValue)
    {
        var doubleValue = value != null ? (double)value : 0.0;
        var floatValue = (float)doubleValue;

        if (ImGui.DragFloat(name, ref floatValue, DRAG_SPEED)) setValue((double)floatValue);
    }

    private void DrawString(string name, object value, Action<object> setValue)
    {
        var stringValue = value as string ?? "";
        var buffer = new byte[256];
        System.Text.Encoding.UTF8.GetBytes(stringValue, 0, Math.Min(stringValue.Length, 255), buffer, 0);

        ImGui.Text($"{name}: ");
        ImGui.SameLine();

        if (!ImGui.InputText($"##{name}", buffer, (uint)buffer.Length)) return;

        var nullIndex = Array.IndexOf(buffer, (byte)0);
        var newString = System.Text.Encoding.UTF8.GetString(buffer, 0, nullIndex >= 0 ? nullIndex : buffer.Length);
        setValue(newString);
    }

    #endregion

    #region Vector Type Drawing

    private void DrawVector2(string name, object value, Action<object> setValue)
    {
        var vector = value != null ? (Vector2)value : Vector2.Zero;
        var vectorCopy = new Vector2(vector.X, vector.Y);

        if (ImGui.DragFloat2(name, ref vectorCopy, DRAG_SPEED)) setValue(vectorCopy);
    }

    private void DrawVector3(string name, object value, Action<object> setValue)
    {
        var vector = value != null ? (Vector3)value : Vector3.Zero;
        var vectorCopy = new Vector3(vector.X, vector.Y, vector.Z);

        if (ImGui.DragFloat3(name, ref vectorCopy, DRAG_SPEED)) setValue(vectorCopy);
    }

    private void DrawVector4Numerics(string name, object value, Action<object> setValue)
    {
        var vector = value != null
            ? (Vector4)value
            : Vector4.Zero;

        var color = new Vector4(vector.X, vector.Y, vector.Z, vector.W);

        if (ImGui.ColorEdit4(name, ref color)) setValue(new Vector4(color.X, color.Y, color.Z, color.W));
    }

    private void DrawVector4OpenTK(string name, object value, Action<object> setValue)
    {
        var vector = value != null
            ? (OpenTK.Mathematics.Vector4)value
            : OpenTK.Mathematics.Vector4.Zero;

        // Convert OpenTK Vector4 to System.Numerics Vector4 for ImGui
        var color = new Vector4(vector.X, vector.Y, vector.Z, vector.W);

        if (!ImGui.ColorEdit4(name, ref color)) return;
        // Convert back to OpenTK Vector4
        var newVector = new OpenTK.Mathematics.Vector4(color.X, color.Y, color.Z, color.W);
        setValue(newVector);
    }

    private void DrawColor4OpenTK(string name, object value, Action<object> setValue)
    {
        var color = value != null
            ? (OpenTK.Mathematics.Color4)value
            : OpenTK.Mathematics.Color4.White;

        // Convert OpenTK Color4 to System.Numerics Vector4 for ImGui
        var colorVector = new Vector4(color.R, color.G, color.B, color.A);

        if (!ImGui.ColorEdit4(name, ref colorVector)) return;
        // Convert back to OpenTK Color4
        var newColor = new OpenTK.Mathematics.Color4(
            colorVector.X,
            colorVector.Y,
            colorVector.Z,
            colorVector.W
        );
        setValue(newColor);
    }

    #endregion

    #region Engine-Specific Type Drawing

    private void DrawShaderSelector(string name, object value)
    {
        var shaderSelector = value as ShaderSelector;
        if (shaderSelector == null)
            return;

        var activeShaderName = shaderSelector.GetCurrentShaderName();
        var availableShaderNames = shaderSelector.GetAvailableShaderNames();
        var currentIndex = availableShaderNames.ToList().IndexOf(activeShaderName);

        ImGui.Text($"{name}: ");
        ImGui.SameLine();

        if (!ImGui.Combo($"##{name}", ref currentIndex, availableShaderNames.ToArray(),
                availableShaderNames.Count)) return;

        var selectedShaderName = availableShaderNames[currentIndex];
        shaderSelector.BindShader(selectedShaderName);
    }

    private void DrawTexture(string name, object value, Action<object> setValue)
    {
        var textureDatabase = SystemManager.GetSystem<DatabaseSystem>()
            .GetDatabase<TextureDataBase>();

        var allTextureNames = textureDatabase.TextureNames.ToArray();
        var currentIndex = FindTextureIndex(value, allTextureNames);


        ImGui.Text($"{name}: ");
        ImGui.SameLine();

        if (!ImGui.Combo($"##{name}", ref currentIndex, allTextureNames, allTextureNames.Length)) return;

        if (currentIndex >= 0)
        {
            var selectedTextureName = allTextureNames[currentIndex];
            var texture = textureDatabase.LoadTexture(selectedTextureName);

            var textureValue = value as Texture;
            textureValue?.Dispose();

            setValue(texture);
        }
        else
        {
            setValue(null);
        }
    }

    private int FindTextureIndex(object value, string[] textureNames)
    {
        var currentTextureName = (value as Texture)?.Name ?? "";

        for (var i = 0; i < textureNames.Length; i++)
            if (textureNames[i] == currentTextureName)
                return i;

        return -1;
    }

    #endregion

    #region Complex Type Drawing

    private void DrawNestedObject(string name, Type objectType, object value, int currentDepth)
    {
        if (value != null && ImGui.TreeNode($"{name} ({objectType.Name})"))
        {
            DrawObjectProperties(value, currentDepth + 1);
            ImGui.TreePop();
        }
        else if (value == null)
        {
            ImGui.Text($"{name}: null");
        }
    }

    private void DrawCollection(string name, object value, int currentDepth)
    {
        if (value == null)
        {
            ImGui.Text($"{name}: null");
            return;
        }

        if (value is not System.Collections.IEnumerable enumerable)
            return;

        var itemList = enumerable.Cast<object>().ToList();

        if (ImGui.TreeNode($"{name} [{itemList.Count}]"))
        {
            DrawCollectionItems(itemList, currentDepth);
            ImGui.TreePop();
        }
    }

    private void DrawCollectionItems(List<object> items, int currentDepth)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            if (item == null)
            {
                ImGui.Text($"[{index}]: null");
                continue;
            }

            if (IsPrimitiveOrBuiltInType(item.GetType()))
            {
                ImGui.Text($"[{index}]: {item}");
            }
            else
            {
                if (ImGui.TreeNode($"[{index}] ({item.GetType().Name})"))
                {
                    DrawObjectProperties(item, currentDepth + 1);
                    ImGui.TreePop();
                }
            }
        }
    }

    private void DrawUnknownType(string name, object value)
    {
        ImGui.Text($"{name}: {value?.ToString() ?? "null"}");
    }

    #endregion

    #region Helper Methods

    private bool HasJsonIgnoreAttribute(MemberInfo member)
    {
        // Check for System.Text.Json JsonIgnore attribute
        if (member.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null)
            return true;

        // Check for Newtonsoft.Json JsonIgnore attribute (if used)
        var hasNewtonsoftIgnore = member.GetCustomAttributes(false)
            .Any(attr => attr.GetType().Name == "JsonIgnoreAttribute");

        return hasNewtonsoftIgnore;
    }

    private bool IsPrimitiveOrBuiltInType(Type type)
    {
        return type.IsPrimitive
               || type == typeof(string)
               || type == typeof(decimal)
               || type.IsEnum;
    }

    private bool IsGenericList(Type type)
    {
        return type.IsGenericType
               && type.GetGenericTypeDefinition() == typeof(List<>);
    }

    #endregion
}
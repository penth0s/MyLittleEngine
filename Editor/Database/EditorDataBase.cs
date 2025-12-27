using System.Reflection;
using Engine.Components;

namespace Editor.Database;

internal static class EditorDataBase
{
    public static IEnumerable<Type> GetAllComponentTypes()
    {
        var componentBaseType = typeof(Component);

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(asm =>
            {
                try
                {
                    return asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.Where(t => t != null)!;
                }
            })
            .Where(t =>
                t != null &&
                componentBaseType.IsAssignableFrom(t) &&
                t != componentBaseType &&
                !t.IsAbstract &&
                !t.IsInterface);
    }
}
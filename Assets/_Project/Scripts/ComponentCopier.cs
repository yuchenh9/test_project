using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;

public static class ComponentCopier
{
    public static IEnumerable<Component> AddAllComponentCopy(this GameObject target, GameObject source, IEnumerable<string> exceptFields = null)
    {
        var sourceComponents = source.GetComponents<Component>();

        var addComponentCopyMethod = typeof(ComponentCopier)
            .GetMethod(nameof(AddComponentCopy), BindingFlags.Public | BindingFlags.Static);
        if (addComponentCopyMethod == null)
        {
            Debug.LogError("AddComponentCopy method not found.");
            return null;
        }

        var copiedComponents = new List<Component>();

        foreach (var sourceComponent in sourceComponents)
        {
            var type = sourceComponent.GetType();
            var genericMethod = addComponentCopyMethod.MakeGenericMethod(type);
            var copiedComponent = genericMethod.Invoke(null, new object[] { target, source, exceptFields }) as Component;

            if (copiedComponent != null)
            {
                copiedComponents.Add(copiedComponent);
            }
        }

        return copiedComponents;
    }

    public static T AddComponentCopy<T>(this GameObject target, GameObject source, IEnumerable<string> exceptFields = null) where T : Component
    {
        if (!source.TryGetComponent<T>(out var sourceComponent))
        {
            Debug.LogError($"Source object {source.name} does not have component of type {typeof(T)}.");
            return null;
        }

        if (!target.TryGetComponent<T>(out var targetComponent))
        {
            targetComponent = target.AddComponent<T>();
        }

        CopyComponentFields(sourceComponent, targetComponent, exceptFields);

        return targetComponent;
    }
    
    private static void CopyComponentFields<T>(T sourceComponent, T targetComponent, IEnumerable<string> exceptFields = null) where T : Component
    {
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        var excludeFieldsSet = exceptFields != null ? new HashSet<string>(exceptFields) : null;

        foreach (var field in fields)
        {
            if (excludeFieldsSet != null && excludeFieldsSet.Contains(field.Name))
            {
                continue;
            }
            
            if (field.GetCustomAttribute<HideInInspector>() == null && 
                (field.IsPublic || !field.IsPublic && field.GetCustomAttribute<SerializeField>() != null))
            {
                try
                {
                    var value = field.GetValue(sourceComponent);
                    field.SetValue(targetComponent, value);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to copy field {field.Name} from {typeof(T)}: {ex.Message}");
                }
            }
        }
    }
}

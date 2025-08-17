using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using Obi;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class ComponentPreset
{
    public string presetName;
    public List<ComponentInfo> components = new List<ComponentInfo>();
}

[System.Serializable]
public class ComponentInfo
{
    public string componentTypeName;
    public List<FieldAssignment> fieldAssignments = new List<FieldAssignment>();
}

[System.Serializable]
public class FieldAssignment
{
    public string fieldName;
    public AssignmentType assignmentType;
    public string targetComponentType; // For component references
    public int componentIndex; // Which instance if multiple of same type
    public string stringValue; // For string values
    public float floatValue; // For float values
    public bool boolValue; // For bool values
}

public enum AssignmentType
{
    Self, // Assign this GameObject
    ComponentOnSelf, // Assign a component on this GameObject
    ObjectMaterial, // Assign the first material from the object's renderer
    StringValue,
    FloatValue,
    BoolValue,
    Null
}

public class ComponentPresetManager : MonoBehaviour
{
    [Header("Preset Selection")]
    [Tooltip("Select which preset to apply")]
    public string selectedPreset = "obiSoftBody";
    
    [Tooltip("Toggle this to apply the selected preset")]
    public bool applyPreset = false;
    private bool lastApplyState = false;

    [Header("Component Management")]
    [Tooltip("Toggle this to clear all components (except Transform)")]
    public bool clearAllComponents = false;
    private bool lastClearState = false;

    [Header("Preset Definitions")]
    public List<ComponentPreset> presets = new List<ComponentPreset>();

    [Header("Options")]
    [Tooltip("Remove existing components of the same type before adding")]
    public bool replaceExisting = false;
    
    [Tooltip("Show debug information")]
    public bool showDebugInfo = true;

    [Header("Debug Info")]
    [SerializeField, TextArea(5, 10)]
    private string debugInfo = "";

    void Start()
    {
        lastApplyState = applyPreset;
        lastClearState = clearAllComponents;
        InitializeDefaultPresets();
    }

    void Awake()
    {
        InitializeDefaultPresets();
    }

    void Update()
    {
        // Detect preset apply toggle change
        if (applyPreset != lastApplyState)
        {
            lastApplyState = applyPreset;
            if (applyPreset)
            {
                ApplySelectedPreset();
                applyPreset = false; // Reset toggle
            }
        }

        // Detect clear components toggle change at runtime only
        if (Application.isPlaying && clearAllComponents != lastClearState)
        {
            lastClearState = clearAllComponents;
            if (clearAllComponents)
            {
                ClearAllComponents();
                clearAllComponents = false; // Reset toggle
            }
        }
    }

    void OnValidate()
    {
        // Ensure presets are initialized in editor mode
        InitializeDefaultPresets();
        
        // Handle preset application in editor mode
        if (!Application.isPlaying && applyPreset != lastApplyState)
        {
            lastApplyState = applyPreset;
            if (applyPreset)
            {
                ApplySelectedPreset();
                applyPreset = false; // Reset toggle
            }
        }

        // Do not clear components from OnValidate. Use the context menu or runtime toggle instead.
    }

    void InitializeDefaultPresets()
    {
        // Only add default presets if none exist or if the obiSoftBody preset is missing
        if (presets.Count == 0 || FindPreset("obiSoftBody") == null)
        {
            CreateObiSoftBodyPreset();
        }
    }

    void CreateObiSoftBodyPreset()
    {
        // Remove existing preset if it exists to avoid duplicates
        ComponentPreset existingPreset = FindPreset("obiSoftBody");
        if (existingPreset != null)
        {
            presets.Remove(existingPreset);
        }

        ComponentPreset obiPreset = new ComponentPreset();
        obiPreset.presetName = "obiSoftBody";

        // 1. ObiSoftbody component (from Obi namespace)
        ComponentInfo obiSoftbodyInfo = new ComponentInfo();
        obiSoftbodyInfo.componentTypeName = "Obi.ObiSoftbody";
        obiPreset.components.Add(obiSoftbodyInfo);

        // 2. ObiSoftbodySkinner component (from Obi namespace)
        ComponentInfo skinnerInfo = new ComponentInfo();
        skinnerInfo.componentTypeName = "Obi.ObiSoftbodySkinner";
        
        // Auto-assign the softbody field to the ObiSoftbody component
        FieldAssignment softbodyAssignment = new FieldAssignment();
        softbodyAssignment.fieldName = "softbody";
        softbodyAssignment.assignmentType = AssignmentType.ComponentOnSelf;
        softbodyAssignment.targetComponentType = "Obi.ObiSoftbody";
        softbodyAssignment.componentIndex = 0;
        skinnerInfo.fieldAssignments.Add(softbodyAssignment);
        
        obiPreset.components.Add(skinnerInfo);

        // 3. MeshTarget component (from DynamicMeshCutter namespace)
        ComponentInfo meshTargetInfo = new ComponentInfo();
        meshTargetInfo.componentTypeName = "DynamicMeshCutter.MeshTarget";
        
        // Auto-assign the GameobjectRoot field to this GameObject (note: correct field name is GameobjectRoot, not gameObjectRoot)
        FieldAssignment rootAssignment = new FieldAssignment();
        rootAssignment.fieldName = "GameobjectRoot";
        rootAssignment.assignmentType = AssignmentType.Self;
        meshTargetInfo.fieldAssignments.Add(rootAssignment);
        
        // Auto-assign the OverrideFaceMaterial field to the object's material
        FieldAssignment materialAssignment = new FieldAssignment();
        materialAssignment.fieldName = "FaceMaterial";
        materialAssignment.assignmentType = AssignmentType.ObjectMaterial;
        meshTargetInfo.fieldAssignments.Add(materialAssignment);
        
        obiPreset.components.Add(meshTargetInfo);

        presets.Add(obiPreset);
        LogInfo("Created default 'obiSoftBody' preset with correct component names and namespaces");
    }

    [ContextMenu("Apply Selected Preset")]
    public void ApplySelectedPreset()
    {
        ComponentPreset preset = FindPreset(selectedPreset);
        if (preset == null)
        {
            LogError($"Preset '{selectedPreset}' not found!");
            return;
        }

        ApplyPreset(preset);
    }

    [ContextMenu("Force Replace Components")]
    public void ForceReplaceComponents()
    {
        ComponentPreset preset = FindPreset(selectedPreset);
        if (preset == null)
        {
            LogError($"Preset '{selectedPreset}' not found!");
            return;
        }

        // Temporarily enable replace mode
        bool originalReplace = replaceExisting;
        replaceExisting = true;
        
        ApplyPresetWithReplace(preset);
        
        // Restore original setting
        replaceExisting = originalReplace;
    }

    [ContextMenu("Clear All Components")]
    public void ClearAllComponents()
    {
        LogInfo($"Clearing all components from '{gameObject.name}' (except Transform)");

        // Get all components except Transform and this script
        Component[] allComponents = GetComponents<Component>();
        
        List<string> removedComponents = new List<string>();
        List<string> skippedComponents = new List<string>();
        List<string> failedComponents = new List<string>();

        foreach (Component comp in allComponents)
        {
            if (comp == null) continue;

            string componentType = comp.GetType().Name;
            
            // Never remove Transform, RectTransform, or this ComponentPresetManager
            if (comp is Transform || comp is RectTransform || comp == this)
            {
                skippedComponents.Add($"{componentType} (protected)");
                continue;
            }

            try
            {
                LogInfo($"Removing component: {componentType}");
                
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Use Undo system in editor for safety
                    UnityEditor.Undo.DestroyObjectImmediate(comp);
                }
                else
                #endif
                {
                    Destroy(comp);
                }
                
                removedComponents.Add(componentType);
            }
            catch (System.Exception e)
            {
                failedComponents.Add($"{componentType} ({e.Message})");
                LogError($"Failed to remove {componentType}: {e.Message}");
            }
        }

        // Update debug info
        string summary = $"COMPONENT CLEARING SUMMARY:\n" +
                        $"Target: {gameObject.name}\n" +
                        $"Total components found: {allComponents.Length}\n" +
                        $"Successfully removed: {removedComponents.Count}\n" +
                        $"Skipped (protected): {skippedComponents.Count}\n" +
                        $"Failed: {failedComponents.Count}\n\n";

        if (removedComponents.Count > 0)
        {
            summary += "REMOVED COMPONENTS:\n";
            foreach (string comp in removedComponents)
                summary += $"✓ {comp}\n";
            summary += "\n";
        }

        if (skippedComponents.Count > 0)
        {
            summary += "SKIPPED COMPONENTS:\n";
            foreach (string comp in skippedComponents)
                summary += $"- {comp}\n";
            summary += "\n";
        }

        if (failedComponents.Count > 0)
        {
            summary += "FAILED COMPONENTS:\n";
            foreach (string comp in failedComponents)
                summary += $"✗ {comp}\n";
        }

        debugInfo = summary;
        LogInfo($"Component clearing completed! Removed {removedComponents.Count} components");
    }

    public void ApplyPresetWithReplace(ComponentPreset preset)
    {
        LogInfo($"Applying preset '{preset.presetName}' with component replacement to '{gameObject.name}'");

        List<string> addedComponents = new List<string>();
        List<string> failedComponents = new List<string>();
        List<Component> newComponents = new List<Component>();

        // First pass: Remove existing components if replace is enabled
        foreach (ComponentInfo compInfo in preset.components)
        {
            try
            {
                Type componentType = FindComponentType(compInfo.componentTypeName);
                if (componentType == null) continue;

                Component existingComp = GetComponent(componentType);
                if (existingComp != null)
                {
                    LogInfo($"Removing existing {compInfo.componentTypeName}");
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                        UnityEditor.Undo.DestroyObjectImmediate(existingComp);
                    else
                    #endif
                        Destroy(existingComp);
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to remove existing {compInfo.componentTypeName}: {e.Message}");
            }
        }

        // Second pass: Add new components
        foreach (ComponentInfo compInfo in preset.components)
        {
            try
            {
                Type componentType = FindComponentType(compInfo.componentTypeName);
                if (componentType == null)
                {
                    failedComponents.Add($"{compInfo.componentTypeName} (type not found)");
                    continue;
                }

                // Add the component
                Component newComp = gameObject.AddComponent(componentType);
                newComponents.Add(newComp);
                addedComponents.Add(compInfo.componentTypeName);
                LogInfo($"Added component: {compInfo.componentTypeName}");
            }
            catch (Exception e)
            {
                failedComponents.Add($"{compInfo.componentTypeName} ({e.Message})");
                LogError($"Failed to add {compInfo.componentTypeName}: {e.Message}");
            }
        }

        // Third pass: Assign fields
        for (int i = 0; i < preset.components.Count && i < newComponents.Count; i++)
        {
            ComponentInfo compInfo = preset.components[i];
            Component component = newComponents[i];
            
            if (component == null) continue;

            ApplyFieldAssignments(component, compInfo.fieldAssignments);
        }

        // Update debug info
        string summary = $"PRESET REPLACEMENT SUMMARY:\n" +
                        $"Preset: {preset.presetName}\n" +
                        $"Target: {gameObject.name}\n" +
                        $"Components replaced: {preset.components.Count}\n" +
                        $"Successfully added: {addedComponents.Count}\n" +
                        $"Failed: {failedComponents.Count}\n\n";

        if (addedComponents.Count > 0)
        {
            summary += "REPLACED COMPONENTS:\n";
            foreach (string comp in addedComponents)
                summary += $"✓ {comp}\n";
            summary += "\n";
        }

        if (failedComponents.Count > 0)
        {
            summary += "FAILED COMPONENTS:\n";
            foreach (string comp in failedComponents)
                summary += $"✗ {comp}\n";
        }

        debugInfo = summary;
        LogInfo($"Preset replacement completed! Replaced {addedComponents.Count} components");
    }

    public void ApplyPreset(ComponentPreset preset)
    {
        LogInfo($"Applying preset '{preset.presetName}' to '{gameObject.name}'");

        List<string> addedComponents = new List<string>();
        List<string> failedComponents = new List<string>();
        List<Component> newComponents = new List<Component>();

        // First pass: Add all components
        foreach (ComponentInfo compInfo in preset.components)
        {
            try
            {
                Type componentType = FindComponentType(compInfo.componentTypeName);
                if (componentType == null)
                {
                    failedComponents.Add($"{compInfo.componentTypeName} (type not found)");
                    continue;
                }

                // Check if component already exists
                Component existingComp = GetComponent(componentType);
                if (existingComp != null)
                {
                    LogInfo($"Component {compInfo.componentTypeName} already exists, using existing component");
                    newComponents.Add(existingComp);
                    continue;
                }

                // Add the component
                Component newComp = gameObject.AddComponent(componentType);
                newComponents.Add(newComp);
                addedComponents.Add(compInfo.componentTypeName);
                LogInfo($"Added component: {compInfo.componentTypeName}");
            }
            catch (Exception e)
            {
                failedComponents.Add($"{compInfo.componentTypeName} ({e.Message})");
                LogError($"Failed to add {compInfo.componentTypeName}: {e.Message}");
            }
        }

        // Second pass: Assign fields
        for (int i = 0; i < preset.components.Count && i < newComponents.Count; i++)
        {
            ComponentInfo compInfo = preset.components[i];
            Component component = newComponents[i];
            
            if (component == null) continue;

            ApplyFieldAssignments(component, compInfo.fieldAssignments);
        }

        // Update debug info
        string summary = $"PRESET APPLICATION SUMMARY:\n" +
                        $"Preset: {preset.presetName}\n" +
                        $"Target: {gameObject.name}\n" +
                        $"Components to add: {preset.components.Count}\n" +
                        $"Successfully added: {addedComponents.Count}\n" +
                        $"Failed: {failedComponents.Count}\n\n";

        if (addedComponents.Count > 0)
        {
            summary += "ADDED COMPONENTS:\n";
            foreach (string comp in addedComponents)
                summary += $"✓ {comp}\n";
            summary += "\n";
        }

        if (failedComponents.Count > 0)
        {
            summary += "FAILED COMPONENTS:\n";
            foreach (string comp in failedComponents)
                summary += $"✗ {comp}\n";
        }

        debugInfo = summary;
        LogInfo($"Preset application completed! Added {addedComponents.Count} components");
    }

    void ApplyFieldAssignments(Component component, List<FieldAssignment> assignments)
    {
        Type componentType = component.GetType();

        foreach (FieldAssignment assignment in assignments)
        {
            try
            {
                FieldInfo field = componentType.GetField(assignment.fieldName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (field == null)
                {
                    LogWarning($"Field '{assignment.fieldName}' not found on {componentType.Name}");
                    continue;
                }

                object value = GetAssignmentValue(assignment);
                if (value != null || assignment.assignmentType == AssignmentType.Null)
                {
                    field.SetValue(component, value);
                    LogInfo($"Set {componentType.Name}.{assignment.fieldName} = {value}");
                }
                else
                {
                    LogWarning($"Could not resolve value for {componentType.Name}.{assignment.fieldName}");
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to set field {assignment.fieldName}: {e.Message}");
            }
        }
    }

    object GetAssignmentValue(FieldAssignment assignment)
    {
        switch (assignment.assignmentType)
        {
            case AssignmentType.Self:
                return gameObject;

            case AssignmentType.ComponentOnSelf:
                Type targetType = FindComponentType(assignment.targetComponentType);
                if (targetType == null) return null;
                
                Component[] components = GetComponents(targetType);
                if (components.Length > assignment.componentIndex)
                    return components[assignment.componentIndex];
                return null;

            case AssignmentType.ObjectMaterial:
                return GetObjectMaterial();

            case AssignmentType.StringValue:
                return assignment.stringValue;

            case AssignmentType.FloatValue:
                return assignment.floatValue;

            case AssignmentType.BoolValue:
                return assignment.boolValue;

            case AssignmentType.Null:
                return null;

            default:
                return null;
        }
    }

    Material GetObjectMaterial()
    {
        // Try to get material from various renderer components
        
        // 1. Try MeshRenderer first
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            LogInfo($"Found material from MeshRenderer: {meshRenderer.sharedMaterial.name}");
            return meshRenderer.sharedMaterial;
        }

        // 2. Try SkinnedMeshRenderer
        SkinnedMeshRenderer skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMaterial != null)
        {
            LogInfo($"Found material from SkinnedMeshRenderer: {skinnedMeshRenderer.sharedMaterial.name}");
            return skinnedMeshRenderer.sharedMaterial;
        }

        // 3. Try any Renderer component
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            LogInfo($"Found material from Renderer: {renderer.sharedMaterial.name}");
            return renderer.sharedMaterial;
        }

        LogWarning("No material found on this GameObject - no renderer components with materials");
        return null;
    }

    Type FindComponentType(string typeName)
    {
        // Try common namespaces
        string[] namespaces = { "", "UnityEngine.", "Obi.", "System." };
        
        foreach (string ns in namespaces)
        {
            string fullName = ns + typeName;
            Type type = Type.GetType(fullName);
            if (type != null) return type;
            
            // Also try searching in all loaded assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null) return type;
            }
        }

        return null;
    }

    ComponentPreset FindPreset(string presetName)
    {
        return presets.Find(p => p.presetName == presetName);
    }

    [ContextMenu("List Available Presets")]
    public void ListAvailablePresets()
    {
        string list = "AVAILABLE PRESETS:\n";
        for (int i = 0; i < presets.Count; i++)
        {
            ComponentPreset preset = presets[i];
            list += $"{i + 1}. {preset.presetName} ({preset.components.Count} components)\n";
            foreach (ComponentInfo comp in preset.components)
            {
                list += $"   - {comp.componentTypeName}\n";
            }
        }
        
        debugInfo = list;
        LogInfo(list);
    }

    [ContextMenu("Create Custom Preset")]
    public void CreateCustomPreset()
    {
        // This creates a template for custom presets
        ComponentPreset customPreset = new ComponentPreset();
        customPreset.presetName = "CustomPreset";
        
        ComponentInfo exampleComponent = new ComponentInfo();
        exampleComponent.componentTypeName = "ExampleComponent";
        
        FieldAssignment exampleAssignment = new FieldAssignment();
        exampleAssignment.fieldName = "exampleField";
        exampleAssignment.assignmentType = AssignmentType.Self;
        exampleComponent.fieldAssignments.Add(exampleAssignment);
        
        customPreset.components.Add(exampleComponent);
        presets.Add(customPreset);
        
        LogInfo("Created custom preset template");
    }

    private void LogInfo(string message)
    {
        if (showDebugInfo)
            Debug.Log($"[ComponentPresetManager] {message}");
    }

    private void LogWarning(string message)
    {
        if (showDebugInfo)
            Debug.LogWarning($"[ComponentPresetManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ComponentPresetManager] {message}");
    }
}

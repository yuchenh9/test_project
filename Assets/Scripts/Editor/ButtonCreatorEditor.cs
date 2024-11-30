using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.Events;
using System.Reflection;
namespace DynamicMeshCutter
{
public class ButtonCreatorEditor : EditorWindow
{
    private GameObject parentObject;
    private string buttonName = "New Button";
    private Vector2 buttonSize = new Vector2(160, 30);
    private string buttonText = "Click Me";
    private GameObject targetObject;
    //private string method;
    [MenuItem("Tools/Button Creator")]
    public static void ShowWindow()
    {
        GetWindow<ButtonCreatorEditor>("Button Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Button Settings", EditorStyles.boldLabel);

        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);
        buttonName = EditorGUILayout.TextField("Button Name", buttonName);
        buttonSize = EditorGUILayout.Vector2Field("Button Size", buttonSize);
        buttonText = EditorGUILayout.TextField("Button Text", buttonText);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
        //method = EditorGUILayout.TextField("Method Text", method);
        if (GUILayout.Button("Create Button"))
        {
            CreateButton();
        }
    }
    public void SetupOnClick(Button button, GameObject targetObject, string buttonName, string method)
    {

        // Add a new listener with a string parameter
        button.onClick.AddListener(() => 
        {
            targetObject.SendMessage(method, buttonName, SendMessageOptions.DontRequireReceiver);
        });
    }
    void CreateButton()
    {
        if (parentObject == null)
        {
            Debug.LogError("Please assign a parent object.");
            return;
        }

        // Create a new button GameObject
        GameObject buttonObject = new GameObject(buttonName);
        buttonObject.transform.SetParent(parentObject.transform, false);

        // Add a RectTransform component
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = buttonSize;

        // Add a CanvasRenderer component
        buttonObject.AddComponent<CanvasRenderer>();

        // Add an Image component
        Image image = buttonObject.AddComponent<Image>();
        image.color = Color.white; // Default button color

        // Add a Button component
        Button button = buttonObject.AddComponent<Button>();

        // Add a child text GameObject
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);

        // Add a RectTransform component to the text object
        RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
        textRectTransform.sizeDelta = buttonSize;

        // Add a Text component
        Text text = textObject.AddComponent<Text>();
        text.text = buttonText;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.color = Color.black;

        // Set default button target graphic to the image component
        button.targetGraphic = image;
        //SetupOnClick(button,targetObject,buttonName,method);
        AddOnClickListener(button, targetObject.GetComponent<UnifiedButtonHandler>(), "HandleButtonClick", buttonName);

        // Select the newly created button in the editor
        Selection.activeGameObject = buttonObject;
    }
    void AddOnClickListener(Button button, MonoBehaviour target, string methodName, string argument)
{
    // Create a SerializedObject from the button's onClick event
    SerializedObject serializedObject = new SerializedObject(button);
    SerializedProperty onClickProperty = serializedObject.FindProperty("m_OnClick");

    // Find the target method using reflection
    MethodInfo methodInfo = target.GetType().GetMethod(methodName);

    if (methodInfo == null)
    {
        Debug.LogError($"Method {methodName} not found on target {target.name}.");
        return;
    }

    // Create a PersistentCall instance
    var persistentCallGroupType = typeof(UnityEventBase).Assembly.GetType("UnityEngine.Events.PersistentCallGroup");
    var persistentCallType = typeof(UnityEventBase).Assembly.GetType("UnityEngine.Events.PersistentCall");

    // Add new persistent call
    var calls = onClickProperty.FindPropertyRelative("m_Calls.m_PersistentCalls.m_Calls");
    calls.InsertArrayElementAtIndex(calls.arraySize);
    SerializedProperty call = calls.GetArrayElementAtIndex(calls.arraySize - 1);

    // Set target, method name, and argument
    call.FindPropertyRelative("m_Target").objectReferenceValue = target;
    call.FindPropertyRelative("m_MethodName").stringValue = methodName;
    call.FindPropertyRelative("m_Mode").enumValueIndex = 1; // Call mode is "String"
    call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue = typeof(string).AssemblyQualifiedName;
    call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = argument;

    // Apply the changes to the serialized object
    serializedObject.ApplyModifiedProperties();
}

}
}
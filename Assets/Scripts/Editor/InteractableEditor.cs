#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Interactable), true)]
public class InteractableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Interactable interactable = (Interactable)target;

        // Display promptMessage
        EditorGUILayout.PropertyField(serializedObject.FindProperty("promptMessage"));

        // Display useEvents toggle
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useEvents"));

        if (interactable.useEvents)
        {
            // Show UnityEvent field when useEvents is enabled
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onInteract"));
        }

        // Draw default inspector for any subclass serialized fields
        DrawPropertiesExcluding(serializedObject, "m_Script", "promptMessage", "useEvents", "onInteract");

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

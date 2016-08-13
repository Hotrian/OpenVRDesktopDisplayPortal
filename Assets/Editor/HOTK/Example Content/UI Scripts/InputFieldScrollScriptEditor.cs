using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InputFieldScrollScript))]
public class InputFieldScrollScriptEditor : Editor
{
    private static GUIStyle _boldFoldoutStyle;

    public override void OnInspectorGUI()
    {
        if (_boldFoldoutStyle == null)
        {
            _boldFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
        }

        var field = (InputFieldScrollScript)target;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("UseInt"), new GUIContent() { text = "Use Integer Values" });
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ValMultiplier"), new GUIContent() { text = "Scroll Value Multiplier" });

        field.UseLimits = EditorGUILayout.Foldout(field.UseLimits, "Use Limits", _boldFoldoutStyle);
        if (field.UseLimits)
        {
            field.UseLowerLimit = EditorGUILayout.Foldout(field.UseLowerLimit, "Use Minimum Value", _boldFoldoutStyle);
            if (field.UseLowerLimit)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MinVal"), new GUIContent() { text = "Minimum Value" });
            }
            field.UseUpperLimit = EditorGUILayout.Foldout(field.UseUpperLimit, "Use Maximum Value", _boldFoldoutStyle);
            if (field.UseUpperLimit)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxVal"), new GUIContent() { text = "Maximum Value" });
            }
        }
        

        serializedObject.ApplyModifiedProperties();
    }
}

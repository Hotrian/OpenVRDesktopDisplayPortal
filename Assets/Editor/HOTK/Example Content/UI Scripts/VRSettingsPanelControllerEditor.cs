using UnityEngine;
using UnityEditor;

[CustomEditor(typeof (VRSettingsPanelController))]
public class VRSettingsPanelControllerEditor : Editor
{
    private static GUIStyle _boldFoldoutStyle;

    public override void OnInspectorGUI()
    {
        if (_boldFoldoutStyle == null)
        {
            _boldFoldoutStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
        }
        var editing = (VRSettingsPanelController)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("Overlay"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SettingsButton"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SettingsButtonText"));

        GUILayout.Label("");
        GUILayout.Label("Button Label Settings", _boldFoldoutStyle);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ButtonOpenText"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ButtonCloseText"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ButtonOpeningText"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ButtonClosingText"));

        GUILayout.Label("");
        GUILayout.Label("Open Settings", _boldFoldoutStyle);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("OpenTime"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("OpenPivotTime"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("OpenPivotX"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("OpenPosition"));

        GUILayout.Label("");
        GUILayout.Label("Close Settings", _boldFoldoutStyle);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ClosedTime"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ClosedPivotTime"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ClosedPivotX"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ClosedPosition"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("AimYLow"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AimXLow"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AimXHigh"));

        GUILayout.Label("");

        if (GUILayout.Button("Activate"))
        {
            editing.TogglePanel();
        }

        serializedObject.ApplyModifiedProperties();
    }
}

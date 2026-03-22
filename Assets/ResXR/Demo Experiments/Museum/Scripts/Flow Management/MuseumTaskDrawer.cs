#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Museum_Task))]
public class MuseumTaskDrawer : PropertyDrawer
{
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // When collapsed: 1 line (foldout)
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        var taskTypeProp = property.FindPropertyRelative("taskType");
        bool showDuration = (Museum_TaskType)taskTypeProp.enumValueIndex == Museum_TaskType.FreeExploration;

        // Expanded: taskType line + (optional duration line) + foldout line
        int lines = 2 + (showDuration ? 1 : 0);
        return lines * EditorGUIUtility.singleLineHeight
               + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var lineH = EditorGUIUtility.singleLineHeight;
        var spacing = EditorGUIUtility.standardVerticalSpacing;

        // Foldout
        var r = new Rect(position.x, position.y, position.width, lineH);
        property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, label, true);

        if (!property.isExpanded)
            return;

        EditorGUI.indentLevel++;

        // Task Type
        r.y += lineH + spacing;
        var taskTypeProp = property.FindPropertyRelative("taskType");
        EditorGUI.PropertyField(r, taskTypeProp);

        // Duration (only for FreeExploration)
        bool showDuration = (Museum_TaskType)taskTypeProp.enumValueIndex == Museum_TaskType.FreeExploration;
        if (showDuration)
        {
            r.y += lineH + spacing;
            var durationProp = property.FindPropertyRelative("durationInSeconds");
            EditorGUI.PropertyField(r, durationProp, new GUIContent("Duration In Seconds"));
        }

        EditorGUI.indentLevel--;
    }
}
#endif

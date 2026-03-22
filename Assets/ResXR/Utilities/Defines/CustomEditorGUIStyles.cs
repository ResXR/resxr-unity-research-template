using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class CustomEditorGUIStyles
{
    public static GUIStyle BoldMultiLineLabel = new GUIStyle()
    {
        wordWrap = true,
        fontStyle = FontStyle.Bold,
    };

    public static GUIStyle ErrorLabel = new GUIStyle()
    {
        wordWrap = true,
        normal = new GUIStyleState() { textColor = Color.red }
    };
}
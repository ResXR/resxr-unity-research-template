using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CollisionDetector))]
public class CollisionDetectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty detectSpecificCollisionProperty = serializedObject.FindProperty("_detectSpecificCollision");
        EditorGUILayout.PropertyField(detectSpecificCollisionProperty);
        if (detectSpecificCollisionProperty.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_colliderTag"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
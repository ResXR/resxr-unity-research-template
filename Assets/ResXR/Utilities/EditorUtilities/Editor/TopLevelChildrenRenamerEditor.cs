using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChildrenRenamer))]
public class TopLevelChildrenRenamerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (GUILayout.Button("Rename top level children"))
		{
			(target as ChildrenRenamer).RenameTopLevelChildren();
		}
	}
}

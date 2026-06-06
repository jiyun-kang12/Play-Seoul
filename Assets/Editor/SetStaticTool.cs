using UnityEngine;
using UnityEditor;

public class SetStaticTool : EditorWindow
{
    private bool includeInactive = true;

    [MenuItem("Tools/Map Tools/Set Static Recursively")]
    public static void Open()
    {
        GetWindow<SetStaticTool>("Set Static Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Selected Objects Static Setter", EditorStyles.boldLabel);

        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        GUILayout.Space(10);

        if (GUILayout.Button("Set Selected Hierarchy Static"))
        {
            SetSelectedStatic(true);
        }

        if (GUILayout.Button("Unset Selected Hierarchy Static"))
        {
            SetSelectedStatic(false);
        }
    }

    private void SetSelectedStatic(bool value)
    {
        foreach (GameObject root in Selection.gameObjects)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive);

            foreach (Transform t in children)
            {
                GameObject obj = t.gameObject;

                if (ShouldSkip(obj))
                    continue;

                Undo.RecordObject(obj, value ? "Set Static" : "Unset Static");

                obj.isStatic = value;

                EditorUtility.SetDirty(obj);
            }
        }

        Debug.Log(value ? "Selected hierarchy set to Static." : "Selected hierarchy unset from Static.");
    }

    private bool ShouldSkip(GameObject obj)
    {
        string n = obj.name.ToLower();

        if (n.Contains("player")) return true;
        if (n.Contains("npc")) return true;
        if (n.Contains("character")) return true;
        if (n.Contains("car")) return true;
        if (n.Contains("vehicle")) return true;
        if (n.Contains("door")) return true;
        if (n.Contains("train")) return true;

        return false;
    }
}
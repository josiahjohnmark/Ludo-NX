using UnityEngine;
using UnityEditor;

public class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts")]
    static void FindMissingScripts()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int count = 0;

        foreach (GameObject go in allObjects)
        {
            if (EditorUtility.IsPersistent(go)) continue;

            Component[] components = go.GetComponents<Component>();
            foreach (Component c in components)
            {
                if (c == null)
                {
                    Debug.Log("Missing script found on: " + go.name, go);
                    count++;
                }
            }
        }

        Debug.Log($"Scan complete. Found {count} missing script(s).");
    }

    [MenuItem("Tools/Remove All Missing Scripts")]
    static void RemoveMissingScripts()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int removedCount = 0;

        foreach (GameObject go in allObjects)
        {
            // Only touch objects currently in the scene, not prefab assets on disk
            if (EditorUtility.IsPersistent(go)) continue;

            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            removedCount += removed;
        }

        Debug.Log($"Removed missing scripts from a total of {removedCount} component slot(s).");
    }
}
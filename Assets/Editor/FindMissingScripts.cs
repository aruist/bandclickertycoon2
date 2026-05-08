using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FindMissingScripts
{
    [MenuItem("Tools/FindMissingScripts")]
    public static void FindMissingScriptsInScene()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("FindMissingScripts: No active loaded scene.");
            return;
        }

        int missingCount = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            missingCount += FindMissingInGameObject(roots[i], "Scene '" + scene.name + "'");
        }

        if (missingCount == 0)
        {
            Debug.Log("FindMissingScripts: No missing components found in scene '" + scene.name + "'.");
        }
        else
        {
            Debug.LogWarning("FindMissingScripts: Found " + missingCount + " missing component(s) in scene '" + scene.name + "'.");
        }
    }

    [MenuItem("Tools/FindMissingScripts/Project Prefabs")]
    public static void FindMissingScriptsInProjectPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int prefabsWithMissing = 0;
        int totalMissing = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
            {
                continue;
            }

            int missingInPrefab = FindMissingInGameObject(prefabRoot, "Prefab '" + prefabPath + "'");
            if (missingInPrefab > 0)
            {
                prefabsWithMissing++;
                totalMissing += missingInPrefab;
            }
        }

        if (prefabsWithMissing == 0)
        {
            Debug.Log("FindMissingScripts: No missing components found in project prefabs.");
        }
        else
        {
            Debug.LogWarning(
                "FindMissingScripts: Found " + totalMissing + " missing component(s) in " +
                prefabsWithMissing + " prefab(s).");
        }
    }

    private static int FindMissingInGameObject(GameObject gameObject, string ownerContext)
    {
        int found = 0;
        Component[] components = gameObject.GetComponents<Component>();

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                found++;
                Debug.LogError(
                    "Missing Component in " + ownerContext + " at GameObject: " + GetHierarchyPath(gameObject),
                    gameObject);
            }
        }

        Transform transform = gameObject.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            found += FindMissingInGameObject(transform.GetChild(i).gameObject, ownerContext);
        }

        return found;
    }

    private static string GetHierarchyPath(GameObject gameObject)
    {
        Transform current = gameObject.transform;
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}

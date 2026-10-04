using UnityEditor;
using UnityEngine;

public static class MonsterPrefabSetup
{
    [MenuItem("Tools/Gun Quest/Attach AI To Monster Prefabs")]
    private static void SetupMonsters()
    {
        SetupPrefab("Assets/Prefabs/Monster2.prefab");
        SetupPrefab("Assets/Prefabs/Monster3.prefab");
        Debug.Log("Monster prefab setup completed.");
    }

    private static void SetupPrefab(string path)
    {
        GameObject monster = PrefabUtility.LoadPrefabContents(path);
        if (monster == null)
        {
            Debug.LogWarning("Monster prefab was not found at " + path);
            return;
        }

        try
        {
            if (monster.GetComponent<EnemyHealth>() == null) monster.AddComponent<EnemyHealth>();
            if (monster.GetComponent<EnemyAI>() == null) monster.AddComponent<EnemyAI>();
            PrefabUtility.SaveAsPrefabAsset(monster, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(monster);
        }
    }
}

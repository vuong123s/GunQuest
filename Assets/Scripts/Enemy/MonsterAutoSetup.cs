using UnityEngine;

public static class MonsterAutoSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SetupMonsters()
    {
        foreach (Animator animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            Transform monsterRoot = FindMonsterRoot(animator.transform);
            if (monsterRoot == null) continue;
            GameObject monster = monsterRoot.gameObject;
            if (monster.name.StartsWith("Monster1") && monster.GetComponent<MonsterEffects>() == null)
            {
                monster.AddComponent<MonsterEffects>();
            }

            if (monster.GetComponent<EnemyAI>() == null) monster.AddComponent<EnemyAI>();
        }
    }

    private static Transform FindMonsterRoot(Transform transform)
    {
        Transform result = null;
        while (transform != null)
        {
            if (transform.name.StartsWith("Monster")) result = transform;
            transform = transform.parent;
        }

        return result;
    }
}

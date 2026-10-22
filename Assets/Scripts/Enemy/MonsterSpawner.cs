using UnityEngine;
using UnityEngine.AI;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField, Min(0.1f)] private float navMeshSearchRadius = 3f;

    private void Start()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("MonsterSpawner needs a monster prefab.", this);
            return;
        }

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
            {
                Debug.LogWarning("No NavMesh near monster spawn point: " + point.name, point);
                continue;
            }

            Instantiate(monsterPrefab, hit.position, point.rotation, transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position, 0.5f);
            Gizmos.DrawLine(point.position, point.position + point.forward * 1.5f);
        }
    }
}

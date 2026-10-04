using UnityEngine;

public class DamageTest : MonoBehaviour
{
    [SerializeField]
    private float damageAmount = 15f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth health))
        {
            health.TakeDamage(damageAmount);
        }
    }
}


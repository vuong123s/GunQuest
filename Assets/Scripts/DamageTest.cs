using UnityEngine;

public class DamageTest : MonoBehaviour
{
    [SerializeField]
    private float damageAmount = 15f;
    [SerializeField]
    private float healAmount = 20f;

    // Damage is applied when the player walks into this trigger zone.
    // To test healing, call RestoreHealth() directly from another script or event.
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth health))
        {
            health.TakeDamage(damageAmount);
        }
    }
}


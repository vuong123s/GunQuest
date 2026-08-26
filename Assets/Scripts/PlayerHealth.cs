using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private float health;
    private float lerpTimer;

    [Header("Health Settings")]
    [SerializeField]
    public float maxHealth = 100f;
    [SerializeField]
    public float chipSpeed = 2f;

    [Header("UI Health Bars")]
    [SerializeField]
    public Image frontHealthBar;
    [SerializeField]
    public Image backHealthBar;

    [Header("Damage Screen Overlay")]
    [SerializeField]
    public Image overlay;
    [SerializeField]
    public float duration = 1f;
    [SerializeField]
    public float fadeSpeed = 1.5f;
    private float durationTimer;

    void Start()
    {
        health = maxHealth;
        if (overlay != null)
        {
            overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, 0f);
        }
    }

    void Update()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthUI();
        UpdateDamageOverlay();
    }

    public void UpdateHealthUI()
    {
        if (frontHealthBar == null || backHealthBar == null)
        {
            return;
        }

        float fillF = frontHealthBar.fillAmount;
        float fillB = backHealthBar.fillAmount;
        float hFraction = health / maxHealth;

        // When taking damage (back bar chips down smoothly)
        if (fillB > hFraction)
        {
            frontHealthBar.fillAmount = hFraction;
            backHealthBar.color = Color.red;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / chipSpeed;
            percentComplete = percentComplete * percentComplete;
            backHealthBar.fillAmount = Mathf.Lerp(fillB, hFraction, percentComplete);
        }

        // When restoring health (front bar fills smoothly)
        if (fillF < hFraction)
        {
            backHealthBar.color = Color.green;
            backHealthBar.fillAmount = hFraction;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / chipSpeed;
            percentComplete = percentComplete * percentComplete;
            frontHealthBar.fillAmount = Mathf.Lerp(fillF, backHealthBar.fillAmount, percentComplete);
        }
    }

    private void UpdateDamageOverlay()
    {
        if (overlay == null)
        {
            return;
        }

        // Low health pulsing or fading effect
        if (overlay.color.a > 0)
        {
            if (health < 30)
            {
                return; // Keep blood vignette when health is critically low
            }

            durationTimer += Time.deltaTime;
            if (durationTimer > duration)
            {
                float tempAlpha = overlay.color.a - (Time.deltaTime * fadeSpeed);
                overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, Mathf.Clamp01(tempAlpha));
            }
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        lerpTimer = 0f;
        durationTimer = 0f;

        if (overlay != null)
        {
            overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, 0.8f);
        }

        if (health <= 0)
        {
            Die();
        }
    }

    public void RestoreHealth(float healAmount)
    {
        health += healAmount;
        lerpTimer = 0f;
    }

    private void Die()
    {
        Debug.Log("Player has died!");
    }

    public float GetCurrentHealth() => health;
}

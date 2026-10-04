using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float chipSpeed = 2f;
    [Header("UI Health Bars")]
    public Image frontHealthBar;
    public Image backHealthBar;
    [Header("Damage Screen Overlay")]
    public Image overlay;
    public float duration = 1f;
    public float fadeSpeed = 1.5f;

    private float health;
    private float overlayTimer;
    private PlayerMotor motor;

    public bool IsDead => health <= 0f;

    private void Start()
    {
        motor = GetComponent<PlayerMotor>();
        health = maxHealth;
        SetOverlayAlpha(0f);
        UpdateHealthUI();
    }

    private void Update()
    {
        UpdateHealthUI();
        overlayTimer += Time.deltaTime;
        if (overlay != null && overlayTimer >= duration) SetOverlayAlpha(Mathf.MoveTowards(overlay.color.a, 0f, fadeSpeed * Time.deltaTime));
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || IsDead) return;
        health = Mathf.Max(0f, health - damage);
        overlayTimer = 0f;
        SetOverlayAlpha(0.8f);
        if (IsDead) motor?.PlayDieAnimation();
        else motor?.PlayHitAnimation();
    }

    public void RestoreHealth(float amount)
    {
        if (amount > 0f) health = Mathf.Min(maxHealth, health + amount);
    }

    public float GetCurrentHealth() => health;

    public void UpdateHealthUI()
    {
        if (maxHealth <= 0f) return;
        float target = health / maxHealth;
        if (frontHealthBar != null) frontHealthBar.fillAmount = Mathf.MoveTowards(frontHealthBar.fillAmount, target, chipSpeed * Time.deltaTime);
        if (backHealthBar != null) backHealthBar.fillAmount = Mathf.MoveTowards(backHealthBar.fillAmount, target, chipSpeed * Time.deltaTime);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (overlay == null) return;
        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }
}

using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float barHeight = 2.8f;
    [SerializeField] private float deathDelay = 2.8f;

    private float health;
    private Image fill;
    private Transform healthBar;
    private Camera mainCamera;
    private EnemyAI enemyAI;
    private MonsterEffects effects;
    private static Sprite whiteSprite;

    public event System.Action<EnemyHealth> Died;
    public bool IsDead => health <= 0f;
    public float NormalizedHealth => maxHealth > 0f ? health / maxHealth : 0f;

    private void Awake()
    {
        health = maxHealth;
        mainCamera = Camera.main;
        enemyAI = GetComponent<EnemyAI>();
        effects = GetComponent<MonsterEffects>();
        CreateHealthBar();
    }

    private void LateUpdate()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (healthBar != null && mainCamera != null)
        {
            healthBar.forward = healthBar.position - mainCamera.transform.position;
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position + Vector3.up * (barHeight * 0.5f));
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (damage <= 0f || IsDead) return;

        health = Mathf.Max(0f, health - damage);
        if (fill != null) fill.fillAmount = NormalizedHealth;
        effects?.PlayHit(hitPoint);

        if (IsDead)
        {
            Die();
        }
        else
        {
            enemyAI?.OnDamaged(damage);
        }
    }

    private void Die()
    {
        Died?.Invoke(this);
        if (healthBar != null) healthBar.gameObject.SetActive(false);
        enemyAI?.DieNow();
        effects?.PlayDeath();

        foreach (Collider bodyCollider in GetComponentsInChildren<Collider>())
        {
            bodyCollider.enabled = false;
        }

        Destroy(gameObject, deathDelay);
    }

    private void CreateHealthBar()
    {
        GameObject canvasObject = new GameObject("HealthBar", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = Vector3.up * barHeight;
        canvasObject.transform.localScale = Vector3.one * 0.01f;
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        healthBar = canvasObject.transform;

        Image background = CreateImage("Background", canvasObject.transform, new Color(0.05f, 0.05f, 0.05f, 0.9f));
        background.rectTransform.sizeDelta = new Vector2(120f, 14f);
        fill = CreateImage("Fill", background.transform, new Color(0.85f, 0.08f, 0.12f));
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = new Vector2(2f, 2f);
        fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.color = color;
        return image;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }
}

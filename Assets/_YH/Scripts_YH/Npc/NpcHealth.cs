using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NpcHealth : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("체력바 프리팹")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);
    private GameObject healthBarInstance;
    private Image healthBarFillImage;
    private float targetFill;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("피격 효과")]
    [SerializeField] private float invincibilityTime = 0.5f;
    [SerializeField] private float blinkRate = 0.1f;
    private bool isInvincible = false;

    [Header("데미지 텍스트")]
    [SerializeField] private GameObject floatingDamageTextPrefab;
    [SerializeField] private Canvas worldCanvas;

    private Animator animator;
    private SpriteRenderer[] spriteRenderers;
    private Npc npcScript;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        npcScript = GetComponent<Npc>();

        Invoke(nameof(InitializeHealth), 0.1f);
        CreateHealthBar();
    }

    private void CreateHealthBar()
    {
        if (!healthBarPrefab)
        {
            Debug.LogError("HealthBar 프리팹이 할당되지 않았습니다!");
            return;
        }

        healthBarInstance = Instantiate(healthBarPrefab, transform);
        healthBarInstance.transform.position = transform.position + healthBarOffset;
        healthBarFillImage = healthBarInstance.transform.Find("Fill")?.GetComponent<Image>();
    }

    private void InitializeHealth()
    {
        maxHealth = (npcScript?.NpcEntry?.health ?? 1f) * 10f;
        maxHealth = Mathf.Max(maxHealth, 10f);
        currentHealth = maxHealth;
        targetFill = 1f;
        UpdateHealthBar();
    }

    private void Update()
    {
        if (!healthBarInstance) return;

        healthBarInstance.transform.position = transform.position + healthBarOffset;

        if (Camera.main)
        {
            healthBarInstance.transform.LookAt(healthBarInstance.transform.position + Camera.main.transform.forward);
        }

        if (healthBarFillImage)
        {
            healthBarFillImage.fillAmount = Mathf.Lerp(healthBarFillImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
        }
    }

    public void TakeDamage(float damage, Vector2 hitPosition = default)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        ShowDamageText(damage);
        UpdateHealthBar();

        animator?.SetTrigger("3_Damaged");

        if (gameObject.activeInHierarchy)
        {
            isInvincible = true;
            StartCoroutine(InvincibilityCoroutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        float endTime = Time.time + invincibilityTime;
        bool visible = false;

        while (Time.time < endTime)
        {
            visible = !visible;
            SetCharacterVisibility(visible);
            yield return new WaitForSeconds(blinkRate);
        }

        RestoreOriginalColors();
        isInvincible = false;
    }

    private void SetCharacterVisibility(bool visible)
    {
        if (spriteRenderers == null) return;

        foreach (var renderer in spriteRenderers)
        {
            if (!renderer) continue;

            var color = Color.white;
            color.a = visible ? 1f : 0.5f;
            renderer.color = color;
        }
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers == null) return;

        foreach (var renderer in spriteRenderers)
        {
            if (renderer)
            {
                renderer.color = Color.white;
            }
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private void UpdateHealthBar()
    {
        if (!healthBarFillImage) return;

        targetFill = Mathf.Clamp01(currentHealth / maxHealth);
        healthBarFillImage.fillAmount = targetFill;
    }

    private void ShowDamageText(float damage)
    {
        if (!floatingDamageTextPrefab || !worldCanvas || !Camera.main) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);
        GameObject damageTextObj = Instantiate(floatingDamageTextPrefab, worldCanvas.transform);
        damageTextObj.transform.position = screenPos;

        if (damageTextObj.TryGetComponent(out TextMeshProUGUI tmpText))
        {
            tmpText.text = damage.ToString("0");
            tmpText.color = Color.red;
            tmpText.fontSize = 16f;
        }
        else if (damageTextObj.TryGetComponent(out TextMesh textMesh))
        {
            textMesh.text = damage.ToString("0");
            textMesh.color = Color.red;
            textMesh.fontSize = 16;
        }
        else if (damageTextObj.TryGetComponent(out Text uiText))
        {
            uiText.text = damage.ToString("0");
            uiText.color = Color.red;
            uiText.fontSize = 16;
        }

        StartCoroutine(AnimateDamageText(damageTextObj));
    }

    private System.Collections.IEnumerator AnimateDamageText(GameObject textObj)
    {
        float duration = 1.0f;
        float startTime = Time.time;
        Vector3 startPosition = textObj.transform.position;

        while (Time.time < startTime + duration)
        {
            float progress = (Time.time - startTime) / duration;
            textObj.transform.position = startPosition + Vector3.up * progress * 50f;

            if (textObj.TryGetComponent(out TextMeshProUGUI tmpText))
            {
                var color = tmpText.color;
                color.a = 1f - progress;
                tmpText.color = color;
            }
            else if (textObj.TryGetComponent(out TextMesh textMesh))
            {
                var color = textMesh.color;
                color.a = 1f - progress;
                textMesh.color = color;
            }
            else if (textObj.TryGetComponent(out Text uiText))
            {
                var color = uiText.color;
                color.a = 1f - progress;
                uiText.color = color;
            }

            yield return null;
        }

        Destroy(textObj);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();
    }

    public void SetMaxHealth(float health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthBar();
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthRatio() => currentHealth / maxHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
}

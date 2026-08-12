using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [InspectorLabel("플레이어 스탯")]
    [SerializeField] private PlayerStat playerStat;
    
    [InspectorLabel("애니메이션 핸들러")]
    [SerializeField] private AnimationHandler animationHandler;

    [InspectorLabel("체력 슬라이더")]
    [SerializeField] private Slider hpSlider;
    
    [InspectorLabel("죽음 대기")]
    [SerializeField] private float deathEffectDelay;

    private float currentHp;
    private float cachedMaxHp;
    private bool isDead;
    private float regenBuffer;
    private float damageTextBuffer;
    public float CurrentHp => currentHp;
    public float MaxHp => playerStat != null ? playerStat.MaxHp : 0;
    public bool IsDead => isDead;

    public event Action<float, float> OnHpChanged;
    public event Action OnDead;

    private void Awake()
    {
        if (playerStat == null)
            playerStat = GetComponent<PlayerStat>();

        if (animationHandler == null)
            animationHandler = GetComponentInChildren<AnimationHandler>(true);

        cachedMaxHp = MaxHp;
        currentHp = MaxHp;
    }

    private void OnEnable()
    {
        if (playerStat != null)
            playerStat.OnMaxHpChanged += HandleMaxHpChanged;
    }

    private void OnDisable()
    {
        if (playerStat != null)
            playerStat.OnMaxHpChanged -= HandleMaxHpChanged;
    }

    private void Start()
    {
        RefreshHpUI();
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    private void Update()
    {
        RegenerateHp();
    }

    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead || damage <= 0f)
            return;

        currentHp = Mathf.Max(0f, currentHp - damage);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SfxType.PlayerHit);

        damageTextBuffer += damage;
        int showDamage = Mathf.FloorToInt(damageTextBuffer);

        if (showDamage > 0)
        {
            damageTextBuffer -= showDamage;
            DamageTextManager.ShowPlayerDamage(showDamage, transform.position, isCritical);
        }

        animationHandler?.PlayHit();

        RefreshHpUI();
        OnHpChanged?.Invoke(currentHp, MaxHp);

        if (currentHp <= 0f)
            Die();
    }

    public void Heal(float value, bool showText = true)
    {
        if (isDead || value <= 0f)
            return;

        float beforeHp = currentHp;
        currentHp = Mathf.Min(MaxHp, currentHp + value);

        float healedAmount = currentHp - beforeHp;

        if (showText && healedAmount >= 1f)
            DamageTextManager.ShowHeal(Mathf.FloorToInt(healedAmount), transform.position);

        RefreshHpUI();
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    private void HandleMaxHpChanged()
    {
        float newMaxHp = MaxHp;
        float increasedAmount = newMaxHp - cachedMaxHp;

        if (increasedAmount > 0f)
            currentHp += increasedAmount;

        currentHp = Mathf.Min(currentHp, newMaxHp);
        cachedMaxHp = newMaxHp;

        RefreshHpUI();
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    private void RefreshHpUI()
    {
        if (hpSlider == null)
            return;

        hpSlider.maxValue = MaxHp;
        hpSlider.value = currentHp;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SfxType.PlayerDie);

        animationHandler?.PlayDeath();

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();

        StartCoroutine(DeathRoutine());
    }
    
    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathEffectDelay);

        OnDead?.Invoke();
    }

    private void RegenerateHp()
    {
        if (isDead)
            return;

        if (playerStat == null)
            return;

        if (playerStat.HpRegenPerSecond <= 0f)
            return;

        if (currentHp >= MaxHp)
            return;

        regenBuffer += playerStat.HpRegenPerSecond * Time.deltaTime;

        if (regenBuffer >= 1f)
        {
            float healAmount = Mathf.Floor(regenBuffer);
            regenBuffer -= healAmount;

            Heal(healAmount, true);
        }
    }
}
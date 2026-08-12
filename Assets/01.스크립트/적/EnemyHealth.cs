using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [InspectorLabel("애니메이션 핸들러")]
    [SerializeField] private AnimationHandler animationHandler;

    [InspectorLabel("피격 플래시")]
    [SerializeField] private EnemyHitFlash hitFlash;

    [InspectorLabel("엘리트 시각 효과")]
    [SerializeField] private EliteVisual eliteVisual;
    
    [InspectorLabel("사망 후 반환 시간")]
    [SerializeField] private float deathReturnDelay;

    [InspectorLabel("피격 후 사망까지 대기 시간")]
    [SerializeField] private float hitReactionBeforeDeathDelay;

    [InspectorLabel("경험치 오브 드롭 여부")]
    [SerializeField] private bool dropExpOrb = true;

    private string expOrbKey = "ExpOrb";
    private float currentHp;
    private PlayerLevel playerLevel;
    private EnemyStat enemyStat;
    private float damageTextBuffer;
    private bool isDead;
    private Coroutine returnRoutine;

    public float CurrentHp => currentHp;
    public bool IsDead => isDead;
    
    private const float HitReactionInterval = 0.1f;
    private float nextHitReactionTime;

    private void Awake()
    {
        RefreshReferences();
    }

    private void OnEnable()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        ResetHealth();
    }

    public void ResetHealth()
    {
        RefreshReferences();

        currentHp = enemyStat != null ? enemyStat.MaxHp : 1f;
        damageTextBuffer = 0f;
        isDead = false;

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = true;
        }

        animationHandler?.ResetState();
    }

    public void TakeDamage(float amount, bool isCritical = false)
    {
        if (isDead)
            return;

        if (amount <= 0f)
            return;

        currentHp = Mathf.Max(0f, currentHp - amount);
        
        if (Time.time >= nextHitReactionTime)
        {
            nextHitReactionTime =
                Time.time + HitReactionInterval;

            SoundManager.Instance?.PlaySFX(
                SfxType.PlayerHit);

            animationHandler?.PlayHit();
            hitFlash?.PlayFlash();
        }

        Vector2 random = Random.insideUnitCircle * 8f;

        Vector3 effectPosition =
            transform.position +
            new Vector3(
                random.x,
                Random.Range(0.3f, 1.5f),
                random.y);

        float randomScale = Random.Range(5f, 10f);

        EffectManager.Play(
            "Hit",
            effectPosition,
            randomScale);

        damageTextBuffer += amount;
        int showDamage =
            Mathf.FloorToInt(damageTextBuffer);

        if (showDamage > 0)
        {
            damageTextBuffer -= showDamage;

            DamageTextManager.ShowEnemyDamage(
                showDamage,
                transform.position,
                isCritical);
        }

        if (currentHp <= 0f)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        eliteVisual?.DisableOutline();

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        if (dropExpOrb)
            SpawnExpOrb();

        returnRoutine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {

        if (hitReactionBeforeDeathDelay > 0f)
            yield return new WaitForSeconds(hitReactionBeforeDeathDelay);

        animationHandler?.PlayDeath();

        yield return new WaitForSeconds(deathReturnDelay);

        returnRoutine = null;
        ObjectPool.Return(gameObject);
    }

    private void SpawnExpOrb()
    {
        if (playerLevel == null)
            playerLevel = FindFirstObjectByType<PlayerLevel>();

        if (playerLevel == null)
            return;

        GameObject orb = ObjectPool.Get(expOrbKey);
        int expReward = GetExpReward();

        if (orb == null)
        {
            playerLevel.AddExp(expReward);
            return;
        }

        orb.transform.position = transform.position;
        orb.transform.rotation = Quaternion.identity;
        orb.SetActive(true);

        if (orb.TryGetComponent<ExpOrb>(out var expOrb))
        {
            expOrb.Init(playerLevel.transform, expReward, expOrbKey);
        }
        else
        {
            playerLevel.AddExp(expReward);
            ObjectPool.Return(expOrbKey, orb);
        }
    }

    private int GetExpReward()
    {
        if (enemyStat == null)
            enemyStat = GetComponent<EnemyStat>();

        return enemyStat != null ? enemyStat.ExpReward : 1;
    }

    private void RefreshReferences()
    {
        if (playerLevel == null)
            playerLevel = FindFirstObjectByType<PlayerLevel>();

        if (enemyStat == null)
            enemyStat = GetComponent<EnemyStat>();

        if (animationHandler == null)
            animationHandler = GetComponentInChildren<AnimationHandler>(true);

        if (eliteVisual == null)
            eliteVisual = GetComponent<EliteVisual>();

        if (hitFlash == null)
            hitFlash = GetComponent<EnemyHitFlash>();
    }
}
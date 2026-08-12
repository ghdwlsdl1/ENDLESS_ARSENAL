using System.Collections.Generic;
using UnityEngine;

public class AuraWeapon : MonoBehaviour, IInventoryBonusReceiver
{
    [InspectorLabel("공격 대상")]
    [SerializeField] private TargetType targetType = TargetType.Enemy;

    [InspectorLabel("공격 범위 콜라이더")]
    [SerializeField] private Collider attackCollider;

    [InspectorLabel("오라 이펙트 프리팹")]
    [SerializeField] private GameObject auraEffectPrefab;

    [InspectorLabel("오라 이펙트 크기 배율")]
    [SerializeField] private float auraEffectPrefabSize;

    [InspectorLabel("기본 공격력")]
    [SerializeField] private float baseDamage;

    [InspectorLabel("공격력 반영 비율")]
    [SerializeField] private float statDamageMultiplier;

    [InspectorLabel("공격 범위")]
    [SerializeField] private float radius;

    [InspectorLabel("타격 간격")]
    [SerializeField] private float hitInterval;

    [InspectorLabel("최대 탐색 수")]
    [SerializeField] private int maxTargetCount;

    [Header("사운드")]

    [InspectorLabel("효과음 사용")]
    [SerializeField] private bool useHitSfx;

    [InspectorLabel("타격 효과음")]
    [SerializeField] private SfxType hitSfx;

    [InspectorLabel("효과음 간격")]
    [SerializeField] private float sfxInterval;

    private float sfxTimer;
    
    private Collider[] targetBuffer;

    private Transform auraEffect;
    private WeaponInventoryBonus inventoryBonus;
    
    private PlayerStat playerStat;
    private EnemyStat enemyStat;
    private EnemyHealth enemyHealth;
    
    private readonly Dictionary<IDamageable, float> nextHitTimes =
        new Dictionary<IDamageable, float>();

    private readonly HashSet<IDamageable> currentTargets =
        new HashSet<IDamageable>();

    private readonly List<IDamageable> targetRemoveBuffer =
        new List<IDamageable>();

    private void Awake()
    {
        CacheOwnerReferences();

        if (attackCollider == null)
            attackCollider = GetComponent<Collider>();

        maxTargetCount = Mathf.Max(1, maxTargetCount);
        targetBuffer = new Collider[maxTargetCount];
    }

    private void CacheOwnerReferences()
    {
        Transform root = transform.root;

        if (root == null)
            return;

        if (targetType == TargetType.Enemy)
            playerStat = root.GetComponentInChildren<PlayerStat>();
        else
        {
            enemyStat = root.GetComponentInChildren<EnemyStat>();
            enemyHealth = root.GetComponentInChildren<EnemyHealth>();
        }
    }

    private void OnEnable()
    {
        CacheOwnerReferences();

        nextHitTimes.Clear();
        currentTargets.Clear();

        UpdateAttackCollider();
        CreateAuraEffect();
    }

    private void OnDisable()
    {
        nextHitTimes.Clear();
        currentTargets.Clear();

        DestroyAuraEffect();
    }

    public void SetInventoryBonus(WeaponInventoryBonus bonus)
    {
        inventoryBonus = bonus;
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.IsGameOver)
        {
            return;
        }

        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        UpdateAttackCollider();
        UpdateAuraEffect();

        sfxTimer += Time.deltaTime;

        DamageTargetsInRange();
    }

    private void CreateAuraEffect()
    {
        if (auraEffect != null)
            return;

        if (auraEffectPrefab == null)
            return;

        GameObject effectObj = Instantiate(auraEffectPrefab, transform);
        effectObj.transform.localPosition = Vector3.zero;
        effectObj.transform.localRotation = Quaternion.identity;

        auraEffect = effectObj.transform;

        UpdateAuraEffect();
    }

    private void DestroyAuraEffect()
    {
        if (auraEffect == null)
            return;

        Destroy(auraEffect.gameObject);
        auraEffect = null;
    }

    private void UpdateAuraEffect()
    {
        if (auraEffect == null)
            return;

        float scale = GetRadius();

        auraEffect.localScale = new Vector3(
            scale * auraEffectPrefabSize,
            10f,
            scale * auraEffectPrefabSize);
    }

    private void UpdateAttackCollider()
    {
        float currentRadius = GetRadius();

        if (attackCollider is SphereCollider sphereCollider)
        {
            sphereCollider.radius = currentRadius;
            return;
        }

        if (attackCollider is BoxCollider boxCollider)
        {
            boxCollider.size = new Vector3(
                currentRadius * 2f,
                boxCollider.size.y,
                currentRadius * 2f);
        }
    }

    private void DamageTargetsInRange()
    {
        if (attackCollider == null)
            return;

        currentTargets.Clear();

        int count = GetTargetsByCollider();
        (float damage, bool isCritical) = GetDamage();
        float currentHitInterval = GetHitInterval();

        for (int i = 0; i < count; i++)
        {
            Collider target = targetBuffer[i];

            if (target == null)
                continue;

            DamageTarget(
                target,
                damage,
                isCritical,
                currentHitInterval);
        }

        RemoveTargetsOutsideRange();
    }

    private int GetTargetsByCollider()
    {
        if (attackCollider is SphereCollider sphereCollider)
        {
            return Physics.OverlapSphereNonAlloc(
                sphereCollider.transform.TransformPoint(sphereCollider.center),
                GetWorldSphereRadius(sphereCollider),
                targetBuffer,
                ~0,
                QueryTriggerInteraction.Collide);
        }

        if (attackCollider is BoxCollider boxCollider)
        {
            return Physics.OverlapBoxNonAlloc(
                boxCollider.transform.TransformPoint(boxCollider.center),
                Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale) * 0.5f,
                targetBuffer,
                boxCollider.transform.rotation,
                ~0,
                QueryTriggerInteraction.Collide);
        }

        return Physics.OverlapSphereNonAlloc(
            attackCollider.bounds.center,
            attackCollider.bounds.extents.magnitude,
            targetBuffer,
            ~0,
            QueryTriggerInteraction.Collide);
    }

    private void DamageTarget(
        Collider target,
        float damage,
        bool isCritical,
        float hitInterval)
    {
        if (target == null)
            return;

        if (target.transform.root == transform.root)
            return;

        if (IsWeaponCollider(target))
            return;

        if (targetType == TargetType.Enemy)
        {
            if (!target.CompareTag("Enemy"))
                return;
        }
        else
        {
            if (!target.CompareTag("Player"))
                return;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();

        if (damageable == null || damageable.IsDead)
            return;

        currentTargets.Add(damageable);

        if (nextHitTimes.TryGetValue(damageable, out float nextHitTime) &&
            Time.time < nextHitTime)
        {
            return;
        }

        nextHitTimes[damageable] = Time.time + hitInterval;

        damageable.TakeDamage(damage, isCritical);
        PlayHitSound();
    }
    
    private bool IsWeaponCollider(Collider target)
    {
        if (target == null)
            return true;

        if (target.GetComponentInParent<OrbitWeaponHitBox>() != null)
            return true;

        return false;
    }

    private float GetWorldSphereRadius(SphereCollider sphereCollider)
    {
        Vector3 scale = sphereCollider.transform.lossyScale;
        float maxScale = Mathf.Max(scale.x, scale.y, scale.z);

        return sphereCollider.radius * maxScale;
    }

    private (float damage, bool isCritical) GetDamage()
    {
        float statDamage = 0f;
        float weaponDamageMultiplier = 1f;
        float inventoryMultiplier = 1f;
        bool isCritical = false;

        if (targetType == TargetType.Enemy)
        {
            statDamage = playerStat != null ? playerStat.Damage : 0f;
            weaponDamageMultiplier = playerStat != null ? playerStat.WeaponDamageMultiplier : 1f;
            inventoryMultiplier += inventoryBonus.DamageBonus;
        }
        else
        {
            statDamage = enemyStat != null ? enemyStat.Damage : 0f;
        }

        float finalDamage =
            (baseDamage + statDamage * statDamageMultiplier) *
            weaponDamageMultiplier *
            inventoryMultiplier;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            if (Random.value < playerStat.CriticalChance)
            {
                isCritical = true;
                finalDamage *= playerStat.CriticalDamageMultiplier;
            }
        }

        return (Mathf.Max(0.01f, finalDamage), isCritical);
    }

    private float GetRadius()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponRangeMultiplier;
            multiplier += inventoryBonus.RangeBonus;
        }
        
        float rangeIncrease = multiplier - 1f;
        float appliedMultiplier = 1f + rangeIncrease * 0.5f;

        return radius * Mathf.Max(0.01f, appliedMultiplier);
    }

    private float GetHitInterval()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponAttackSpeedMultiplier;
            multiplier += inventoryBonus.AttackSpeedBonus;
        }

        return hitInterval / Mathf.Max(0.01f, multiplier);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackCollider == null)
            return;

        if (attackCollider is SphereCollider sphereCollider)
        {
            GizmoUtility.DrawCircleXZ(
                sphereCollider.transform.TransformPoint(sphereCollider.center),
                radius,
                Color.blue);

            return;
        }

        if (attackCollider is BoxCollider boxCollider)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.color = Color.blue;
            Gizmos.matrix = Matrix4x4.TRS(
                boxCollider.transform.position,
                boxCollider.transform.rotation,
                boxCollider.transform.lossyScale);

            Vector3 size = new Vector3(
                radius * 2f,
                boxCollider.size.y,
                radius * 2f);

            Gizmos.DrawWireCube(boxCollider.center, size);

            Gizmos.matrix = previousMatrix;
        }
    }
    private void PlayHitSound()
    {
        if (!useHitSfx)
            return;

        if (sfxTimer < sfxInterval)
            return;

        if (SoundManager.Instance == null)
            return;

        sfxTimer = 0f;
        SoundManager.Instance.PlaySFX(hitSfx);
    }
    
    private void RemoveTargetsOutsideRange()
    {
        targetRemoveBuffer.Clear();

        foreach (var pair in nextHitTimes)
        {
            IDamageable target = pair.Key;

            if (target == null ||
                target.IsDead ||
                !currentTargets.Contains(target))
            {
                targetRemoveBuffer.Add(target);
            }
        }

        for (int i = 0; i < targetRemoveBuffer.Count; i++)
        {
            nextHitTimes.Remove(targetRemoveBuffer[i]);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PulseWeaponAimType
{
    [InspectorName("가장 가까운 대상")]
    NearestTarget,

    [InspectorName("이동 방향")]
    MoveDirection,

    [InspectorName("고정 방향")]
    Forward,

    [InspectorName("랜덤 방향")]
    RandomDirection
}

public enum PulseWeaponAttackType
{
    [InspectorName("공격 방향만")]
    ForwardOnly,

    [InspectorName("공격 방향 + 반대 방향")]
    ForwardAndBackward
}

public class PulseWeapon : MonoBehaviour, IInventoryBonusReceiver
{
    [InspectorLabel("공격 대상")]
    [SerializeField] private TargetType targetType = TargetType.Enemy;

    [InspectorLabel("공격 위치")]
    [SerializeField] private Transform attackPoint;

    [InspectorLabel("조준 방식")]
    [SerializeField] private PulseWeaponAimType aimType = PulseWeaponAimType.NearestTarget;

    [InspectorLabel("공격 형식")]
    [SerializeField] private PulseWeaponAttackType attackType = PulseWeaponAttackType.ForwardOnly;

    [InspectorLabel("공격 각도", "aimType", PulseWeaponAimType.Forward)]
    [SerializeField] private float aimAngleOffset = 0f;

    [InspectorLabel("범위 안 대상이 있을 때만 공격")]
    [SerializeField] private bool attackOnlyWhenTargetInRange = true;


    [Header("공격")]

    [InspectorLabel("기본 공격력")]
    [SerializeField] private float baseDamage;

    [InspectorLabel("공격력 반영 비율")]
    [SerializeField] private float statDamageMultiplier;

    [InspectorLabel("공격 간격")]
    [SerializeField] private float attackInterval;

    [InspectorLabel("공격 인지거리")]
    [SerializeField] private float detectRange;

    [InspectorLabel("공격 폭")]
    [SerializeField] private float attackWidth;

    [InspectorLabel("공격 사거리")]
    [SerializeField] private float attackDistance;

    [InspectorLabel("공격 시작 거리")]
    [SerializeField] private float attackOffset;

    private float attackHeight = 5f;


    [Header("연속 공격")]

    [InspectorLabel("연속 공격 횟수")]
    [SerializeField] private int comboCount;

    [InspectorLabel("연속 공격 최소 간격")]
    [SerializeField] private float comboIntervalMin;

    [InspectorLabel("연속 공격 최대 간격")]
    [SerializeField] private float comboIntervalMax;


    [Header("이펙트")]

    [InspectorLabel("이펙트 풀링 키")]
    [SerializeField] private string effectPoolKey;
    
    [InspectorLabel("이펙트 폭 배율")]
    [SerializeField] private float effectWidthMultiplier;

    [InspectorLabel("이펙트 길이 배율")]
    [SerializeField] private float effectDistanceMultiplier;

    [InspectorLabel("공격 중앙에 생성")]
    [SerializeField] private bool spawnEffectAtCenter = true;
    
    [Header("사운드")]

    [InspectorLabel("공격 시작 효과음 사용")]
    [SerializeField] private bool useAttackStartSfx;

    [InspectorLabel("공격 시작 효과음")]
    [SerializeField] private SfxType attackStartSfx;

    [InspectorLabel("콤보 효과음 사용")]
    [SerializeField] private bool useComboSfx;

    [InspectorLabel("콤보 효과음")]
    [SerializeField] private SfxType comboSfx;
    
    private PlayerMove playerMove;
    
    private Vector3 lastMoveDirection = Vector3.forward;
    private float attackTimer;
    private bool isAttacking;
    
    private WeaponInventoryBonus inventoryBonus;
    
    private PlayerStat playerStat;
    private EnemyStat enemyStat;

    private void Awake()
    {
        CacheOwnerReferences();

        if (playerMove == null)
            playerMove = GetComponentInParent<PlayerMove>();
    }

    private void CacheOwnerReferences()
    {
        Transform root = transform.root;

        if (root == null)
            return;

        if (targetType == TargetType.Enemy)
            playerStat = root.GetComponentInChildren<PlayerStat>();
        else
            enemyStat = root.GetComponentInChildren<EnemyStat>();
    }

    private void OnEnable()
    {
        CacheOwnerReferences();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        attackTimer = 0f;
        isAttacking = false;
    }

    public void SetInventoryBonus(WeaponInventoryBonus bonus)
    {
        inventoryBonus = bonus;
    }
    
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (isAttacking)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer < GetAttackInterval())
            return;

        if (attackOnlyWhenTargetInRange && !HasTargetInDetectRange())
            return;

        attackTimer = 0f;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        PlayAttackStartSound();
        
        int count = Mathf.Max(1, comboCount);

        for (int i = 0; i < count; i++)
        {
            PlayComboSound();
            
            AttackAll();

            if (i < count - 1)
                yield return new WaitForSeconds(GetComboInterval());
        }

        isAttacking = false;
    }

    private void AttackAll()
    {
        Vector3 direction = GetAimDirection();

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Attack(direction);

        if (attackType == PulseWeaponAttackType.ForwardAndBackward)
            Attack(-direction);
    }

    private void Attack(Vector3 direction)
    {
        SpawnEffect(direction);

        Vector3 center = GetAttackCenter(direction);
        Vector3 halfExtents = GetAttackHalfExtents();
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            rotation,
            ~0,
            QueryTriggerInteraction.Collide);

        HashSet<IDamageable> hitTargets = new();
        (float damage, bool isCritical) = GetDamage();

        for (int i = 0; i < hits.Length; i++)
        {
            string requiredTag = targetType == TargetType.Player ? "Player" : "Enemy";

            if (!hits[i].CompareTag(requiredTag))
                continue;

            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();

            if (damageable == null || damageable.IsDead)
                continue;

            if (!hitTargets.Add(damageable))
                continue;

            damageable.TakeDamage(damage, isCritical);
        }
    }

    private void SpawnEffect(Vector3 direction)
    {
        Vector3 position =
            spawnEffectAtCenter
                ? GetAttackCenter(direction)
                : GetAttackPosition();

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        EffectManager.Play(effectPoolKey, position, rotation, GetEffectScale());
    }

    private Vector3 GetAimDirection()
    {
        switch (aimType)
        {
            case PulseWeaponAimType.NearestTarget:
            {
                Transform target = FindNearestTarget();

                if (target == null)
                    return GetForwardDirection();

                return GetDirectionTo(target);
            }

            case PulseWeaponAimType.MoveDirection:
                return GetMoveDirection();

            case PulseWeaponAimType.RandomDirection:
                return GetRandomDirection();

            case PulseWeaponAimType.Forward:
            default:
                return GetForwardDirection();
        }
    }

    private Transform FindNearestTarget()
    {
        if (targetType == TargetType.Player)
            return FindPlayerInRange(GetDetectRange());

        return FindNearestEnemyInRange(GetDetectRange());
    }

    private static Transform cachedPlayerTransform;

    private static Transform GetCachedPlayerTransform()
    {
        if (cachedPlayerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            cachedPlayerTransform = player != null ? player.transform : null;
        }

        return cachedPlayerTransform;
    }

    private Transform FindPlayerInRange(float range)
    {
        Transform player = GetCachedPlayerTransform();

        if (player == null)
            return null;

        if (!IsTargetInRange(player, range))
            return null;

        return player;
    }

    private Transform FindNearestEnemyInRange(float range)
    {
        EnemyController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (EnemyController enemy in EnemyRegistry.AliveEnemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead)
                continue;

            if (!IsTargetInRange(enemy.transform, range))
                continue;

            Vector3 diff = enemy.transform.position - GetAttackPosition();
            diff.y = 0f;

            float dist = diff.sqrMagnitude;

            if (dist >= nearestDistance)
                continue;

            nearestDistance = dist;
            nearest = enemy;
        }

        return nearest != null ? nearest.transform : null;
    }

    private bool HasTargetInDetectRange()
    {
        if (targetType == TargetType.Player)
            return FindPlayerInRange(GetDetectRange()) != null;

        return FindNearestEnemyInRange(GetDetectRange()) != null;
    }

    private bool IsTargetInRange(Transform target, float range)
    {
        if (target == null)
            return false;

        if (range <= 0f)
            return true;

        Vector3 diff = target.position - GetAttackPosition();
        diff.y = 0f;

        return diff.sqrMagnitude <= range * range;
    }

    private Vector3 GetDirectionTo(Transform target)
    {
        Vector3 dir = target.position - GetAttackPosition();
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            return GetForwardDirection();

        return dir.normalized;
    }

    private Vector3 GetAttackPosition()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
    }

    private Vector3 GetForwardDirection()
    {
        Vector3 dir = transform.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector3.forward;

        dir.Normalize();

        if (!Mathf.Approximately(aimAngleOffset, 0f))
            dir = Quaternion.Euler(0f, aimAngleOffset, 0f) * dir;

        return dir.normalized;
    }

    private Vector3 GetMoveDirection()
    {
        if (playerMove != null)
        {
            Vector3 dir = playerMove.MoveDirection;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                lastMoveDirection = dir.normalized;
                return lastMoveDirection;
            }
        }

        return lastMoveDirection;
    }

    private Vector3 GetRandomDirection()
    {
        Vector3 dir = Random.insideUnitSphere;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector3.forward;

        return dir.normalized;
    }

    private Vector3 GetAttackCenter(Vector3 direction)
    {
        return GetAttackPosition() + direction.normalized * GetAttackCenterDistance();
    }

    private Vector3 GetAttackHalfExtents()
    {
        return new Vector3(
            GetAttackWidth() * 0.5f,
            Mathf.Max(0.01f, attackHeight) * 0.5f,
            GetAttackDistance() * 0.5f);
    }

    private float GetAttackCenterDistance()
    {
        return attackOffset + GetAttackDistance() * 0.5f;
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

    private float GetAttackInterval()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponAttackSpeedMultiplier;
            multiplier += inventoryBonus.AttackSpeedBonus;
        }

        return attackInterval / Mathf.Max(0.01f, multiplier);
    }


    private float GetDetectRange()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponRangeMultiplier;
            multiplier += inventoryBonus.RangeBonus;
        }

        return Mathf.Max(0f, detectRange * multiplier);
    }

    private float GetAttackDistance()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponRangeMultiplier;
            multiplier += inventoryBonus.RangeBonus;
        }

        return Mathf.Max(0f, attackDistance * multiplier);
    }

    private float GetAttackWidth()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponScaleMultiplier;
            multiplier += inventoryBonus.WeaponScaleBonus;
        }

        return Mathf.Max(0f, attackWidth * multiplier);
    }

    private Vector3 GetEffectScale()
    {
        return new Vector3(
            Mathf.Max(0.01f, GetAttackWidth() * effectWidthMultiplier),
            1f,
            Mathf.Max(0.01f, GetAttackDistance() * effectDistanceMultiplier));
    }

    private float GetComboInterval()
    {
        float min = Mathf.Max(0f, comboIntervalMin);
        float max = Mathf.Max(min, comboIntervalMax);

        if (Mathf.Approximately(min, max))
            return min;

        return Random.Range(min, max);
    }

    private void OnDrawGizmosSelected()
    {
        GizmoUtility.DrawCircleXZ(
            GetAttackPosition(),
            GetDetectRange(),
            Color.green);

        Vector3 direction = Application.isPlaying ? GetAimDirection() : GetForwardDirection();

        DrawAttackGizmo(direction, Color.red);

        if (attackType == PulseWeaponAttackType.ForwardAndBackward)
            DrawAttackGizmo(-direction, Color.magenta);
    }

    private void DrawAttackGizmo(Vector3 direction, Color color)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Vector3 center = GetAttackCenter(direction);

        Vector3 size = new Vector3(
            GetAttackWidth(),
            Mathf.Max(0.01f, attackHeight),
            GetAttackDistance());

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        GizmoUtility.DrawWireBox(center, size, rotation, color);

        GizmoUtility.DrawArrow(
            GetAttackPosition(),
            direction.normalized * GetAttackCenterDistance(),
            color,
            4f);
    }
    
    private void PlayAttackStartSound()
    {
        if (!useAttackStartSfx)
            return;

        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySFX(attackStartSfx);
    }

    private void PlayComboSound()
    {
        if (!useComboSfx)
            return;

        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySFX(comboSfx);
    }
}
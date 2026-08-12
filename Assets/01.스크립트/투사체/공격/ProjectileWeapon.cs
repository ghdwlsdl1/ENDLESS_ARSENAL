using UnityEngine;
using System.Collections;

public enum ProjectileAimType
{
    [InspectorName("가장 가까운 적")]
    NearestEnemy,

    [InspectorName("랜덤 적")]
    RandomEnemy,

    [InspectorName("고정 방향")]
    Forward,

    [InspectorName("이동 방향")]
    MoveDirection,

    [InspectorName("랜덤 방향")]
    RandomDirection,

    [InspectorName("원형 탄막")]
    Circular
}

public class ProjectileWeapon : MonoBehaviour, IInventoryBonusReceiver
{
    [InspectorLabel("공격 대상")]
    [SerializeField] private TargetType targetType = TargetType.Enemy;
    
    [InspectorLabel("투사체 풀 키")]
    [SerializeField] private string projectileKey = "Projectile";
    
    [InspectorLabel("발사 위치")]
    [SerializeField] private Transform firePoint;

    [InspectorLabel("조준 방식")]
    [SerializeField] private ProjectileAimType aimType = ProjectileAimType.NearestEnemy;

    [InspectorLabel("발사 각도", "aimType", ProjectileAimType.Forward)]
    [SerializeField] private float aimAngleOffset = 0f;

    [InspectorLabel("반대 방향 발사", "aimType", ProjectileAimType.Forward)]
    [SerializeField] private bool reverseDirection;
    
    [InspectorLabel("범위 안 대상이 있을 때만 발사")]
    [SerializeField] private bool fireOnlyWhenTargetInRange = true;


    [Header("공격")]

    [InspectorLabel("기본 공격력")]
    [SerializeField] private float baseDamage;

    [InspectorLabel("공격력 반영 비율")]
    [SerializeField] private float statDamageMultiplier;

    [InspectorLabel("공격 간격")]
    [SerializeField] private float attackInterval;

    [InspectorLabel("공격 사거리")]
    [SerializeField] private float attackRange;


    [Header("투사체")]

    [InspectorLabel("투사체 속도")]
    [SerializeField] private float projectileSpeed;

    [InspectorLabel("최대 생존 시간")]
    [SerializeField] private float projectileLifeTime;

    [InspectorLabel("투사체 크기")]
    [SerializeField] private float projectileScale;
    
    [InspectorLabel("발사 시작 거리")]
    [SerializeField] private float fireOffset;
    
    [InspectorLabel("최소 사거리 보정")]
    [SerializeField] private float minRangeOffset;

    [InspectorLabel("최대 사거리 보정")]
    [SerializeField] private float maxRangeOffset;

    [InspectorLabel("타겟 좌우 오차 반경")]
    [SerializeField] private float targetSideErrorRange;


    [Header("다중 발사")]

    [InspectorLabel("발사 개수")]
    [SerializeField] private int projectileCount;

    [InspectorLabel("퍼짐 각도")]
    [SerializeField] private float spreadAngle;

    [InspectorLabel("투사체 시작 간격")]
    [SerializeField] private float projectileSpacing;
    
    [InspectorLabel("연속 공격 횟수")]
    [SerializeField] private int comboCount;

    [InspectorLabel("연속 공격 최소 간격")]
    [SerializeField] private float comboIntervalMin;

    [InspectorLabel("연속 공격 최대 간격")]
    [SerializeField] private float comboIntervalMax;


    [Header("관통")]

    [InspectorLabel("관통 여부")]
    [SerializeField] private bool isPiercing;

    [InspectorLabel("최대 관통 수", "isPiercing", true)]
    [SerializeField] private int maxPierceCount;


    [Header("폭발")]

    [InspectorLabel("폭발 여부")]
    [SerializeField] private bool isExplosive;

    [InspectorLabel("사거리 종료 시 폭발", "isExplosive", true)]
    [SerializeField] private bool explodeOnExpire = false;

    [InspectorLabel("폭발 범위", "isExplosive", true)]
    [SerializeField] private float explosionRadius;

    [InspectorLabel("폭발 피해 비율", "isExplosive", true)]
    [SerializeField] private float explosionDamageRatio;

    [InspectorLabel("폭발 효과음 사용", "isExplosive", true)]
    [SerializeField] private bool useExplosionSfx;

    [InspectorLabel("폭발 효과음", "useExplosionSfx", true)]
    [SerializeField] private SfxType explosionSfx;

    [Header("유도")]

    [InspectorLabel("유도 여부")]
    [SerializeField] private bool isHoming;

    [InspectorLabel("유도 방식", "isHoming", true)]
    [SerializeField] private HomingMoveType homingMoveType = HomingMoveType.Smooth;

    [InspectorLabel("유도 시작 지연", "isHoming", true)]
    [SerializeField] private float homingDelay;

    [InspectorLabel("유도 탐색 범위", "isHoming", true)]
    [SerializeField] private float homingSearchRange;

    [InspectorLabel("유도 회전 속도", "isHoming", true)]
    [SerializeField] private float homingRotateSpeed;

    [InspectorLabel("유도 재탐색 여부", "isHoming", true)]
    [SerializeField] private bool canRetarget = false;

    [InspectorLabel("유도 재탐색 간격", "canRetarget", true)]
    [SerializeField] private float retargetInterval;
    
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
    
    private bool isAttacking;
    private Vector3 lastMoveDirection = Vector3.forward;
    private float attackTimer;
    
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

        if (fireOnlyWhenTargetInRange && !HasTargetInAttackRange())
            return;

        attackTimer = 0f;

        StartCoroutine(AttackRoutine());
    }

    private void FireAll()
    {
        int count = projectileCount;

        if (targetType == TargetType.Enemy)
            count += inventoryBonus.ProjectileCountBonus;

        count = Mathf.Max(1, count);

        for (int i = 0; i < count; i++)
        {
            Fire(i, count);
        }
    }
    
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        PlayAttackStartSound();
        
        int count = Mathf.Max(1, comboCount);

        for (int i = 0; i < count; i++)
        {
            PlayComboSound();
            
            FireAll();

            if (i < count - 1)
                yield return new WaitForSeconds(GetComboInterval());
        }

        isAttacking = false;
    }

    private float GetComboInterval()
    {
        float min = Mathf.Max(0f, comboIntervalMin);
        float max = Mathf.Max(min, comboIntervalMax);

        if (Mathf.Approximately(min, max))
            return min;

        return Random.Range(min, max);
    }
    private void Fire(int index, int count)
    {
        Vector3 baseDirection = GetAimDirection();

        if (baseDirection.sqrMagnitude <= 0.0001f)
            return;

        if (aimType == ProjectileAimType.Circular)
            baseDirection = GetCircularDirection(index, count);
        else
            baseDirection = ApplySpread(baseDirection, index, count);

        Transform target = GetHomingTarget();

        SpawnProjectile(baseDirection, target, index, count);
        
        if (aimType == ProjectileAimType.Forward && reverseDirection)
        {
            SpawnProjectile(-baseDirection, target, index, count);
        }
    }

    private void SpawnProjectile(Vector3 direction, Transform target, int index, int count)
    {
        Vector3 spawnPos = GetProjectileSpawnPosition(direction, index, count);

        GameObject obj = ObjectPool.Get(projectileKey);

        if (obj == null)
            return;

        obj.transform.position = spawnPos;
        obj.transform.rotation = Quaternion.identity;

        if (!obj.TryGetComponent<Projectile>(out var projectile))
        {
            ObjectPool.Return(projectileKey, obj);
            return;
        }

        (float damage, bool isCritical) = GetDamage();

        ProjectileConfig config = new()
        {
            Direction = direction,
            Damage = damage,
            IsCritical = isCritical,
            Speed = GetProjectileSpeed(),
            MaxDistance = GetRandomProjectileRange(),
            LifeTime = GetProjectileLifeTime(),
            PoolKey = projectileKey,
            Scale = GetProjectileScale(),
            IsPiercing = isPiercing,
            MaxPierceCount = maxPierceCount,
            IsExplosive = isExplosive,
            ExplodeOnExpire = explodeOnExpire,
            ExplosionRadius = GetExplosionRadius(),
            ExplosionDamageRatio = explosionDamageRatio,
            IsHoming = isHoming,
            CanRetarget = canRetarget,
            HomingRotateSpeed = homingRotateSpeed,
            HomingSearchRange = GetHomingSearchRange(),
            RetargetInterval = retargetInterval,
            HomingDelay = homingDelay,
            HomingMoveType = homingMoveType,
            HomingTarget = target,
            TargetType = targetType,
            UseExplosionSfx = useExplosionSfx,
            ExplosionSfx = explosionSfx
        };

        projectile.Init(config);
    }

    private Vector3 GetProjectileSpawnPosition(Vector3 direction, int index, int count)
    {
        Vector3 spawnPos = GetSpawnPosition();

        if (direction.sqrMagnitude > 0.0001f)
            spawnPos += direction.normalized * fireOffset;

        if (count <= 1 || projectileSpacing <= 0f)
            return spawnPos;

        float centerIndex = (count - 1) * 0.5f;
        float offset = index - centerIndex;

        Vector3 right = Vector3.Cross(Vector3.up, direction.normalized).normalized;

        return spawnPos + right * offset * projectileSpacing;
    }

    private Vector3 GetAimDirection()
    {
        switch (aimType)
        {
            case ProjectileAimType.NearestEnemy:
            {
                Transform target = FindNearestTarget();

                if (target == null)
                    return ApplyDirectionSideError(GetForwardDirection());

                return GetDirectionTo(target);
            }

            case ProjectileAimType.RandomEnemy:
            {
                Transform target = FindRandomTarget();

                if (target == null)
                    return ApplyDirectionSideError(GetForwardDirection());

                return GetDirectionTo(target);
            }

            case ProjectileAimType.MoveDirection:
                return ApplyDirectionSideError(GetMoveDirection());

            case ProjectileAimType.RandomDirection:
                return GetRandomDirection();

            case ProjectileAimType.Circular:
                return ApplyDirectionSideError(GetForwardDirection());

            case ProjectileAimType.Forward:
            default:
                return ApplyDirectionSideError(GetForwardDirection());
        }
    }

    private Transform GetHomingTarget()
    {
        if (!isHoming)
            return null;

        if (aimType == ProjectileAimType.RandomEnemy)
            return FindRandomHomingTarget();

        return FindNearestHomingTarget();
    }

    private Transform FindNearestTarget()
    {
        if (targetType == TargetType.Player)
            return FindPlayerInRange(GetAttackRange());

        return FindNearestEnemyInRange(GetAttackRange());
    }

    private Transform FindNearestHomingTarget()
    {
        if (targetType == TargetType.Player)
            return FindPlayerInRange(GetHomingSearchRange());

        return FindNearestEnemyInRange(GetHomingSearchRange());
    }

    private Transform FindRandomTarget()
    {
        if (targetType == TargetType.Player)
            return FindPlayerInRange(GetAttackRange());

        return FindRandomEnemyInRange(GetAttackRange());
    }

    private Transform FindRandomHomingTarget()
    {
        if (targetType == TargetType.Player)
            return FindPlayerInRange(GetHomingSearchRange());

        return FindRandomEnemyInRange(GetHomingSearchRange());
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

            Vector3 diff = enemy.transform.position - transform.position;
            diff.y = 0f;

            float dist = diff.sqrMagnitude;

            if (dist >= nearestDistance)
                continue;

            nearestDistance = dist;
            nearest = enemy;
        }

        return nearest != null ? nearest.transform : null;
    }

    private Transform FindRandomEnemyInRange(float range)
    {
        Transform result = null;
        int validCount = 0;

        foreach (EnemyController enemy in EnemyRegistry.AliveEnemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead)
                continue;

            if (!IsTargetInRange(enemy.transform, range))
                continue;

            validCount++;

            if (Random.Range(0, validCount) == 0)
                result = enemy.transform;
        }

        return result;
    }

    private bool HasTargetInAttackRange()
    {
        if (targetType == TargetType.Player)
            return FindPlayerInRange(GetAttackRange()) != null;

        return FindNearestEnemyInRange(GetAttackRange()) != null;
    }

    private bool IsTargetInRange(Transform target, float range)
    {
        if (target == null)
            return false;

        if (range <= 0f)
            return true;

        Vector3 diff = target.position - transform.position;
        diff.y = 0f;

        return diff.sqrMagnitude <= range * range;
    }

    private Vector3 GetDirectionTo(Transform target)
    {
        Vector3 targetPosition = GetTargetPositionWithSideError(target);

        Vector3 dir = targetPosition - GetSpawnPosition();
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            return GetForwardDirection();

        return dir.normalized;
    }

    private Vector3 GetTargetPositionWithSideError(Transform target)
    {
        if (target == null)
            return GetSpawnPosition();

        Vector3 targetPosition = target.position;

        float errorRange = Mathf.Max(0f, targetSideErrorRange);

        if (errorRange <= 0f)
            return targetPosition;

        Vector3 toTarget = targetPosition - GetSpawnPosition();
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
            return targetPosition;

        Vector3 right = Vector3.Cross(Vector3.up, toTarget.normalized).normalized;
        float offset = Random.Range(-errorRange, errorRange);

        return targetPosition + right * offset;
    }

    private Vector3 GetSpawnPosition()
    {
        return firePoint != null ? firePoint.position : transform.position;
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

    private Vector3 ApplyDirectionSideError(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return GetForwardDirection();

        direction.Normalize();

        float errorRange = Mathf.Max(0f, targetSideErrorRange);

        if (errorRange <= 0f)
            return direction;

        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        float angle = Random.Range(-targetSideErrorRange, targetSideErrorRange);

        return Quaternion.Euler(0f, angle, 0f) * direction;
    }
    
    private Vector3 ApplySpread(Vector3 direction, int index, int count)
    {
        if (count <= 1 || spreadAngle <= 0f)
            return direction.normalized;

        float step = spreadAngle / (count - 1);
        float startAngle = -spreadAngle * 0.5f;
        float angle = startAngle + step * index;

        return Quaternion.Euler(0f, angle, 0f) * direction.normalized;
    }

    private Vector3 GetCircularDirection(int index, int count)
    {
        float angle = 360f / count * index;
        return Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
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

    private float GetAttackRange()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponRangeMultiplier;
            multiplier += inventoryBonus.RangeBonus;
        }

        return Mathf.Max(0f, attackRange * multiplier);
    }

    private float GetMinProjectileRange()
    {
        return Mathf.Max(0f, GetAttackRange() - Mathf.Max(0f, minRangeOffset));
    }

    private float GetMaxProjectileRange()
    {
        return Mathf.Max(GetMinProjectileRange(), GetAttackRange() + Mathf.Max(0f, maxRangeOffset));
    }

    private float GetRandomProjectileRange()
    {
        float minRange = GetMinProjectileRange();
        float maxRange = GetMaxProjectileRange();

        if (maxRange <= minRange)
            return maxRange;

        return Random.Range(minRange, maxRange);
    }

    private float GetProjectileSpeed()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponSpeedMultiplier;
            multiplier += inventoryBonus.ProjectileSpeedBonus;
        }

        return projectileSpeed * Mathf.Max(0.01f, multiplier);
    }

    private float GetProjectileLifeTime()
    {
        return projectileLifeTime;
    }

    private float GetProjectileScale()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponScaleMultiplier;
            multiplier += inventoryBonus.WeaponScaleBonus;
        }

        return projectileScale * Mathf.Max(0.01f, multiplier);
    }

    private float GetExplosionRadius()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponRangeMultiplier;
            multiplier += inventoryBonus.RangeBonus;
        }

        return explosionRadius * Mathf.Max(0.01f, multiplier);
    }

    private float GetHomingSearchRange()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponRangeMultiplier;
            multiplier += inventoryBonus.RangeBonus;
        }

        return homingSearchRange * Mathf.Max(0.01f, multiplier);
    }

    private void OnDrawGizmosSelected()
    {
        float attackRange = GetAttackRange();
        float minProjectileRange = GetMinProjectileRange();
        float maxProjectileRange = GetMaxProjectileRange();
        
        GizmoUtility.DrawCircleXZ(transform.position, attackRange, Color.green);
        
        if (minProjectileRange > 0f)
            GizmoUtility.DrawCircleXZ(transform.position, minProjectileRange, Color.gray);

        if (maxProjectileRange > attackRange)
            GizmoUtility.DrawCircleXZ(transform.position, maxProjectileRange, Color.black);

        Vector3 direction = GetForwardDirection();

        GizmoUtility.DrawArrow(
            transform.position,
            direction * maxProjectileRange,
            Color.yellow,
            4f);

        Vector3 endPosition = transform.position + direction * maxProjectileRange;

        if (targetSideErrorRange > 0f)
        {
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(
                endPosition - right * targetSideErrorRange,
                endPosition + right * targetSideErrorRange);
        }

        if (isExplosive)
        {
            GizmoUtility.DrawCircleXZ(
                endPosition,
                GetExplosionRadius(),
                Color.red);
        }

        if (isHoming)
        {
            GizmoUtility.DrawCircleXZ(
                endPosition,
                GetHomingSearchRange(),
                Color.cyan);
        }
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
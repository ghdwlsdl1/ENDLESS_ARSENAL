using System.Collections.Generic;
using UnityEngine;

public enum HomingMoveType
{
    [InspectorName("곡선 유도")]
    Smooth,
    [InspectorName("직선 유도")]
    Snap
}

public struct ProjectileConfig
{
    public Vector3 Direction;
    public float Damage;
    public bool IsCritical;
    public float Speed;
    public float MaxDistance;
    public float LifeTime;
    public string PoolKey;
    public float Scale;

    public bool IsPiercing;
    public int MaxPierceCount;

    public bool IsExplosive;
    public bool ExplodeOnExpire;
    public float ExplosionRadius;
    public float ExplosionDamageRatio;
    public bool UseExplosionSfx;
    public SfxType ExplosionSfx;

    public bool IsHoming;
    public bool CanRetarget;
    public float HomingRotateSpeed;
    public float HomingSearchRange;
    public float RetargetInterval;
    public float HomingDelay;
    public HomingMoveType HomingMoveType;
    public Transform HomingTarget;

    public TargetType TargetType;
}

public class Projectile : MonoBehaviour
{
    private readonly HashSet<IDamageable> hitTargets = new();

    private TargetType targetType;
    private Rigidbody rb;
    private Vector3 moveDir;
    private Vector3 startPosition;

    private float damage;
    private bool isCritical;
    private float speed;
    private float maxDistance;
    private float lifeTime;
    private float lifeTimer;
    private string poolKey;

    private bool isPiercing;
    private int maxPierceCount;
    private int pierceCount;

    private bool isExplosive;
    private bool explodeOnExpire;
    private float explosionRadius;
    private float explosionDamageRatio;

    private bool isHoming;
    private bool canRetarget;
    private float homingRotateSpeed;
    private float homingSearchRange;
    private float retargetInterval;
    private float retargetTimer;
    private Transform homingTarget;
    private float homingDelay;
    private float homingDelayTimer;
    private HomingMoveType homingMoveType;
    
    private bool useExplosionSfx;
    private SfxType explosionSfx;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void OnEnable()
    {
        lifeTimer = 0f;
        retargetTimer = 0f;
        homingDelayTimer = 0f;

        pierceCount = 0;
        hitTargets.Clear();
    }

    public void Init(ProjectileConfig config)
    {
        damage = config.Damage;
        isCritical = config.IsCritical;
        speed = Mathf.Max(0f, config.Speed);
        maxDistance = Mathf.Max(0f, config.MaxDistance);
        lifeTime = Mathf.Max(0.1f, config.LifeTime);
        poolKey = config.PoolKey;
        targetType = config.TargetType;

        isPiercing = config.IsPiercing;
        maxPierceCount = Mathf.Max(0, config.MaxPierceCount);

        isExplosive = config.IsExplosive;
        explodeOnExpire = config.ExplodeOnExpire;
        explosionRadius = Mathf.Max(0f, config.ExplosionRadius);
        explosionDamageRatio = Mathf.Max(0f, config.ExplosionDamageRatio);
        useExplosionSfx = config.UseExplosionSfx;
        explosionSfx = config.ExplosionSfx;

        isHoming = config.IsHoming;
        canRetarget = config.CanRetarget;
        homingRotateSpeed = Mathf.Max(0f, config.HomingRotateSpeed);
        homingSearchRange = Mathf.Max(0f, config.HomingSearchRange);
        retargetInterval = Mathf.Max(0.05f, config.RetargetInterval);
        homingDelay = Mathf.Max(0f, config.HomingDelay);
        homingMoveType = config.HomingMoveType;
        homingTarget = config.HomingTarget;

        Vector3 direction = config.Direction;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.forward;

        moveDir = direction.normalized;
        startPosition = transform.position;

        transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
        transform.localScale = Vector3.one * Mathf.Max(0.01f, config.Scale);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        UpdateHoming();
        Move();
        UpdateDistance();
        UpdateLifeTime();
    }

    private void UpdateHoming()
    {
        if (!isHoming)
            return;

        homingDelayTimer += Time.deltaTime;

        if (homingDelayTimer < homingDelay)
            return;

        if (homingMoveType == HomingMoveType.Snap)
        {
            UpdateSnapHoming();
            return;
        }

        UpdateSmoothHoming();
    }

    private void UpdateSmoothHoming()
    {
        if (!IsValidTarget(homingTarget))
            homingTarget = FindNearestTargetInHomingRange();

        if (!IsValidTarget(homingTarget))
            return;

        if (canRetarget)
        {
            retargetTimer += Time.deltaTime;

            if (retargetTimer >= retargetInterval)
            {
                retargetTimer = 0f;

                Transform newTarget = FindNearestTargetInHomingRange();

                if (newTarget != null)
                    homingTarget = newTarget;
            }
        }

        Vector3 targetDir = homingTarget.position - transform.position;
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude <= 0.0001f)
            return;

        moveDir = Vector3.RotateTowards(
            moveDir,
            targetDir.normalized,
            homingRotateSpeed * Mathf.Deg2Rad * Time.deltaTime,
            0f);

        moveDir.Normalize();

        transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
    }

    private void UpdateSnapHoming()
    {
        retargetTimer += Time.deltaTime;

        if (retargetTimer < retargetInterval)
            return;

        retargetTimer = 0f;

        Transform target = FindNearestTargetInHomingRange();

        if (!IsValidTarget(target))
            return;

        Vector3 targetDir = target.position - transform.position;
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude <= 0.0001f)
            return;

        homingTarget = target;
        moveDir = targetDir.normalized;

        transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
    }

    private void Move()
    {
        transform.position += moveDir * speed * Time.deltaTime;
    }

    private void UpdateDistance()
    {
        if (maxDistance <= 0f)
            return;

        if (isHoming && IsValidTarget(homingTarget))
            return;

        Vector3 moved = transform.position - startPosition;
        moved.y = 0f;

        if (moved.sqrMagnitude < maxDistance * maxDistance)
            return;

        ReturnAfterExpire();
    }

    private void UpdateLifeTime()
    {
        lifeTimer += Time.deltaTime;

        if (lifeTimer < lifeTime)
            return;

        ReturnAfterExpire();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsTargetCollider(other))
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null || damageable.IsDead)
            return;

        if (hitTargets.Contains(damageable))
            return;

        hitTargets.Add(damageable);

        damageable.TakeDamage(damage, isCritical);

        if (isExplosive)
            Explode(damageable);

        if (!isPiercing)
        {
            ReturnToPool();
            return;
        }

        pierceCount++;

        if (pierceCount > maxPierceCount)
            ReturnToPool();
    }

    private bool IsTargetCollider(Collider other)
    {
        if (other == null)
            return false;

        if (targetType == TargetType.Enemy)
            return other.CompareTag("Enemy");

        return other.CompareTag("Player");
    }

    private void ReturnAfterExpire()
    {
        if (isExplosive && explodeOnExpire)
            Explode(null);

        ReturnToPool();
    }

    private static readonly Collider[] explosionHitsBuffer = new Collider[64];

    private void Explode(IDamageable directHitTarget)
    {
        if (explosionRadius <= 0f)
            return;
        
        if (useExplosionSfx && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(explosionSfx);

        EffectManager.Play(
            "Explosion1",
            transform.position,
            Quaternion.identity,
            Vector3.one * explosionRadius);

        if (explosionDamageRatio <= 0f)
            return;

        float explosionDamage = damage * explosionDamageRatio;

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            explosionRadius,
            explosionHitsBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = explosionHitsBuffer[i];

            if (!IsTargetCollider(hit))
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null || damageable.IsDead)
                continue;

            if (damageable == directHitTarget)
                continue;

            damageable.TakeDamage(explosionDamage, isCritical);
        }
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

    private Transform FindNearestTargetInHomingRange()
    {
        if (targetType == TargetType.Player)
        {
            Transform player = GetCachedPlayerTransform();

            if (player == null)
                return null;

            if (homingSearchRange <= 0f)
                return player;

            Vector3 playerDiff = player.position - transform.position;
            playerDiff.y = 0f;

            if (playerDiff.sqrMagnitude > homingSearchRange * homingSearchRange)
                return null;

            return player;
        }

        EnemyController enemy = EnemyRegistry.GetNearest(transform.position);

        if (enemy == null)
            return null;

        if (homingSearchRange <= 0f)
            return enemy.transform;

        Vector3 enemyDiff = enemy.transform.position - transform.position;
        enemyDiff.y = 0f;

        if (enemyDiff.sqrMagnitude > homingSearchRange * homingSearchRange)
            return null;

        return enemy.transform;
    }

    private bool IsValidTarget(Transform target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        if (targetType == TargetType.Enemy)
            return target.CompareTag("Enemy");

        return target.CompareTag("Player");
    }

    private void ReturnToPool()
    {
        moveDir = Vector3.zero;
        homingTarget = null;
        hitTargets.Clear();

        if (!string.IsNullOrEmpty(poolKey))
            ObjectPool.Return(poolKey, gameObject);
        else
            gameObject.SetActive(false);
    }
}
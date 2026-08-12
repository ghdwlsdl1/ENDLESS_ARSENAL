using System.Collections.Generic;
using UnityEngine;

public class OrbitWeapon : MonoBehaviour, IInventoryBonusReceiver
{
    public enum OrbitMovePattern
    {
        [InspectorName("원형")] Circle,
        [InspectorName("반지름 진동")] RadiusWave,
        [InspectorName("타원")] Ellipse,
        [InspectorName("흔들림")] Wobble,
        [InspectorName("꽃잎")] Flower
    }

    [InspectorLabel("공격 대상")]
    [SerializeField] private TargetType targetType = TargetType.Enemy;

    [InspectorLabel("회전 중심")]
    [SerializeField] private Transform center;

    [InspectorLabel("무기 프리팹")]
    [SerializeField] private GameObject weaponPrefab;

    [InspectorLabel("무기 개수")]
    [SerializeField] private int weaponCount;

    [InspectorLabel("최대 무기 개수")]
    [SerializeField] private int maxWeaponCount;

    [InspectorLabel("기본 공격력")]
    [SerializeField] private float baseDamage;

    [InspectorLabel("공격력 반영 비율")]
    [SerializeField] private float statDamageMultiplier;

    [InspectorLabel("회전 반지름")]
    [SerializeField] private float radius;

    [InspectorLabel("회전 속도")]
    [SerializeField] private float rotateSpeed;

    [InspectorLabel("무기 크기")]
    [SerializeField] private float weaponScale;

    [Header("변칙 회전")]
    [InspectorLabel("회전 패턴")]
    [SerializeField] private OrbitMovePattern movePattern = OrbitMovePattern.Circle;

    [InspectorLabel("반지름 변화량", "movePattern", OrbitMovePattern.RadiusWave)]
    [SerializeField] private float radiusWaveAmount;

    [InspectorLabel("반지름 변화 속도", "movePattern", OrbitMovePattern.RadiusWave)]
    [SerializeField] private float radiusWaveSpeed;

    [InspectorLabel("타원 X 배율", "movePattern", OrbitMovePattern.Ellipse)]
    [SerializeField] private float ellipseXMultiplier;

    [InspectorLabel("타원 Z 배율", "movePattern", OrbitMovePattern.Ellipse)]
    [SerializeField] private float ellipseZMultiplier;

    [InspectorLabel("흔들림 각도", "movePattern", OrbitMovePattern.Wobble)]
    [SerializeField] private float wobbleAngleAmount;

    [InspectorLabel("흔들림 속도", "movePattern", OrbitMovePattern.Wobble)]
    [SerializeField] private float wobbleSpeed;

    [InspectorLabel("꽃잎 개수", "movePattern", OrbitMovePattern.Flower)]
    [SerializeField] private int flowerPetalCount;

    [InspectorLabel("꽃잎 강도", "movePattern", OrbitMovePattern.Flower)]
    [SerializeField] private float flowerAmount ;
    
    [Header("사운드")]

    [InspectorLabel("효과음 사용")]
    [SerializeField] private bool useHitSfx;
    
    [InspectorLabel("타격 효과음")]
    [SerializeField] private SfxType hitSfx;

    [InspectorLabel("효과음 간격")]
    [SerializeField] private float sfxInterval;

    private float sfxTimer;

    private readonly List<Transform> weapons = new();
    private readonly List<Vector3> weaponBaseScales = new();

    private float angle;
    private WeaponInventoryBonus inventoryBonus;

    private PlayerStat playerStat;
    private EnemyStat enemyStat;
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        CacheOwnerReferences();

        if (center == null)
            center = transform.root;
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
        RefreshWeapons();
    }

    private void OnDisable()
    {
        ClearWeapons();
    }

    public void SetInventoryBonus(WeaponInventoryBonus bonus)
    {
        int previousCount = weapons.Count;

        inventoryBonus = bonus;
        
        if (GetTargetWeaponCount() != previousCount)
            RefreshWeapons();
    }

    private int GetTargetWeaponCount()
    {
        int count = weaponCount;

        if (targetType == TargetType.Enemy)
            count += inventoryBonus.ProjectileCountBonus;

        return Mathf.Clamp(count, 0, maxWeaponCount);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        sfxTimer += Time.deltaTime;

        if (weapons.Count <= 0)
            return;

        angle += GetRotateSpeed() * Time.deltaTime;

        float currentScale = GetWeaponScale();

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null)
                continue;

            Vector3 offset = GetOrbitOffset(i);
            weapons[i].position = center.position + offset;

            if (offset != Vector3.zero)
                weapons[i].forward = offset.normalized;

            if (i < weaponBaseScales.Count)
                weapons[i].localScale = weaponBaseScales[i] * currentScale;
        }
    }

    private Vector3 GetOrbitOffset(int index)
    {
        float spacing = 360f / weapons.Count;
        float currentAngle = angle + spacing * index;

        if (movePattern == OrbitMovePattern.Wobble)
            currentAngle += Mathf.Sin(Time.time * wobbleSpeed + index) * wobbleAngleAmount;

        float radian = currentAngle * Mathf.Deg2Rad;
        float currentRadius = GetRadius();

        if (movePattern == OrbitMovePattern.RadiusWave)
            currentRadius += Mathf.Sin(Time.time * radiusWaveSpeed + index) * radiusWaveAmount;
        else if (movePattern == OrbitMovePattern.Flower)
            currentRadius += Mathf.Cos(radian * flowerPetalCount) * flowerAmount;

        currentRadius = Mathf.Max(0.1f, currentRadius);

        float x = Mathf.Cos(radian) * currentRadius;
        float z = Mathf.Sin(radian) * currentRadius;

        if (movePattern == OrbitMovePattern.Ellipse)
        {
            x *= ellipseXMultiplier;
            z *= ellipseZMultiplier;
        }

        return new Vector3(x, 0f, z);
    }

    public (float damage, bool isCritical) GetDamage()
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

        return radius * Mathf.Max(0.01f, multiplier);
    }

    private float GetRotateSpeed()
    {
        float speedMultiplier = 1f;
        float attackSpeedMultiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            speedMultiplier = playerStat.WeaponSpeedMultiplier;
            attackSpeedMultiplier = playerStat.WeaponAttackSpeedMultiplier;

            speedMultiplier += inventoryBonus.ProjectileSpeedBonus;
            attackSpeedMultiplier += inventoryBonus.AttackSpeedBonus;
        }

        return rotateSpeed *
               Mathf.Max(0.01f, speedMultiplier) *
               Mathf.Max(0.01f, attackSpeedMultiplier);
    }

    private float GetWeaponScale()
    {
        float multiplier = 1f;

        if (targetType == TargetType.Enemy && playerStat != null)
        {
            multiplier = playerStat.WeaponScaleMultiplier;
            multiplier += inventoryBonus.WeaponScaleBonus;
        }

        return weaponScale * Mathf.Max(0.01f, multiplier);
    }

    public IDamageable GetDamageTarget(Collider other)
    {
        if (other == null)
            return null;

        if (other.transform.root == transform.root)
            return null;

        if (targetType == TargetType.Enemy)
        {
            if (!other.CompareTag("Enemy"))
                return null;
        }
        else
        {
            if (!other.CompareTag("Player"))
                return null;
        }

        if (IsWeaponCollider(other))
            return null;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        return damageable != null && !damageable.IsDead ? damageable : null;
    }

    private bool IsWeaponCollider(Collider other)
    {
        if (other == null)
            return true;

        if (other.GetComponentInParent<OrbitWeaponHitBox>() != null)
            return true;

        return false;
    }

    public bool TryDamageTarget(IDamageable target)
    {
        if (target == null || target.IsDead)
            return false;

        (float damage, bool isCritical) = GetDamage();

        target.TakeDamage(damage, isCritical);
        PlayHitSound();

        return true;
    }

    private void RefreshWeapons()
    {
        ClearWeapons();

        if (weaponPrefab == null)
            return;

        int count = GetTargetWeaponCount();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(weaponPrefab, transform);
            obj.SetActive(true);

            if (obj.TryGetComponent<OrbitWeaponHitBox>(out var hitBox))
                hitBox.Init(this);

            weapons.Add(obj.transform);
            weaponBaseScales.Add(obj.transform.localScale);
        }
    }

    private void ClearWeapons()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
                Destroy(weapons[i].gameObject);
        }

        weapons.Clear();
        weaponBaseScales.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Transform drawCenter = center != null ? center : transform;
        GizmoUtility.DrawCircleXZ(drawCenter.position, GetRadius(), Color.cyan);
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
}
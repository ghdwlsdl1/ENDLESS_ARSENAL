using System;
using UnityEngine;

public struct InventoryStatBonus
{
    public float MaxHpBonus;
    public float HpRegenBonus;
    public float MoveSpeedBonus;
    public float ExpAttractRangeBonus;
    public float CameraFovBonus;

    public void AddEffect(EquipmentEffectType effectType, float value)
    {
        switch (effectType)
        {
            case EquipmentEffectType.MaxHp:
                MaxHpBonus += value;
                break;

            case EquipmentEffectType.HpRegen:
                HpRegenBonus += value;
                break;

            case EquipmentEffectType.MoveSpeed:
                MoveSpeedBonus += value;
                break;

            case EquipmentEffectType.ExpAttractRange:
                ExpAttractRangeBonus += value;
                break;

            case EquipmentEffectType.CameraSize:
                CameraFovBonus += value;
                break;
        }
    }
}

public struct CharacterBaseStats
{
    public float MaxHp;
    public float HpRegenPerSecond;
    public float MoveSpeed;
    public float Damage;
    public float ExpAttractRange;

    public float WeaponDamageMultiplier;
    public float WeaponAttackSpeedMultiplier;
    public float WeaponRangeMultiplier;
    public float WeaponScaleMultiplier;
    public float WeaponSpeedMultiplier;

    public float CriticalChance;
    public float CriticalDamageMultiplier;
}

public class PlayerStat : MonoBehaviour
{
    [Header("플레이어 스탯")]

    [InspectorLabel("최대 체력")]
    [SerializeField] private float maxHp;

    [InspectorLabel("초당 체력 회복량")]
    [SerializeField] private float hpRegenPerSecond;

    [InspectorLabel("이동 속도")]
    [SerializeField] private float moveSpeed;

    [InspectorLabel("공격력")]
    [SerializeField] private float damage;

    [InspectorLabel("경험치 흡수 범위")]
    [SerializeField] private float expAttractRange;

    [InspectorLabel("카메라 시야(FOV)")]
    [SerializeField] private float baseCameraFov;


    [Header("무기 보정 스탯")]

    [InspectorLabel("피해 배율")]
    [SerializeField] private float weaponDamageMultiplier;

    [InspectorLabel("공격 속도 배율")]
    [SerializeField] private float weaponAttackSpeedMultiplier;

    [InspectorLabel("사정 거리 배율")]
    [SerializeField] private float weaponRangeMultiplier;

    [InspectorLabel("공격 범위 배율")]
    [SerializeField] private float weaponScaleMultiplier;
    
    [InspectorLabel("치명타 확률")]
    [SerializeField] private float criticalChance;
    
    [Header("특수 스탯")]
    [InspectorLabel("투사체 속도 배율")]
    [SerializeField] private float weaponSpeedMultiplier;
    
    [InspectorLabel("치명타 피해 배율")]
    [SerializeField] private float criticalDamageMultiplier;

    private InventoryStatBonus inventoryBonus;

    public float MaxHp => Mathf.Max(1f, maxHp + inventoryBonus.MaxHpBonus);
    public float HpRegenPerSecond => hpRegenPerSecond + inventoryBonus.HpRegenBonus;
    public float MoveSpeed => moveSpeed + inventoryBonus.MoveSpeedBonus;
    public float Damage => damage;
    public float ExpAttractRange => expAttractRange + inventoryBonus.ExpAttractRangeBonus;
    public float CameraFov => Mathf.Clamp(baseCameraFov + inventoryBonus.CameraFovBonus, 10f, 170f);

    public float WeaponDamageMultiplier => weaponDamageMultiplier;
    public float WeaponAttackSpeedMultiplier => weaponAttackSpeedMultiplier;
    public float WeaponRangeMultiplier => weaponRangeMultiplier;
    public float WeaponScaleMultiplier => weaponScaleMultiplier;
    public float CriticalChance => criticalChance;
    
    public float WeaponSpeedMultiplier => weaponSpeedMultiplier;
    public float CriticalDamageMultiplier => criticalDamageMultiplier;

    public event Action OnStatChanged;
    public event Action OnMaxHpChanged;

    public void SetInventoryBonus(InventoryStatBonus bonus)
    {
        float beforeMaxHp = MaxHp;

        inventoryBonus = bonus;

        if (!Mathf.Approximately(beforeMaxHp, MaxHp))
            OnMaxHpChanged?.Invoke();

        OnStatChanged?.Invoke();
    }

    public void ClearInventoryBonus()
    {
        SetInventoryBonus(new InventoryStatBonus());
    }
    
    public void SetBaseStats(CharacterBaseStats stats)
    {
        float beforeMaxHp = MaxHp;

        maxHp = stats.MaxHp;
        hpRegenPerSecond = stats.HpRegenPerSecond;
        moveSpeed = stats.MoveSpeed;
        damage = stats.Damage;
        expAttractRange = stats.ExpAttractRange;

        weaponDamageMultiplier = stats.WeaponDamageMultiplier;
        weaponAttackSpeedMultiplier = stats.WeaponAttackSpeedMultiplier;
        weaponRangeMultiplier = stats.WeaponRangeMultiplier;
        weaponSpeedMultiplier = stats.WeaponSpeedMultiplier;
        weaponScaleMultiplier = stats.WeaponScaleMultiplier;

        criticalChance = Mathf.Clamp01(stats.CriticalChance);
        criticalDamageMultiplier = Mathf.Max(1f, stats.CriticalDamageMultiplier);

        if (!Mathf.Approximately(beforeMaxHp, MaxHp))
            OnMaxHpChanged?.Invoke();

        OnStatChanged?.Invoke();
    }

    public void AddMaxHp(float value)
    {
        if (value <= 0)
            return;

        maxHp += value;

        OnMaxHpChanged?.Invoke();
        OnStatChanged?.Invoke();
    }

    public void AddHpRegen(float value)
    {
        hpRegenPerSecond += value;
        OnStatChanged?.Invoke();
    }

    public void AddMoveSpeed(float value)
    {
        moveSpeed += value;
        OnStatChanged?.Invoke();
    }

    public void AddDamage(float value)
    {
        damage += value;
        OnStatChanged?.Invoke();
    }

    public void AddExpAttractRange(float value)
    {
        expAttractRange += value;
        OnStatChanged?.Invoke();
    }

    public void AddCameraFov(float value)
    {
        baseCameraFov = Mathf.Clamp(baseCameraFov + value, 10f, 170f);
        OnStatChanged?.Invoke();
    }

    public void AddWeaponDamageMultiplier(float value)
    {
        weaponDamageMultiplier = Mathf.Max(0.01f, weaponDamageMultiplier + value);
        OnStatChanged?.Invoke();
    }

    public void AddWeaponAttackSpeedMultiplier(float value)
    {
        weaponAttackSpeedMultiplier = Mathf.Max(0.01f, weaponAttackSpeedMultiplier + value);
        OnStatChanged?.Invoke();
    }

    public void AddWeaponRangeMultiplier(float value)
    {
        weaponRangeMultiplier = Mathf.Max(0.01f, weaponRangeMultiplier + value);
        OnStatChanged?.Invoke();
    }

    public void AddWeaponSpeedMultiplier(float value)
    {
        weaponSpeedMultiplier = Mathf.Max(0.01f, weaponSpeedMultiplier + value);
        OnStatChanged?.Invoke();
    }
    
    public void AddCriticalChance(float value)
    {
        criticalChance = Mathf.Clamp01(criticalChance + value);
        OnStatChanged?.Invoke();
    }
    
    public void AddWeaponScaleMultiplier(float value)
    {
        weaponScaleMultiplier = Mathf.Max(0.01f, weaponScaleMultiplier + value);
        OnStatChanged?.Invoke();
    }
    
    public void AddCriticalDamageMultiplier(float value)
    {
        criticalDamageMultiplier = Mathf.Max(1f, criticalDamageMultiplier + value);
        OnStatChanged?.Invoke();
    }
}
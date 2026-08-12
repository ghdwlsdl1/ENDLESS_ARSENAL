using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    ProjectileWeapon,
    OrbitWeapon,
    AuraWeapon,
    PulseWeapon
}

public interface IInventoryBonusReceiver
{
    void SetInventoryBonus(WeaponInventoryBonus bonus);
}

public struct WeaponInventoryBonus
{
    public float DamageBonus;
    public float AttackSpeedBonus;
    public float RangeBonus;
    public float ProjectileSpeedBonus;
    public float WeaponScaleBonus;
    public int ProjectileCountBonus;

    public void AddEffect(EquipmentEffectType effectType, float value)
    {
        switch (effectType)
        {
            case EquipmentEffectType.Damage:
                DamageBonus += value;
                break;

            case EquipmentEffectType.AttackSpeed:
                AttackSpeedBonus += value;
                break;

            case EquipmentEffectType.Range:
                RangeBonus += value;
                break;

            case EquipmentEffectType.ProjectileSpeed:
                ProjectileSpeedBonus += value;
                break;

            case EquipmentEffectType.ProjectileCount:
                ProjectileCountBonus += Mathf.RoundToInt(value);
                break;

            case EquipmentEffectType.WeaponScale:
                WeaponScaleBonus += value;
                break;
        }
    }
}

public class WeaponManager : MonoBehaviour
{
    private class EquippedWeapon
    {
        public InventoryItem Item;
        public WeaponType WeaponType;
        public GameObject Instance;
        public Behaviour WeaponBehaviour;
    }

    [Header("생성 위치")]

    [InspectorLabel("무기 부모")]
    [SerializeField] private Transform weaponRoot;

    private readonly List<EquippedWeapon> equippedWeapons = new();

    private void Awake()
    {
        if (weaponRoot == null)
            weaponRoot = transform;
    }

    public void ApplyInventoryWeapons(List<InventoryItem> weaponItems)
    {
        weaponItems ??= new List<InventoryItem>();

        for (int i = equippedWeapons.Count - 1; i >= 0; i--)
        {
            EquippedWeapon equipped = equippedWeapons[i];

            if (equipped != null && weaponItems.Contains(equipped.Item))
                continue;

            RemoveEquippedWeapon(equipped);
            equippedWeapons.RemoveAt(i);
        }

        foreach (InventoryItem item in weaponItems)
        {
            if (item == null || item.Data == null)
                continue;

            if (item.Data.ItemType != InventoryItemType.Weapon)
                continue;

            if (IsAlreadyEquipped(item))
                continue;

            EquipWeapon(item);
        }
    }

    private bool IsAlreadyEquipped(InventoryItem item)
    {
        foreach (EquippedWeapon equipped in equippedWeapons)
        {
            if (equipped != null && equipped.Item == item)
                return true;
        }

        return false;
    }

    private void RemoveEquippedWeapon(EquippedWeapon equipped)
    {
        if (equipped == null)
            return;

        if (equipped.WeaponBehaviour is IInventoryBonusReceiver receiver)
            receiver.SetInventoryBonus(new WeaponInventoryBonus());

        if (equipped.Instance != null)
            Destroy(equipped.Instance);
    }

    private void EquipWeapon(InventoryItem item)
    {
        string path = item.Data.WeaponPrefabPath;

        if (string.IsNullOrEmpty(path))
            return;

        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab, weaponRoot);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Behaviour weaponBehaviour = GetWeaponBehaviour(instance);

        if (weaponBehaviour == null)
        {
            Destroy(instance);
            return;
        }

        weaponBehaviour.enabled = true;

        equippedWeapons.Add(new EquippedWeapon
        {
            Item = item,
            WeaponType = item.Data.WeaponType,
            Instance = instance,
            WeaponBehaviour = weaponBehaviour
        });
    }

    private Behaviour GetWeaponBehaviour(GameObject instance)
    {
        if (instance == null)
            return null;

        ProjectileWeapon projectileWeapon = instance.GetComponentInChildren<ProjectileWeapon>(true);
        if (projectileWeapon != null)
            return projectileWeapon;

        OrbitWeapon orbitWeapon = instance.GetComponentInChildren<OrbitWeapon>(true);
        if (orbitWeapon != null)
            return orbitWeapon;

        AuraWeapon auraWeapon = instance.GetComponentInChildren<AuraWeapon>(true);
        if (auraWeapon != null)
            return auraWeapon;

        PulseWeapon pulseWeapon = instance.GetComponentInChildren<PulseWeapon>(true);
        if (pulseWeapon != null)
            return pulseWeapon;

        return null;
    }

    public void ApplyInventoryWeaponBonuses(Dictionary<InventoryItem, WeaponInventoryBonus> bonuses)
    {
        ClearInventoryBonuses();

        if (bonuses == null)
            return;

        foreach (EquippedWeapon equippedWeapon in equippedWeapons)
        {
            if (equippedWeapon == null || equippedWeapon.WeaponBehaviour == null)
                continue;

            if (!bonuses.TryGetValue(equippedWeapon.Item, out WeaponInventoryBonus bonus))
                continue;

            if (equippedWeapon.WeaponBehaviour is IInventoryBonusReceiver receiver)
                receiver.SetInventoryBonus(bonus);
        }
    }

    private void ClearInventoryBonuses()
    {
        foreach (EquippedWeapon equippedWeapon in equippedWeapons)
        {
            if (equippedWeapon == null || equippedWeapon.WeaponBehaviour == null)
                continue;

            if (equippedWeapon.WeaponBehaviour is IInventoryBonusReceiver receiver)
                receiver.SetInventoryBonus(new WeaponInventoryBonus());
        }
    }

    public void ClearEquippedWeapons()
    {
        ClearInventoryBonuses();

        foreach (EquippedWeapon equippedWeapon in equippedWeapons)
        {
            if (equippedWeapon == null || equippedWeapon.Instance == null)
                continue;

            Destroy(equippedWeapon.Instance);
        }

        equippedWeapons.Clear();
    }

    public List<WeaponType> GetOwnedWeaponTypes()
    {
        List<WeaponType> result = new();

        foreach (EquippedWeapon equippedWeapon in equippedWeapons)
        {
            if (equippedWeapon == null)
                continue;

            if (result.Contains(equippedWeapon.WeaponType))
                continue;

            result.Add(equippedWeapon.WeaponType);
        }

        return result;
    }

    public bool HasWeapon(WeaponType type)
    {
        foreach (EquippedWeapon equippedWeapon in equippedWeapons)
        {
            if (equippedWeapon == null)
                continue;

            if (equippedWeapon.WeaponType == type)
                return true;
        }

        return false;
    }
}
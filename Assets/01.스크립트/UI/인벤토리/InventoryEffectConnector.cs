using System.Collections.Generic;
using UnityEngine;

public class InventoryEffectConnector : MonoBehaviour
{
    [Header("연결")]

    [InspectorLabel("인벤토리")]
    [SerializeField] private Inventory inventory;

    [InspectorLabel("인벤토리 UI")]
    [SerializeField] private InventoryUI inventoryUI;

    [InspectorLabel("무기 매니저")]
    [SerializeField] private WeaponManager weaponManager;

    [InspectorLabel("플레이어 스탯")]
    [SerializeField] private PlayerStat playerStat;

    private readonly Dictionary<InventoryItem, WeaponInventoryBonus> weaponBonuses = new();
    private readonly List<InventoryItem> equippedWeapons = new();

    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>();

        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();

        if (playerStat == null)
            playerStat = FindFirstObjectByType<PlayerStat>();
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshEffects;

        RefreshEffects();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshEffects;
    }
    
    public WeaponInventoryBonus GetWeaponBonus(InventoryItem item)
    {
        if (item == null)
            return new WeaponInventoryBonus();

        return weaponBonuses.TryGetValue(item, out WeaponInventoryBonus bonus)
            ? bonus
            : new WeaponInventoryBonus();
    }

    public void RefreshEffects()
    {
        weaponBonuses.Clear();
        equippedWeapons.Clear();

        InventoryStatBonus statBonus = new InventoryStatBonus();

        if (inventory == null)
        {
            if (playerStat != null)
                playerStat.ClearInventoryBonus();

            if (weaponManager != null)
                weaponManager.ClearEquippedWeapons();

            return;
        }

        if (weaponManager == null)
        {
            if (playerStat != null)
                playerStat.SetInventoryBonus(statBonus);

            return;
        }

        List<InventoryItem> placedItems = inventory.GetPlacedItems();

        foreach (InventoryItem item in placedItems)
        {
            if (item == null || item.Data == null)
                continue;

            if (item.Data.ItemType != InventoryItemType.Weapon)
                continue;

            equippedWeapons.Add(item);
        }

        weaponManager.ApplyInventoryWeapons(equippedWeapons);

        foreach (InventoryItem item in placedItems)
        {
            if (item == null || item.Data == null)
                continue;

            if (item.Data.ItemType != InventoryItemType.Equipment)
                continue;

            ApplyEquipmentEffects(item, ref statBonus);
        }

        weaponManager.ApplyInventoryWeaponBonuses(weaponBonuses);

        if (playerStat != null)
            playerStat.SetInventoryBonus(statBonus);
    }

    private void ApplyEquipmentEffects(InventoryItem equipmentItem, ref InventoryStatBonus statBonus)
    {
        EquipmentData equipmentData = equipmentItem.Data.EquipmentData;

        if (equipmentData == null)
            return;

        bool conditionPassed =
            IsPositionConditionPassed(equipmentItem, equipmentData.PositionConditions) &&
            IsSearchConditionPassed(equipmentItem, equipmentData.SearchConditions);

        SetItemConditionVisual(equipmentItem, conditionPassed);

        if (!conditionPassed)
            return;

        if (equipmentData.Effects == null || equipmentData.Effects.Length == 0)
            return;

        AddEffectsToPlayerStat(ref statBonus, equipmentData.Effects);

        List<InventoryItem> targetWeapons = GetEffectTargetWeapons(equipmentItem, equipmentData.SearchConditions);

        foreach (InventoryItem targetWeapon in targetWeapons)
        {
            if (targetWeapon == null || targetWeapon.Data == null)
                continue;

            AddEffectsToWeapon(targetWeapon, equipmentData.Effects);
        }
    }

    private void AddEffectsToPlayerStat(ref InventoryStatBonus statBonus, EquipmentEffectData[] effects)
    {
        foreach (EquipmentEffectData effect in effects)
        {
            if (effect == null)
                continue;

            if (!IsPlayerStatEffect(effect.EffectType))
                continue;

            statBonus.AddEffect(effect.EffectType, effect.EffectValue);
        }
    }

    private bool IsPlayerStatEffect(EquipmentEffectType effectType)
    {
        switch (effectType)
        {
            case EquipmentEffectType.MaxHp:
            case EquipmentEffectType.HpRegen:
            case EquipmentEffectType.MoveSpeed:
            case EquipmentEffectType.ExpAttractRange:
            case EquipmentEffectType.CameraSize:
                return true;

            default:
                return false;
        }
    }

    private bool IsWeaponEffect(EquipmentEffectType effectType)
    {
        switch (effectType)
        {
            case EquipmentEffectType.Damage:
            case EquipmentEffectType.AttackSpeed:
            case EquipmentEffectType.Range:
            case EquipmentEffectType.ProjectileSpeed:
            case EquipmentEffectType.ProjectileCount:
            case EquipmentEffectType.WeaponScale:
                return true;

            default:
                return false;
        }
    }

    private void AddEffectsToWeapon(InventoryItem weaponItem, EquipmentEffectData[] effects)
    {
        if (weaponItem == null)
            return;

        if (!weaponBonuses.TryGetValue(weaponItem, out WeaponInventoryBonus bonus))
            bonus = new WeaponInventoryBonus();

        foreach (EquipmentEffectData effect in effects)
        {
            if (effect == null)
                continue;

            if (!IsWeaponEffect(effect.EffectType))
                continue;

            bonus.AddEffect(effect.EffectType, effect.EffectValue);
        }

        weaponBonuses[weaponItem] = bonus;
    }

    private List<InventoryItem> GetEffectTargetWeapons(InventoryItem equipmentItem, EquipmentSearchConditionData[] searchConditions)
    {
        List<InventoryItem> result = new();

        if (searchConditions == null || searchConditions.Length == 0)
        {
            AddAllPlacedWeapons(result);
            return result;
        }

        foreach (EquipmentSearchConditionData condition in searchConditions)
        {
            if (condition == null)
                continue;

            List<InventoryItem> placedItems = inventory.GetPlacedItems();

            foreach (InventoryItem target in placedItems)
            {
                if (target == null || target.Data == null)
                    continue;

                if (target == equipmentItem)
                    continue;

                if (target.Data.ItemType != InventoryItemType.Weapon)
                    continue;

                if (!IsInConditionRange(equipmentItem, target, condition))
                    continue;

                if (result.Contains(target))
                    continue;

                result.Add(target);
            }
        }

        return result;
    }

    private void AddAllPlacedWeapons(List<InventoryItem> result)
    {
        List<InventoryItem> placedItems = inventory.GetPlacedItems();

        foreach (InventoryItem item in placedItems)
        {
            if (item == null || item.Data == null)
                continue;

            if (item.Data.ItemType != InventoryItemType.Weapon)
                continue;

            if (result.Contains(item))
                continue;

            result.Add(item);
        }
    }

    private void SetItemConditionVisual(InventoryItem item, bool conditionPassed)
    {
        if (inventoryUI == null)
            return;

        InventoryItemUI itemUI = inventoryUI.GetItemUI(item);

        if (itemUI != null)
            itemUI.SetConditionActive(conditionPassed);
    }

    private bool IsPositionConditionPassed(InventoryItem item, EquipmentPositionConditionData[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        if (!inventory.TryGetItemPosition(item, out int x, out int y))
            return false;

        foreach (EquipmentPositionConditionData condition in conditions)
        {
            if (condition == null)
                continue;

            if (!IsPositionConditionPassed(item, x, y, condition.ConditionType))
                return false;
        }

        return true;
    }

    private bool IsPositionConditionPassed(InventoryItem item, int x, int y, EquipmentPositionConditionType conditionType)
    {
        switch (conditionType)
        {
            case EquipmentPositionConditionType.TopRow:
                return y == inventory.GetUnlockedTopRow();

            case EquipmentPositionConditionType.BottomRow:
                return y + item.Height - 1 == inventory.GetUnlockedBottomRow();

            case EquipmentPositionConditionType.LeftColumn:
                return x == inventory.GetUnlockedLeftColumn();

            case EquipmentPositionConditionType.RightColumn:
                return x + item.Width - 1 == inventory.GetUnlockedRightColumn();

            default:
                return false;
        }
    }

    private bool IsSearchConditionPassed(InventoryItem item, EquipmentSearchConditionData[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        foreach (EquipmentSearchConditionData condition in conditions)
        {
            if (condition == null)
                continue;

            int count = GetConditionTargetCount(item, condition);

            if (count < condition.RequiredCount)
                return false;
        }

        return true;
    }

    private int GetConditionTargetCount(InventoryItem item, EquipmentSearchConditionData condition)
    {
        List<InventoryItem> placedItems = inventory.GetPlacedItems();
        int count = 0;

        foreach (InventoryItem target in placedItems)
        {
            if (target == null || target.Data == null)
                continue;

            if (target == item)
                continue;

            if (!IsTargetTypeMatched(target, condition.Target))
                continue;

            if (!IsInConditionRange(item, target, condition))
                continue;

            count++;
        }

        return count;
    }

    private bool IsTargetTypeMatched(InventoryItem target, EquipmentConditionTarget conditionTarget)
    {
        switch (conditionTarget)
        {
            case EquipmentConditionTarget.Weapon:
                return target.Data.ItemType == InventoryItemType.Weapon;

            case EquipmentConditionTarget.Equipment:
                return target.Data.ItemType == InventoryItemType.Equipment;

            case EquipmentConditionTarget.AnyItem:
                return true;

            default:
                return false;
        }
    }

    private bool IsInConditionRange(InventoryItem origin, InventoryItem target, EquipmentSearchConditionData condition)
    {
        if (!inventory.TryGetItemPosition(origin, out int originX, out int originY))
            return false;

        if (!inventory.TryGetItemPosition(target, out int targetX, out int targetY))
            return false;

        int originMinX = originX;
        int originMaxX = originX + origin.Width - 1;
        int originMinY = originY;
        int originMaxY = originY + origin.Height - 1;

        int targetMinX = targetX;
        int targetMaxX = targetX + target.Width - 1;
        int targetMinY = targetY;
        int targetMaxY = targetY + target.Height - 1;

        int xDistance = GetRectDistance(originMinX, originMaxX, targetMinX, targetMaxX);
        int yDistance = GetRectDistance(originMinY, originMaxY, targetMinY, targetMaxY);

        switch (condition.ConditionType)
        {
            case EquipmentSearchConditionType.Around:
                return xDistance <= condition.Range && yDistance <= condition.Range;

            case EquipmentSearchConditionType.SameRow:
                return yDistance == 0 && xDistance <= condition.Range;

            case EquipmentSearchConditionType.SameColumn:
                return xDistance == 0 && yDistance <= condition.Range;

            default:
                return false;
        }
    }

    private int GetRectDistance(int aMin, int aMax, int bMin, int bMax)
    {
        if (aMax < bMin)
            return bMin - aMax;

        if (bMax < aMin)
            return aMin - bMax;

        return 0;
    }
}
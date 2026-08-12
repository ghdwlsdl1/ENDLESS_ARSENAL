using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ItemInfoUI : MonoBehaviour
{
    [InspectorLabel("플레이어 스탯")]
    [SerializeField] private PlayerStat playerStat;

    [InspectorLabel("인벤토리 효과 커넥터")]
    [SerializeField] private InventoryEffectConnector inventoryEffectConnector;

    private WeaponInventoryBonus currentWeaponBonus;
    
    [InspectorLabel("아이템 정보 연출")]
    [SerializeField] private ItemInfoAnimator itemInfoAnimator;
    
    [Header("텍스트")]
    [InspectorLabel("아이템 정보창")]
    [SerializeField] private GameObject itemInfoRoot;

    [InspectorLabel("이름")]
    [SerializeField] private TMP_Text nameText;

    [InspectorLabel("크기")]
    [SerializeField] private TMP_Text sizeText;

    [InspectorLabel("설명")]
    [SerializeField] private TMP_Text descriptionText;


    [Header("스탯")]

    [InspectorLabel("스탯 부모")]
    [SerializeField] private Transform statRoot;

    [InspectorLabel("스탯 라인 프리팹")]
    [SerializeField] private StatLineUI statLinePrefab;

    [InspectorLabel("스탯 라인 풀 키")]
    [SerializeField] private string statLinePoolKey =
        "StatLine";

    [Header("레이아웃")]

    [InspectorLabel("정보창 RectTransform")]
    [SerializeField] private RectTransform panelRect;

    [InspectorLabel("정보창 부모")]
    [SerializeField] private RectTransform canvasRect;

    [InspectorLabel("아이템과 정보창 간격")]
    [SerializeField] private float positionOffset;

    [InspectorLabel("화면 가장자리 여백")]
    [SerializeField] private float screenPadding;

    private readonly List<StatLineUI> spawnedLines = new List<StatLineUI>();
    private Coroutine positionCoroutine;


    private void Awake()
    {
        if (playerStat == null)
        {
            playerStat =
                FindFirstObjectByType<PlayerStat>();
        }

        if (inventoryEffectConnector == null)
        {
            inventoryEffectConnector =
                FindFirstObjectByType<InventoryEffectConnector>();
        }

        if (itemInfoAnimator != null)
        {
            itemInfoAnimator.HideImmediate();
        }
        else
        {
            SetVisible(false);
        }
    }


    public void Show(
        InventoryItem item,
        RectTransform selectedItemRect)
    {
        InventoryItemData itemData =
            item != null ? item.Data : null;

        if (itemData == null)
        {
            Hide();
            return;
        }

        currentWeaponBonus =
            itemData.ItemType == InventoryItemType.Weapon &&
            inventoryEffectConnector != null
                ? inventoryEffectConnector.GetWeaponBonus(item)
                : new WeaponInventoryBonus();

        if (positionCoroutine != null)
        {
            StopCoroutine(
                positionCoroutine);

            positionCoroutine = null;
        }

        ClearStatLines();

        SetBasicInfo(
            itemData);

        switch (itemData.ItemType)
        {
            case InventoryItemType.Weapon:
                AddWeaponInfo(
                    itemData);
                break;

            case InventoryItemType.Equipment:
                AddEquipmentInfo(
                    itemData.EquipmentData);
                break;
        }

        if (itemInfoAnimator != null)
            itemInfoAnimator.PrepareShow();
        else
            SetVisible(true);

        positionCoroutine =
            StartCoroutine(
                RefreshLayoutAndPosition(
                    selectedItemRect));
    }
    
    private IEnumerator RefreshLayoutAndPosition(
        RectTransform selectedItemRect)
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (statRoot is RectTransform statRect)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    statRect);
        }

        if (panelRect != null)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    panelRect);
        }

        Canvas.ForceUpdateCanvases();

        yield return new WaitForEndOfFrame();

        if (statRoot is RectTransform finalStatRect)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    finalStatRect);
        }

        if (panelRect != null)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    panelRect);
        }

        Canvas.ForceUpdateCanvases();

        SetPosition(
            selectedItemRect);

        if (itemInfoAnimator != null)
            itemInfoAnimator.PlayShow();

        positionCoroutine = null;
    }

    public void Hide()
    {
        if (positionCoroutine != null)
        {
            StopCoroutine(
                positionCoroutine);

            positionCoroutine = null;
        }

        if (itemInfoAnimator != null)
        {
            itemInfoAnimator.PlayHide(
                ClearStatLines);

            return;
        }

        ClearStatLines();
        SetVisible(false);
    }


    private void SetBasicInfo(
        InventoryItemData itemData)
    {
        if (nameText != null)
        {
            nameText.text =
                itemData.ItemName;
        }

        if (sizeText != null)
        {
            int slotCount =
                itemData.Width *
                itemData.Height;

            sizeText.text =
                $"{slotCount}칸";
        }

        if (descriptionText == null)
            return;

        bool hasDescription =
            !string.IsNullOrWhiteSpace(
                itemData.Description);

        descriptionText.gameObject.SetActive(
            hasDescription);

        descriptionText.text =
            hasDescription
                ? itemData.Description
                : string.Empty;
    }


    private void AddWeaponInfo(
        InventoryItemData itemData)
    {
        if (string.IsNullOrWhiteSpace(
                itemData.WeaponPrefabPath))
        {
            AddStatLine(
                "무기 정보",
                "프리팹 경로 없음");

            return;
        }

        GameObject weaponPrefab =
            Resources.Load<GameObject>(
                itemData.WeaponPrefabPath);

        if (weaponPrefab == null)
        {
            AddStatLine(
                "무기 정보",
                "프리팹을 찾을 수 없음");

            return;
        }

        ProjectileWeapon projectileWeapon =
            weaponPrefab.GetComponentInChildren<
                ProjectileWeapon>(true);

        if (projectileWeapon != null)
        {
            AddProjectileWeaponInfo(
                projectileWeapon);

            return;
        }

        AuraWeapon auraWeapon =
            weaponPrefab.GetComponentInChildren<
                AuraWeapon>(true);

        if (auraWeapon != null)
        {
            AddAuraWeaponInfo(
                auraWeapon);

            return;
        }

        OrbitWeapon orbitWeapon =
            weaponPrefab.GetComponentInChildren<
                OrbitWeapon>(true);

        if (orbitWeapon != null)
        {
            AddOrbitWeaponInfo(
                orbitWeapon);

            return;
        }

        PulseWeapon pulseWeapon =
            weaponPrefab.GetComponentInChildren<
                PulseWeapon>(true);

        if (pulseWeapon != null)
        {
            AddPulseWeaponInfo(
                pulseWeapon);

            return;
        }

        AddStatLine(
            "무기 정보",
            "지원하지 않는 무기 형식");
    }

    private void AddProjectileWeaponInfo(
        ProjectileWeapon weapon)
    {
        AddStatLine("무기 유형", "투사체");
        
        AddSpacer();
        
        AddDamageInfo(weapon);
        
        AddSpacer();
        
        AddAttackIntervalInfo(weapon, "attackInterval");

        AddRangeInfo(weapon, "attackRange", "공격 사거리");
        
        AddSpacer();

        AddIntField(weapon, "projectileCount", "투사체 개수", "개", currentWeaponBonus.ProjectileCountBonus);

        AddScaleInfo(weapon, "projectileScale", "투사체 크기");

        AddProjectilePiercingInfo(weapon);

        AddProjectileExplosionInfo(weapon);
        
        AddSpacer();

        AddEnumField(weapon, "aimType", "조준 방식");
    }
    
    private void AddAuraWeaponInfo(
        AuraWeapon weapon)
    {
        AddStatLine("무기 유형", "오라");
        
        AddSpacer();

        AddDamageInfo(weapon);
        
        AddSpacer();

        AddAttackIntervalInfo(weapon, "hitInterval");

        AddRangeInfo(weapon, "radius", "공격 범위", isAuraRadius: true);
    }

    private void AddOrbitWeaponInfo(
        OrbitWeapon weapon)
    {
        AddStatLine("무기 유형", "오비트");
        
        AddSpacer();

        AddDamageInfo(weapon);
        
        AddSpacer();
      
        AddIntField(weapon, "weaponCount", "무기 개수", "개", currentWeaponBonus.ProjectileCountBonus);

        AddRangeInfo(weapon, "radius", "회전 반지름");

        AddFloatField(weapon, "rotateSpeed", "회전 속도");

        AddScaleInfo(weapon, "weaponScale", "무기 크기");
        
        AddSpacer();

        AddEnumField(weapon, "movePattern", "회전 방식");
    }

    private void AddPulseWeaponInfo(
        PulseWeapon weapon)
    {
        AddStatLine("무기 유형", "펄스");
        
        AddSpacer();

        AddDamageInfo(weapon, "statDamageMultiplier", "playerDamageMultiplier");
        
        AddSpacer();

        AddAttackIntervalInfo(weapon, "attackInterval");

        AddScaleInfo(weapon, "attackWidth", "공격 폭");

        AddRangeInfo(weapon, "attackDistance", "공격 사거리");
        
        AddSpacer();

        AddIntField(weapon, "comboCount", "연속 공격", "회");

        AddSpacer();
        
        AddEnumField(weapon, "aimType", "조준 방식");
    }
    
    private bool AddDamageInfo(
        object target,
        string multiplierFieldName =
            "statDamageMultiplier",
        string fallbackMultiplierFieldName =
            null)
    {
        if (!TryGetFieldValue(
                target,
                "baseDamage",
                out float baseDamage))
        {
            return false;
        }

        float statDamageMultiplier;

        if (!TryGetFieldValue(
                target,
                multiplierFieldName,
                out statDamageMultiplier))
        {
            if (string.IsNullOrWhiteSpace(
                    fallbackMultiplierFieldName) ||
                !TryGetFieldValue(
                    target,
                    fallbackMultiplierFieldName,
                    out statDamageMultiplier))
            {
                return false;
            }
        }

        float playerDamage =
            playerStat != null
                ? playerStat.Damage
                : 0f;

        float weaponDamageMultiplier =
            playerStat != null
                ? playerStat.WeaponDamageMultiplier
                : 1f;

        float inventoryDamageMultiplier =
            1f + currentWeaponBonus.DamageBonus;

        float appliedBaseDamage =
            baseDamage +
            playerDamage *
            statDamageMultiplier;

        float finalDamage =
            appliedBaseDamage *
            weaponDamageMultiplier *
            inventoryDamageMultiplier;

        string baseDamageText =
            $"{FormatNumber(baseDamage)} + " +
            $"{statDamageMultiplier * 100f:0.##}% → " +
            $"<color=#55FF55>" +
            $"{FormatNumber(appliedBaseDamage)}" +
            $"</color>";

        AddStatLine("기본 공격력", baseDamageText);

        AddStatLine("최종 공격력", $"<color=#55FF55>{FormatNumber(finalDamage)}</color>");

        return true;
    }
    
    private bool AddAttackIntervalInfo(
        object target,
        string fieldName)
    {
        if (!TryGetFieldValue(
                target,
                fieldName,
                out float baseInterval))
        {
            return false;
        }

        float attackSpeedMultiplier =
            playerStat != null
                ? playerStat
                    .WeaponAttackSpeedMultiplier
                : 1f;

        attackSpeedMultiplier += currentWeaponBonus.AttackSpeedBonus;

        attackSpeedMultiplier =
            Mathf.Max(
                0.01f,
                attackSpeedMultiplier);

        float appliedInterval =
            baseInterval /
            attackSpeedMultiplier;

        AddStatLine(
            "공격 간격",
            FormatOriginalAndApplied(
                baseInterval,
                appliedInterval,
                "초"));

        return true;
    }
    
    private string FormatOriginalAndApplied(
        float originalValue,
        float appliedValue,
        string suffix)
    {
        string originalText =
            $"{FormatNumber(originalValue)}{suffix}";

        if (Mathf.Approximately(
                originalValue,
                appliedValue))
        {
            return originalText;
        }

        string appliedText =
            $"{FormatNumber(appliedValue)}{suffix}";

        return
            $"{originalText} → " +
            $"<color=#55FF55>{appliedText}</color>";
    }
    
    private void AddProjectilePiercingInfo(
        ProjectileWeapon weapon)
    {
        if (!TryGetFieldValue(
                weapon,
                "isPiercing",
                out bool isPiercing))
        {
            return;
        }

        if (!isPiercing)
            return;

        AddIntField(
            weapon,
            "maxPierceCount",
            "관통 횟수",
            "회");
    }
    
    private void AddProjectileExplosionInfo(
        ProjectileWeapon weapon)
    {
        if (!TryGetFieldValue(
                weapon,
                "isExplosive",
                out bool isExplosive))
        {
            return;
        }

        if (!isExplosive)
            return;

        AddFloatField(
            weapon,
            "explosionRadius",
            "폭발 범위");

        AddPercentField(
            weapon,
            "explosionDamageRatio",
            "폭발 피해 비율");
    }
    
    private bool AddRangeInfo(
        object target,
        string fieldName,
        string label,
        bool isAuraRadius = false)
    {
        if (!TryGetFieldValue(
                target,
                fieldName,
                out float baseValue))
        {
            return false;
        }

        float rangeMultiplier =
            playerStat != null
                ? playerStat.WeaponRangeMultiplier
                : 1f;

        rangeMultiplier += currentWeaponBonus.RangeBonus;
        
        if (isAuraRadius)
            rangeMultiplier = 1f + (rangeMultiplier - 1f) * 0.5f;

        float appliedValue =
            baseValue * rangeMultiplier;

        AddStatLine(
            label,
            FormatOriginalAndApplied(
                baseValue,
                appliedValue,
                ""));

        return true;
    }

    private bool AddScaleInfo(
        object target,
        string fieldName,
        string label)
    {
        if (!TryGetFieldValue(
                target,
                fieldName,
                out float baseValue))
        {
            return false;
        }

        float scaleMultiplier =
            playerStat != null
                ? playerStat.WeaponScaleMultiplier
                : 1f;

        scaleMultiplier += currentWeaponBonus.WeaponScaleBonus;
        scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);

        float appliedValue =
            baseValue * scaleMultiplier;

        AddStatLine(
            label,
            FormatOriginalAndApplied(
                baseValue,
                appliedValue,
                ""));

        return true;
    }
    
    private void AddEquipmentInfo(
        EquipmentData equipmentData)
    {
        if (equipmentData == null)
            return;

        AddPositionConditions(
            equipmentData.PositionConditions);

        AddSearchConditions(
            equipmentData.SearchConditions);

        AddEffects(
            equipmentData.Effects);
    }


    private void AddPositionConditions(
        EquipmentPositionConditionData[] conditions)
    {
        if (conditions == null)
            return;

        foreach (
            EquipmentPositionConditionData condition
            in conditions)
        {
            if (condition == null)
                continue;

            AddStatLine(
                "위치 조건",
                GetPositionConditionName(
                    condition.ConditionType));
        }
    }


    private void AddSearchConditions(
        EquipmentSearchConditionData[] conditions)
    {
        if (conditions == null)
            return;

        foreach (
            EquipmentSearchConditionData condition
            in conditions)
        {
            if (condition == null)
                continue;

            string value =
                $"{GetSearchConditionName(condition.ConditionType)} / " +
                $"{GetTargetName(condition.Target)} / " +
                $"{condition.Range}칸 / " +
                $"{condition.RequiredCount}개";

            AddStatLine(
                "탐색 조건",
                value);
        }
    }


    private void AddEffects(
        EquipmentEffectData[] effects)
    {
        if (effects == null)
            return;

        foreach (
            EquipmentEffectData effect
            in effects)
        {
            if (effect == null)
                continue;

            if (effect.EffectType ==
                EquipmentEffectType.ProjectileSpeed)
            {
                continue;
            }

            AddStatLine(
                GetEffectName(
                    effect.EffectType),
                FormatEffectValue(
                    effect.EffectType,
                    effect.EffectValue));
        }
    }
    
    private void SetPosition(
        RectTransform selectedItemRect)
    {
        if (panelRect == null ||
            canvasRect == null ||
            selectedItemRect == null)
        {
            return;
        }

        Vector3[] itemCorners =
            new Vector3[4];

        Vector3[] panelCorners =
            new Vector3[4];

        Vector3[] canvasCorners =
            new Vector3[4];

        selectedItemRect.GetWorldCorners(
            itemCorners);

        panelRect.GetWorldCorners(
            panelCorners);

        canvasRect.GetWorldCorners(
            canvasCorners);

        float panelWidth =
            panelCorners[2].x -
            panelCorners[0].x;

        float panelHeight =
            panelCorners[2].y -
            panelCorners[0].y;

        float halfPanelWidth =
            panelWidth * 0.5f;

        float halfPanelHeight =
            panelHeight * 0.5f;

        float canvasScaleX =
            Mathf.Abs(
                canvasRect.lossyScale.x);

        float canvasScaleY =
            Mathf.Abs(
                canvasRect.lossyScale.y);

        float offsetX =
            positionOffset *
            canvasScaleX;

        float paddingX =
            screenPadding *
            canvasScaleX;

        float paddingY =
            screenPadding *
            canvasScaleY;

        float itemCenterY =
            (itemCorners[0].y +
             itemCorners[2].y) *
            0.5f;

        float availableRight =
            canvasCorners[2].x -
            paddingX -
            itemCorners[2].x -
            offsetX;

        float availableLeft =
            itemCorners[0].x -
            canvasCorners[0].x -
            paddingX -
            offsetX;

        bool showOnRight;

        if (availableRight >= panelWidth)
        {
            showOnRight = true;
        }
        else if (availableLeft >= panelWidth)
        {
            showOnRight = false;
        }
        else
        {
            showOnRight =
                availableRight >=
                availableLeft;
        }

        float targetX;

        if (showOnRight)
        {
            targetX =
                itemCorners[2].x +
                offsetX +
                halfPanelWidth;
        }
        else
        {
            targetX =
                itemCorners[0].x -
                offsetX -
                halfPanelWidth;
        }

        float minX =
            canvasCorners[0].x +
            paddingX +
            halfPanelWidth;

        float maxX =
            canvasCorners[2].x -
            paddingX -
            halfPanelWidth;

        float minY =
            canvasCorners[0].y +
            paddingY +
            halfPanelHeight;

        float maxY =
            canvasCorners[2].y -
            paddingY -
            halfPanelHeight;

        if (minX <= maxX)
        {
            targetX =
                Mathf.Clamp(
                    targetX,
                    minX,
                    maxX);
        }
        else
        {
            targetX =
                (canvasCorners[0].x +
                 canvasCorners[2].x) *
                0.5f;
        }

        float targetY;

        if (minY <= maxY)
        {
            targetY =
                Mathf.Clamp(
                    itemCenterY,
                    minY,
                    maxY);
        }
        else
        {
            targetY =
                (canvasCorners[0].y +
                 canvasCorners[2].y) *
                0.5f;
        }

        panelRect.position =
            new Vector3(
                targetX,
                targetY,
                panelRect.position.z);
    }

    private bool AddFloatField(
        object target,
        string fieldName,
        string label,
        string suffix = "")
    {
        if (!TryGetFieldValue(
                target,
                fieldName,
                out float value))
        {
            return false;
        }

        AddStatLine(
            label,
            $"{FormatNumber(value)}{suffix}");

        return true;
    }


    private bool AddPercentField(
        object target,
        string fieldName,
        string label)
    {
        if (!TryGetFieldValue(
                target,
                fieldName,
                out float value))
        {
            return false;
        }

        AddStatLine(
            label,
            $"{value * 100f:0.##}%");

        return true;
    }

    private bool AddIntField(
        object target,
        string fieldName,
        string label,
        string suffix = "",
        int bonus = 0)
    {
        if (!TryGetFieldValue(
                target,
                fieldName,
                out int value))
        {
            return false;
        }

        int appliedValue = value + bonus;

        if (bonus == 0)
        {
            AddStatLine(
                label,
                $"{value}{suffix}");

            return true;
        }

        AddStatLine(
            label,
            $"{value}{suffix} → " +
            $"<color=#55FF55>{appliedValue}{suffix}</color>");

        return true;
    }


    private bool AddEnumField(
        object target,
        string fieldName,
        string label)
    {
        FieldInfo fieldInfo =
            GetFieldInfo(
                target,
                fieldName);

        if (fieldInfo == null)
            return false;

        object value =
            fieldInfo.GetValue(target);

        if (value == null)
            return false;

        AddStatLine(
            label,
            GetEnumDisplayName(
                value));

        return true;
    }


    private bool TryGetFieldValue<T>(
        object target,
        string fieldName,
        out T value)
    {
        value = default;

        FieldInfo fieldInfo =
            GetFieldInfo(
                target,
                fieldName);

        if (fieldInfo == null)
            return false;

        object fieldValue =
            fieldInfo.GetValue(target);

        if (!(fieldValue is T typedValue))
            return false;

        value = typedValue;
        return true;
    }


    private FieldInfo GetFieldInfo(
        object target,
        string fieldName)
    {
        if (target == null)
            return null;

        return target
            .GetType()
            .GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public);
    }


    private string GetEnumDisplayName(
        object enumValue)
    {
        switch (enumValue)
        {
            case ProjectileAimType.NearestEnemy:
                return "가장 가까운 적";

            case ProjectileAimType.RandomEnemy:
                return "랜덤 적";

            case ProjectileAimType.Forward:
                return "고정 방향";

            case ProjectileAimType.MoveDirection:
                return "이동 방향";

            case ProjectileAimType.RandomDirection:
                return "랜덤 방향";

            case ProjectileAimType.Circular:
                return "원형 탄막";

            case PulseWeaponAimType.NearestTarget:
                return "가장 가까운 대상";

            case PulseWeaponAimType.MoveDirection:
                return "이동 방향";

            case PulseWeaponAimType.Forward:
                return "고정 방향";

            case PulseWeaponAimType.RandomDirection:
                return "랜덤 방향";

            case PulseWeaponAttackType.ForwardOnly:
                return "공격 방향";

            case PulseWeaponAttackType.ForwardAndBackward:
                return "양방향";

            case OrbitWeapon.OrbitMovePattern.Circle:
                return "원형";

            case OrbitWeapon.OrbitMovePattern.RadiusWave:
                return "반지름 진동";

            case OrbitWeapon.OrbitMovePattern.Ellipse:
                return "타원";

            case OrbitWeapon.OrbitMovePattern.Wobble:
                return "흔들림";

            case OrbitWeapon.OrbitMovePattern.Flower:
                return "꽃잎";

            default:
                return enumValue.ToString();
        }
    }


    private void AddStatLine(
        string label,
        string value)
    {
        if (statRoot == null)
            return;

        StatLineUI line =
            GetStatLine();

        if (line == null)
            return;

        line.transform.SetParent(
            statRoot,
            false);

        RectTransform lineRect =
            line.transform as RectTransform;

        if (lineRect != null)
        {
            lineRect.localScale =
                Vector3.one;
        }

        line.SetData(
            label,
            value);

        spawnedLines.Add(line);
    }

    private void AddSpacer()
    {
        AddStatLine(
            string.Empty,
            string.Empty);
    }

    private StatLineUI GetStatLine()
    {
        if (!string.IsNullOrWhiteSpace(
                statLinePoolKey))
        {
            GameObject pooledObject =
                ObjectPool.Get(
                    statLinePoolKey);

            if (pooledObject != null &&
                pooledObject.TryGetComponent(
                    out StatLineUI pooledLine))
            {
                return pooledLine;
            }

            if (pooledObject != null)
            {
                ObjectPool.Return(
                    pooledObject);
            }
        }

        if (statLinePrefab == null)
            return null;

        return Instantiate(
            statLinePrefab);
    }


    private void ClearStatLines()
    {
        foreach (
            StatLineUI line
            in spawnedLines)
        {
            if (line == null)
                continue;

            PoolObject poolObject =
                line.GetComponent<PoolObject>();

            if (poolObject != null &&
                !poolObject.IsPooled)
            {
                ObjectPool.Return(
                    line.gameObject);
            }
            else if (poolObject == null)
            {
                Destroy(
                    line.gameObject);
            }
        }

        spawnedLines.Clear();
    }


    private void SetVisible(
        bool visible)
    {
        if (itemInfoRoot != null)
        {
            itemInfoRoot.SetActive(
                visible);
        }
    }


    private string FormatNumber(
        float value)
    {
        return Mathf.Approximately(
            value,
            Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.##");
    }


    private string GetPositionConditionName(
        EquipmentPositionConditionType type)
    {
        switch (type)
        {
            case EquipmentPositionConditionType.TopRow:
                return "맨 위";

            case EquipmentPositionConditionType.BottomRow:
                return "맨 아래";

            case EquipmentPositionConditionType.LeftColumn:
                return "맨 왼쪽";

            case EquipmentPositionConditionType.RightColumn:
                return "맨 오른쪽";

            default:
                return type.ToString();
        }
    }


    private string GetSearchConditionName(
        EquipmentSearchConditionType type)
    {
        switch (type)
        {
            case EquipmentSearchConditionType.Around:
                return "주변";

            case EquipmentSearchConditionType.SameRow:
                return "같은 행";

            case EquipmentSearchConditionType.SameColumn:
                return "같은 열";

            default:
                return type.ToString();
        }
    }


    private string GetTargetName(
        EquipmentConditionTarget target)
    {
        switch (target)
        {
            case EquipmentConditionTarget.Weapon:
                return "무기";

            case EquipmentConditionTarget.Equipment:
                return "장비";

            case EquipmentConditionTarget.AnyItem:
                return "모든 아이템";

            default:
                return target.ToString();
        }
    }


    private string GetEffectName(
        EquipmentEffectType type)
    {
        switch (type)
        {
            case EquipmentEffectType.Damage:
                return "공격력";

            case EquipmentEffectType.AttackSpeed:
                return "공격 속도";

            case EquipmentEffectType.Range:
                return "사거리";

            case EquipmentEffectType.ProjectileCount:
                return "투사체 개수";

            case EquipmentEffectType.WeaponScale:
                return "무기 크기";

            case EquipmentEffectType.MaxHp:
                return "최대 체력";

            case EquipmentEffectType.HpRegen:
                return "체력 재생";

            case EquipmentEffectType.MoveSpeed:
                return "이동 속도";

            case EquipmentEffectType.ExpAttractRange:
                return "경험치 획득 범위";

            case EquipmentEffectType.CameraSize:
                return "카메라 시야";

            default:
                return type.ToString();
        }
    }


    private string FormatEffectValue(
        EquipmentEffectType type,
        float value)
    {
        switch (type)
        {
            case EquipmentEffectType.Damage:
            case EquipmentEffectType.AttackSpeed:
            case EquipmentEffectType.Range:
            case EquipmentEffectType.WeaponScale:
                return
                    $"{value * 100f:+0;-0;0}%";

            case EquipmentEffectType.ProjectileCount:
                return
                    $"{value:+0;-0;0}개";

            default:
                return
                    $"{value:+0.##;-0.##;0}";
        }
    }
}
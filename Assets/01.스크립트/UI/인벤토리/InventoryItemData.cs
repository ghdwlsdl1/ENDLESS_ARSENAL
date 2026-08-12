using UnityEngine;

public enum InventoryItemType
{
    [InspectorName("무기")]
    Weapon,

    [InspectorName("장비")]
    Equipment
}

public enum EquipmentPositionConditionType
{
    [InspectorName("맨 위")]
    TopRow,

    [InspectorName("맨 아래")]
    BottomRow,

    [InspectorName("맨 왼쪽")]
    LeftColumn,

    [InspectorName("맨 오른쪽")]
    RightColumn
}

public enum EquipmentSearchConditionType
{
    [InspectorName("주변")]
    Around,

    [InspectorName("같은 행")]
    SameRow,

    [InspectorName("같은 열")]
    SameColumn
}

public enum EquipmentConditionTarget
{
    [InspectorName("무기")]
    Weapon,

    [InspectorName("장비")]
    Equipment,

    [InspectorName("모든 아이템")]
    AnyItem
}

public enum EquipmentEffectType
{
    [InspectorName("공격력")]
    Damage,

    [InspectorName("공격속도")]
    AttackSpeed,

    [InspectorName("사거리")]
    Range,

    [InspectorName("투사체 속도")]
    ProjectileSpeed,

    [InspectorName("투사체 개수")]
    ProjectileCount,

    [InspectorName("무기 크기")]
    WeaponScale,

    [InspectorName("최대 체력")]
    MaxHp,

    [InspectorName("체력 재생")]
    HpRegen,

    [InspectorName("이동 속도")]
    MoveSpeed,

    [InspectorName("경험치 획득 범위")]
    ExpAttractRange,

    [InspectorName("카메라 시야")]
    CameraSize
}

[System.Serializable]
public class EquipmentPositionConditionData
{
    [InspectorLabel("조건")]
    [SerializeField] private EquipmentPositionConditionType conditionType;

    public EquipmentPositionConditionType ConditionType => conditionType;
}

[System.Serializable]
public class EquipmentSearchConditionData
{
    [InspectorLabel("조건")]
    [SerializeField] private EquipmentSearchConditionType conditionType;

    [InspectorLabel("대상")]
    [SerializeField] private EquipmentConditionTarget target;

    [InspectorLabel("범위")]
    [SerializeField] private int range = 1;

    [InspectorLabel("필요 개수")]
    [SerializeField] private int requiredCount = 1;

    public EquipmentSearchConditionType ConditionType => conditionType;
    public EquipmentConditionTarget Target => target;
    public int Range => Mathf.Max(1, range);
    public int RequiredCount => Mathf.Max(1, requiredCount);
}

[System.Serializable]
public class EquipmentEffectData
{
    [InspectorLabel("효과")]
    [SerializeField] private EquipmentEffectType effectType;

    [InspectorLabel("효과 값")]
    [SerializeField] private float effectValue;

    public EquipmentEffectType EffectType => effectType;
    public float EffectValue => effectValue;
}

[System.Serializable]
public class EquipmentData
{
    [Header("위치 조건")]
    [SerializeField] private EquipmentPositionConditionData[] positionConditions;

    [Header("탐색 조건")]
    [SerializeField] private EquipmentSearchConditionData[] searchConditions;

    [Header("효과")]
    [SerializeField] private EquipmentEffectData[] effects;

    public EquipmentPositionConditionData[] PositionConditions => positionConditions;
    public EquipmentSearchConditionData[] SearchConditions => searchConditions;
    public EquipmentEffectData[] Effects => effects;
}

[CreateAssetMenu(fileName = "InventoryItemData", menuName = "Inventory/Item Data")]
public class InventoryItemData : ScriptableObject
{
    [Header("기본")]

    [InspectorLabel("아이템 이름")]
    [SerializeField] private string itemName;

    [InspectorLabel("아이콘")]
    [SerializeField] private Sprite icon;

    [InspectorLabel("가로")]
    [SerializeField] private int width = 1;

    [InspectorLabel("세로")]
    [SerializeField] private int height = 1;
    
    [SerializeField] [TextArea(2, 5)] private string description;

    [InspectorLabel("아이템 타입")]
    [SerializeField] private InventoryItemType itemType;

    [Header("정보")]
    
    [InspectorLabel("무기 타입", "itemType", InventoryItemType.Weapon)]
    [SerializeField] private WeaponType weaponType;

    [InspectorLabel("무기 경로", "itemType", InventoryItemType.Weapon)]
    [SerializeField] private string weaponPrefabPath;

    
    [InspectorLabel("장비 데이터", "itemType", InventoryItemType.Equipment)]
    [SerializeField] private EquipmentData equipmentData;

    public string ItemName => itemName;
    public Sprite Icon => icon;

    public int Width => Mathf.Max(1, width);
    public int Height => Mathf.Max(1, height);
    public string Description => description;

    public InventoryItemType ItemType => itemType;
    public WeaponType WeaponType => weaponType;
    public string WeaponPrefabPath => weaponPrefabPath;
    public EquipmentData EquipmentData => equipmentData;
}
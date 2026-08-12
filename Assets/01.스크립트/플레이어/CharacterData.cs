using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("시작 아이템")]

    [InspectorLabel("시작 아이템 목록")]
    [SerializeField] private InventoryItemData[] startItems;

    [Header("기본 스탯")]

    [InspectorLabel("캐릭터 이름")]
    [SerializeField] private string characterName;
    
    [InspectorLabel("최대 체력")]
    [SerializeField] private float maxHp;

    [InspectorLabel("초당 체력 회복")]
    [SerializeField] private float hpRegenPerSecond;

    [InspectorLabel("이동 속도")]
    [SerializeField] private float moveSpeed;

    [InspectorLabel("공격력")]
    [SerializeField] private float damage;

    [InspectorLabel("경험치 흡수 범위")]
    [SerializeField] private float expAttractRange;
    
    [Header("무기 보정 스탯")]

    [InspectorLabel("피해 배율")]
    [SerializeField] private float weaponDamageMultiplier;

    [InspectorLabel("공격 속도 배율")]
    [SerializeField] private float weaponAttackSpeedMultiplier;

    [InspectorLabel("사거리 배율")]
    [SerializeField] private float weaponRangeMultiplier;

    [InspectorLabel("공격 범위 배율")]
    [SerializeField] private float weaponScaleMultiplier;

    [InspectorLabel("치명타 확률")]
    [SerializeField] private float criticalChance;

    public InventoryItemData[] StartItems => startItems;
    
    public string CharacterName => characterName;
    public float MaxHp => maxHp;
    public float HpRegenPerSecond => hpRegenPerSecond;
    public float MoveSpeed => moveSpeed;
    public float Damage => damage;
    public float ExpAttractRange => expAttractRange;
    
    public float WeaponDamageMultiplier => weaponDamageMultiplier;
    public float WeaponAttackSpeedMultiplier => weaponAttackSpeedMultiplier;
    public float WeaponRangeMultiplier => weaponRangeMultiplier;
    public float WeaponScaleMultiplier => weaponScaleMultiplier;
    public float CriticalChance => criticalChance;
}
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LevelUpRewardUI : MonoBehaviour
{
    private enum RewardType
    {
        MaxHp,
        HpRegen,
        MoveSpeed,
        Damage,
        ExpAttractRange,
        WeaponDamage,
        WeaponAttackSpeed,
        WeaponRange,
        WeaponScale,
        CriticalChance
    }

    private enum RewardGrade
    {
        Common,
        Rare,
        Unique,
        Legendary,
        Epic
    }

    private struct RewardOption
    {
        public RewardType Type;
        public RewardGrade Grade;
        public float Value;

        public RewardOption(
            RewardType type,
            RewardGrade grade,
            float value)
        {
            Type = type;
            Grade = grade;
            Value = value;
        }
    }

    [InspectorLabel("UI 관리자")]
    [SerializeField] private UIManager uiManager;

    [InspectorLabel("인벤토리 UI")]
    [SerializeField] private InventoryUI inventoryUI;
    
    [InspectorLabel("지도 UI")]
    [SerializeField] private SimpleMapUI simpleMapUI;
    
    [InspectorLabel("애니메이션")]
    [SerializeField] private LevelUpRewardAnimator rewardAnimator;
    
    [InspectorLabel("플레이어 레벨")]
    [SerializeField] private PlayerLevel playerLevel;

    [InspectorLabel("플레이어 스탯")]
    [SerializeField] private PlayerStat playerStat;

    [InspectorLabel("인벤토리 확장 레벨 간격")]
    [SerializeField] private int inventoryExpandLevelInterval = 5;

    [InspectorLabel("기본 리롤 횟수")]
    [SerializeField] private int baseRerollCount = 3;

    [Header("텍스트")]

    [InspectorLabel("현재 스탯")]
    [SerializeField] private TMP_Text currentStatText;

    [InspectorLabel("리롤 횟수")]
    [SerializeField] private TMP_Text rerollCountText;

    [InspectorLabel("보상1")]
    [SerializeField] private TMP_Text rewardText1;

    [InspectorLabel("보상2")]
    [SerializeField] private TMP_Text rewardText2;

    [InspectorLabel("보상3")]
    [SerializeField] private TMP_Text rewardText3;

    [InspectorLabel("보상4")]
    [SerializeField] private TMP_Text rewardText4;

    [Header("카드 테두리")]

    [InspectorLabel("보상 카드 테두리 1")]
    [SerializeField] private Image rewardCard1;

    [InspectorLabel("보상 카드 테두리 2")]
    [SerializeField] private Image rewardCard2;

    [InspectorLabel("보상 카드 테두리 3")]
    [SerializeField] private Image rewardCard3;

    [InspectorLabel("보상 카드 테두리 4")]
    [SerializeField] private Image rewardCard4;
    
    [Header("보상 기준 수치")]

    [InspectorLabel("최대 체력 증가량")]
    [SerializeField] private float maxHpAddValue;

    [InspectorLabel("초당 체력 회복 증가량")]
    [SerializeField] private float hpRegenAddValue;

    [InspectorLabel("이동 속도 증가량")]
    [SerializeField] private float moveSpeedAddValue;

    [InspectorLabel("공격력 증가량")]
    [SerializeField] private float damageAddValue;

    [InspectorLabel("경험치 흡수 범위 증가량")]
    [SerializeField] private float expAttractRangeAddValue;

    [InspectorLabel("피해 배율 증가량")]
    [SerializeField] private float weaponDamageAddValue;

    [InspectorLabel("공격 속도 배율 증가량")]
    [SerializeField] private float weaponAttackSpeedAddValue;

    [InspectorLabel("사정 거리 배율 증가량")]
    [SerializeField] private float weaponRangeAddValue;
    
    [InspectorLabel("공격 범위 배율 증가량")]
    [SerializeField] private float weaponScaleAddValue;
    
    [InspectorLabel("치명타 확률 증가량")]
    [SerializeField] private float criticalChanceAddValue;

    [Header("보상 등급 확률")]

    [InspectorLabel("일반 확률")]
    [SerializeField] private float commonChance;

    [InspectorLabel("레어 확률")]
    [SerializeField] private float rareChance;

    [InspectorLabel("유니크 확률")]
    [SerializeField] private float uniqueChance;

    [InspectorLabel("레전더리 확률")]
    [SerializeField] private float legendaryChance;

    [InspectorLabel("에픽 확률")]
    [SerializeField] private float epicChance;

    [Header("보상 등급 배율")]

    [InspectorLabel("최소 보상 배율")]
    [SerializeField] private float minRewardMultiplier;

    [InspectorLabel("최대 보상 배율")]
    [SerializeField] private float maxRewardMultiplier;

    private readonly RewardOption[] currentRewards =
        new RewardOption[4];

    private readonly Queue<int> pendingLevelUps = new();

    private bool isProcessingLevelUp;
    
    private int bonusRerollCount;
    private int currentRerollCount;
    
    private bool reopenMapAfterClose;
    
    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>();

        if (simpleMapUI == null)
            simpleMapUI = FindFirstObjectByType<SimpleMapUI>();

        if (playerLevel == null)
            playerLevel = FindFirstObjectByType<PlayerLevel>();

        if (playerStat == null)
            playerStat = FindFirstObjectByType<PlayerStat>();
    }

    private void Start()
    {
        if (playerLevel != null)
            playerLevel.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (playerLevel != null)
            playerLevel.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int level)
    {
        pendingLevelUps.Enqueue(level);

        if (isProcessingLevelUp)
            return;

        ProcessNextLevelUp();
    }

    private void ProcessNextLevelUp()
    {
        if (pendingLevelUps.Count <= 0)
        {
            isProcessingLevelUp = false;

            if (uiManager != null)
            {
                uiManager.HidePanel(
                    UIPanelType.Compensation);
            }

            if (reopenMapAfterClose &&
                simpleMapUI != null)
            {
                simpleMapUI.OpenMap();
            }

            reopenMapAfterClose = false;

            if (GameManager.Instance != null)
                GameManager.Instance.ResumeGame();

            return;
        }

        isProcessingLevelUp = true;

        Open();
    }

    private bool ShouldExpandInventory(int level)
    {
        if (inventoryExpandLevelInterval <= 0)
            return false;

        return level % inventoryExpandLevelInterval == 0;
    }

    private void OpenInventoryExpand()
    {
        if (inventoryUI == null)
        {
            ProcessNextLevelUp();
            return;
        }

        inventoryUI.OpenInventoryPaused();

        inventoryUI.StartUnlockMode(
            OnInventoryExpandComplete);
    }

    private void OnInventoryExpandComplete()
    {
        DOVirtual.DelayedCall(
                0.35f,
                () =>
                {
                    if (inventoryUI != null)
                    {
                        inventoryUI.CloseInventoryForReward(
                            ProcessNextLevelUp);

                        return;
                    }

                    ProcessNextLevelUp();
                })
            .SetUpdate(true);
    }

    public void RerollButton()
    {
        SoundManager.Instance.PlaySFX(SfxType.Button, 0);
        
        if (currentRerollCount <= 0)
            return;

        if (rewardAnimator != null &&
            rewardAnimator.IsPlaying)
        {
            return;
        }

        currentRerollCount--;

        RefreshRerollText();

        if (rewardAnimator != null)
        {
            rewardAnimator.PlayReroll(
                RefreshRewards,
                currentRerollCount > 0);

            return;
        }

        RefreshRewards();
    }

    private void Open()
    {
        if (!reopenMapAfterClose &&
            simpleMapUI != null &&
            simpleMapUI.IsOpen)
        {
            reopenMapAfterClose = true;
            simpleMapUI.CloseMap();
        }

        if (inventoryUI != null)
            inventoryUI.ForceCloseInventory();

        currentRerollCount =
            Mathf.Max(0, baseRerollCount + bonusRerollCount);

        RefreshRewards();
        RefreshCurrentStats();
        RefreshRerollText();

        if (uiManager != null)
            uiManager.ShowPanel(UIPanelType.Compensation);

        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();

        if (rewardAnimator != null)
        {
            rewardAnimator.PlayOpen(
                currentRerollCount > 0);
        }
    }

    private void RefreshRewards()
    {
        List<RewardType> rewards = new()
        {
            RewardType.MaxHp,
            RewardType.HpRegen,
            RewardType.MoveSpeed,
            RewardType.Damage,
            RewardType.ExpAttractRange,
            RewardType.WeaponDamage,
            RewardType.WeaponAttackSpeed,
            RewardType.WeaponRange,
            RewardType.WeaponScale,
            RewardType.CriticalChance
        };

        for (int i = 0; i < currentRewards.Length; i++)
        {
            int randomIndex =
                Random.Range(0, rewards.Count);

            RewardType rewardType =
                rewards[randomIndex];

            currentRewards[i] =
                CreateRewardOption(rewardType);

            rewards.RemoveAt(randomIndex);
        }

        RefreshRewardTexts();
    }

    private RewardOption CreateRewardOption(
        RewardType rewardType)
    {
        RewardGrade grade = RollRewardGrade();

        float multiplier =
            RollRewardMultiplier(grade);

        float value =
            GetBaseRewardValue(rewardType) * multiplier;

        value =
            RoundRewardValue(rewardType, value);

        return new RewardOption(
            rewardType,
            grade,
            value);
    }

    private RewardGrade RollRewardGrade()
    {
        float common =
            Mathf.Max(0f, commonChance);

        float rare =
            Mathf.Max(0f, rareChance);

        float unique =
            Mathf.Max(0f, uniqueChance);

        float legendary =
            Mathf.Max(0f, legendaryChance);

        float epic =
            Mathf.Max(0f, epicChance);

        float totalChance =
            common +
            rare +
            unique +
            legendary +
            epic;

        if (totalChance <= 0f)
            return RewardGrade.Common;

        float roll =
            Random.Range(0f, totalChance);

        if (roll < common)
            return RewardGrade.Common;

        roll -= common;

        if (roll < rare)
            return RewardGrade.Rare;

        roll -= rare;

        if (roll < unique)
            return RewardGrade.Unique;

        roll -= unique;

        if (roll < legendary)
            return RewardGrade.Legendary;

        return RewardGrade.Epic;
    }

    private float RollRewardMultiplier(RewardGrade grade)
    {
        float minMultiplier = Mathf.Min(
            this.minRewardMultiplier,
            this.maxRewardMultiplier);

        float maxMultiplier = Mathf.Max(
            this.minRewardMultiplier,
            this.maxRewardMultiplier);

        if (Mathf.Approximately(minMultiplier, maxMultiplier))
            return minMultiplier;

        float rareMultiplier = Mathf.Clamp(
            1f,
            minMultiplier,
            maxMultiplier);

        float upperRange = maxMultiplier - rareMultiplier;

        float uniqueMultiplier =
            rareMultiplier + upperRange * 0.4f;

        float legendaryMultiplier =
            rareMultiplier + upperRange * 0.7f;

        float gradeMin;
        float gradeMax;

        switch (grade)
        {
            case RewardGrade.Common:
                gradeMin = minMultiplier;
                gradeMax = rareMultiplier;
                break;

            case RewardGrade.Rare:
                gradeMin = rareMultiplier;
                gradeMax = uniqueMultiplier;
                break;

            case RewardGrade.Unique:
                gradeMin = uniqueMultiplier;
                gradeMax = legendaryMultiplier;
                break;

            case RewardGrade.Legendary:
                gradeMin = legendaryMultiplier;
                gradeMax = rareMultiplier + upperRange * 0.9f;
                break;

            case RewardGrade.Epic:
                gradeMin = rareMultiplier + upperRange * 0.9f;
                gradeMax = maxMultiplier;
                break;

            default:
                gradeMin = minMultiplier;
                gradeMax = rareMultiplier;
                break;
        }

        if (gradeMax < gradeMin)
        {
            float temp = gradeMin;
            gradeMin = gradeMax;
            gradeMax = temp;
        }

        float multiplier = Random.Range(gradeMin, gradeMax);

        return Mathf.Round(multiplier * 100f) / 100f;
    }

    private float GetBaseRewardValue(
        RewardType rewardType)
    {
        switch (rewardType)
        {
            case RewardType.MaxHp:
                return maxHpAddValue;

            case RewardType.HpRegen:
                return hpRegenAddValue;

            case RewardType.MoveSpeed:
                return moveSpeedAddValue;

            case RewardType.Damage:
                return damageAddValue;

            case RewardType.ExpAttractRange:
                return expAttractRangeAddValue;

            case RewardType.WeaponDamage:
                return weaponDamageAddValue;

            case RewardType.WeaponAttackSpeed:
                return weaponAttackSpeedAddValue;

            case RewardType.WeaponRange:
                return weaponRangeAddValue;

            case RewardType.WeaponScale:
                return weaponScaleAddValue;
            
            case RewardType.CriticalChance:
                return criticalChanceAddValue;
        }

        return 0f;
    }

    private float RoundRewardValue(
        RewardType rewardType,
        float value)
    {
        switch (rewardType)
        {
            case RewardType.WeaponDamage:
            case RewardType.WeaponAttackSpeed:
            case RewardType.WeaponRange:
            case RewardType.WeaponScale:
            case RewardType.CriticalChance:
                return Mathf.Round(value * 100f) / 100f;

            default:
                return Mathf.Round(value * 10f) / 10f;
        }
    }

    public void SelectReward1()
    {
        SelectReward(0);
    }

    public void SelectReward2()
    {
        SelectReward(1);
    }

    public void SelectReward3()
    {
        SelectReward(2);
    }

    public void SelectReward4()
    {
        SelectReward(3);
    }

    private void SelectReward(int index)
    {
        SoundManager.Instance.PlaySFX(SfxType.Button, 0);
        
        if (index < 0 ||
            index >= currentRewards.Length)
        {
            return;
        }

        if (rewardAnimator != null &&
            rewardAnimator.IsPlaying)
        {
            return;
        }

        RewardOption selectedReward =
            currentRewards[index];

        if (rewardAnimator != null)
        {
            rewardAnimator.PlaySelect(
                index,
                () => ApplyReward(selectedReward));

            return;
        }

        ApplyReward(selectedReward);
    }

    private void ApplyReward(RewardOption reward)
    {
        if (playerStat == null)
            return;

        switch (reward.Type)
        {
            case RewardType.MaxHp:
                playerStat.AddMaxHp(reward.Value);
                break;

            case RewardType.HpRegen:
                playerStat.AddHpRegen(reward.Value);
                break;

            case RewardType.MoveSpeed:
                playerStat.AddMoveSpeed(reward.Value);
                break;

            case RewardType.Damage:
                playerStat.AddDamage(reward.Value);
                break;

            case RewardType.ExpAttractRange:
                playerStat.AddExpAttractRange(reward.Value);
                break;

            case RewardType.WeaponDamage:
                playerStat.AddWeaponDamageMultiplier(
                    reward.Value);
                break;

            case RewardType.WeaponAttackSpeed:
                playerStat.AddWeaponAttackSpeedMultiplier(
                    reward.Value);
                break;

            case RewardType.WeaponRange:
                playerStat.AddWeaponRangeMultiplier(
                    reward.Value);
                break;

            case RewardType.WeaponScale:
                playerStat.AddWeaponScaleMultiplier(
                    reward.Value);
                break;
            
            case RewardType.CriticalChance:
                playerStat.AddCriticalChance(reward.Value);
                break;
        }

        Close();
    }

    private void Close()
    {
        if (rewardAnimator != null)
        {
            rewardAnimator.PlayClose(
                CompleteClose);

            return;
        }

        CompleteClose();
    }

    private void CompleteClose()
    {
        if (pendingLevelUps.Count <= 0)
        {
            ProcessNextLevelUp();
            return;
        }

        int level =
            pendingLevelUps.Dequeue();

        if (uiManager != null)
        {
            uiManager.HidePanel(
                UIPanelType.Compensation);
        }

        if (ShouldExpandInventory(level))
        {
            OpenInventoryExpand();
            return;
        }

        ProcessNextLevelUp();
    }

    private void RefreshRewardTexts()
    {
        if (rewardText1 != null)
        {
            rewardText1.text =
                GetRewardText(currentRewards[0]);
        }

        if (rewardText2 != null)
        {
            rewardText2.text =
                GetRewardText(currentRewards[1]);
        }

        if (rewardText3 != null)
        {
            rewardText3.text =
                GetRewardText(currentRewards[2]);
        }

        if (rewardText4 != null)
        {
            rewardText4.text =
                GetRewardText(currentRewards[3]);
        }
        
        RefreshRewardCardColors();
    }
    
    private void RefreshRewardCardColors()
    {
        SetRewardCardColor(rewardCard1, currentRewards[0].Grade);
        SetRewardCardColor(rewardCard2, currentRewards[1].Grade);
        SetRewardCardColor(rewardCard3, currentRewards[2].Grade);
        SetRewardCardColor(rewardCard4, currentRewards[3].Grade);
    }
    
    private void SetRewardCardColor(
        Image image,
        RewardGrade grade)
    {
        if (image == null)
            return;

        image.color = GetRewardCardColor(grade);
    }
    
    private Color GetRewardCardColor(
        RewardGrade grade)
    {
        switch (grade)
        {
            case RewardGrade.Common:
                return new Color32(128, 128, 128, 255);

            case RewardGrade.Rare:
                return new Color32(162, 89, 255, 255);

            case RewardGrade.Unique:
                return new Color32(255, 79, 195, 255);

            case RewardGrade.Legendary:
                return new Color32(255, 140, 0, 255);

            case RewardGrade.Epic:
                return new Color32(255, 215, 0, 255);
        }

        return Color.white;
    }
    
    private string GetRewardText(
        RewardOption reward) 
    { 
        if (playerStat == null) 
            return string.Empty;
        
        string gradeColor = 
            GetRewardGradeColor(reward.Grade);
        
        switch (reward.Type) 
        { 
            case RewardType.MaxHp: 
                return CreateRewardText(
                    "최대 체력", 
                    $"+{reward.Value:0.#}", 
                    playerStat.MaxHp.ToString("0.#"), 
                    (playerStat.MaxHp + reward.Value)
                    .ToString("0.#"), 
                    gradeColor);
            
            case RewardType.HpRegen: 
                return CreateRewardText(
                    "체력 재생", 
                    $"+{reward.Value:0.#}", 
                    playerStat.HpRegenPerSecond
                        .ToString("0.#"), 
                    (playerStat.HpRegenPerSecond + 
                     reward.Value)
                    .ToString("0.#"), 
                    gradeColor);
            
            case RewardType.MoveSpeed: 
                return CreateRewardText(
                    "이동 속도", 
                    $"+{reward.Value:0.#}", 
                    playerStat.MoveSpeed
                        .ToString("0.#"), 
                    (playerStat.MoveSpeed + 
                     reward.Value)
                    .ToString("0.#"), 
                    gradeColor);
            
            case RewardType.Damage: 
                return CreateRewardText(
                    "공 격 력", 
                    $"+{reward.Value:0.#}", 
                    playerStat.Damage
                        .ToString("0.#"), 
                    (playerStat.Damage + 
                     reward.Value)
                    .ToString("0.#"), 
                    gradeColor);
            
            case RewardType.ExpAttractRange: 
                return CreateRewardText(
                    "흡수 범위", 
                    $"+{reward.Value:0.#}", 
                    playerStat.ExpAttractRange
                        .ToString("0.#"), 
                    (playerStat.ExpAttractRange + 
                     reward.Value)
                    .ToString("0.#"), 
                    gradeColor);
            
            case RewardType.WeaponDamage: 
                return CreateMultiplierRewardText(
                    "피 해 량", 
                    reward, 
                    playerStat.WeaponDamageMultiplier, 
                    gradeColor);
            
            case RewardType.WeaponAttackSpeed: 
                return CreateMultiplierRewardText(
                    "공격 속도", 
                    reward, 
                    playerStat.WeaponAttackSpeedMultiplier, 
                    gradeColor);
            
            case RewardType.WeaponRange: 
                return CreateMultiplierRewardText(
                    "사정 거리", 
                    reward, 
                    playerStat.WeaponRangeMultiplier, 
                    gradeColor);
            
            case RewardType.WeaponScale: 
                return CreateMultiplierRewardText(
                    "공격 범위", 
                    reward, 
                    playerStat.WeaponScaleMultiplier, 
                    gradeColor);
            
            case RewardType.CriticalChance: 
                return CreateRewardText(
                    "치명타율", 
                    $"+{reward.Value * 100f:0}%", 
                    FormatMultiplier(playerStat.CriticalChance + 1f), 
                    FormatMultiplier(playerStat.CriticalChance + reward.Value + 1f), 
                    gradeColor); 
        }
        
        return string.Empty; 
    }

    private string CreateMultiplierRewardText(
        string rewardName,
        RewardOption reward,
        float currentValue,
        string gradeColor)
    {
        return CreateRewardText(
            rewardName,
            $"+{reward.Value * 100f:0}%",
            FormatMultiplier(currentValue),
            FormatMultiplier(currentValue + reward.Value),
            gradeColor);
    }

    private string CreateRewardText(
        string rewardName,
        string addValue,
        string currentValue,
        string resultValue,
        string gradeColor)
    {
        return
            $"{rewardName}\n\n" +
            $"<size=170%>" +
            $"<color={gradeColor}>" +
            $"{addValue}" +
            $"</color>" +
            $"</size>\n\n" +
            $"{currentValue} → {resultValue}";
    }

    private void RefreshCurrentStats()
    {
        if (currentStatText == null ||
            playerStat == null)
        {
            return;
        }

        currentStatText.text =
            $": {playerStat.MaxHp:0.#}\n" +
            $": {playerStat.HpRegenPerSecond:0.#}\n" +
            $": {playerStat.MoveSpeed:0.#}\n" +
            $": {playerStat.Damage:0.#}\n" +
            $": {playerStat.ExpAttractRange:0.#}\n\n" +
            $": {FormatMultiplier(playerStat.WeaponDamageMultiplier)}\n" +
            $": {FormatMultiplier(playerStat.WeaponAttackSpeedMultiplier)}\n" +
            $": {FormatMultiplier(playerStat.WeaponRangeMultiplier)}\n" +
            $": {FormatMultiplier(playerStat.WeaponScaleMultiplier)}\n" +
            $": {playerStat.CriticalChance * 100f:0}%";
    }
    private string FormatMultiplier(float value)
    {
        float percent = (value - 1f) * 100f;
        return $"{percent:0;-0;0}%";
    }

    private void RefreshRerollText()
    {
        if (rerollCountText != null)
        {
            rerollCountText.text =
                currentRerollCount.ToString();
        }

        if (rewardAnimator != null)
        {
            rewardAnimator.SetRerollAvailable(
                currentRerollCount > 0);
        }
    }

    private string GetRewardGradeColor(
        RewardGrade grade)
    {
        switch (grade)
        {
            case RewardGrade.Common:
                return "#808080";

            case RewardGrade.Rare:
                return "#A259FF";

            case RewardGrade.Unique:
                return "#FF4FC3";

            case RewardGrade.Legendary:
                return "#FF8C00";

            case RewardGrade.Epic:
                return "#FFD700";
        }

        return "#FFFFFF";
    }
    
    public void AddRerollCount(int value)
    {
        bonusRerollCount += value;

        if (bonusRerollCount < 0)
            bonusRerollCount = 0;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterScreenUI : MonoBehaviour
{
    [System.Serializable]
    private class CharacterButtonData
    {
        [InspectorLabel("버튼")]
        public Button button;

        [InspectorLabel("애니메이터")]
        public Animator animator;

        [InspectorLabel("선택 테두리")]
        public GameObject border;

        [InspectorLabel("캐릭터 데이터")]
        public CharacterData characterData;
    }

    [InspectorLabel("UI 관리자")]
    [SerializeField] private UIManager uiManager;

    [InspectorLabel("인벤토리 UI")]
    [SerializeField] private InventoryUI inventoryUI;

    [InspectorLabel("플레이어 스탯")]
    [SerializeField] private PlayerStat playerStat;

    [InspectorLabel("시작 버튼")]
    [SerializeField] private Button startButton;

    [InspectorLabel("캐릭터 정보 텍스트")]
    [SerializeField] private TMP_Text characterInfoText;

    [InspectorLabel("캐릭터 능력치 영역")]
    [SerializeField] private GameObject characterStatRoot;

    [InspectorLabel("기본 능력치 값")]
    [SerializeField] private TMP_Text baseStatValueText;

    [InspectorLabel("무기 능력치 값")]
    [SerializeField] private TMP_Text weaponStatValueText;

    [Header("캐릭터")]

    [InspectorLabel("캐릭터 버튼목록")]
    [SerializeField] private CharacterButtonData[] characters;

    [InspectorLabel("캐릭터 목록")]
    [SerializeField] private GameObject[] playerCharacters;

    private int selectedIndex = -1;
    private bool isStarting;

    public int SelectedIndex => selectedIndex;

    private static readonly int IsSelectedHash =
        Animator.StringToHash("IsSelected");

    private void Awake()
    {
        RegisterButtons();
    }

    private void OnEnable()
    {
        isStarting = false;
        selectedIndex = -1;

        ClearSelection();
        ClearCharacterInfo();

        if (characterStatRoot != null)
            characterStatRoot.SetActive(false);

        RefreshStartButton();
    }

    private void ClearSelection()
    {
        if (characters == null)
            return;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            Animator animator = characters[i].animator;

            if (animator != null &&
                animator.runtimeAnimatorController != null &&
                animator.isActiveAndEnabled)
            {
                animator.SetBool(IsSelectedHash, false);
            }

            if (characters[i].border != null)
                characters[i].border.SetActive(false);
        }
    }

    private void RegisterButtons()
    {
        if (characters == null)
            return;

        for (int i = 0; i < characters.Length; i++)
        {
            int index = i;

            if (characters[i] == null ||
                characters[i].button == null)
            {
                continue;
            }

            characters[i].button.onClick.RemoveAllListeners();
            characters[i].button.onClick.AddListener(
                () => SelectCharacter(index));
        }
    }

    public void SelectCharacter(int index)
    {
        if (isStarting)
            return;

        if (characters == null ||
            characters.Length <= 0)
        {
            return;
        }

        if (index < 0 ||
            index >= characters.Length)
        {
            return;
        }

        CharacterButtonData selectedCharacter =
            characters[index];

        if (selectedCharacter == null)
            return;

        selectedIndex = index;

        SoundManager.Instance?.PlaySFX(
            SfxType.Button,
            1);

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            bool isSelected =
                i == selectedIndex;

            Animator animator =
                characters[i].animator;

            if (animator != null &&
                animator.runtimeAnimatorController != null &&
                animator.isActiveAndEnabled)
            {
                animator.SetBool(
                    IsSelectedHash,
                    isSelected);
            }

            if (characters[i].border != null)
            {
                characters[i].border.SetActive(
                    isSelected);
            }
        }

        if (characterStatRoot != null)
            characterStatRoot.SetActive(true);

        UpdateCharacterInfo(
            selectedCharacter.characterData);

        RefreshStartButton();
    }

    private void RefreshStartButton()
    {
        if (startButton != null)
        {
            startButton.interactable =
                selectedIndex >= 0 &&
                !isStarting;
        }
    }

    public void StartButton()
    {
        SoundManager.Instance?.PlaySFX(
            SfxType.Button,
            0);

        if (isStarting)
            return;

        if (selectedIndex < 0)
            return;

        StartCoroutine(
            StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        isStarting = true;
        RefreshStartButton();

        if (uiManager != null)
        {
            yield return StartCoroutine(
                uiManager.FadeOut());
        }

        ApplySelectedCharacter();
        ApplySelectedCharacterStats();
        ApplySelectedStartItems();

        if (uiManager != null)
        {
            uiManager.HidePanel(
                UIPanelType.CharacterScreen);

            uiManager.HidePanel(
                UIPanelType.StartScreen);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();

        if (uiManager != null)
        {
            yield return StartCoroutine(
                uiManager.FadeIn());
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();

        if (inventoryUI != null)
            inventoryUI.OpenInventoryPaused();

        isStarting = false;
        RefreshStartButton();
    }

    private void ApplySelectedStartItems()
    {
        if (inventoryUI == null)
            return;

        if (characters == null)
            return;

        if (selectedIndex < 0 ||
            selectedIndex >= characters.Length)
        {
            return;
        }

        CharacterData characterData =
            characters[selectedIndex].characterData;

        if (characterData == null)
            return;

        inventoryUI.CreateStartItems(
            characterData.StartItems);
    }

    public void CloseButton()
    {
        if (isStarting)
            return;

        if (uiManager != null)
        {
            uiManager.HidePanel(
                UIPanelType.CharacterScreen);
        }
    }

    private void ApplySelectedCharacter()
    {
        if (playerCharacters == null ||
            playerCharacters.Length <= 0)
        {
            return;
        }

        for (int i = 0;
             i < playerCharacters.Length;
             i++)
        {
            if (playerCharacters[i] == null)
                continue;

            playerCharacters[i].SetActive(
                i == selectedIndex);
        }
    }

    private void ClearCharacterInfo()
    {
        if (characterInfoText != null)
        {
            characterInfoText.text =
                "캐릭터를 선택해주세요.\n\n" +
                "선택한 캐릭터의\n" +
                "시작 무기와 능력치가 표시됩니다.";
        }

        if (baseStatValueText != null)
            baseStatValueText.text = string.Empty;

        if (weaponStatValueText != null)
            weaponStatValueText.text = string.Empty;
    }

    private void UpdateCharacterInfo(
        CharacterData characterData)
    {
        if (characterData == null)
        {
            ClearCharacterInfo();

            if (characterStatRoot != null)
                characterStatRoot.SetActive(false);

            return;
        }

        string startItemName = "없음";

        InventoryItemData[] startItems =
            characterData.StartItems;

        if (startItems != null &&
            startItems.Length > 0)
        {
            for (int i = 0;
                 i < startItems.Length;
                 i++)
            {
                if (startItems[i] == null)
                    continue;

                startItemName =
                    startItems[i].ItemName;

                break;
            }
        }

        if (characterInfoText != null)
        {
            characterInfoText.text =
                $"{characterData.CharacterName}\n" +
                $"<size=25>   {startItemName}</size>";
        }

        if (baseStatValueText != null)
        {
            baseStatValueText.text =
                $": {FormatValue(characterData.MaxHp)}\n" +
                $": {FormatValue(characterData.HpRegenPerSecond)}\n" +
                $": {FormatValue(characterData.MoveSpeed)}\n" +
                $": {FormatValue(characterData.Damage)}\n" +
                $": {FormatValue(characterData.ExpAttractRange)}";
        }

        if (weaponStatValueText != null)
        {
            weaponStatValueText.text =
                $": {FormatMultiplier(characterData.WeaponDamageMultiplier)}\n" +
                $": {FormatMultiplier(characterData.WeaponAttackSpeedMultiplier)}\n" +
                $": {FormatMultiplier(characterData.WeaponRangeMultiplier)}\n" +
                $": {FormatMultiplier(characterData.WeaponScaleMultiplier)}\n" +
                $": {characterData.CriticalChance * 100f:0}%";
        }
    }
    private string FormatMultiplier(float value)
    {
        float percent = (value - 1f) * 100f;
        return $"{percent:0;-0;0}%";
    }

    private string FormatValue(float value)
    {
        return Mathf.Approximately(
            value,
            Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.##");
    }

    private void ApplySelectedCharacterStats()
    {
        if (playerStat == null)
            return;

        if (characters == null)
            return;

        if (selectedIndex < 0 ||
            selectedIndex >= characters.Length)
        {
            return;
        }

        CharacterButtonData selectedCharacter =
            characters[selectedIndex];

        if (selectedCharacter == null)
            return;

        CharacterData characterData =
            selectedCharacter.characterData;

        if (characterData == null)
            return;

        playerStat.SetBaseStats(new CharacterBaseStats
        {
            MaxHp = characterData.MaxHp,
            HpRegenPerSecond = characterData.HpRegenPerSecond,
            MoveSpeed = characterData.MoveSpeed,
            Damage = characterData.Damage,
            ExpAttractRange = characterData.ExpAttractRange,
            WeaponDamageMultiplier = characterData.WeaponDamageMultiplier,
            WeaponAttackSpeedMultiplier = characterData.WeaponAttackSpeedMultiplier,
            WeaponRangeMultiplier = characterData.WeaponRangeMultiplier,
            WeaponScaleMultiplier = characterData.WeaponScaleMultiplier,
            CriticalChance = characterData.CriticalChance,
            WeaponSpeedMultiplier = 1f,
            CriticalDamageMultiplier = 1.5f
        });
    }
}
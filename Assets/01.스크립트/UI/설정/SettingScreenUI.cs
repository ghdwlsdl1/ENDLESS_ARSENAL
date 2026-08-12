using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingScreenUI : MonoBehaviour
{
    [InspectorLabel("UI 관리자")]
    [SerializeField] private UIManager uiManager;

    [InspectorLabel("플레이어 스탯")]
    [SerializeField] private PlayerStat playerStat;

    [InspectorLabel("설정창 연출")]
    [SerializeField] private SettingScreenAnimator settingAnimator;


    [Header("볼륨")]

    [InspectorLabel("마스터 볼륨 슬라이더")]
    [SerializeField] private Slider masterSlider;

    [InspectorLabel("배경음 볼륨 슬라이더")]
    [SerializeField] private Slider bgmSlider;

    [InspectorLabel("효과음 볼륨 슬라이더")]
    [SerializeField] private Slider sfxSlider;

    [InspectorLabel("마스터 볼륨 퍼센트")]
    [SerializeField] private TMP_Text masterPercentText;

    [InspectorLabel("배경음 볼륨 퍼센트")]
    [SerializeField] private TMP_Text bgmPercentText;

    [InspectorLabel("효과음 볼륨 퍼센트")]
    [SerializeField] private TMP_Text sfxPercentText;


    [Header("현재 스탯")]

    [InspectorLabel("스탯 창")]
    [SerializeField] private GameObject statPanel;

    [InspectorLabel("현재 스탯")]
    [SerializeField] private TMP_Text currentStatText;


    private void Awake()
    {
        if (uiManager == null)
        {
            uiManager =
                FindFirstObjectByType<UIManager>();
        }

        if (settingAnimator == null)
        {
            settingAnimator =
                GetComponent<SettingScreenAnimator>();
        }

        FindPlayerStat();

        if (statPanel != null)
        {
            statPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        InitSliders();
        RefreshStatPanel();
    }

    public void SettingButton()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(
                SfxType.Button,
                0);
        }

        if (uiManager != null)
        {
            uiManager.ShowPopup(
                UIPanelType.SettingScreen);
        }

        InitSliders();
        RefreshStatPanel();

        if (settingAnimator != null)
        {
            settingAnimator.PlayOpen(
                ShouldShowStatPanel());
        }

        if (GameManager.Instance != null &&
            GameManager.Instance.IsStarted &&
            !GameManager.Instance.IsGameOver)
        {
            GameManager.Instance.PauseGame();
        }
    }

    public void BackButton()
    {
        if (settingAnimator != null &&
            settingAnimator.IsPlaying)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(
                SfxType.Button,
                0);
        }

        if (settingAnimator != null)
        {
            settingAnimator.PlayClose(
                CompleteClose);

            return;
        }

        CompleteClose();
    }

    private void CompleteClose()
    {
        if (uiManager != null)
        {
            uiManager.HidePanel(
                UIPanelType.SettingScreen);
        }

        if (GameManager.Instance != null &&
            GameManager.Instance.IsStarted &&
            !GameManager.Instance.IsGameOver)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    private void FindPlayerStat()
    {
        if (playerStat != null)
            return;

        playerStat =
            FindFirstObjectByType<PlayerStat>();
    }

    private bool ShouldShowStatPanel()
    {
        return
            GameManager.Instance != null &&
            GameManager.Instance.IsStarted &&
            !GameManager.Instance.IsGameOver;
    }

    private void InitSliders()
    {
        if (SoundManager.Instance == null)
            return;

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(
                SoundManager.Instance.MasterVolume);
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(
                SoundManager.Instance.BgmVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(
                SoundManager.Instance.SfxVolume);
        }

        RefreshText();
    }

    private void RefreshStatPanel()
    {
        bool showStat =
            ShouldShowStatPanel();

        if (statPanel != null)
        {
            statPanel.SetActive(
                showStat);
        }

        if (!showStat)
            return;

        FindPlayerStat();
        RefreshCurrentStats();
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
        float percent =
            (value - 1f) * 100f;

        return $"{percent:0;-0;0}%";
    }

    public void OnChangeMasterVolume(
        float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMasterVolume(
                value);
        }

        if (masterPercentText != null)
        {
            masterPercentText.text =
                $"{value * 100f:0}%";
        }
    }

    public void OnChangeBgmVolume(
        float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBgmVolume(
                value);
        }

        if (bgmPercentText != null)
        {
            bgmPercentText.text =
                $"{value * 100f:0}%";
        }
    }

    public void OnChangeSfxVolume(
        float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSfxVolume(
                value);
        }

        if (sfxPercentText != null)
        {
            sfxPercentText.text =
                $"{value * 100f:0}%";
        }
    }

    private void RefreshText()
    {
        if (masterSlider != null &&
            masterPercentText != null)
        {
            masterPercentText.text =
                $"{masterSlider.value * 100f:0}%";
        }

        if (bgmSlider != null &&
            bgmPercentText != null)
        {
            bgmPercentText.text =
                $"{bgmSlider.value * 100f:0}%";
        }

        if (sfxSlider != null &&
            sfxPercentText != null)
        {
            sfxPercentText.text =
                $"{sfxSlider.value * 100f:0}%";
        }
    }
}
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplanationScreenUI : MonoBehaviour
{
    [System.Serializable]
    private class ExplanationPage
    {
        [InspectorLabel("이미지 그룹")]
        public Sprite[] screenshots;

        [InspectorLabel("제목")]
        public string titleText;

        [InspectorLabel("텍스트")]
        [TextArea(3, 10)]
        public string explanationText;
    }

    [Header("연결")]

    [InspectorLabel("UI 관리자")]
    [SerializeField] private UIManager uiManager;

    [InspectorLabel("설명창 연출")]
    [SerializeField] private ExplanationScreenAnimator explanationAnimator;

    [InspectorLabel("스크린샷 이미지")]
    [SerializeField] private Image screenshotImage;

    [InspectorLabel("제목 텍스트")]
    [SerializeField] private TMP_Text titleText;

    [InspectorLabel("설명 텍스트")]
    [SerializeField] private TMP_Text explanationText;

    [InspectorLabel("다음 버튼")]
    [SerializeField] private Button nextButton;

    [InspectorLabel("이전 버튼")]
    [SerializeField] private Button previousButton;

    [InspectorLabel("프레임 전환 간격(초)")]
    [SerializeField] private float frameInterval;

    [Header("페이지 목록")]

    [InspectorLabel("페이지")]
    [SerializeField] private ExplanationPage[] pages;

    private int currentIndex;
    private Coroutine cycleCoroutine;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        if (explanationAnimator == null)
            explanationAnimator = GetComponent<ExplanationScreenAnimator>();
    }
    
    private void OnDisable()
    {
        StopImageCycle();
    }

    public void ExplanationButton()
    {
        if (explanationAnimator != null &&
            explanationAnimator.IsPlaying)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(
                SfxType.Button,
                0);
        }

        currentIndex = 0;

        if (uiManager != null)
            uiManager.ShowPopup(UIPanelType.ExplanationScreen);
        else
            gameObject.SetActive(true);

        RefreshPage();

        if (explanationAnimator != null)
            explanationAnimator.PlayOpen();
    }

    public void CloseButton()
    {
        if (explanationAnimator != null &&
            explanationAnimator.IsPlaying)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(
                SfxType.Button,
                0);
        }

        if (explanationAnimator != null)
        {
            explanationAnimator.PlayClose(CompleteClose);
            return;
        }

        CompleteClose();
    }

    private void CompleteClose()
    {
        StopImageCycle();

        if (uiManager != null)
            uiManager.HidePanel(UIPanelType.ExplanationScreen);
        else
            gameObject.SetActive(false);
    }
    
    public void NextButton()
    {
        if (pages == null || pages.Length == 0)
            return;

        if (currentIndex >= pages.Length - 1)
            return;

        currentIndex++;
        RefreshPage();
    }

    public void PreviousButton()
    {
        if (currentIndex <= 0)
            return;

        currentIndex--;
        RefreshPage();
    }

    private void RefreshPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        ExplanationPage page = pages[currentIndex];

        StartImageCycle(page.screenshots);

        if (titleText != null)
            titleText.text = page.titleText;

        if (explanationText != null)
            explanationText.text = page.explanationText;
        
        if (previousButton != null)
            previousButton.interactable = currentIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentIndex < pages.Length - 1;
    }

    private void StartImageCycle(Sprite[] screenshots)
    {
        StopImageCycle();

        if (screenshotImage == null)
            return;

        if (screenshots == null || screenshots.Length == 0)
        {
            screenshotImage.enabled = false;
            return;
        }

        screenshotImage.enabled = true;
        screenshotImage.sprite = screenshots[0];

        if (screenshots.Length > 1)
        {
            cycleCoroutine =
                StartCoroutine(
                    CycleImages(screenshots));
        }
    }

    private IEnumerator CycleImages(Sprite[] screenshots)
    {
        int frameIndex = 0;

        WaitForSecondsRealtime wait =
            new WaitForSecondsRealtime(
                Mathf.Max(0.05f, frameInterval));

        while (true)
        {
            yield return wait;

            frameIndex = (frameIndex + 1) % screenshots.Length;

            if (screenshotImage != null)
                screenshotImage.sprite = screenshots[frameIndex];
        }
    }

    private void StopImageCycle()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
    }
}
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ItemSelectAnimator : MonoBehaviour
{
    [Header("연결")]

    [InspectorLabel("선택 화면")]
    [SerializeField] private RectTransform screenRect;

    [InspectorLabel("선택 화면 그룹")]
    [SerializeField] private CanvasGroup screenCanvasGroup;

    [InspectorLabel("후보 슬롯")]
    [SerializeField] private RectTransform[] candidateSlots;

    [InspectorLabel("리롤 버튼")]
    [SerializeField] private Button rerollButton;

    [Header("화면 등장")]

    [InspectorLabel("화면 시작 크기")]
    [SerializeField] private float screenStartScale;

    [InspectorLabel("화면 등장 시간")]
    [SerializeField] private float screenOpenDuration;

    [Header("후보 등장")]

    [InspectorLabel("후보 시작 크기")]
    [SerializeField] private float candidateStartScale;

    [InspectorLabel("후보 확대 크기")]
    [SerializeField] private float candidateOvershootScale;

    [InspectorLabel("후보 등장 시간")]
    [SerializeField] private float candidateOpenDuration;

    [InspectorLabel("후보 복귀 시간")]
    [SerializeField] private float candidateReturnDuration;

    [InspectorLabel("후보 등장 간격")]
    [SerializeField] private float candidateInterval;

    [Header("리롤")]

    [InspectorLabel("리롤 축소 시간")]
    [SerializeField] private float rerollShrinkDuration;

    [InspectorLabel("리롤 확대 시간")]
    [SerializeField] private float rerollExpandDuration;

    [InspectorLabel("리롤 축소 크기")]
    [SerializeField] private float rerollShrinkScale;

    [Header("종료")]

    [InspectorLabel("종료 시간")]
    [SerializeField] private float closeDuration;

    [InspectorLabel("종료 크기")]
    [SerializeField] private float closeScale;

    private Sequence currentSequence;
    private bool isPlaying;
    private bool rerollAvailable;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        Initialize();
        RestoreImmediately();
    }

    private void OnDisable()
    {
        KillSequence();
        RestoreImmediately();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    private void Initialize()
    {
        if (screenRect == null)
            screenRect = transform as RectTransform;

        if (screenCanvasGroup == null)
        {
            screenCanvasGroup =
                GetComponent<CanvasGroup>();

            if (screenCanvasGroup == null)
            {
                screenCanvasGroup =
                    gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    public void PlayOpen(
        bool canReroll,
        Action onComplete = null)
    {
        Initialize();
        KillSequence();

        rerollAvailable = canReroll;
        SetInteraction(false);

        if (screenRect != null)
            screenRect.localScale =
                Vector3.one * screenStartScale;

        if (screenCanvasGroup != null)
            screenCanvasGroup.alpha = 0f;

        PrepareCandidateSlots();

        isPlaying = true;

        currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (screenRect != null)
        {
            currentSequence.Join(
                screenRect.DOScale(
                        1f,
                        screenOpenDuration)
                    .SetEase(Ease.OutQuad));
        }

        if (screenCanvasGroup != null)
        {
            currentSequence.Join(
                screenCanvasGroup.DOFade(
                        1f,
                        screenOpenDuration)
                    .SetEase(Ease.OutQuad));
        }

        float candidateStartTime =
            screenOpenDuration * 0.35f;

        for (int i = 0;
             i < candidateSlots.Length;
             i++)
        {
            RectTransform slot = candidateSlots[i];

            if (slot == null)
                continue;

            CanvasGroup slotCanvasGroup =
                GetOrCreateCanvasGroup(slot);

            float startTime =
                candidateStartTime +
                i * candidateInterval;

            currentSequence.Insert(
                startTime,
                slot.DOScale(
                        candidateOvershootScale,
                        candidateOpenDuration)
                    .SetEase(Ease.OutBack));

            currentSequence.Insert(
                startTime,
                slotCanvasGroup.DOFade(
                        1f,
                        candidateOpenDuration)
                    .SetEase(Ease.OutQuad));

            currentSequence.Insert(
                startTime + candidateOpenDuration,
                slot.DOScale(
                        1f,
                        candidateReturnDuration)
                    .SetEase(Ease.OutQuad));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;
            isPlaying = false;

            RestoreCandidateSlots();
            SetInteraction(true);

            onComplete?.Invoke();
        });
    }

    public void PlayReroll(
        Action refreshCallback,
        bool canRerollAfter,
        Action onComplete = null)
    {
        if (isPlaying)
            return;

        KillSequence();

        rerollAvailable = canRerollAfter;
        SetInteraction(false);

        isPlaying = true;

        currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        for (int i = 0;
             i < candidateSlots.Length;
             i++)
        {
            RectTransform slot = candidateSlots[i];

            if (slot == null)
                continue;

            CanvasGroup slotCanvasGroup =
                GetOrCreateCanvasGroup(slot);

            float delay =
                i * candidateInterval;

            currentSequence.Insert(
                delay,
                slot.DOScale(
                        rerollShrinkScale,
                        rerollShrinkDuration)
                    .SetEase(Ease.InBack));

            currentSequence.Insert(
                delay,
                slotCanvasGroup.DOFade(
                        0f,
                        rerollShrinkDuration)
                    .SetEase(Ease.InQuad));
        }

        float refreshTime =
            rerollShrinkDuration +
            Mathf.Max(
                0,
                candidateSlots.Length - 1) *
            candidateInterval;

        currentSequence.InsertCallback(
            refreshTime,
            () =>
            {
                refreshCallback?.Invoke();
                PrepareCandidateSlots();
            });

        for (int i = 0;
             i < candidateSlots.Length;
             i++)
        {
            RectTransform slot = candidateSlots[i];

            if (slot == null)
                continue;

            CanvasGroup slotCanvasGroup =
                GetOrCreateCanvasGroup(slot);

            float startTime =
                refreshTime +
                i * candidateInterval;

            currentSequence.Insert(
                startTime,
                slot.DOScale(
                        candidateOvershootScale,
                        rerollExpandDuration)
                    .SetEase(Ease.OutBack));

            currentSequence.Insert(
                startTime,
                slotCanvasGroup.DOFade(
                        1f,
                        rerollExpandDuration)
                    .SetEase(Ease.OutQuad));

            currentSequence.Insert(
                startTime + rerollExpandDuration,
                slot.DOScale(
                        1f,
                        candidateReturnDuration)
                    .SetEase(Ease.OutQuad));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;
            isPlaying = false;

            RestoreCandidateSlots();
            SetInteraction(true);

            onComplete?.Invoke();
        });
    }

    public void PlayClose(
        Action onComplete = null)
    {
        Initialize();
        KillSequence();

        SetInteraction(false);
        isPlaying = true;

        currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (screenRect != null)
        {
            currentSequence.Join(
                screenRect.DOScale(
                        closeScale,
                        closeDuration)
                    .SetEase(Ease.InQuad));
        }

        if (screenCanvasGroup != null)
        {
            currentSequence.Join(
                screenCanvasGroup.DOFade(
                        0f,
                        closeDuration)
                    .SetEase(Ease.InQuad));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;
            isPlaying = false;

            RestoreImmediately();
            onComplete?.Invoke();
        });
    }

    public void SetRerollAvailable(
        bool available)
    {
        rerollAvailable = available;

        if (rerollButton == null)
            return;

        rerollButton.gameObject.SetActive(
            rerollAvailable);

        rerollButton.interactable =
            !isPlaying &&
            rerollAvailable;
    }

    public void StopAndRestore()
    {
        KillSequence();
        RestoreImmediately();
    }

    private void PrepareCandidateSlots()
    {
        if (candidateSlots == null)
            return;

        foreach (RectTransform slot
                 in candidateSlots)
        {
            if (slot == null)
                continue;

            slot.localScale =
                Vector3.one * candidateStartScale;

            CanvasGroup canvasGroup =
                GetOrCreateCanvasGroup(slot);

            canvasGroup.alpha = 0f;
        }
    }

    private void RestoreCandidateSlots()
    {
        if (candidateSlots == null)
            return;

        foreach (RectTransform slot
                 in candidateSlots)
        {
            if (slot == null)
                continue;

            slot.localScale = Vector3.one;

            CanvasGroup canvasGroup =
                GetOrCreateCanvasGroup(slot);

            canvasGroup.alpha = 1f;
        }
    }

    private void RestoreImmediately()
    {
        Initialize();

        isPlaying = false;

        if (screenRect != null)
            screenRect.localScale = Vector3.one;

        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.alpha = 1f;
            screenCanvasGroup.blocksRaycasts = true;
            screenCanvasGroup.interactable = true;
        }

        RestoreCandidateSlots();

        if (rerollButton != null)
        {
            rerollButton.gameObject.SetActive(
                rerollAvailable);

            rerollButton.interactable =
                rerollAvailable;
        }
    }

    private CanvasGroup GetOrCreateCanvasGroup(
        RectTransform target)
    {
        CanvasGroup canvasGroup =
            target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                target.gameObject
                    .AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private void SetInteraction(
        bool enabled)
    {
        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.interactable =
                enabled;

            screenCanvasGroup.blocksRaycasts =
                enabled;
        }

        if (rerollButton != null)
        {
            rerollButton.gameObject.SetActive(
                rerollAvailable);

            rerollButton.interactable =
                enabled &&
                rerollAvailable;
        }
    }

    private void KillSequence()
    {
        if (currentSequence != null &&
            currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        currentSequence = null;
        isPlaying = false;
    }
}
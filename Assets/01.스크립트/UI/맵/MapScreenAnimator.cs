using System;
using DG.Tweening;
using UnityEngine;

public class MapScreenAnimator : MonoBehaviour
{
    [Header("연결")]

    [InspectorLabel("지도 화면 캔버스 그룹")]
    [SerializeField] private CanvasGroup screenCanvasGroup;

    [InspectorLabel("지도 패널")]
    [SerializeField] private RectTransform mapPanel;


    [Header("열기 연출")]

    [InspectorLabel("열기 시간")]
    [SerializeField] private float openDuration;

    [InspectorLabel("열기 시작 크기")]
    [SerializeField] private float openStartScale;


    [Header("닫기 연출")]

    [InspectorLabel("닫기 시간")]
    [SerializeField] private float closeDuration;

    [InspectorLabel("닫기 목표 크기")]
    [SerializeField] private float closeTargetScale;


    private Sequence currentSequence;

    private Vector3 originalScale = Vector3.one;

    private bool isInitialized;


    public bool IsPlaying =>
        currentSequence != null &&
        currentSequence.IsActive() &&
        currentSequence.IsPlaying();


    private void Awake()
    {
        Initialize();
        RestoreImmediately();
    }

    private void OnDisable()
    {
        KillCurrentSequence();
        RestoreImmediately();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (mapPanel == null)
            mapPanel = transform as RectTransform;

        if (mapPanel != null)
            originalScale = mapPanel.localScale;

        if (screenCanvasGroup == null)
        {
            screenCanvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (screenCanvasGroup == null)
        {
            screenCanvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }

        isInitialized = true;
    }

    public void PlayOpen(
        Action onComplete = null)
    {
        Initialize();
        KillCurrentSequence();

        PrepareOpenState();

        currentSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        if (mapPanel != null)
        {
            currentSequence.Join(
                mapPanel
                    .DOScale(
                        originalScale,
                        openDuration)
                    .SetEase(Ease.OutBack));
        }

        if (screenCanvasGroup != null)
        {
            currentSequence.Join(
                screenCanvasGroup
                    .DOFade(
                        1f,
                        openDuration * 0.75f)
                    .SetEase(Ease.OutQuad));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            CompleteOpenState();

            onComplete?.Invoke();
        });
    }

    public void PlayClose(
        Action onComplete)
    {
        Initialize();
        KillCurrentSequence();

        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.interactable = false;
            screenCanvasGroup.blocksRaycasts = true;
        }

        currentSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        if (mapPanel != null)
        {
            currentSequence.Join(
                mapPanel
                    .DOScale(
                        originalScale * closeTargetScale,
                        closeDuration)
                    .SetEase(Ease.InBack));
        }

        if (screenCanvasGroup != null)
        {
            currentSequence.Join(
                screenCanvasGroup
                    .DOFade(
                        0f,
                        closeDuration)
                    .SetEase(Ease.InQuad));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            onComplete?.Invoke();
        });
    }

    public void StopAndRestore()
    {
        Initialize();
        KillCurrentSequence();
        RestoreImmediately();
    }

    private void PrepareOpenState()
    {
        if (mapPanel != null)
        {
            mapPanel.localScale =
                originalScale * openStartScale;
        }

        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.alpha = 0f;
            screenCanvasGroup.interactable = false;
            screenCanvasGroup.blocksRaycasts = true;
        }
    }

    private void CompleteOpenState()
    {
        if (mapPanel != null)
            mapPanel.localScale = originalScale;

        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.alpha = 1f;
            screenCanvasGroup.interactable = true;
            screenCanvasGroup.blocksRaycasts = true;
        }
    }

    private void RestoreImmediately()
    {
        Initialize();

        if (mapPanel != null)
            mapPanel.localScale = originalScale;

        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.alpha = 1f;
            screenCanvasGroup.interactable = true;
            screenCanvasGroup.blocksRaycasts = true;
        }
    }

    private void KillCurrentSequence()
    {
        if (currentSequence == null)
            return;

        currentSequence.Kill();
        currentSequence = null;
    }
}
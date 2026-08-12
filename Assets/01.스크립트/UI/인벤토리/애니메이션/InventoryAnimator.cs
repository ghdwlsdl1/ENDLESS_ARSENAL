using System;
using DG.Tweening;
using UnityEngine;

public class InventoryAnimator : MonoBehaviour
{
    [Header("연결")]

    [InspectorLabel("화면 캔버스 그룹")]
    [SerializeField] private CanvasGroup screenCanvasGroup;

    [InspectorLabel("인벤토리 패널")]
    [SerializeField] private RectTransform inventoryPanel;

    [Header("열기 연출")]

    [InspectorLabel("열기 시간")]
    [SerializeField] private float openDuration;

    [InspectorLabel("열기 시작 크기")]
    [SerializeField] private float openStartScale;

    [InspectorLabel("열기 시작 Y 이동")]
    [SerializeField] private float openStartOffsetY;

    [Header("닫기 연출")]

    [InspectorLabel("닫기 시간")]
    [SerializeField] private float closeDuration;

    [InspectorLabel("닫기 목표 크기")]
    [SerializeField] private float closeTargetScale;

    [InspectorLabel("닫기 목표 Y 이동")]
    [SerializeField] private float closeTargetOffsetY;

    private Sequence currentSequence;

    private Vector2 originalPosition;
    private Vector3 originalScale;

    public bool IsPlaying =>
        currentSequence != null &&
        currentSequence.IsActive() &&
        currentSequence.IsPlaying();

    private void Awake()
    {
        CacheOriginalState();
        RestoreImmediately();
    }

    private void OnDisable()
    {
        KillCurrentSequence();
        RestoreImmediately();
    }

    private void CacheOriginalState()
    {
        if (inventoryPanel == null)
            return;

        originalPosition =
            inventoryPanel.anchoredPosition;

        originalScale =
            inventoryPanel.localScale;
    }

    public void PlayOpen(Action onComplete = null)
    {
        KillCurrentSequence();

        PrepareOpenState();

        currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (screenCanvasGroup != null)
        {
            currentSequence.Join(
                screenCanvasGroup
                    .DOFade(1f, openDuration * 0.7f)
                    .SetEase(Ease.OutQuad));
        }

        if (inventoryPanel != null)
        {
            currentSequence.Join(
                inventoryPanel
                    .DOAnchorPos(
                        originalPosition,
                        openDuration)
                    .SetEase(Ease.OutCubic));

            currentSequence.Join(
                inventoryPanel
                    .DOScale(
                        originalScale,
                        openDuration)
                    .SetEase(Ease.OutBack));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            if (screenCanvasGroup != null)
            {
                screenCanvasGroup.alpha = 1f;
                screenCanvasGroup.interactable = true;
                screenCanvasGroup.blocksRaycasts = true;
            }

            onComplete?.Invoke();
        });
    }

    public void PlayClose(Action onComplete)
    {
        KillCurrentSequence();

        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.interactable = false;
            screenCanvasGroup.blocksRaycasts = true;
        }

        currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (inventoryPanel != null)
        {
            currentSequence.Join(
                inventoryPanel
                    .DOAnchorPos(
                        originalPosition +
                        Vector2.down * closeTargetOffsetY,
                        closeDuration)
                    .SetEase(Ease.InCubic));

            currentSequence.Join(
                inventoryPanel
                    .DOScale(
                        originalScale * closeTargetScale,
                        closeDuration)
                    .SetEase(Ease.InCubic));
        }

        if (screenCanvasGroup != null)
        {
            currentSequence.Join(
                screenCanvasGroup
                    .DOFade(0f, closeDuration)
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
        KillCurrentSequence();
        RestoreImmediately();
    }

    private void PrepareOpenState()
    {
        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.alpha = 0f;
            screenCanvasGroup.interactable = false;
            screenCanvasGroup.blocksRaycasts = true;
        }

        if (inventoryPanel == null)
            return;

        inventoryPanel.anchoredPosition =
            originalPosition +
            Vector2.down * openStartOffsetY;

        inventoryPanel.localScale =
            originalScale * openStartScale;
    }

    private void RestoreImmediately()
    {
        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.alpha = 1f;
            screenCanvasGroup.interactable = true;
            screenCanvasGroup.blocksRaycasts = true;
        }

        if (inventoryPanel == null)
            return;

        inventoryPanel.anchoredPosition =
            originalPosition;

        inventoryPanel.localScale =
            originalScale;
    }

    private void KillCurrentSequence()
    {
        if (currentSequence == null)
            return;

        currentSequence.Kill();
        currentSequence = null;
    }
}
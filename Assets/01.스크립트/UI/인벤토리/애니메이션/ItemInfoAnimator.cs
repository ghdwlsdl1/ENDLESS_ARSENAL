using System;
using DG.Tweening;
using UnityEngine;

public class ItemInfoAnimator : MonoBehaviour
{
    [Header("연결")]

    [InspectorLabel("아이템 정보창")]
    [SerializeField] private GameObject itemInfoRoot;

    [InspectorLabel("정보창 RectTransform")]
    [SerializeField] private RectTransform panelRect;

    [InspectorLabel("정보창 캔버스 그룹")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("등장 연출")]

    [InspectorLabel("등장 시간")]
    [SerializeField] private float showDuration;

    [InspectorLabel("등장 이동 거리")]
    [SerializeField] private float showOffsetX;

    [Header("숨김 연출")]

    [InspectorLabel("숨김 시간")]
    [SerializeField] private float hideDuration;

    private Sequence currentSequence;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    private void Initialize()
    {
        if (panelRect == null &&
            itemInfoRoot != null)
        {
            panelRect =
                itemInfoRoot.transform
                    as RectTransform;
        }

        if (canvasGroup == null &&
            itemInfoRoot != null)
        {
            canvasGroup =
                itemInfoRoot.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    itemInfoRoot.AddComponent<CanvasGroup>();
            }
        }
    }

    public void PrepareShow()
    {
        Initialize();
        KillSequence();

        if (itemInfoRoot != null)
            itemInfoRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void PlayShow()
    {
        Initialize();
        KillSequence();

        if (itemInfoRoot == null)
            return;

        itemInfoRoot.SetActive(true);

        Vector2 targetPosition =
            panelRect != null
                ? panelRect.anchoredPosition
                : Vector2.zero;

        if (panelRect != null)
        {
            panelRect.anchoredPosition =
                targetPosition +
                Vector2.right * showOffsetX;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        currentSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        if (canvasGroup != null)
        {
            currentSequence.Join(
                canvasGroup
                    .DOFade(
                        1f,
                        showDuration)
                    .SetEase(
                        Ease.OutQuad));
        }

        if (panelRect != null)
        {
            currentSequence.Join(
                panelRect
                    .DOAnchorPos(
                        targetPosition,
                        showDuration)
                    .SetEase(
                        Ease.OutCubic));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        });
    }

    public void PlayHide(
        Action onComplete = null)
    {
        Initialize();
        KillSequence();

        if (itemInfoRoot == null ||
            !itemInfoRoot.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        currentSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        if (canvasGroup != null)
        {
            currentSequence.Join(
                canvasGroup
                    .DOFade(
                        0f,
                        hideDuration)
                    .SetEase(
                        Ease.InQuad));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            if (itemInfoRoot != null)
                itemInfoRoot.SetActive(false);

            onComplete?.Invoke();
        });
    }

    public void HideImmediate()
    {
        Initialize();
        KillSequence();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (itemInfoRoot != null)
            itemInfoRoot.SetActive(false);
    }

    private void KillSequence()
    {
        if (currentSequence != null &&
            currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        currentSequence = null;
    }
}
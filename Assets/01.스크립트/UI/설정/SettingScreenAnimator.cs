using System;
using DG.Tweening;
using UnityEngine;

public class SettingScreenAnimator : MonoBehaviour
{
    [Header("전체 설정창")]

    [InspectorLabel("전체 콘텐츠")]
    [SerializeField] private RectTransform contentRoot;

    [InspectorLabel("전체 캔버스 그룹")]
    [SerializeField] private CanvasGroup contentCanvasGroup;


    [Header("스탯 패널")]

    [InspectorLabel("스탯 패널")]
    [SerializeField] private RectTransform statPanel;

    [InspectorLabel("스탯 패널 캔버스 그룹")]
    [SerializeField] private CanvasGroup statCanvasGroup;


    [Header("열기 연출")]

    [InspectorLabel("열기 시간")]
    [SerializeField] private float openDuration;

    [InspectorLabel("열기 시작 크기")]
    [SerializeField] private float openStartScale;

    [InspectorLabel("스탯 패널 등장 지연")]
    [SerializeField] private float statOpenDelay;

    [InspectorLabel("스탯 패널 등장 시간")]
    [SerializeField] private float statOpenDuration;

    [InspectorLabel("스탯 패널 시작 위치")]
    [SerializeField] private float statStartOffsetX;


    [Header("닫기 연출")]

    [InspectorLabel("닫기 시간")]
    [SerializeField] private float closeDuration;

    [InspectorLabel("닫기 종료 크기")]
    [SerializeField] private float closeEndScale;


    private Sequence currentSequence;

    private Vector2 statOriginalPosition;

    private bool isInitialized;


    public bool IsPlaying =>
        currentSequence != null &&
        currentSequence.IsActive() &&
        currentSequence.IsPlaying();


    private void Awake()
    {
        Initialize();
        SetClosedState();
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (contentRoot == null)
        {
            contentRoot =
                transform as RectTransform;
        }

        if (contentCanvasGroup == null &&
            contentRoot != null)
        {
            contentCanvasGroup =
                contentRoot.GetComponent<CanvasGroup>();
        }

        if (contentCanvasGroup == null &&
            contentRoot != null)
        {
            contentCanvasGroup =
                contentRoot.gameObject
                    .AddComponent<CanvasGroup>();
        }

        if (statPanel != null)
        {
            statOriginalPosition =
                statPanel.anchoredPosition;
        }

        if (statCanvasGroup == null &&
            statPanel != null)
        {
            statCanvasGroup =
                statPanel.GetComponent<CanvasGroup>();
        }

        if (statCanvasGroup == null &&
            statPanel != null)
        {
            statCanvasGroup =
                statPanel.gameObject
                    .AddComponent<CanvasGroup>();
        }

        isInitialized = true;
    }

    public void PlayOpen(
        bool showStatPanel)
    {
        Initialize();
        KillSequence();

        if (contentRoot == null)
            return;

        contentRoot.gameObject.SetActive(true);

        contentRoot.localScale =
            Vector3.one * openStartScale;

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = 0f;
            contentCanvasGroup.interactable = false;
            contentCanvasGroup.blocksRaycasts = false;
        }

        PrepareStatPanel(
            showStatPanel);

        currentSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        currentSequence.Join(
            contentRoot
                .DOScale(
                    Vector3.one,
                    openDuration)
                .SetEase(Ease.OutBack));

        if (contentCanvasGroup != null)
        {
            currentSequence.Join(
                contentCanvasGroup
                    .DOFade(
                        1f,
                        openDuration)
                    .SetEase(Ease.OutQuad));
        }

        if (showStatPanel &&
            statPanel != null &&
            statPanel.gameObject.activeInHierarchy)
        {
            currentSequence.Insert(
                statOpenDelay,
                statPanel
                    .DOAnchorPos(
                        statOriginalPosition,
                        statOpenDuration)
                    .SetEase(Ease.OutCubic));

            if (statCanvasGroup != null)
            {
                currentSequence.Insert(
                    statOpenDelay,
                    statCanvasGroup
                        .DOFade(
                            1f,
                            statOpenDuration)
                        .SetEase(Ease.OutQuad));
            }
        }

        currentSequence.OnComplete(() =>
        {
            CompleteOpenState();
            currentSequence = null;
        });
    }

    public void PlayClose(
        Action onComplete)
    {
        Initialize();

        if (IsPlaying)
            return;

        KillSequence();

        if (contentRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.interactable = false;
            contentCanvasGroup.blocksRaycasts = false;
        }

        currentSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        currentSequence.Join(
            contentRoot
                .DOScale(
                    Vector3.one * closeEndScale,
                    closeDuration)
                .SetEase(Ease.InCubic));

        if (contentCanvasGroup != null)
        {
            currentSequence.Join(
                contentCanvasGroup
                    .DOFade(
                        0f,
                        closeDuration)
                    .SetEase(Ease.InQuad));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            SetClosedState();

            onComplete?.Invoke();
        });
    }

    private void PrepareStatPanel(
        bool showStatPanel)
    {
        if (statPanel == null)
            return;

        statPanel.DOKill();

        if (showStatPanel)
        {
            statPanel.anchoredPosition =
                statOriginalPosition +
                Vector2.right *
                statStartOffsetX;

            if (statCanvasGroup != null)
            {
                statCanvasGroup.alpha = 0f;
            }

            return;
        }

        statPanel.anchoredPosition =
            statOriginalPosition;

        if (statCanvasGroup != null)
        {
            statCanvasGroup.alpha = 1f;
        }
    }

    private void CompleteOpenState()
    {
        if (contentRoot != null)
        {
            contentRoot.localScale =
                Vector3.one;
        }

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = 1f;
            contentCanvasGroup.interactable = true;
            contentCanvasGroup.blocksRaycasts = true;
        }

        if (statPanel != null &&
            statPanel.gameObject.activeSelf)
        {
            statPanel.anchoredPosition =
                statOriginalPosition;

            if (statCanvasGroup != null)
            {
                statCanvasGroup.alpha = 1f;
            }
        }
    }

    private void SetClosedState()
    {
        if (contentRoot != null)
        {
            contentRoot.localScale =
                Vector3.one;
        }

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = 0f;
            contentCanvasGroup.interactable = false;
            contentCanvasGroup.blocksRaycasts = false;
        }

        if (statPanel != null)
        {
            statPanel.anchoredPosition =
                statOriginalPosition;
        }

        if (statCanvasGroup != null)
        {
            statCanvasGroup.alpha = 1f;
        }
    }

    private void KillSequence()
    {
        if (currentSequence != null)
        {
            currentSequence.Kill();
            currentSequence = null;
        }

        if (contentRoot != null)
        {
            contentRoot.DOKill();
        }

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.DOKill();
        }

        if (statPanel != null)
        {
            statPanel.DOKill();
        }

        if (statCanvasGroup != null)
        {
            statCanvasGroup.DOKill();
        }
    }
}
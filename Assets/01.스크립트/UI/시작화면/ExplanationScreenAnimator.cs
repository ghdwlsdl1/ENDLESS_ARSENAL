using System;
using DG.Tweening;
using UnityEngine;

public class ExplanationScreenAnimator : MonoBehaviour
{
    [Header("전체 설명창")]

    [InspectorLabel("전체 콘텐츠")]
    [SerializeField] private RectTransform contentRoot;

    [InspectorLabel("전체 캔버스 그룹")]
    [SerializeField] private CanvasGroup contentCanvasGroup;


    [Header("제목 텍스트")]

    [InspectorLabel("제목 텍스트")]
    [SerializeField] private RectTransform titleRoot;

    [InspectorLabel("제목 텍스트 캔버스 그룹")]
    [SerializeField] private CanvasGroup titleCanvasGroup;


    [Header("열기 연출")]

    [InspectorLabel("열기 시간")]
    [SerializeField] private float openDuration;

    [InspectorLabel("열기 시작 크기")]
    [SerializeField] private float openStartScale;


    [Header("닫기 연출")]

    [InspectorLabel("닫기 시간")]
    [SerializeField] private float closeDuration;

    [InspectorLabel("닫기 종료 크기")]
    [SerializeField] private float closeEndScale;


    private Sequence currentSequence;

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

        if (titleCanvasGroup == null &&
            titleRoot != null)
        {
            titleCanvasGroup =
                titleRoot.GetComponent<CanvasGroup>();
        }

        if (titleCanvasGroup == null &&
            titleRoot != null)
        {
            titleCanvasGroup =
                titleRoot.gameObject
                    .AddComponent<CanvasGroup>();
        }

        isInitialized = true;
    }

    public void PlayOpen()
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

        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.alpha = 0f;
        }

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

        if (titleCanvasGroup != null)
        {
            currentSequence.Join(
                titleCanvasGroup
                    .DOFade(
                        1f,
                        openDuration)
                    .SetEase(Ease.OutQuad));
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

        if (titleCanvasGroup != null)
        {
            currentSequence.Join(
                titleCanvasGroup
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

        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.alpha = 1f;
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

        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.alpha = 0f;
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

        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.DOKill();
        }
    }
}
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpRewardAnimator : MonoBehaviour
{
    [Header("전체 패널")]

    [InspectorLabel("전체 캔버스 그룹")]
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [InspectorLabel("연출 기준 오브젝트")]
    [SerializeField] private RectTransform contentRoot;


    [Header("현재 스탯 패널")]

    [InspectorLabel("현재 스탯 패널")]
    [SerializeField] private RectTransform statPanel;

    [InspectorLabel("현재 스탯 캔버스 그룹")]
    [SerializeField] private CanvasGroup statCanvasGroup;


    [Header("보상 카드")]

    [InspectorLabel("보상 카드 1")]
    [SerializeField] private RectTransform rewardCard1;

    [InspectorLabel("보상 카드 2")]
    [SerializeField] private RectTransform rewardCard2;

    [InspectorLabel("보상 카드 3")]
    [SerializeField] private RectTransform rewardCard3;

    [InspectorLabel("보상 카드 4")]
    [SerializeField] private RectTransform rewardCard4;


    [Header("버튼")]

    [InspectorLabel("보상 선택 버튼 1")]
    [SerializeField] private Button rewardButton1;

    [InspectorLabel("보상 선택 버튼 2")]
    [SerializeField] private Button rewardButton2;

    [InspectorLabel("보상 선택 버튼 3")]
    [SerializeField] private Button rewardButton3;

    [InspectorLabel("보상 선택 버튼 4")]
    [SerializeField] private Button rewardButton4;

    [InspectorLabel("리롤 버튼")]
    [SerializeField] private Button rerollButton;


    [Header("전체 등장 연출")]

    [InspectorLabel("등장 페이드 시간")]
    [SerializeField] private float openFadeDuration;

    [InspectorLabel("전체 시작 크기")]
    [SerializeField] private float openStartScale;

    [InspectorLabel("전체 확대 시간")]
    [SerializeField] private float openScaleDuration;


    [Header("스탯 패널 등장 연출")]

    [InspectorLabel("스탯 패널 등장 지연")]
    [SerializeField] private float statOpenDelay;

    [InspectorLabel("스탯 패널 등장 시간")]
    [SerializeField] private float statOpenDuration;

    [InspectorLabel("스탯 패널 시작 X 오프셋")]
    [SerializeField] private float statOpenOffsetX;


    [Header("카드 등장 연출")]

    [InspectorLabel("카드 등장 Y 오프셋")]
    [SerializeField] private float cardOpenOffsetY;

    [InspectorLabel("카드 시작 크기")]
    [SerializeField] private float cardOpenStartScale;

    [InspectorLabel("카드 등장 시간")]
    [SerializeField] private float cardOpenDuration;

    [InspectorLabel("카드 등장 시작 지연")]
    [SerializeField] private float cardOpenStartDelay;

    [InspectorLabel("카드 등장 간격")]
    [SerializeField] private float cardOpenDelay;


    [Header("리롤 연출")]

    [InspectorLabel("카드 접힘 시간")]
    [SerializeField] private float rerollFoldDuration;

    [InspectorLabel("카드 펼침 시간")]
    [SerializeField] private float rerollUnfoldDuration;

    [InspectorLabel("카드 리롤 간격")]
    [SerializeField] private float rerollCardDelay;


    [Header("선택 연출")]

    [InspectorLabel("선택 카드 확대 배율")]
    [SerializeField] private float selectedCardScale ;

    [InspectorLabel("비선택 카드 축소 배율")]
    [SerializeField] private float unselectedCardScale;

    [InspectorLabel("비선택 카드 이동 거리")]
    [SerializeField] private float unselectedCardMoveY;

    [InspectorLabel("카드 선택 연출 시간")]
    [SerializeField] private float selectDuration;

    [InspectorLabel("선택 유지 시간")]
    [SerializeField] private float selectHoldDuration;


    [Header("종료 연출")]

    [InspectorLabel("종료 페이드 시간")]
    [SerializeField] private float closeFadeDuration;

    [InspectorLabel("종료 크기")]
    [SerializeField] private float closeTargetScale;


    private readonly RectTransform[] rewardCards =
        new RectTransform[4];

    private readonly Button[] rewardButtons =
        new Button[4];

    private readonly Vector2[] initialCardPositions =
        new Vector2[4];

    private readonly Vector3[] initialCardScales =
        new Vector3[4];

    private readonly Vector3[] initialCardRotations =
        new Vector3[4];


    private Vector3 initialContentScale =
        Vector3.one;

    private Vector2 initialStatPosition;

    private Sequence currentSequence;

    private bool rerollAvailable = true;
    private bool hasCachedInitialValues;


    public bool IsPlaying =>
        currentSequence != null &&
        currentSequence.IsActive() &&
        currentSequence.IsPlaying();


    private void Awake()
    {
        CacheReferences();
        FindCanvasGroups();
    }

    private void OnDisable()
    {
        KillCurrentSequence();

        if (hasCachedInitialValues)
        {
            RestoreVisualState();
        }
    }

    private void FindCanvasGroups()
    {
        if (panelCanvasGroup == null &&
            contentRoot != null)
        {
            panelCanvasGroup =
                contentRoot.GetComponent<CanvasGroup>();
        }

        if (statCanvasGroup == null &&
            statPanel != null)
        {
            statCanvasGroup =
                statPanel.GetComponent<CanvasGroup>();
        }
    }


    public void PlayOpen(
        bool canReroll,
        Action onComplete = null)
    {
        rerollAvailable =
            canReroll;

        KillCurrentSequence();

        FindCanvasGroups();
        RefreshLayoutAndCache();
        RestoreVisualState();

        SetAllButtonsInteractable(false);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        if (contentRoot != null)
        {
            float startScale =
                Mathf.Max(
                    0f,
                    openStartScale);

            contentRoot.localScale =
                initialContentScale *
                startScale;
        }

        SetStatPanelToOpenStartState();
        SetCardsToOpenStartState();

        currentSequence =
            CreateSequence();

        if (panelCanvasGroup != null)
        {
            currentSequence.Insert(
                0f,
                panelCanvasGroup
                    .DOFade(
                        1f,
                        Mathf.Max(
                            0.01f,
                            openFadeDuration))
                    .SetEase(
                        Ease.OutQuad));
        }

        if (contentRoot != null)
        {
            currentSequence.Insert(
                0f,
                contentRoot
                    .DOScale(
                        initialContentScale,
                        Mathf.Max(
                            0.01f,
                            openScaleDuration))
                    .SetEase(
                        Ease.OutBack));
        }

        PlayStatPanelOpen();
        PlayCardsOpen();

        currentSequence.OnComplete(
            () =>
            {
                RestoreStatPanelOnly();
                EnableCanvasInteraction();
                UnlockButtons();

                onComplete?.Invoke();
            });
    }

    private void PlayStatPanelOpen()
    {
        if (statPanel == null ||
            !statPanel.gameObject.activeInHierarchy)
        {
            return;
        }

        float delay =
            Mathf.Max(
                0f,
                statOpenDelay);

        float duration =
            Mathf.Max(
                0.01f,
                statOpenDuration);

        currentSequence.Insert(
            delay,
            statPanel
                .DOAnchorPos(
                    initialStatPosition,
                    duration)
                .SetEase(
                    Ease.OutCubic));

        if (statCanvasGroup != null)
        {
            currentSequence.Insert(
                delay,
                statCanvasGroup
                    .DOFade(
                        1f,
                        duration)
                    .SetEase(
                        Ease.OutQuad));
        }
    }

    private void PlayCardsOpen()
    {
        float startDelay =
            Mathf.Max(
                0f,
                cardOpenStartDelay);

        float duration =
            Mathf.Max(
                0.01f,
                cardOpenDuration);

        float delay =
            Mathf.Max(
                0f,
                cardOpenDelay);

        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            float startTime =
                startDelay +
                i * delay;

            currentSequence.Insert(
                startTime,
                card
                    .DOAnchorPos(
                        initialCardPositions[i],
                        duration)
                    .SetEase(
                        Ease.OutCubic));

            currentSequence.Insert(
                startTime,
                card
                    .DOScale(
                        initialCardScales[i],
                        duration)
                    .SetEase(
                        Ease.OutBack));
        }
    }


    public void PlayReroll(
        Action refreshRewards,
        bool canRerollAfterAnimation,
        Action onComplete = null)
    {
        if (IsPlaying)
            return;

        rerollAvailable =
            canRerollAfterAnimation;

        KillCurrentSequence();
        RestoreCardsOnly();
        SetAllButtonsInteractable(false);

        float foldDuration =
            Mathf.Max(
                0.01f,
                rerollFoldDuration);

        float unfoldDuration =
            Mathf.Max(
                0.01f,
                rerollUnfoldDuration);

        float cardDelay =
            Mathf.Max(
                0f,
                rerollCardDelay);

        currentSequence =
            CreateSequence();

        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            Vector3 foldRotation =
                initialCardRotations[i];

            foldRotation.y += 90f;

            currentSequence.Insert(
                i * cardDelay,
                card
                    .DOLocalRotate(
                        foldRotation,
                        foldDuration,
                        RotateMode.Fast)
                    .SetEase(
                        Ease.InCubic));
        }

        float refreshTime =
            foldDuration +
            cardDelay *
            (rewardCards.Length - 1);

        currentSequence.InsertCallback(
            refreshTime,
            () =>
            {
                refreshRewards?.Invoke();
                SetCardsToOppositeFold();
            });

        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            currentSequence.Insert(
                refreshTime +
                i * cardDelay,
                card
                    .DOLocalRotate(
                        initialCardRotations[i],
                        unfoldDuration,
                        RotateMode.Fast)
                    .SetEase(
                        Ease.OutCubic));
        }

        currentSequence.OnComplete(
            () =>
            {
                UnlockButtons();
                onComplete?.Invoke();
            });
    }


    public void PlaySelect(
        int selectedIndex,
        Action onComplete)
    {
        if (IsPlaying)
            return;

        if (selectedIndex < 0 ||
            selectedIndex >= rewardCards.Length)
        {
            return;
        }

        KillCurrentSequence();
        RestoreCardsOnly();
        SetAllButtonsInteractable(false);

        float duration =
            Mathf.Max(
                0.01f,
                selectDuration);

        currentSequence =
            CreateSequence();

        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            if (i == selectedIndex)
            {
                currentSequence.Insert(
                    0f,
                    card
                        .DOScale(
                            initialCardScales[i] *
                            Mathf.Max(
                                0f,
                                selectedCardScale),
                            duration)
                        .SetEase(
                            Ease.OutBack));

                continue;
            }

            currentSequence.Insert(
                0f,
                card
                    .DOScale(
                        initialCardScales[i] *
                        Mathf.Max(
                            0f,
                            unselectedCardScale),
                        duration)
                    .SetEase(
                        Ease.InQuad));

            currentSequence.Insert(
                0f,
                card
                    .DOAnchorPosY(
                        initialCardPositions[i].y +
                        unselectedCardMoveY,
                        duration)
                    .SetEase(
                        Ease.InQuad));
        }

        currentSequence.AppendInterval(
            Mathf.Max(
                0f,
                selectHoldDuration));

        currentSequence.OnComplete(
            () =>
            {
                onComplete?.Invoke();
            });
    }


    public void PlayClose(
        Action onComplete)
    {
        KillCurrentSequence();
        SetAllButtonsInteractable(false);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        currentSequence =
            CreateSequence();

        float duration =
            Mathf.Max(
                0.01f,
                closeFadeDuration);

        if (panelCanvasGroup != null)
        {
            currentSequence.Insert(
                0f,
                panelCanvasGroup
                    .DOFade(
                        0f,
                        duration)
                    .SetEase(
                        Ease.InQuad));
        }

        if (contentRoot != null)
        {
            currentSequence.Insert(
                0f,
                contentRoot
                    .DOScale(
                        initialContentScale *
                        Mathf.Max(
                            0f,
                            closeTargetScale),
                        duration)
                    .SetEase(
                        Ease.InQuad));
        }

        currentSequence.OnComplete(
            () =>
            {
                onComplete?.Invoke();
            });
    }


    public void SetRerollAvailable(
        bool available)
    {
        rerollAvailable =
            available;

        if (rerollButton == null ||
            IsPlaying)
        {
            return;
        }

        rerollButton.interactable =
            available;
    }

    public void ResetAnimationState(
        bool canReroll)
    {
        rerollAvailable =
            canReroll;

        KillCurrentSequence();

        if (hasCachedInitialValues)
        {
            RestoreVisualState();
        }
    }


    private Sequence CreateSequence()
    {
        Sequence sequence =
            DOTween.Sequence();

        sequence.SetUpdate(true);

        sequence.OnKill(
            () =>
            {
                if (currentSequence == sequence)
                {
                    currentSequence = null;
                }
            });

        return sequence;
    }

    private void SetStatPanelToOpenStartState()
    {
        if (statPanel == null ||
            !statPanel.gameObject.activeInHierarchy)
        {
            return;
        }

        statPanel.anchoredPosition =
            initialStatPosition +
            Vector2.right *
            statOpenOffsetX;

        if (statCanvasGroup != null)
        {
            statCanvasGroup.alpha = 0f;
        }
    }

    private void SetCardsToOpenStartState()
    {
        float startScale =
            Mathf.Max(
                0f,
                cardOpenStartScale);

        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            card.anchoredPosition =
                initialCardPositions[i] +
                Vector2.up *
                cardOpenOffsetY;

            card.localScale =
                initialCardScales[i] *
                startScale;

            card.localEulerAngles =
                initialCardRotations[i];
        }
    }

    private void SetCardsToOppositeFold()
    {
        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            Vector3 rotation =
                initialCardRotations[i];

            rotation.y -= 90f;

            card.localEulerAngles =
                rotation;
        }
    }

    private void RestoreStatPanelOnly()
    {
        if (!hasCachedInitialValues ||
            statPanel == null)
        {
            return;
        }

        statPanel.anchoredPosition =
            initialStatPosition;

        if (statCanvasGroup != null)
        {
            statCanvasGroup.alpha = 1f;
        }
    }

    private void RestoreCardsOnly()
    {
        if (!hasCachedInitialValues)
            return;

        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            card.anchoredPosition =
                initialCardPositions[i];

            card.localScale =
                initialCardScales[i];

            card.localEulerAngles =
                initialCardRotations[i];
        }
    }

    private void RestoreVisualState()
    {
        if (!hasCachedInitialValues)
            return;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        if (contentRoot != null)
        {
            contentRoot.localScale =
                initialContentScale;
        }

        RestoreStatPanelOnly();
        RestoreCardsOnly();
        UnlockButtons();
    }

    private void EnableCanvasInteraction()
    {
        if (panelCanvasGroup == null)
            return;

        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
    }

    private void UnlockButtons()
    {
        SetRewardButtonsInteractable(true);

        if (rerollButton != null)
        {
            rerollButton.interactable =
                rerollAvailable;
        }
    }

    private void SetAllButtonsInteractable(
        bool interactable)
    {
        SetRewardButtonsInteractable(
            interactable);

        if (rerollButton != null)
        {
            rerollButton.interactable =
                interactable;
        }
    }

    private void SetRewardButtonsInteractable(
        bool interactable)
    {
        for (int i = 0;
             i < rewardButtons.Length;
             i++)
        {
            if (rewardButtons[i] == null)
                continue;

            rewardButtons[i].interactable =
                interactable;
        }
    }

    private void CacheReferences()
    {
        rewardCards[0] =
            rewardCard1;

        rewardCards[1] =
            rewardCard2;

        rewardCards[2] =
            rewardCard3;

        rewardCards[3] =
            rewardCard4;

        rewardButtons[0] =
            rewardButton1;

        rewardButtons[1] =
            rewardButton2;

        rewardButtons[2] =
            rewardButton3;

        rewardButtons[3] =
            rewardButton4;
    }

    private void CacheInitialValues()
    {
        if (contentRoot != null)
        {
            initialContentScale =
                contentRoot.localScale;
        }

        if (statPanel != null)
        {
            initialStatPosition =
                statPanel.anchoredPosition;
        }

        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            initialCardPositions[i] =
                card.anchoredPosition;

            initialCardScales[i] =
                card.localScale;

            initialCardRotations[i] =
                card.localEulerAngles;
        }

        hasCachedInitialValues =
            true;
    }

    private void RefreshLayoutAndCache()
    {
        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    contentRoot);
        }

        if (statPanel != null)
        {
            RectTransform statParent =
                statPanel.parent as RectTransform;

            if (statParent != null)
            {
                LayoutRebuilder
                    .ForceRebuildLayoutImmediate(
                        statParent);
            }
        }

        for (int i = 0;
             i < rewardCards.Length;
             i++)
        {
            RectTransform card =
                rewardCards[i];

            if (card == null)
                continue;

            RectTransform parent =
                card.parent as RectTransform;

            if (parent != null)
            {
                LayoutRebuilder
                    .ForceRebuildLayoutImmediate(
                        parent);
            }
        }

        Canvas.ForceUpdateCanvases();

        CacheInitialValues();
    }

    private void KillCurrentSequence()
    {
        if (currentSequence == null)
            return;

        currentSequence.Kill();
        currentSequence = null;
    }
}
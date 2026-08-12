using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryCellUI : MonoBehaviour, IPointerClickHandler
{
    [Header("색상")]

    [InspectorLabel("열린 칸 색상")]
    [SerializeField] private Color unlockedColor;

    [InspectorLabel("잠긴 칸 색상")]
    [SerializeField] private Color lockedColor;

    [InspectorLabel("확장 가능 색상")]
    [SerializeField] private Color unlockableColor;

    [Header("확장 가능 연출")]

    [InspectorLabel("펄스 최소 크기")]
    [SerializeField] private float unlockablePulseScale;

    [InspectorLabel("펄스 시간")]
    [SerializeField] private float unlockablePulseDuration;

    [InspectorLabel("펄스 투명도")]
    [SerializeField] private float unlockablePulseAlpha;

    [Header("잠금 해제 연출")]

    [InspectorLabel("해금 시작 크기")]
    [SerializeField] private float unlockStartScale;

    [InspectorLabel("해금 확대 크기")]
    [SerializeField] private float unlockOvershootScale;

    [InspectorLabel("해금 확대 시간")]
    [SerializeField] private float unlockExpandDuration;

    [InspectorLabel("해금 복귀 시간")]
    [SerializeField] private float unlockReturnDuration;
    
    private Image backgroundImage;
    private InventoryUI inventoryUI;
    private RectTransform rectTransform;

    private Sequence stateSequence;
    private Tween pulseTween;

    private bool stateInitialized;
    private bool previousUnlocked;
    private bool previousCanUnlock;
    
    public int X { get; private set; }
    public int Y { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        KillTweens();
        RestoreTransform();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void Initialize()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    public void Init(
        InventoryUI inventoryUI,
        int x,
        int y)
    {
        Initialize();

        this.inventoryUI = inventoryUI;
        X = x;
        Y = y;
    }

    public void Refresh(
        bool isUnlocked,
        bool canUnlock)
    {
        Initialize();

        if (backgroundImage == null)
            return;

        if (!stateInitialized)
        {
            stateInitialized = true;
            previousUnlocked = isUnlocked;
            previousCanUnlock = canUnlock;

            ApplyStateImmediately(
                isUnlocked,
                canUnlock);

            return;
        }

        bool wasJustUnlocked =
            !previousUnlocked &&
            isUnlocked;

        bool unlockableStateChanged =
            previousCanUnlock != canUnlock;

        previousUnlocked = isUnlocked;
        previousCanUnlock = canUnlock;

        if (wasJustUnlocked)
        {
            PlayUnlockAnimation();
            return;
        }

        if (isUnlocked &&
            stateSequence != null &&
            stateSequence.IsActive())
        {
            return;
        }

        if (unlockableStateChanged)
        {
            ApplyStateImmediately(
                isUnlocked,
                canUnlock);

            return;
        }

        if (isUnlocked)
        {
            StopUnlockablePulse();

            if (stateSequence != null &&
                stateSequence.IsActive())
            {
                return;
            }

            backgroundImage.color = unlockedColor;
            return;
        }

        if (canUnlock)
        {
            if (pulseTween == null ||
                !pulseTween.IsActive())
            {
                StartUnlockablePulse();
            }

            return;
        }

        StopUnlockablePulse();
        backgroundImage.color = lockedColor;
    }

    private void ApplyStateImmediately(
        bool isUnlocked,
        bool canUnlock)
    {
        KillStateSequence();
        StopUnlockablePulse();
        RestoreTransform();

        if (isUnlocked)
        {
            backgroundImage.color = unlockedColor;
            return;
        }

        if (canUnlock)
        {
            backgroundImage.color = unlockableColor;
            StartUnlockablePulse();
            return;
        }

        backgroundImage.color = lockedColor;
    }

    private void StartUnlockablePulse()
    {
        StopUnlockablePulse();
        RestoreTransform();

        if (backgroundImage == null ||
            rectTransform == null)
        {
            return;
        }

        Color pulseColor = unlockableColor;
        pulseColor.a = unlockablePulseAlpha;

        backgroundImage.color = unlockableColor;

        Sequence pulseSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLoops(-1, LoopType.Yoyo);

        pulseSequence.Join(
            rectTransform.DOScale(
                    unlockablePulseScale,
                    unlockablePulseDuration)
                .SetEase(Ease.InOutSine));

        pulseSequence.Join(
            backgroundImage.DOColor(
                    pulseColor,
                    unlockablePulseDuration)
                .SetEase(Ease.InOutSine));

        pulseTween = pulseSequence;
    }

    private void StopUnlockablePulse()
    {
        if (pulseTween != null &&
            pulseTween.IsActive())
        {
            pulseTween.Kill();
        }

        pulseTween = null;
    }

    private void PlayUnlockAnimation()
    {
        KillStateSequence();
        StopUnlockablePulse();

        if (backgroundImage == null ||
            rectTransform == null)
        {
            ApplyStateImmediately(true, false);
            return;
        }

        rectTransform.localScale =
            Vector3.one * unlockStartScale;

        backgroundImage.color = unlockedColor;

        stateSequence = DOTween.Sequence()
            .SetUpdate(true);

        stateSequence.Append(
            rectTransform.DOScale(
                    unlockOvershootScale,
                    unlockExpandDuration)
                .SetEase(Ease.OutBack));

        stateSequence.Append(
            rectTransform.DOScale(
                    1f,
                    unlockReturnDuration)
                .SetEase(Ease.OutQuad));

        stateSequence.OnComplete(() =>
        {
            stateSequence = null;

            RestoreTransform();

            if (backgroundImage != null)
                backgroundImage.color = unlockedColor;
        });
    }

    private void KillStateSequence()
    {
        if (stateSequence != null &&
            stateSequence.IsActive())
        {
            stateSequence.Kill();
        }

        stateSequence = null;
    }

    private void KillTweens()
    {
        KillStateSequence();
        StopUnlockablePulse();
    }

    private void RestoreTransform()
    {
        if (rectTransform != null)
            rectTransform.localScale = Vector3.one;
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (inventoryUI == null)
            return;

        inventoryUI.TryUnlockCell(this);
    }
}
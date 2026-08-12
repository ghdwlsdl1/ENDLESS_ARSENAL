using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [InspectorLabel("아이콘")]
    [SerializeField] private Image iconImage;

    [InspectorLabel("외곽선")]
    [SerializeField] private Image borderImage;
    
    [Header("버리기 연출")]

    [InspectorLabel("연출 시간")]
    [SerializeField] private float discardDuration;

    [InspectorLabel("최종 크기")]
    [SerializeField] private float discardScale;

    [InspectorLabel("회전 각도")]
    [SerializeField] private float discardRotateAngle;

    [InspectorLabel("조건 미충족 색상")]
    [SerializeField] private Color conditionInactiveColor;

    private InventoryUI inventoryUI;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private LayoutElement layoutElement;
    private Outline outline;

    private bool isInitialized;
    private bool isSelected;
    private bool isDragging;
    private bool isPlacementBlocked;
    private bool isConditionActive = true;

    public InventoryItem Item { get; private set; }
    public bool IsPlaced { get; private set; }
    public int PlacedX { get; private set; }
    public int PlacedY { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    public void Init(InventoryUI inventoryUI, InventoryItem item)
    {
        Initialize();

        this.inventoryUI = inventoryUI;
        Item = item;
        Refresh();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        backgroundImage = GetComponent<Image>();
        layoutElement = GetComponent<LayoutElement>();
        outline = GetComponent<Outline>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();

        if (outline == null)
            outline = gameObject.AddComponent<Outline>();

        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(1f, 1f, 1f, 0f);
            backgroundImage.raycastTarget = true;
        }

        outline.effectDistance = new Vector2(2f, -2f);
        outline.enabled = false;

        isInitialized = true;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshStateColor();
    }
    
    public void SetPlacementBlocked(
        bool blocked)
    {
        isPlacementBlocked = blocked;
        RefreshStateColor();
    }
    
    public void SetConditionActive(bool active)
    {
        isConditionActive = active;
        RefreshIconColor();
    }

    public void SetPlaced(int x, int y)
    {
        IsPlaced = true;
        isDragging = false;

        PlacedX = x;
        PlacedY = y;

        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
    }

    public void SetWaiting()
    {
        IsPlaced = false;
        isDragging = false;

        PlacedX = -1;
        PlacedY = -1;

        if (layoutElement != null)
            layoutElement.ignoreLayout = false;
        
        SetConditionActive(true);
    }

    public void SetDragging()
    {
        isDragging = true;

        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
    }

    public void Refresh()
    {
        Initialize();

        if (Item == null || Item.Data == null || inventoryUI == null)
            return;

        Vector2 size = inventoryUI.GetItemSize(Item);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        if (layoutElement != null)
        {
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
            layoutElement.minWidth = size.x;
            layoutElement.minHeight = size.y;
        }

        if (borderImage != null)
        {
            RectTransform borderRect = borderImage.rectTransform;
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            borderRect.localScale = Vector3.one;
            borderRect.localRotation = Quaternion.identity;
        }

        RefreshStateColor();

        if (iconImage == null)
            return;

        iconImage.sprite = Item.Data.Icon;
        iconImage.enabled = true;
        iconImage.preserveAspect = false;
        iconImage.raycastTarget = false;

        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.localScale = Vector3.one;

        if (Item.IsRotated)
        {
            iconRect.sizeDelta = new Vector2(size.y, size.x);
            iconRect.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }
        else
        {
            iconRect.sizeDelta = size;
            iconRect.localRotation = Quaternion.identity;
        }

        RefreshIconColor();
    }

    private void RefreshIconColor()
    {
        if (iconImage == null)
            return;

        iconImage.color = isConditionActive ? Color.white : conditionInactiveColor;
    }

    private void RefreshStateColor()
    {
        Color stateColor;
        bool visible;

        if (isPlacementBlocked)
        {
            stateColor = Color.red;
            visible = true;
        }
        else if (isDragging)
        {
            stateColor = Color.yellow;
            visible = true;
        }
        else if (isSelected)
        {
            stateColor = Color.blue;
            visible = true;
        }
        else if (IsPlaced)
        {
            stateColor = Color.green;
            visible = true;
        }
        else
        {
            stateColor = Color.white;
            visible = false;
        }

        float alpha =
            visible ? 120f / 255f : 0f;

        if (borderImage != null)
        {
            borderImage.enabled = true;

            borderImage.color = new Color(
                stateColor.r,
                stateColor.g,
                stateColor.b,
                alpha);
        }

        if (outline != null)
        {
            outline.enabled = visible;

            outline.effectColor = new Color(
                stateColor.r,
                stateColor.g,
                stateColor.b,
                alpha);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        inventoryUI.BeginDrag(this);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        inventoryUI.Drag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        inventoryUI.EndDrag(this, eventData);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryUI != null)
            inventoryUI.SelectItem(this);
    }
    
    public void PlayDiscard(
        Action onComplete)
    {
        transform.DOKill();

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.DOKill();

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        canvasGroup.alpha = 1f;

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true);

        sequence.Join(
            transform.DOScale(
                    discardScale,
                    discardDuration)
                .SetEase(Ease.InBack));

        sequence.Join(
            transform.DORotate(
                new Vector3(
                    0f,
                    0f,
                    discardRotateAngle),
                discardDuration,
                RotateMode.LocalAxisAdd));

        sequence.Join(
            canvasGroup.DOFade(
                0f,
                discardDuration));

        sequence.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemDiscardZoneUI : MonoBehaviour
{
    [InspectorLabel("인벤토리 UI")]
    [SerializeField] private InventoryUI inventoryUI;

    [InspectorLabel("버리기 구역")]
    [SerializeField] private Image background;

    [InspectorLabel("아이템 선택 화면 UI")]
    [SerializeField] private ItemSelectScreenUI itemSelectScreenUI;

    [Header("전체 버리기")]

    [InspectorLabel("확인 팝업")]
    [SerializeField] private GameObject popupWindow;

    [InspectorLabel("길게 누르기 시간")]
    [SerializeField] private float holdDuration;

    [Header("색상")]

    [InspectorLabel("기본")]
    [SerializeField] private Color normalColor;

    [InspectorLabel("경고")]
    [SerializeField] private Color highlightColor;

    private bool isHolding;
    private float holdElapsedTime;

    private void Awake()
    {
        SetNormal();

        if (popupWindow != null)
            popupWindow.SetActive(false);
    }

    private void Update()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            UpdateTouchHold();
            return;
        }

        UpdateMouseHold();
    }

    private void OnDisable()
    {
        CancelHold();
    }

    private void UpdateMouseHold()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        Vector2 pointerPosition =
            mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (CanStartHold() &&
                IsPointerInside(pointerPosition))
            {
                StartHold();
            }

            return;
        }

        if (!isHolding)
            return;

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            CancelHold();
            return;
        }

        if (!mouse.leftButton.isPressed ||
            !IsPointerInside(pointerPosition))
        {
            CancelHold();
            return;
        }

        UpdateHoldTime();
    }

    private void UpdateTouchHold()
    {
        Touchscreen touchscreen =
            Touchscreen.current;

        if (touchscreen == null)
            return;

        var touch =
            touchscreen.primaryTouch;

        Vector2 pointerPosition =
            touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
        {
            if (CanStartHold() &&
                IsPointerInside(pointerPosition))
            {
                StartHold();
            }

            return;
        }

        if (!isHolding)
            return;

        if (touch.press.wasReleasedThisFrame)
        {
            CancelHold();
            return;
        }

        if (!touch.press.isPressed ||
            !IsPointerInside(pointerPosition))
        {
            CancelHold();
            return;
        }

        UpdateHoldTime();
    }

    private bool CanStartHold()
    {
        if (inventoryUI == null ||
            itemSelectScreenUI == null ||
            background == null)
        {
            return false;
        }

        if (!itemSelectScreenUI.IsOpen)
            return false;

        if (inventoryUI.IsDraggingItem())
            return false;

        if (popupWindow != null &&
            popupWindow.activeSelf)
        {
            return false;
        }

        return true;
    }

    private void StartHold()
    {
        isHolding = true;
        holdElapsedTime = 0f;

        SetHighlight();
    }

    private void UpdateHoldTime()
    {
        if (!isHolding)
            return;

        if (!CanContinueHold())
        {
            CancelHold();
            return;
        }

        holdElapsedTime +=
            Time.unscaledDeltaTime;

        if (holdElapsedTime <
            holdDuration)
        {
            return;
        }

        CompleteHold();
    }

    private bool CanContinueHold()
    {
        if (!gameObject.activeInHierarchy)
            return false;

        if (inventoryUI == null ||
            inventoryUI.IsDraggingItem())
        {
            return false;
        }

        if (itemSelectScreenUI == null ||
            !itemSelectScreenUI.IsOpen)
        {
            return false;
        }

        if (popupWindow != null &&
            popupWindow.activeSelf)
        {
            return false;
        }

        return true;
    }

    private void CompleteHold()
    {
        isHolding = false;
        holdElapsedTime = 0f;

        SetNormal();
        OpenPopup();
    }

    private void CancelHold()
    {
        isHolding = false;
        holdElapsedTime = 0f;

        SetNormal();
    }

    private void OpenPopup()
    {
        if (popupWindow != null)
            popupWindow.SetActive(true);
    }

    public void ConfirmButton()
    {
        if (popupWindow != null)
            popupWindow.SetActive(false);

        if (itemSelectScreenUI != null)
        {
            itemSelectScreenUI
                .DiscardRemainingCandidates();
        }

        SetNormal();
    }

    public void CancelButton()
    {
        if (popupWindow != null)
            popupWindow.SetActive(false);

        SetNormal();
    }

    private bool IsPointerInside(
        Vector2 pointerPosition)
    {
        if (background == null ||
            !background.isActiveAndEnabled ||
            !background.gameObject
                .activeInHierarchy)
        {
            return false;
        }

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                background.rectTransform,
                pointerPosition,
                GetEventCamera());
    }

    private Camera GetEventCamera()
    {
        Canvas canvas =
            background != null
                ? background.canvas
                : null;

        if (canvas == null ||
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    public void RefreshHover(
        PointerEventData eventData)
    {
        if (inventoryUI == null ||
            !inventoryUI.IsDraggingItem())
        {
            if (!isHolding)
                SetNormal();

            return;
        }

        if (IsPointerOver(eventData))
            SetHighlight();
        else
            SetNormal();
    }

    public bool IsPointerOver(
        PointerEventData eventData)
    {
        if (itemSelectScreenUI == null ||
            !itemSelectScreenUI.IsOpen)
        {
            return false;
        }

        if (background == null ||
            !background.isActiveAndEnabled ||
            !background.gameObject
                .activeInHierarchy ||
            eventData == null)
        {
            return false;
        }

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                background.rectTransform,
                eventData.position,
                eventData.pressEventCamera);
    }

    public void SetNormal()
    {
        if (background != null)
            background.color = normalColor;
    }

    private void SetHighlight()
    {
        if (background != null)
            background.color = highlightColor;
    }
}
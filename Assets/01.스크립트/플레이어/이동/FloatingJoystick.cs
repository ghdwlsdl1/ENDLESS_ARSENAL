using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [InspectorLabel("조이스틱 루트")]
    [SerializeField] private RectTransform joystickRoot;

    [InspectorLabel("조이스틱 손잡이")]
    [SerializeField] private RectTransform handle;

    [InspectorLabel("플레이어 이동")]
    [SerializeField] private PlayerMove playerMove;

    private float radius = 100f;
    private float deadZone = 0.1f;
    private bool hideWhenReleased = true;
    private Canvas canvas;
    private int activePointerId = int.MinValue;
    private bool isDragging;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();

        ResetJoystick();

        if (joystickRoot != null && hideWhenReleased)
            joystickRoot.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDragging)
            return;

        if (joystickRoot == null ||
            handle == null ||
            playerMove == null)
        {
            return;
        }

        RectTransform parent =
            joystickRoot.parent as RectTransform;

        if (parent == null)
            return;

        activePointerId = eventData.pointerId;
        isDragging = true;

        joystickRoot.gameObject.SetActive(true);

        Camera eventCamera = GetEventCamera(eventData);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                eventData.position,
                eventCamera,
                out Vector2 localPosition))
        {
            CancelJoystick();
            return;
        }

        joystickRoot.anchoredPosition = localPosition;
        handle.anchoredPosition = Vector2.zero;

        playerMove.SetMoveInput(Vector2.zero);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (eventData.pointerId != activePointerId)
            return;

        if (joystickRoot == null ||
            handle == null ||
            playerMove == null)
        {
            return;
        }

        Camera eventCamera = GetEventCamera(eventData);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickRoot,
                eventData.position,
                eventCamera,
                out Vector2 localPosition))
        {
            return;
        }

        float safeRadius = Mathf.Max(1f, radius);

        Vector2 clampedPosition =
            Vector2.ClampMagnitude(localPosition, safeRadius);

        handle.anchoredPosition = clampedPosition;

        Vector2 input =
            clampedPosition / safeRadius;

        float inputMagnitude = input.magnitude;

        if (inputMagnitude <= deadZone)
        {
            input = Vector2.zero;
        }
        else
        {
            float normalizedMagnitude =
                Mathf.InverseLerp(
                    deadZone,
                    1f,
                    inputMagnitude);

            input =
                input.normalized *
                normalizedMagnitude;
        }

        playerMove.SetMoveInput(input);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (eventData.pointerId != activePointerId)
            return;

        CancelJoystick();
    }

    private void CancelJoystick()
    {
        isDragging = false;
        activePointerId = int.MinValue;

        ResetJoystick();

        if (joystickRoot != null && hideWhenReleased)
            joystickRoot.gameObject.SetActive(false);
    }

    private void ResetJoystick()
    {
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (playerMove != null)
            playerMove.SetMoveInput(Vector2.zero);
    }

    private Camera GetEventCamera(
        PointerEventData eventData)
    {
        if (canvas == null)
            return eventData.pressEventCamera;

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return eventData.pressEventCamera;
    }

    private void OnDisable()
    {
        isDragging = false;
        activePointerId = int.MinValue;

        ResetJoystick();
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            CancelJoystick();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            CancelJoystick();
    }
}
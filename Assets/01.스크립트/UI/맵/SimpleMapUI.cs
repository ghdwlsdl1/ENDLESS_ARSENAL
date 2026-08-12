using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class SimpleMapUI : MonoBehaviour
{
    [InspectorLabel("UI 매니저")]
    [SerializeField] private UIManager uiManager;

    [InspectorLabel("플레이어")]
    [SerializeField] private Transform player;
    
    [InspectorLabel("거리 추적")]
    [SerializeField] private PlayerDistanceTracker distanceTracker;

    [InspectorLabel("지도 루트")]
    [SerializeField] private RectTransform mapRoot;

    [InspectorLabel("지도 연출")]
    [SerializeField] private MapScreenAnimator mapAnimator;
    
    [InspectorLabel("포탈 버튼")]
    [SerializeField] private GameObject portal;
    
    [InspectorLabel("워프 확인창")]
    [SerializeField] private GameObject warpPopup;
    
    [InspectorLabel("나가기 가능 표시")]
    [SerializeField] private GameObject possible;


    [Header("프리팹")]
    [InspectorLabel("보급품 아이콘 프리팹")]
    [SerializeField] private RectTransform supplyIconPrefab;

    [InspectorLabel("화살표 아이콘 프리팹")]
    [SerializeField] private RectTransform arrowIconPrefab;

    [Header("지도 설정")]
    [InspectorLabel("최대 표시 거리")]
    [SerializeField] private float visibleWorldRadius;

    [InspectorLabel("아이콘 표시 거리")]
    [SerializeField] private float iconWorldRadius;

    [InspectorLabel("미니맵 아이콘 반지름")]
    [SerializeField] private float mapUiRadius;

    [InspectorLabel("화살표 반지름")]
    [SerializeField] private float arrowRadius;
    
    [Header("탐색")]
    [InspectorLabel("타겟 타입")]
    [SerializeField] private MapTargetType targetType =
        MapTargetType.SupplyCrate;
    
    private readonly List<RectTransform> activeIcons = new();
    private readonly List<RectTransform> activeArrows = new();

    private readonly Stack<RectTransform> iconPool = new();
    private readonly Stack<RectTransform> arrowPool = new();
    
    public bool IsOpen => isOpen;
    
    private bool isOpen;

    private bool isWarping;

    private void Reset()
    {
        mapRoot = GetComponent<RectTransform>();
        mapAnimator = GetComponent<MapScreenAnimator>();
    }

    private void Awake()
    {
        if (uiManager == null)
        {
            uiManager =
                FindFirstObjectByType<UIManager>();
        }

        if (mapAnimator == null)
        {
            mapAnimator =
                GetComponent<MapScreenAnimator>();
        }
    }

    private void Start()
    {
        isOpen = false;

        if (possible != null)
            possible.SetActive(false);

        if (portal != null)
            portal.SetActive(false);
        
        if (warpPopup != null)
            warpPopup.SetActive(false);

        ReturnAll();

        if (uiManager != null)
        {
            uiManager.HidePanel(
                UIPanelType.MapScreen);
        }
    }
    
    public void OpenWarpPopupButton()
    {
        if (!isOpen)
            return;

        if (warpPopup != null)
            warpPopup.SetActive(true);
    }

    public void CloseWarpPopupButton()
    {
        if (warpPopup != null)
            warpPopup.SetActive(false);
    }
    
    public void WarpButton()
    {
        if (isWarping ||
            player == null ||
            distanceTracker == null ||
            uiManager == null)
        {
            return;
        }

        StartCoroutine(WarpRoutine());
    }
    
    private IEnumerator WarpRoutine()
    {
        isWarping = true;

        CloseWarpPopupButton();
        CloseMap();

        yield return StartCoroutine(
            uiManager.FadeOut(3f));

        player.position =
            distanceTracker.StartPosition;

        EnemySpawner enemySpawner =
            FindFirstObjectByType<EnemySpawner>();

        if (enemySpawner != null)
        {
            enemySpawner.RestartInitialSpawn();
        }

        yield return StartCoroutine(
            uiManager.FadeIn(1f));

        isWarping = false;
    }
    
    private void OnDisable()
    {
        ReturnAll();
    }

    private void Update()
    {
        if (!isOpen)
            return;

        RefreshMap();
    }

    public void ToggleMap()
    {
        if (mapAnimator != null &&
            mapAnimator.IsPlaying)
        {
            return;
        }

        if (isOpen)
            CloseMap();
        else
            OpenMap();
    }

    public void OpenMap()
    {
        if (isOpen)
            return;

        if (mapAnimator != null &&
            mapAnimator.IsPlaying)
        {
            return;
        }

        isOpen = true;

        if (uiManager != null)
        {
            uiManager.ShowPopup(
                UIPanelType.MapScreen);
        }

        if (possible != null)
            possible.SetActive(true);

        if (portal != null)
            portal.SetActive(true);

        RefreshMap();

        if (mapAnimator != null)
            mapAnimator.PlayOpen();
    }

    public void CloseMap()
    {
        if (!isOpen)
            return;

        if (mapAnimator != null &&
            mapAnimator.IsPlaying)
        {
            return;
        }

        isOpen = false;

        if (possible != null)
            possible.SetActive(false);

        if (portal != null)
            portal.SetActive(false);

        if (mapAnimator != null)
        {
            mapAnimator.PlayClose(
                CompleteCloseMap);

            return;
        }

        CompleteCloseMap();
    }

    private void CompleteCloseMap()
    {
        ReturnAll();

        if (uiManager != null)
        {
            uiManager.HidePanel(
                UIPanelType.MapScreen);
        }
    }

    private void RefreshMap()
    {
        if (player == null ||
            mapRoot == null)
        {
            return;
        }

        ReturnAll();

        MapTarget[] targets =
            FindObjectsByType<MapTarget>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

        for (int i = 0;
             i < targets.Length;
             i++)
        {
            MapTarget target = targets[i];

            if (target == null)
                continue;

            if (target.TargetType != targetType)
                continue;

            ShowTarget(target);
        }
    }

    private void ShowTarget(
        MapTarget target)
    {
        Vector3 offset =
            target.TargetTransform.position -
            player.position;

        Vector2 worldOffset =
            new Vector2(
                offset.x,
                offset.z);

        float distance =
            worldOffset.magnitude;

        if (distance > visibleWorldRadius)
            return;

        if (distance <= iconWorldRadius)
            ShowIcon(worldOffset);
        else
            ShowArrow(worldOffset);
    }

    private void ShowIcon(
        Vector2 worldOffset)
    {
        RectTransform icon = GetIcon();

        if (icon == null)
            return;

        Vector2 normalized =
            worldOffset /
            iconWorldRadius;

        icon.SetParent(
            mapRoot,
            false);

        icon.localScale =
            Vector3.one;

        icon.localRotation =
            Quaternion.identity;

        icon.anchoredPosition =
            normalized *
            mapUiRadius;

        icon.SetAsLastSibling();
        icon.gameObject.SetActive(true);

        activeIcons.Add(icon);
    }

    private void ShowArrow(
        Vector2 worldOffset)
    {
        RectTransform arrow = GetArrow();

        if (arrow == null)
            return;

        Vector2 direction =
            worldOffset.normalized;

        arrow.SetParent(
            mapRoot,
            false);

        arrow.localScale =
            Vector3.one;

        arrow.anchoredPosition =
            direction *
            arrowRadius;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x) *
            Mathf.Rad2Deg;

        arrow.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle - 90f);

        arrow.SetAsLastSibling();
        arrow.gameObject.SetActive(true);

        activeArrows.Add(arrow);
    }

    private RectTransform GetIcon()
    {
        if (supplyIconPrefab == null)
            return null;

        if (iconPool.Count > 0)
            return iconPool.Pop();

        RectTransform icon =
            Instantiate(
                supplyIconPrefab,
                mapRoot);

        icon.gameObject.SetActive(false);

        return icon;
    }

    private RectTransform GetArrow()
    {
        if (arrowIconPrefab == null)
            return null;

        if (arrowPool.Count > 0)
            return arrowPool.Pop();

        RectTransform arrow =
            Instantiate(
                arrowIconPrefab,
                mapRoot);

        arrow.gameObject.SetActive(false);

        return arrow;
    }

    private void ReturnAll()
    {
        for (int i = 0;
             i < activeIcons.Count;
             i++)
        {
            ReturnIcon(
                activeIcons[i]);
        }

        for (int i = 0;
             i < activeArrows.Count;
             i++)
        {
            ReturnArrow(
                activeArrows[i]);
        }

        activeIcons.Clear();
        activeArrows.Clear();
    }

    private void ReturnIcon(
        RectTransform icon)
    {
        if (icon == null)
            return;

        icon.gameObject.SetActive(false);
        iconPool.Push(icon);
    }

    private void ReturnArrow(
        RectTransform arrow)
    {
        if (arrow == null)
            return;

        arrow.gameObject.SetActive(false);
        arrowPool.Push(arrow);
    }
}
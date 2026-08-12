using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;
using DG.Tweening;

public class InventoryUI : MonoBehaviour
{
    [InspectorLabel("UI 매니저")]
    [SerializeField] private UIManager uiManager;
    
    [InspectorLabel("아이템 선택 화면UI")]
    [SerializeField] private ItemSelectScreenUI itemSelectScreenUI;
    
    [InspectorLabel("버리기 구역UI")]
    [SerializeField] private ItemDiscardZoneUI discardZoneUI;
    
    [InspectorLabel("지도 UI")]
    [SerializeField] private SimpleMapUI simpleMapUI;
    
    [InspectorLabel("아이템 정보 UI")]
    [SerializeField] private ItemInfoUI itemInfoUI;
    
    [InspectorLabel("인벤토리 애니메이션")]
    [SerializeField] private InventoryAnimator inventoryAnimator;
    
    [Header("인벤토리 기능")]
    
    [InspectorLabel("한 번 확장 칸 수")]
    [SerializeField] private int unlockCountPerExpand;

    [InspectorLabel("인벤토리 시간 배율")]
    [SerializeField] [Range(0f, 1f)] private float inventoryTimeScale;
    
    [Header("인벤토리")]
    
    [InspectorLabel("인벤토리")]
    [SerializeField] private Inventory inventory;
    
    [InspectorLabel("회전 버튼")]
    [SerializeField] private Button rotateButton;
    
    [InspectorLabel("셀 부모")]
    [SerializeField] private RectTransform cellParent;

    [InspectorLabel("셀 프리팹")]
    [SerializeField] private InventoryCellUI cellPrefab;

    [InspectorLabel("아이템 레이어")]
    [SerializeField] private RectTransform itemLayer;

    [InspectorLabel("대기 아이템 부모")]
    [SerializeField] private RectTransform waitingItemParent;

    [InspectorLabel("아이템 UI 프리팹")]
    [SerializeField] private InventoryItemUI itemUIPrefab;
    
    [InspectorLabel("나가기 가능 표시")]
    [SerializeField] private GameObject possible;

    [Header("크기")]

    [InspectorLabel("셀 크기")]
    [SerializeField] private float cellSize;

    [InspectorLabel("셀 간격")]
    [SerializeField] private float cellSpacing;
    
    private readonly List<InventoryItemUI> itemUIs = new();
    private readonly List<InventoryCellUI> cellUIs = new();
    private InventoryItemUI selectedItemUI;
    private InventoryItemUI draggingItem;
    private bool wasPlaced;
    
    private bool isUnlockMode;
    private int remainUnlockCount;
    
    private bool isOpen;
    private float previousTimeScale = 1f;
    
    private Action onUnlockModeComplete;
    
    private Transform previousParent;
    
    private bool reopenMapAfterClose;
    
    private Coroutine openAnimationCoroutine;
    
    public Vector2 GetItemSize(InventoryItem item)
    {
        float width = item.Width * cellSize + (item.Width - 1) * cellSpacing;
        float height = item.Height * cellSize + (item.Height - 1) * cellSpacing;

        return new Vector2(width, height);
    }
    
    private void Update()
    {
        if (possible != null)
            possible.SetActive(CanCloseInventory());
    }
    
    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();
    }

    private void Start()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshCells;

        CreateCells();

        if (rotateButton != null)
            rotateButton.gameObject.SetActive(false);
    }
    
    public void StartUnlockMode(Action onComplete = null)
    {
        if (inventory == null)
            return;

        if (isUnlockMode)
            return;

        selectedItemUI = null;
        draggingItem = null;
        onUnlockModeComplete = onComplete;

        if (rotateButton != null)
            rotateButton.gameObject.SetActive(false);

        isUnlockMode = true;
        remainUnlockCount = unlockCountPerExpand;

        RefreshCells();
    }

    public void TryUnlockCell(InventoryCellUI cell)
    {
        if (!isUnlockMode)
            return;

        if (inventory == null || cell == null)
            return;

        if (!inventory.UnlockCell(cell.X, cell.Y))
            return;

        remainUnlockCount--;

        if (remainUnlockCount <= 0)
        {
            isUnlockMode = false;
            RefreshCells();

            Action completeCallback = onUnlockModeComplete;
            onUnlockModeComplete = null;
            completeCallback?.Invoke();

            return;
        }

        RefreshCells();
    }
    
    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshCells;
    }
    
    private void RefreshCells()
    {
        if (inventory == null)
            return;

        foreach (InventoryCellUI cell in cellUIs)
        {
            if (cell == null)
                continue;

            bool isUnlocked = inventory.IsUnlockedCell(cell.X, cell.Y);
            bool canUnlock = isUnlockMode && inventory.CanUnlockCell(cell.X, cell.Y);

            cell.Refresh(isUnlocked, canUnlock);
        }
    }
    
    private void CreateCells()
    {
        if (inventory == null || cellParent == null || cellPrefab == null)
            return;

        cellUIs.Clear();

        for (int i = cellParent.childCount - 1; i >= 0; i--)
            Destroy(cellParent.GetChild(i).gameObject);

        for (int y = 0; y < inventory.Height; y++)
        {
            for (int x = 0; x < inventory.Width; x++)
            {
                InventoryCellUI cell = Instantiate(cellPrefab, cellParent);
                cell.Init(this, x, y);

                bool isUnlocked = inventory.IsUnlockedCell(x, y);
                bool canUnlock = isUnlockMode && inventory.CanUnlockCell(x, y);

                cell.Refresh(isUnlocked, canUnlock);
                cellUIs.Add(cell);
            }
        }
    }

    public void CreateStartItems(InventoryItemData[] items)
    {
        if (items == null || itemUIPrefab == null || waitingItemParent == null)
            return;

        foreach (InventoryItemData itemData in items)
        {
            if (itemData == null)
                continue;

            CreateItemUI(itemData, waitingItemParent);
        }
    }

    public InventoryItemUI AddWaitingItem(InventoryItemData itemData)
    {
        return CreateItemUI(itemData, waitingItemParent);
    }

    public InventoryItemUI CreateItemUI(InventoryItemData itemData, Transform parent)
    {
        if (itemData == null || itemUIPrefab == null || parent == null)
            return null;

        InventoryItem item = new InventoryItem(itemData);
        InventoryItemUI itemUI = Instantiate(itemUIPrefab, parent, false);

        itemUI.Init(this, item);
        itemUI.SetWaiting();
        itemUI.Refresh();

        itemUIs.Add(itemUI);
        return itemUI;
    }

    public void RemoveItemUI(
        InventoryItemUI itemUI,
        bool notifyResolved = true)
    {
        if (itemUI == null)
            return;

        bool isCandidate =
            itemSelectScreenUI != null &&
            itemSelectScreenUI.IsCandidateItem(itemUI);

        if (inventory != null &&
            itemUI.IsPlaced)
        {
            inventory.RemoveItem(itemUI.Item);
        }

        itemUIs.Remove(itemUI);

        if (selectedItemUI == itemUI)
            ClearSelectedItem();

        if (draggingItem == itemUI)
            draggingItem = null;

        if (notifyResolved &&
            itemSelectScreenUI != null)
        {
            if (isCandidate)
            {
                itemSelectScreenUI.NotifyCandidateRemoved(
                    itemUI);
            }
            else
            {
                itemSelectScreenUI.TryCompleteItemSelect();
            }
        }

        itemUI.transform.DOKill();
        Destroy(itemUI.gameObject);
    }

    public void BeginDrag(
        InventoryItemUI itemUI)
    {
        if (isUnlockMode)
            return;

        if (itemUI == null ||
            itemLayer == null)
        {
            return;
        }

        if (selectedItemUI != itemUI)
        {
            if (selectedItemUI != null)
                selectedItemUI.SetSelected(false);

            selectedItemUI = itemUI;
            selectedItemUI.SetSelected(true);

            if (rotateButton != null)
                rotateButton.gameObject.SetActive(true);
        }

        if (itemInfoUI != null)
            itemInfoUI.Hide();

        draggingItem = itemUI;

        wasPlaced = itemUI.IsPlaced;
        previousParent = itemUI.transform.parent;

        if (wasPlaced &&
            inventory != null)
        {
            inventory.RemoveItem(itemUI.Item);
        }

        itemUI.SetDragging();
        itemUI.transform.SetParent(
            itemLayer,
            true);

        itemUI.transform.SetAsLastSibling();
        itemUI.Refresh();

        if (itemSelectScreenUI != null &&
            itemSelectScreenUI.IsCandidateItem(itemUI))
        {
            itemSelectScreenUI.NotifyCandidateStateChanged(
                itemUI);
        }
    }

    public void Drag(InventoryItemUI itemUI, PointerEventData eventData)
    {
        if (isUnlockMode)
            return;
        
        if (itemUI == null)
            return;

        RectTransform rectTransform = itemUI.GetComponent<RectTransform>();
        rectTransform.position = eventData.position;

        if (discardZoneUI != null)
            discardZoneUI.RefreshHover(eventData);
    }
    
    public void EndDrag(InventoryItemUI itemUI, PointerEventData eventData) 
    { 
        if (isUnlockMode) 
            return;
        
        if (itemUI == null || 
            inventory == null) 
        { 
            return; 
        }
        
        if (draggingItem != itemUI) 
            return;
        
        if (discardZoneUI != null && 
            discardZoneUI.IsPointerOver(eventData)) 
        { 
            discardZoneUI.SetNormal();
            
            draggingItem = null; 
            ClearSelectedItem();
            
            itemUI.PlayDiscard(() => 
            { 
                RemoveItemUI(itemUI); 
            });
            
            return; 
        }
        
        if (discardZoneUI != null) 
            discardZoneUI.SetNormal();
        
        if (TryGetGridPosition(
                itemUI, 
                eventData, 
                out int x, 
                out int y)) 
        { 
            bool canPlaceCandidate = 
                itemSelectScreenUI == null || 
                itemSelectScreenUI.CanPlaceCandidate(itemUI);
            
            if (canPlaceCandidate && 
                inventory.PlaceItem(itemUI.Item, x, y)) 
            { 
                SoundManager.Instance?.PlaySFX(SfxType.Installation);
                
                itemUI.transform.SetParent(itemLayer, false);
                
                itemUI.SetPlaced(x, y); 
                itemUI.Refresh();
                
                RectTransform rectTransform = 
                    itemUI.GetComponent<RectTransform>();
                
                rectTransform.anchoredPosition = 
                    GetItemAnchoredPosition(itemUI.Item, x, y);
                
                if (itemSelectScreenUI != null) 
                { 
                    if (itemSelectScreenUI.IsCandidateItem(itemUI)) 
                    { 
                        itemSelectScreenUI.NotifyItemResolved(itemUI); 
                    }
                    
                    else 
                    { 
                        itemSelectScreenUI.TryCompleteItemSelect(); 
                    } 
                }
                
                draggingItem = null;

                if (selectedItemUI == itemUI)
                    RefreshSelectedItemInfo();

                return;
            } 
        }
        
        ReturnItem(itemUI);
        draggingItem = null;

        if (selectedItemUI == itemUI)
            RefreshSelectedItemInfo();
    }
    
    public void RotateSelectedItem()
    {
        if (isUnlockMode)
            return;
        
        if (selectedItemUI == null || inventory == null)
            return;
        
        SoundManager.Instance?.PlaySFX(SfxType.Rotation);
        
        bool wasItemPlaced = selectedItemUI.IsPlaced;
        int oldX = selectedItemUI.PlacedX;
        int oldY = selectedItemUI.PlacedY;

        if (!wasItemPlaced)
        {
            selectedItemUI.Item.Rotate();
            selectedItemUI.Refresh();

            if (draggingItem != null)
            {
                RectTransform dragRect =
                    draggingItem.GetComponent<RectTransform>();

                dragRect.position =
                    Input.mousePosition;
            }
            else
            {
                RefreshSelectedItemInfo();
            }

            return;
        }

        inventory.RemoveItem(selectedItemUI.Item);

        selectedItemUI.Item.Rotate();

        if (TryFindRotatePosition(selectedItemUI.Item, oldX, oldY, out int newX, out int newY))
        {
            inventory.PlaceItem(selectedItemUI.Item, newX, newY);

            selectedItemUI.SetPlaced(newX, newY);
            selectedItemUI.Refresh();

            RectTransform rectTransform = selectedItemUI.GetComponent<RectTransform>();

            rectTransform.anchoredPosition = GetItemAnchoredPosition(selectedItemUI.Item, newX, newY);

            RefreshSelectedItemInfo();

            return;
        }

        selectedItemUI.Item.Rotate();

        inventory.PlaceItem(selectedItemUI.Item, oldX, oldY);

        selectedItemUI.SetPlaced(oldX, oldY);
        selectedItemUI.Refresh();

        RectTransform returnRect = selectedItemUI.GetComponent<RectTransform>();
        returnRect.anchoredPosition = GetItemAnchoredPosition(selectedItemUI.Item, oldX, oldY);
        
        RefreshSelectedItemInfo();
    }
    
    private bool TryFindRotatePosition(InventoryItem item, int originX, int originY, out int resultX, out int resultY)
    {
        resultX = originX;
        resultY = originY;

        Vector2Int[] offsets =
        {
            new Vector2Int(0, 0),

            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),

            new Vector2Int(-2, 0),
            new Vector2Int(2, 0),
            new Vector2Int(0, -2),
            new Vector2Int(0, 2),

            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),

            new Vector2Int(-2, -1),
            new Vector2Int(2, -1),
            new Vector2Int(-2, 1),
            new Vector2Int(2, 1),
        };

        foreach (Vector2Int offset in offsets)
        {
            int checkX = originX + offset.x;
            int checkY = originY + offset.y;

            if (!inventory.CanPlace(item, checkX, checkY))
                continue;

            resultX = checkX;
            resultY = checkY;
            return true;
        }

        return false;
    }
    
    private void ReturnItem(
        InventoryItemUI itemUI)
    {
        if (itemUI == null)
            return;

        Transform returnParent;

        if (wasPlaced)
        {
            returnParent = waitingItemParent;
        }
        else
        {
            returnParent =
                previousParent != null
                    ? previousParent
                    : waitingItemParent;
        }

        itemUI.transform.SetParent(
            returnParent,
            false);

        itemUI.SetWaiting();
        itemUI.Refresh();

        RectTransform rectTransform =
            itemUI.GetComponent<RectTransform>();

        rectTransform.anchoredPosition =
            Vector2.zero;

        rectTransform.localScale =
            Vector3.one;

        rectTransform.localRotation =
            Quaternion.identity;

        if (itemSelectScreenUI != null &&
            itemSelectScreenUI.IsCandidateItem(itemUI))
        {
            itemSelectScreenUI
                .NotifyCandidateStateChanged(
                    itemUI);
        }
    }

    private bool TryGetGridPosition(InventoryItemUI itemUI, PointerEventData eventData, out int x, out int y)
    {
        x = -1;
        y = -1;

        if (itemUI == null || itemLayer == null || inventory == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                itemLayer,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 centerPoint))
        {
            return false;
        }

        Vector2 itemSize = GetItemSize(itemUI.Item);

        float totalWidth = inventory.Width * cellSize + (inventory.Width - 1) * cellSpacing;
        float totalHeight = inventory.Height * cellSize + (inventory.Height - 1) * cellSpacing;

        float gridLeft = -totalWidth * 0.5f;
        float gridTop = totalHeight * 0.5f;
        
        float itemLeft = centerPoint.x - itemSize.x * 0.5f;
        float itemTop = centerPoint.y + itemSize.y * 0.5f;

        x = Mathf.RoundToInt((itemLeft - gridLeft) / (cellSize + cellSpacing));
        y = Mathf.RoundToInt((gridTop - itemTop) / (cellSize + cellSpacing));

        return x >= 0 && y >= 0 && x < inventory.Width && y < inventory.Height;
    }
    
    private Vector2 GetItemAnchoredPosition(InventoryItem item, int x, int y)
    {
        float totalWidth = inventory.Width * cellSize + (inventory.Width - 1) * cellSpacing;
        float totalHeight = inventory.Height * cellSize + (inventory.Height - 1) * cellSpacing;

        Vector2 itemSize = GetItemSize(item);

        float left = -totalWidth * 0.5f;
        float top = totalHeight * 0.5f;

        float posX = left + x * (cellSize + cellSpacing) + itemSize.x * 0.5f;
        float posY = top - y * (cellSize + cellSpacing) - itemSize.y * 0.5f;

        return new Vector2(posX, posY);
    }
    
    public void SelectItem(
        InventoryItemUI itemUI)
    {
        if (isUnlockMode)
            return;

        if (itemUI == null)
            return;

        if (selectedItemUI == itemUI)
        {
            ClearSelectedItem();
            return;
        }

        if (selectedItemUI != null)
            selectedItemUI.SetSelected(false);

        selectedItemUI = itemUI;
        selectedItemUI.SetSelected(true);

        if (rotateButton != null)
            rotateButton.gameObject.SetActive(true);

        if (itemInfoUI != null &&
            selectedItemUI.Item != null)
        {
            itemInfoUI.Show(
                selectedItemUI.Item,
                selectedItemUI.transform
                    as RectTransform);
        }
    }
    
    public void ClearSelectedItem()
    {
        if (selectedItemUI != null)
            selectedItemUI.SetSelected(false);

        selectedItemUI = null;

        if (rotateButton != null)
            rotateButton.gameObject.SetActive(false);

        if (itemInfoUI != null)
            itemInfoUI.Hide();
    }
    
    private void RefreshSelectedItemInfo()
    {
        if (itemInfoUI == null ||
            selectedItemUI == null ||
            selectedItemUI.Item == null)
        {
            return;
        }

        itemInfoUI.Show(
            selectedItemUI.Item,
            selectedItemUI.transform
                as RectTransform);
    }
    
    public void ToggleInventory()
    {
        if (isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        if (isOpen)
            return;

        reopenMapAfterClose =
            simpleMapUI != null &&
            simpleMapUI.IsOpen;
        
        if (reopenMapAfterClose)
            simpleMapUI.CloseMap();

        isOpen = true;
        previousTimeScale = Time.timeScale;

        if (uiManager != null)
            uiManager.ShowPanel(UIPanelType.InventoryScreen);

        Time.timeScale = inventoryTimeScale;

        RequestOpenAnimation();
    }
    
    private void RequestOpenAnimation()
    {
        if (inventoryAnimator == null)
            return;

        if (openAnimationCoroutine != null)
            StopCoroutine(openAnimationCoroutine);

        openAnimationCoroutine =
            StartCoroutine(PlayOpenNextFrame());
    }

    private IEnumerator PlayOpenNextFrame()
    {
        yield return null;

        openAnimationCoroutine = null;

        if (!isOpen)
            yield break;

        if (inventoryAnimator == null)
            yield break;

        if (inventoryAnimator.IsPlaying)
            yield break;

        inventoryAnimator.PlayOpen();
    }
    
    private void CancelOpenAnimation()
    {
        if (openAnimationCoroutine == null)
            return;

        StopCoroutine(openAnimationCoroutine);
        openAnimationCoroutine = null;
    }
    
    private bool CanCloseInventory()
    {
        if (!isOpen)
            return false;

        if (isUnlockMode)
            return false;

        if (HasWaitingItem())
            return false;

        if (itemSelectScreenUI != null && itemSelectScreenUI.HasRemainingItems())
            return false;

        return true;
    }
    
    public void CloseInventory()
    {
        if (!isOpen)
            return;

        if (isUnlockMode)
            return;

        if (HasWaitingItem())
            return;

        if (itemSelectScreenUI != null &&
            itemSelectScreenUI.HasRemainingItems())
        {
            return;
        }

        if (inventoryAnimator != null &&
            inventoryAnimator.IsPlaying)
        {
            return;
        }

        CancelOpenAnimation();

        ClearSelectedItem();

        if (inventoryAnimator != null)
        {
            inventoryAnimator.PlayClose(
                CompleteCloseInventory);

            return;
        }

        CompleteCloseInventory();
    }

    private void CompleteCloseInventory()
    {
        isOpen = false;

        if (uiManager != null)
        {
            uiManager.HidePanel(
                UIPanelType.InventoryScreen);
        }

        Time.timeScale = previousTimeScale;

        if (reopenMapAfterClose &&
            simpleMapUI != null)
        {
            simpleMapUI.OpenMap();
        }

        reopenMapAfterClose = false;
    }
    
    public bool HasWaitingItem()
    {
        foreach (InventoryItemUI itemUI in itemUIs)
        {
            if (itemUI == null)
                continue;

            if (!itemUI.IsPlaced)
                return true;
        }

        return false;
    }
    
    public InventoryItemUI GetItemUI(InventoryItem item)
    {
        if (item == null)
            return null;

        foreach (InventoryItemUI itemUI in itemUIs)
        {
            if (itemUI != null && itemUI.Item == item)
                return itemUI;
        }

        return null;
    }
    
    public void OpenInventoryForItemSelect()
    {
        OpenInventoryPaused();
    }
    
    public void OpenInventoryPaused()
    {
        if (isOpen)
        {
            Time.timeScale = 0f;
            return;
        }

        isOpen = true;
        previousTimeScale = Time.timeScale;

        if (uiManager != null)
            uiManager.ShowPopup(UIPanelType.InventoryScreen);

        Time.timeScale = 0f;

        RequestOpenAnimation();
    }
    
    public void CloseInventoryForReward(Action onComplete = null)
    {
        if (!isOpen)
        {
            onComplete?.Invoke();
            return;
        }

        CancelOpenAnimation();

        isUnlockMode = false;
        ClearSelectedItem();

        if (inventoryAnimator != null)
        {
            inventoryAnimator.PlayClose(() =>
            {
                CompleteCloseInventoryForReward();
                onComplete?.Invoke();
            });

            return;
        }

        CompleteCloseInventoryForReward();
        onComplete?.Invoke();
    }

    private void CompleteCloseInventoryForReward()
    {
        isOpen = false;

        if (uiManager != null)
            uiManager.HidePanel(UIPanelType.InventoryScreen);

        Time.timeScale = previousTimeScale;
    }
    
    public void ForceCloseInventory()
    {
        if (!isOpen)
            return;

        CancelOpenAnimation();

        if (inventoryAnimator != null)
            inventoryAnimator.StopAndRestore();

        ClearSelectedItem();

        isUnlockMode = false;
        isOpen = false;

        Time.timeScale = previousTimeScale;
        onUnlockModeComplete = null;

        if (uiManager != null)
            uiManager.HidePanel(UIPanelType.InventoryScreen);
    }
    
    public bool IsDraggingItem()
    {
        return draggingItem != null;
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemSelectScreenUI : MonoBehaviour
{
    [InspectorLabel("아이템 선택 화면")]
    [SerializeField] private GameObject itemSelectScreen;

    [InspectorLabel("인벤토리 UI")]
    [SerializeField] private InventoryUI inventoryUI;
    
    [InspectorLabel("보급품 애니메이션")]
    [SerializeField] private ItemSelectAnimator itemSelectAnimator;

    [Header("후보 슬롯")]
    [SerializeField] private Transform[] candidateSlots;

    [Header("버튼")]
    [InspectorLabel("리롤 버튼")]
    [SerializeField] private Button rerollButton;

    [Header("설정")]
    [InspectorLabel("후보 개수")]
    [SerializeField] private int candidateCount;

    [InspectorLabel("보급품 리롤 횟수")]
    [SerializeField] private int supplyRerollCount;
    
    private readonly string[] itemDataResourcePaths = 
    {
        "ItemData/무기",
        "ItemData/장비"
    };
    
    private readonly List<InventoryItemUI> candidateItems = new();
    private readonly List<InventoryItemData> randomPool = new();
    public bool IsOpen => itemSelectScreen != null && itemSelectScreen.activeInHierarchy;
    private InventoryItemData[] itemPool;
    private ItemSelectMode currentMode;
    private int remainingRerollCount;
    private bool hasDiscardedCandidate;

    private void Awake()
    {
        if (rerollButton != null)
            rerollButton.onClick.AddListener(Reroll);

        if (itemSelectScreen != null)
            itemSelectScreen.SetActive(false);
    }

    public void OpenSupply()
    {
        if (itemSelectScreen != null && itemSelectScreen.activeSelf)
            return;

        if (HasRemainingItems())
            return;

        Open(ItemSelectMode.Supply, supplyRerollCount);
    }

    public void Open(
        ItemSelectMode mode,
        int rerollCount)
    {
        currentMode = mode;

        remainingRerollCount =
            Mathf.Max(0, rerollCount);

        hasDiscardedCandidate = false;

        if (inventoryUI != null)
            inventoryUI.OpenInventoryForItemSelect();

        if (itemSelectScreen != null)
            itemSelectScreen.SetActive(true);

        LoadItemPool();
        CreateCandidates();
        RefreshButtons();

        if (itemSelectAnimator != null)
        {
            itemSelectAnimator.PlayOpen(
                remainingRerollCount > 0);
        }
    }
    
    public bool HasRemainingItems()
    {
        candidateItems.RemoveAll(item => item == null);

        foreach (InventoryItemUI itemUI in candidateItems)
        {
            if (!itemUI.IsPlaced)
                return true;
        }

        return false;
    }
    
    public bool IsCandidateItem(InventoryItemUI itemUI)
    {
        return itemUI != null && candidateItems.Contains(itemUI);
    }
    
    private bool HasPlacedCandidate()
    {
        candidateItems.RemoveAll(item => item == null);

        foreach (InventoryItemUI itemUI in candidateItems)
        {
            if (itemUI.IsPlaced)
                return true;
        }

        return false;
    }
    
    public bool CanPlaceCandidate(
        InventoryItemUI itemUI)
    {
        if (!IsCandidateItem(itemUI))
            return true;

        if (currentMode != ItemSelectMode.Supply)
            return true;

        candidateItems.RemoveAll(
            item => item == null);

        foreach (InventoryItemUI candidateItem
                 in candidateItems)
        {
            if (candidateItem == itemUI)
                continue;

            if (candidateItem.IsPlaced)
                return false;
        }

        return true;
    }
    
    private void LoadItemPool()
    {
        List<InventoryItemData> combined = new List<InventoryItemData>();

        if (itemDataResourcePaths != null)
        {
            foreach (string path in itemDataResourcePaths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                InventoryItemData[] loaded = Resources.LoadAll<InventoryItemData>(path);

                if (loaded != null)
                    combined.AddRange(loaded);
            }
        }

        itemPool = combined.ToArray();
    }

    private void CreateCandidates()
    {
        ClearCandidates();

        if (inventoryUI == null || itemPool == null || itemPool.Length <= 0)
            return;

        randomPool.Clear();

        foreach (InventoryItemData itemData in itemPool)
        {
            if (itemData != null)
                randomPool.Add(itemData);
        }

        int count = Mathf.Min(candidateCount, candidateSlots.Length, randomPool.Count);

        for (int i = 0; i < count; i++)
        {
            InventoryItemData itemData = GetRandomItemWithoutDuplicate();

            if (itemData == null || candidateSlots[i] == null)
                continue;

            InventoryItemUI itemUI = inventoryUI.CreateItemUI(itemData, candidateSlots[i]);

            if (itemUI == null)
                continue;

            candidateItems.Add(itemUI);
        }
        RefreshCandidatePlacementStates();
    }

    private InventoryItemData GetRandomItemWithoutDuplicate()
    {
        if (randomPool.Count <= 0)
            return null;

        int index = Random.Range(0, randomPool.Count);
        InventoryItemData itemData = randomPool[index];

        randomPool.RemoveAt(index);
        return itemData;
    }

    private void Reroll()
    {
        if (remainingRerollCount <= 0)
            return;

        if (hasDiscardedCandidate)
            return;

        if (HasPlacedCandidate())
            return;

        if (itemSelectAnimator != null &&
            itemSelectAnimator.IsPlaying)
        {
            return;
        }

        remainingRerollCount--;

        RefreshButtons();

        if (itemSelectAnimator != null)
        {
            bool canRerollAfter =
                remainingRerollCount > 0 &&
                !hasDiscardedCandidate &&
                !HasPlacedCandidate();

            itemSelectAnimator.PlayReroll(
                RefreshCandidates,
                canRerollAfter);

            return;
        }

        RefreshCandidates();
    }
    
    private void RefreshCandidates()
    {
        LoadItemPool();
        CreateCandidates();
        RefreshButtons();
    }

    private void ClearCandidates()
    {
        for (int i = candidateItems.Count - 1; i >= 0; i--)
        {
            if (candidateItems[i] != null && !candidateItems[i].IsPlaced && inventoryUI != null)
                inventoryUI.RemoveItemUI(candidateItems[i], false);
        }

        candidateItems.Clear();
    }

    private void RefreshButtons()
    {
        bool canReroll =
            remainingRerollCount > 0 &&
            !hasDiscardedCandidate &&
            !HasPlacedCandidate();

        if (itemSelectAnimator != null)
        {
            itemSelectAnimator.SetRerollAvailable(
                canReroll);

            return;
        }

        if (rerollButton != null)
        {
            rerollButton.gameObject.SetActive(
                canReroll);

            rerollButton.interactable =
                canReroll;
        }
    }

    public void Close()
    {
        if (HasRemainingItems())
            return;

        if (itemSelectScreen != null)
            itemSelectScreen.SetActive(false);
    }
    
    public void NotifyItemResolved(
        InventoryItemUI itemUI)
    {
        if (!IsCandidateItem(itemUI))
            return;

        candidateItems.RemoveAll(
            item => item == null);

        RefreshCandidatePlacementStates();
        RefreshButtons();
        TryCompleteItemSelect();
    }
    
    public void NotifyCandidateStateChanged(
        InventoryItemUI itemUI)
    {
        if (!IsCandidateItem(itemUI))
            return;

        RefreshCandidatePlacementStates();
        RefreshButtons();
    }
    
    public void NotifyCandidateRemoved(
        InventoryItemUI itemUI)
    {
        if (!IsCandidateItem(itemUI))
            return;

        hasDiscardedCandidate = true;

        candidateItems.RemoveAll(item =>
            item == null ||
            item == itemUI);

        RefreshCandidatePlacementStates();
        RefreshButtons();
        TryCompleteItemSelect();
    }
    
    public void TryCompleteItemSelect()
    {
        if (itemSelectScreen == null ||
            !itemSelectScreen.activeSelf)
        {
            return;
        }

        if (HasRemainingItems())
            return;

        if (inventoryUI != null &&
            inventoryUI.HasWaitingItem())
        {
            return;
        }

        if (itemSelectAnimator != null &&
            itemSelectAnimator.IsPlaying)
        {
            return;
        }

        if (itemSelectAnimator != null)
        {
            itemSelectAnimator.PlayClose(
                CompleteItemSelect);

            return;
        }

        CompleteItemSelect();
    }
    
    private void CompleteItemSelect()
    {
        if (SupplyCrate.Current != null)
            SupplyCrate.Current.CompleteUse();

        if (itemSelectScreen != null)
            itemSelectScreen.SetActive(false);
    }
    
    private void RefreshCandidatePlacementStates()
    {
        candidateItems.RemoveAll(
            item => item == null);

        bool hasPlaced =
            HasPlacedCandidate();

        foreach (InventoryItemUI itemUI in candidateItems)
        {
            bool blocked =
                currentMode == ItemSelectMode.Supply &&
                hasPlaced &&
                !itemUI.IsPlaced;

            itemUI.SetPlacementBlocked(
                blocked);
        }
    }
    
    public void DiscardRemainingCandidates()
    {
        candidateItems.RemoveAll(
            item => item == null);

        bool removedAny = false;

        for (int i = candidateItems.Count - 1;
             i >= 0;
             i--)
        {
            InventoryItemUI candidateItem =
                candidateItems[i];

            if (candidateItem == null)
            {
                candidateItems.RemoveAt(i);
                continue;
            }

            if (candidateItem.IsPlaced)
                continue;

            if (inventoryUI != null)
            {
                inventoryUI.RemoveItemUI(
                    candidateItem,
                    false);
            }

            candidateItems.RemoveAt(i);
            removedAny = true;
        }

        if (removedAny)
            hasDiscardedCandidate = true;

        RefreshCandidatePlacementStates();
        RefreshButtons();
        TryCompleteItemSelect();
    }
}
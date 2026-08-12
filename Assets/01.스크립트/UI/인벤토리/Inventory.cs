using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [InspectorLabel("가로 칸 수")]
    [SerializeField] private int width;

    [InspectorLabel("세로 칸 수")]
    [SerializeField] private int height;

    [Header("잠금 해제")]

    [InspectorLabel("시작 가로 칸")]
    [SerializeField] private int startUnlockWidth;

    [InspectorLabel("시작 세로 칸")]
    [SerializeField] private int startUnlockHeight;

    private InventoryItem[,] cells;
    private bool[,] unlockedCells;
    private bool isInitialized;

    public int Width
    {
        get
        {
            Initialize();
            return width;
        }
    }

    public int Height
    {
        get
        {
            Initialize();
            return height;
        }
    }

    public event Action OnInventoryChanged;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        cells = new InventoryItem[width, height];
        unlockedCells = new bool[width, height];

        UnlockStartArea();

        isInitialized = true;
    }

    public bool IsInside(int x, int y)
    {
        Initialize();

        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public bool IsUnlockedCell(int x, int y)
    {
        Initialize();

        if (!IsInside(x, y))
            return false;

        return unlockedCells[x, y];
    }
    
    public int GetUnlockedTopRow()
    {
        Initialize();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (unlockedCells[x, y])
                    return y;
            }
        }

        return 0;
    }

    public int GetUnlockedBottomRow()
    {
        Initialize();

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (unlockedCells[x, y])
                    return y;
            }
        }

        return height - 1;
    }

    public int GetUnlockedLeftColumn()
    {
        Initialize();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (unlockedCells[x, y])
                    return x;
            }
        }

        return 0;
    }

    public int GetUnlockedRightColumn()
    {
        Initialize();

        for (int x = width - 1; x >= 0; x--)
        {
            for (int y = 0; y < height; y++)
            {
                if (unlockedCells[x, y])
                    return x;
            }
        }

        return width - 1;
    }

    public bool CanUnlockCell(int x, int y)
    {
        Initialize();

        if (!IsInside(x, y))
            return false;

        if (unlockedCells[x, y])
            return false;

        return IsUnlockedCell(x - 1, y) ||
               IsUnlockedCell(x + 1, y) ||
               IsUnlockedCell(x, y - 1) ||
               IsUnlockedCell(x, y + 1);
    }

    public bool UnlockCell(int x, int y)
    {
        Initialize();

        if (!CanUnlockCell(x, y))
            return false;

        unlockedCells[x, y] = true;
        OnInventoryChanged?.Invoke();
        return true;
    }

    private void UnlockStartArea()
    {
        int unlockWidth = Mathf.Clamp(startUnlockWidth, 1, width);
        int unlockHeight = Mathf.Clamp(startUnlockHeight, 1, height);

        int startX = (width - unlockWidth) / 2;
        int startY = (height - unlockHeight) / 2;

        for (int x = startX; x < startX + unlockWidth; x++)
        {
            for (int y = startY; y < startY + unlockHeight; y++)
            {
                unlockedCells[x, y] = true;
            }
        }
    }

    public bool CanPlace(InventoryItem item, int startX, int startY)
    {
        Initialize();

        if (item == null)
            return false;

        if (startX < 0 || startY < 0)
            return false;

        if (startX + item.Width > width)
            return false;

        if (startY + item.Height > height)
            return false;

        for (int x = startX; x < startX + item.Width; x++)
        {
            for (int y = startY; y < startY + item.Height; y++)
            {
                if (!IsUnlockedCell(x, y))
                    return false;

                if (cells[x, y] != null)
                    return false;
            }
        }

        return true;
    }

    public bool PlaceItem(InventoryItem item, int startX, int startY)
    {
        Initialize();

        if (!CanPlace(item, startX, startY))
            return false;

        for (int x = startX; x < startX + item.Width; x++)
        {
            for (int y = startY; y < startY + item.Height; y++)
            {
                cells[x, y] = item;
            }
        }

        item.SetPlaced(startX, startY);

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        Initialize();

        if (item == null)
            return;

        if (!TryGetItemPosition(item, out int startX, out int startY))
            return;

        bool removed = false;

        int endX = Mathf.Min(width, startX + item.Width);
        int endY = Mathf.Min(height, startY + item.Height);

        for (int x = Mathf.Max(0, startX); x < endX; x++)
        {
            for (int y = Mathf.Max(0, startY); y < endY; y++)
            {
                if (cells[x, y] == item)
                {
                    cells[x, y] = null;
                    removed = true;
                }
            }
        }

        if (removed)
        {
            item.ClearPlaced();
            OnInventoryChanged?.Invoke();
        }
    }

    public List<InventoryItem> GetPlacedItems()
    {
        Initialize();

        List<InventoryItem> items = new List<InventoryItem>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventoryItem item = cells[x, y];

                if (item == null)
                    continue;

                if (items.Contains(item))
                    continue;

                items.Add(item);
            }
        }

        return items;
    }

    public bool TryGetItemPosition(InventoryItem item, out int x, out int y)
    {
        Initialize();

        x = -1;
        y = -1;

        if (item == null || !item.IsPlaced)
            return false;

        x = item.PlacedX;
        y = item.PlacedY;
        return IsInside(x, y);
    }

    public List<InventoryItem> GetAroundItems(InventoryItem item)
    {
        Initialize();

        List<InventoryItem> result = new List<InventoryItem>();

        if (!TryGetItemPosition(item, out int startX, out int startY))
            return result;

        int endX = startX + item.Width - 1;
        int endY = startY + item.Height - 1;

        for (int x = startX - 1; x <= endX + 1; x++)
        {
            AddItemIfValid(result, item, x, startY - 1);
            AddItemIfValid(result, item, x, endY + 1);
        }

        for (int y = startY; y <= endY; y++)
        {
            AddItemIfValid(result, item, startX - 1, y);
            AddItemIfValid(result, item, endX + 1, y);
        }

        return result;
    }

    public List<InventoryItem> GetSameRowItems(InventoryItem item)
    {
        Initialize();

        List<InventoryItem> result = new List<InventoryItem>();

        if (!TryGetItemPosition(item, out int startX, out int startY))
            return result;

        for (int y = startY; y < startY + item.Height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                AddItemIfValid(result, item, x, y);
            }
        }

        return result;
    }

    public List<InventoryItem> GetSameColumnItems(InventoryItem item)
    {
        Initialize();

        List<InventoryItem> result = new List<InventoryItem>();

        if (!TryGetItemPosition(item, out int startX, out int startY))
            return result;

        for (int x = startX; x < startX + item.Width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                AddItemIfValid(result, item, x, y);
            }
        }

        return result;
    }

    private void AddItemIfValid(List<InventoryItem> list, InventoryItem originItem, int x, int y)
    {
        if (!IsInside(x, y))
            return;

        InventoryItem item = cells[x, y];

        if (item == null)
            return;

        if (item == originItem)
            return;

        if (list.Contains(item))
            return;

        list.Add(item);
    }
}
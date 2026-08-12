public class InventoryItem
{
    public InventoryItemData Data { get; }
    public bool IsRotated { get; private set; }

    public int PlacedX { get; private set; } = -1;
    public int PlacedY { get; private set; } = -1;
    public bool IsPlaced { get; private set; }

    public int Width => IsRotated ? Data.Height : Data.Width;
    public int Height => IsRotated ? Data.Width : Data.Height;

    public InventoryItem(InventoryItemData data)
    {
        Data = data;
    }

    public void Rotate()
    {
        IsRotated = !IsRotated;
    }

    public void SetPlaced(int x, int y)
    {
        PlacedX = x;
        PlacedY = y;
        IsPlaced = true;
    }

    public void ClearPlaced()
    {
        PlacedX = -1;
        PlacedY = -1;
        IsPlaced = false;
    }
}
using UnityEngine;

public class SupplyCrate : MonoBehaviour
{
    private const string PoolKey = "SupplyCrate";

    public static SupplyCrate Current { get; private set; }
    private string playerTag = "Player";
    private bool isOpened;

    public void Init()
    {
        isOpened = false;
    }

    private void OnEnable()
    {
        isOpened = false;
    }

    private void OnDisable()
    {
        if (Current == this)
            Current = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpened)
            return;

        if (!other.CompareTag(playerTag))
            return;

        OpenSupplyCrate();
    }

    private void OpenSupplyCrate()
    {
        isOpened = true;
        Current = this;

        ItemSelectScreenUI itemSelectScreenUI = FindFirstObjectByType<ItemSelectScreenUI>();

        if (itemSelectScreenUI != null) 
            itemSelectScreenUI.OpenSupply();
    }

    public void CompleteUse()
    {
        if (Current == this) 
            Current = null;

        ObjectPool.Return(PoolKey, gameObject);
    }
}
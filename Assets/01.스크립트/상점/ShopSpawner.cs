using System.Collections.Generic;
using UnityEngine;

public class ShopSpawner : MonoBehaviour
{
    private const string SupplyCratePoolKey = "SupplyCrate";
    private const float TierDistanceInterval = 2000f;

    [Header("기본")]
    [InspectorLabel("플레이어")]
    [SerializeField] private Transform player;

    [InspectorLabel("거리 추적기")]
    [SerializeField] private PlayerDistanceTracker distanceTracker;

    [Header("보급품")]
    [InspectorLabel("최대 보급품 구간")]
    [SerializeField] private int maxSupplyTier;

    private List<GameObject>[] activeSupplyCrates;
    private int[] spawnedSupplyCrateCounts;

    private void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (distanceTracker == null && player != null)
            distanceTracker = player.GetComponent<PlayerDistanceTracker>();

        maxSupplyTier = Mathf.Max(1, maxSupplyTier);

        activeSupplyCrates = new List<GameObject>[maxSupplyTier + 1];
        spawnedSupplyCrateCounts = new int[maxSupplyTier + 1];

        for (int i = 0; i < activeSupplyCrates.Length; i++)
        {
            activeSupplyCrates[i] = new List<GameObject>();
        }
    }

    private void Update()
    {
        if (player == null || distanceTracker == null)
            return;

        int unlockedTier = GetUnlockedTier();

        for (int tier = 1; tier <= unlockedTier; tier++)
        {
            UpdateTierSupplyCrate(tier);
        }
    }

    private void UpdateTierSupplyCrate(int tier)
    {
        if (tier <= 0 || tier >= activeSupplyCrates.Length)
            return;

        RemoveInactiveSupplyCrates(tier);

        if (activeSupplyCrates[tier].Count > 0)
            return;

        int maxSpawnCount = tier;

        if (spawnedSupplyCrateCounts[tier] >= maxSpawnCount)
            return;

        SpawnSupplyCrate(tier);
    }

    private void RemoveInactiveSupplyCrates(int tier)
    {
        activeSupplyCrates[tier].RemoveAll(crate =>
            crate == null || !crate.activeInHierarchy);
    }

    private void SpawnSupplyCrate(int tier)
    {
        GameObject supplyCrateObj = ObjectPool.Get(SupplyCratePoolKey);

        if (supplyCrateObj == null)
            return;

        supplyCrateObj.transform.position = GetRandomPositionInTierArea(tier);
        supplyCrateObj.transform.rotation = Quaternion.identity;

        if (!supplyCrateObj.TryGetComponent<SupplyCrate>(out var supplyCrate))
        {
            ObjectPool.Return(SupplyCratePoolKey, supplyCrateObj);
            return;
        }

        supplyCrate.Init();

        activeSupplyCrates[tier].Add(supplyCrateObj);
        spawnedSupplyCrateCounts[tier]++;
    }

    private int GetUnlockedTier()
    {
        float maxDistance =
            distanceTracker.MaxDistance;

        int unlockedTier = 1;

        for (int tier = 2;
             tier <= maxSupplyTier;
             tier++)
        {
            if (maxDistance >=
                GetTierUnlockDistance(tier))
            {
                unlockedTier = tier;
            }
            else
            {
                break;
            }
        }

        return unlockedTier;
    }

    private float GetTierUnlockDistance(int tier)
    {
        if (tier <= 2)
            return 0f;

        return GetTierMinDistance(tier - 1);
    }

    private Vector3 GetRandomPositionInTierArea(int tier)
    {
        float minDistance = GetTierMinDistance(tier);
        float maxDistance = GetTierMaxDistance(tier);

        float distance =
            tier == 1
                ? 1000f
                : Random.Range(minDistance, maxDistance);

        float angle = Random.Range(0f, 360f);

        Vector3 direction =
            Quaternion.Euler(0f, angle, 0f) *
            Vector3.forward;

        Vector3 centerPosition =
            distanceTracker.StartPosition;

        Vector3 spawnPosition =
            centerPosition +
            direction.normalized * distance;

        spawnPosition.y =
            centerPosition.y;

        return spawnPosition;
    }

    private float GetTierMinDistance(int tier)
    {
        if (tier == 1)
            return 1000f;

        return 1000f + (tier - 2) * TierDistanceInterval;
    }

    private float GetTierMaxDistance(int tier)
    {
        if (tier == 1)
            return 1000f;

        return 1000f + (tier - 1) * TierDistanceInterval;
    }

    private void OnDrawGizmosSelected()
    {
        if (distanceTracker == null)
            return;

        Vector3 centerPosition = distanceTracker.StartPosition;

        for (int tier = 1; tier <= maxSupplyTier; tier++)
        {
            GizmoUtility.DrawCircleXZ(centerPosition, GetTierMinDistance(tier), Color.gray);
            GizmoUtility.DrawCircleXZ(centerPosition, GetTierMaxDistance(tier), Color.yellow);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

public class ExpOrbManager : MonoBehaviour
{
    public static ExpOrbManager Instance { get; private set; }

    [InspectorLabel("플레이어")]
    [SerializeField] private Transform player;

    [InspectorLabel("병합 시작 개수")]
    [SerializeField] private int mergeStartCount = 150;

    [InspectorLabel("병합 대상 거리")]
    [SerializeField] private float mergeTargetDistance = 25f;

    [InspectorLabel("병합 반경")]
    [SerializeField] private float mergeRadius = 6f;

    [InspectorLabel("한 번에 병합할 최대 개수")]
    [SerializeField] private int maxMergePerCheck = 20;

    [InspectorLabel("체크 간격")]
    [SerializeField] private float checkInterval = 0.5f;

    private readonly List<ExpOrb> expOrbs = new();

    private float checkTimer;
    private float mergeTargetDistanceSqr;
    private float mergeRadiusSqr;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (player == null)
        {
            PlayerLevel playerLevel = FindFirstObjectByType<PlayerLevel>();

            if (playerLevel != null)
                player = playerLevel.transform;
        }

        mergeTargetDistanceSqr = mergeTargetDistance * mergeTargetDistance;
        mergeRadiusSqr = mergeRadius * mergeRadius;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer < checkInterval)
            return;

        checkTimer = 0f;

        CleanupNullOrInactive();

        if (expOrbs.Count < mergeStartCount)
            return;

        MergeFarExpOrbs();
    }

    public void Register(ExpOrb expOrb)
    {
        if (expOrb == null)
            return;

        if (expOrbs.Contains(expOrb))
            return;

        expOrbs.Add(expOrb);
    }

    public void Unregister(ExpOrb expOrb)
    {
        if (expOrb == null)
            return;

        expOrbs.Remove(expOrb);
    }

    private void CleanupNullOrInactive()
    {
        for (int i = expOrbs.Count - 1; i >= 0; i--)
        {
            if (expOrbs[i] == null || !expOrbs[i].gameObject.activeInHierarchy)
                expOrbs.RemoveAt(i);
        }
    }

    private void MergeFarExpOrbs()
    {
        int mergeCount = 0;

        for (int i = expOrbs.Count - 1; i >= 0; i--)
        {
            if (mergeCount >= maxMergePerCheck)
                return;

            ExpOrb baseOrb = expOrbs[i];

            if (baseOrb == null || !baseOrb.CanMerge)
                continue;

            if (baseOrb.GetDistanceToPlayerSqr() < mergeTargetDistanceSqr)
                continue;

            for (int j = i - 1; j >= 0; j--)
            {
                if (mergeCount >= maxMergePerCheck)
                    return;

                ExpOrb targetOrb = expOrbs[j];

                if (targetOrb == null || !targetOrb.CanMerge)
                    continue;

                if (targetOrb.GetDistanceToPlayerSqr() < mergeTargetDistanceSqr)
                    continue;

                if (baseOrb.GetDistanceToOrbSqr(targetOrb) > mergeRadiusSqr)
                    continue;

                baseOrb.AddExp(targetOrb.ExpAmount);
                
                i--;

                mergeCount++;
            }
        }
    }
}
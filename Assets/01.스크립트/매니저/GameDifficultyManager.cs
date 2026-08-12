using UnityEngine;

public class GameDifficultyManager : MonoBehaviour
{
    public static GameDifficultyManager Instance { get; private set; }

    [InspectorLabel("거리 추적기")]
    [SerializeField] private PlayerDistanceTracker distanceTracker;

    [InspectorLabel("플레이어 레벨")]
    [SerializeField] private PlayerLevel playerLevel;

    [InspectorLabel("분당 난이도 증가량")]
    [SerializeField] private float timeBonusPerMinute = 0.1f;

    [InspectorLabel("거리 100m당 난이도 증가량")]
    [SerializeField] private float distanceBonusPer100m = 0.15f;

    [InspectorLabel("레벨당 난이도 증가량")]
    [SerializeField] private float levelBonusPerLevel = 0.08f;

    [InspectorLabel("최대 난이도 배율")]
    [SerializeField] private float maxDifficultyMultiplier = 100f;

    [Header("난이도 비선형곡도")]

    [InspectorLabel("초반 유예 시간")]
    [SerializeField] private float graceTimeSeconds = 60f;

    [InspectorLabel("성장 곡선 지수")]
    [SerializeField] private float growthExponent = 1.4f;

    public float DifficultyMultiplier { get; private set; } = 1f;
    public float MaxDifficultyMultiplier => maxDifficultyMultiplier;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (distanceTracker == null)
            distanceTracker = FindFirstObjectByType<PlayerDistanceTracker>();

        if (playerLevel == null)
            playerLevel = FindFirstObjectByType<PlayerLevel>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        float playTime = GameManager.Instance != null ? GameManager.Instance.PlayTime : 0f;
        
        float gracedPlayTime = Mathf.Max(0f, playTime - graceTimeSeconds);
        float elapsedMinutes = gracedPlayTime / 60f;

        float maxDistance = distanceTracker != null ? distanceTracker.MaxDistance : 0f;
        float distanceStep = Mathf.Floor(maxDistance / 100f);

        int level = playerLevel != null ? playerLevel.Level : 1;
        int levelStep = Mathf.Max(0, level - 1);

        float timeBonus = elapsedMinutes * timeBonusPerMinute;
        float distanceBonus = distanceStep * distanceBonusPer100m;
        float levelBonus = levelStep * levelBonusPerLevel;

        float rawGrowth = timeBonus + distanceBonus + levelBonus;
        
        float scaledGrowth = Mathf.Pow(Mathf.Max(0f, rawGrowth), growthExponent);

        DifficultyMultiplier = 1f + scaledGrowth;
        DifficultyMultiplier = Mathf.Clamp(DifficultyMultiplier, 1f, maxDifficultyMultiplier);
    }

    public int GetScaledInt(int baseValue)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseValue * DifficultyMultiplier));
    }

    public float GetScaledFloat(float baseValue)
    {
        return baseValue * DifficultyMultiplier;
    }
}
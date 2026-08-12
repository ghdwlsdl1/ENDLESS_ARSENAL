using TMPro;
using UnityEngine;

public class GameHUDUI : MonoBehaviour
{
    [InspectorLabel("난이도 관리자")]
    [SerializeField] private GameDifficultyManager difficultyManager;
    
    [InspectorLabel("플레이어 레벨")]
    [SerializeField] private PlayerLevel playerLevel;

    [InspectorLabel("거리 추적기")]
    [SerializeField] private PlayerDistanceTracker distanceTracker;

    [InspectorLabel("레벨 표시 텍스트")]
    [SerializeField] private TMP_Text levelText;

    [InspectorLabel("시간 표시 텍스트")]
    [SerializeField] private TMP_Text timeText;

    [InspectorLabel("현재 거리 표시 텍스트")]
    [SerializeField] private TMP_Text currentDistanceText;

    [InspectorLabel("최대 거리 표시 텍스트")]
    [SerializeField] private TMP_Text maxDistanceText;

    [InspectorLabel("난이도 배율 텍스트")]
    [SerializeField] private TMP_Text difficultyText;


    private void Start()
    {
        if (distanceTracker == null)
        {
            distanceTracker =
                FindFirstObjectByType<PlayerDistanceTracker>();
        }

        if (difficultyManager == null)
        {
            difficultyManager =
                FindFirstObjectByType<GameDifficultyManager>();
        }

        RefreshLevel();

        if (playerLevel != null)
        {
            playerLevel.OnLevelUp +=
                HandleLevelUp;
        }
    }

    private void OnDestroy()
    {
        if (playerLevel != null)
        {
            playerLevel.OnLevelUp -=
                HandleLevelUp;
        }
    }

    private void Update()
    {
        RefreshTime();
        RefreshDistance();
        RefreshDifficulty();
    }

    private void HandleLevelUp(
        int level)
    {
        RefreshLevel();
    }

    private void RefreshLevel()
    {
        if (playerLevel == null ||
            levelText == null)
        {
            return;
        }

        levelText.text =
            $"Lv. {playerLevel.Level}";
    }

    private void RefreshTime()
    {
        if (GameManager.Instance == null ||
            timeText == null)
        {
            return;
        }

        float playTime =
            GameManager.Instance.PlayTime;

        int minutes =
            Mathf.FloorToInt(
                playTime / 60f);

        int seconds =
            Mathf.FloorToInt(
                playTime % 60f);

        timeText.text =
            $"{minutes:00}:{seconds:00}";
    }

    private void RefreshDistance()
    {
        if (distanceTracker == null)
            return;

        if (currentDistanceText != null)
        {
            currentDistanceText.text =
                $"현재 거리 : " +
                $"{FormatDistance(distanceTracker.CurrentDistance)}";
        }

        if (maxDistanceText != null)
        {
            maxDistanceText.text =
                $"최대 거리 : " +
                $"{FormatDistance(distanceTracker.MaxDistance)}";
        }
    }

    private string FormatDistance(
        float distance)
    {
        if (distance < 1000f)
        {
            return
                $"{Mathf.FloorToInt(distance)}m";
        }

        return
            $"{distance / 1000f:F2}km";
    }
    private void RefreshDifficulty()
    {
        if (difficultyManager == null ||
            difficultyText == null)
        {
            return;
        }

        difficultyText.text =
            $"난이도 ×{difficultyManager.DifficultyMultiplier:F2}";
    }
}
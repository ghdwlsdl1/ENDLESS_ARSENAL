using System.Collections;
using TMPro;
using UnityEngine;

public class EndScreenUI : MonoBehaviour
{
    [Header("연결")]

    [InspectorLabel("UI 관리자")]
    [SerializeField] private UIManager uiManager;
    
    [InspectorLabel("난이도 관리자")]
    [SerializeField] private GameDifficultyManager difficultyManager;
    
    [InspectorLabel("엔드 화면 연출")]
    [SerializeField] private EndScreenAnimator endScreenAnimator;


    [Header("플레이어 정보")]

    [InspectorLabel("플레이어 체력")]
    [SerializeField] private PlayerHealth playerHealth;

    [InspectorLabel("플레이어 레벨")]
    [SerializeField] private PlayerLevel playerLevel;

    [InspectorLabel("거리 추적기")]
    [SerializeField] private PlayerDistanceTracker distanceTracker;


    [Header("결과 화면")]

    [InspectorLabel("결과 정보 텍스트")]
    [SerializeField] private TMP_Text resultText;


    private bool isRetrying;


    private void Awake()
    {
        if (endScreenAnimator == null)
        {
            endScreenAnimator =
                GetComponent<EndScreenAnimator>();
        }
    }

    private void Start()
    {
        if (playerHealth != null)
            playerHealth.OnDead += ShowGameOver;

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
        
        if (endScreenAnimator != null)
            endScreenAnimator.SetHiddenImmediately();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDead -= ShowGameOver;
    }

    private void ShowGameOver()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();

        float playTime =
            GameManager.Instance != null
                ? GameManager.Instance.PlayTime
                : 0f;

        int minutes =
            Mathf.FloorToInt(playTime / 60f);

        int seconds =
            Mathf.FloorToInt(playTime % 60f);

        int level =
            playerLevel != null
                ? playerLevel.Level
                : 0;

        float maxDistance =
            distanceTracker != null
                ? distanceTracker.MaxDistance
                : 0f;

        float difficultyMultiplier =
            difficultyManager != null
                ? difficultyManager.DifficultyMultiplier
                : 1f;

        if (resultText != null)
        {
            resultText.text =
                $"생존 시간 : {minutes:00}:{seconds:00}\n" +
                $"도달 레벨 : {level}\n" +
                $"최대 거리 : {FormatDistance(maxDistance)}\n" +
                $"최종 난이도 : {difficultyMultiplier:F2}x";
        }

        isRetrying = false;

        if (uiManager != null)
        {
            uiManager.ShowPanel(
                UIPanelType.EndScreen);
        }

        if (endScreenAnimator != null)
            endScreenAnimator.PlayOpen();
    }

    public void RetryButton()
    {
        if (isRetrying)
            return;

        if (endScreenAnimator != null &&
            !endScreenAnimator.CanRetry)
        {
            return;
        }

        isRetrying = true;

        SoundManager.Instance?.PlaySFX(
            SfxType.Button,
            0);

        if (endScreenAnimator != null)
        {
            endScreenAnimator.PlayClose(
                StartRetry);

            return;
        }

        StartRetry();
    }

    private void StartRetry()
    {
        StartCoroutine(
            RetryRoutine());
    }

    private IEnumerator RetryRoutine()
    {
        if (uiManager != null)
        {
            yield return StartCoroutine(
                uiManager.FadeOut());
        }

        if (GameManager.Instance != null)
            GameManager.Instance.Retry();

        if (uiManager != null)
        {
            yield return StartCoroutine(
                uiManager.FadeIn());
        }

        isRetrying = false;
    }

    private string FormatDistance(
        float distance)
    {
        if (distance >= 1000f)
            return $"{distance / 1000f:F2}km";

        return $"{distance:F0}m";
    }
}
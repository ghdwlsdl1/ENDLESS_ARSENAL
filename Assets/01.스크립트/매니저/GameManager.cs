using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    private UIManager uiManager;
    public static GameManager Instance { get; private set; }

    public bool IsPaused { get; private set; }
    public bool IsGameOver { get; private set; }
    
    public float PlayTime { get; private set; }
    public bool IsStarted { get; private set; }
    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        ObjectPool.Clear();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        uiManager = FindFirstObjectByType<UIManager>();

        IsStarted = false;

        if (uiManager != null)
            uiManager.ShowPanel(UIPanelType.StartScreen);

        PauseGame();
    }
    
    private void Start()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(BgmType.Lobby);
    }
    
    private void Update()
    {
        if (!IsStarted || IsPaused || IsGameOver)
            return;

        PlayTime += Time.deltaTime;
    }
    public void StartGame()
    {
        IsStarted = true;
        IsGameOver = false;
        IsPaused = true;
        Time.timeScale = 0f;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(BgmType.Battle, randomNext: true);

        if (uiManager != null)
            uiManager.HidePanel(UIPanelType.StartScreen);
    }
    
    public void PauseGame()
    {
        if (IsGameOver)
            return;

        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
    }

    public void Retry()
    {
        IsPaused = false;
        IsGameOver = false;
        IsStarted = false;

        PlayTime = 0f;

        Time.timeScale = 1f;
        
        SoundManager.Instance?.StopAllSounds();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }
}
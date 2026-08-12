using UnityEngine;

public class EnemyVisualByDifficulty : MonoBehaviour
{
    [InspectorLabel("난이도별 외형 목록")]
    [SerializeField] private GameObject[] visuals;

    [InspectorLabel("애니메이션 핸들러")]
    [SerializeField] private AnimationHandler animationHandler;

    private void Reset()
    {
        RefreshReferences();
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private void OnEnable()
    {
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (visuals == null || visuals.Length == 0)
            return;

        float difficulty = 1f;
        float maxDifficulty = 100f;

        if (GameDifficultyManager.Instance != null)
        {
            difficulty =
                GameDifficultyManager.Instance.DifficultyMultiplier;

            maxDifficulty =
                GameDifficultyManager.Instance.MaxDifficultyMultiplier;
        }

        maxDifficulty = Mathf.Max(1f, maxDifficulty);

        float progress =
            Mathf.Clamp01(difficulty / maxDifficulty);

        int index =
            Mathf.FloorToInt(progress * visuals.Length);

        index =
            Mathf.Clamp(index, 0, visuals.Length - 1);

        SetVisual(index);

        if (animationHandler == null)
            RefreshReferences();

        if (animationHandler != null)
        {
            animationHandler.RefreshAnimator();
            animationHandler.ResetState();
        }
    }

    private void SetVisual(int activeIndex)
    {
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null)
                continue;

            visuals[i].SetActive(i == activeIndex);
        }
    }

    private void RefreshReferences()
    {
        if (animationHandler == null)
        {
            animationHandler =
                GetComponentInChildren<AnimationHandler>(true);
        }
    }
}
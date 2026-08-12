using UnityEngine;
using System;

public class PlayerLevel : MonoBehaviour
{
    [InspectorLabel("현재 레벨")]
    [SerializeField] private int level;

    [InspectorLabel("현재 경험치")]
    [SerializeField] private int currentExp;

    [InspectorLabel("다음 레벨 필요 경험치")]
    [SerializeField] private int expToNext = 5;

    [InspectorLabel("경험치 증가 배율")]
    [SerializeField] private float expGrowthMultiplier = 1.25f;

    [InspectorLabel("경험치 증가 고정값")]
    [SerializeField] private int expGrowthAdd = 2;

    public int Level => level;
    public int CurrentExp => currentExp;
    public int ExpToNext => expToNext;

    public event Action<int> OnLevelUp;

    public void AddExp(int amount)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (amount <= 0)
            return;

        currentExp += amount;

        while (currentExp >= expToNext)
        {
            currentExp -= expToNext;
            LevelUp();

            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;
        }
    }

    private void LevelUp()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        level++;

        expToNext = Mathf.CeilToInt(expToNext * expGrowthMultiplier + expGrowthAdd);

        OnLevelUp?.Invoke(level);
    }
}
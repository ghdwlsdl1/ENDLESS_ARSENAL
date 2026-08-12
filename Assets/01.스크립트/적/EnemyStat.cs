using UnityEngine;

public class EnemyStat : MonoBehaviour
{
    [Header("기본 능력치")]

    [InspectorLabel("최대 체력")]
    [SerializeField] private float baseMaxHp;

    [InspectorLabel("공격력")]
    [SerializeField] private float baseDamage;

    [InspectorLabel("이동 속도")]
    [SerializeField] private float baseMoveSpeed;

    [InspectorLabel("경험치 보상")]
    [SerializeField] private int baseExpReward;

    private float currentMaxHp;
    private float currentDamage;
    private float currentMoveSpeed;
    private int currentExpReward;
    
    private float difficultyMultiplier = 1f;
    private float eliteHpMultiplier = 1f;
    private float eliteDamageMultiplier = 1f;
    private float eliteExpMultiplier = 1f;
    private EliteTier eliteTier = EliteTier.None;

    private bool isInitialized;

    public float MaxHp
    {
        get
        {
            EnsureInitialized();
            return currentMaxHp;
        }
    }

    public float Damage
    {
        get
        {
            EnsureInitialized();
            return currentDamage;
        }
    }

    public float MoveSpeed
    {
        get
        {
            EnsureInitialized();
            return currentMoveSpeed;
        }
    }

    public int ExpReward
    {
        get
        {
            EnsureInitialized();
            return currentExpReward;
        }
    }

    public EliteTier EliteTier => eliteTier;
    public bool IsElite => eliteTier != EliteTier.None;

    private void Awake()
    {
        ResetStat();
    }

    public void ResetStat()
    {
        difficultyMultiplier = 1f;
        ClearEliteInternal();

        RecomputeStats();

        isInitialized = true;
    }

    public void ApplyDifficulty(float multiplier)
    {
        difficultyMultiplier = Mathf.Max(0.01f, multiplier);

        RecomputeStats();

        isInitialized = true;
    }
    
    public void ApplyElite(
        EliteTier tier,
        float hpMultiplier,
        float damageMultiplier,
        float expRewardMultiplier)
    {
        eliteTier = tier;
        eliteHpMultiplier = Mathf.Max(0.01f, hpMultiplier);
        eliteDamageMultiplier = Mathf.Max(0.01f, damageMultiplier);
        eliteExpMultiplier = Mathf.Max(0.01f, expRewardMultiplier);

        RecomputeStats();
    }
    
    public void ClearElite()
    {
        ClearEliteInternal();
        RecomputeStats();
    }

    private void ClearEliteInternal()
    {
        eliteTier = EliteTier.None;
        eliteHpMultiplier = 1f;
        eliteDamageMultiplier = 1f;
        eliteExpMultiplier = 1f;
    }

    private void RecomputeStats()
    {
        currentMaxHp = Mathf.Max(1f, baseMaxHp * difficultyMultiplier * eliteHpMultiplier);
        currentDamage = Mathf.Max(0.01f, baseDamage * difficultyMultiplier * eliteDamageMultiplier);
        
        currentMoveSpeed = Mathf.Max(0.01f, baseMoveSpeed * difficultyMultiplier);

        currentExpReward = Mathf.Max(
            1,
            Mathf.RoundToInt(baseExpReward * difficultyMultiplier * eliteExpMultiplier));
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

        ResetStat();
    }
}
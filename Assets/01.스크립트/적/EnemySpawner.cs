using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    private class EnemySpawnData
    {
        [InspectorLabel("적 풀 키")]
        public string enemyKey;

        [InspectorLabel("스폰 가중치")]
        public int weight;
    }

    [System.Serializable]
    private class EliteTierSetting
    {
        [InspectorLabel("단계")]
        public EliteTier tier;

        [InspectorLabel("오라 색상")]
        public Color auraColor;

        [InspectorLabel("출현 가중치")]
        public float weight;

        [Header("체력 배율")]
        public float minHpMultiplier;
        public float maxHpMultiplier;

        [Header("공격력 배율")]
        public float minDamageMultiplier;
        public float maxDamageMultiplier;

        [Header("크기 배율")]
        public float minScaleMultiplier;
        public float maxScaleMultiplier;

        [Header("경험치 보상 배율")]
        public float minExpMultiplier;
        public float maxExpMultiplier;
    }

    [InspectorLabel("플레이어")]
    [SerializeField] private Transform player;

    [InspectorLabel("스폰 적 목록")]
    [SerializeField] private EnemySpawnData[] enemies;

    [InspectorLabel("최소 스폰 거리")]
    [SerializeField] private float minSpawnRadius;

    [InspectorLabel("최대 스폰 거리")]
    [SerializeField] private float maxSpawnRadius;

    [InspectorLabel("적 자동 반환 거리")]
    [SerializeField] private float enemyDespawnDistance;

    [InspectorLabel("기본 스폰 간격")]
    [SerializeField] private float baseSpawnInterval;

    [InspectorLabel("기본 최대 적 수")]
    [SerializeField] private int baseMaxAlive;

    [InspectorLabel("최소 스폰 간격")]
    [SerializeField] private float minSpawnInterval;

    [InspectorLabel("최대 적 수 증가 배율")]
    [SerializeField] private float maxAliveMultiplier;

    [InspectorLabel("최대 생존 적 수 상한")]
    [SerializeField] private int hardMaxAliveCap;

    [Header("엘리트")]

    [InspectorLabel("엘리트 해금 난이도 배율")]
    [SerializeField] private float eliteUnlockDifficultyMultiplier;

    [InspectorLabel("엘리트 기본 출현 확률")]
    [Range(0f, 1f)]
    [SerializeField] private float baseEliteChance;

    [InspectorLabel("난이도 배율 1당 확률 증가")]
    [Range(0f, 1f)]
    [SerializeField] private float eliteChancePerDifficulty;

    [InspectorLabel("엘리트 최대 출현 확률")]
    [Range(0f, 1f)]
    [SerializeField] private float maxEliteChance;
    
    [InspectorLabel("엘리트 단계별 설정")] 
    [SerializeField] private EliteTierSetting[] eliteTierSettings;

    private float spawnTimer;

    private readonly System.Collections.Generic.List<EnemyController> despawnTargets = new();

    private bool initialSpawnComplete;

    private void Update()
    {
        if (player == null)
            return;

        DespawnFarEnemies();

        float difficultyMultiplier = GetDifficultyMultiplier();

        int currentMaxAlive = 
            Mathf.RoundToInt(baseMaxAlive * (1f + (difficultyMultiplier - 1f) * maxAliveMultiplier));
        
        if (hardMaxAliveCap > 0)
            currentMaxAlive = Mathf.Min(currentMaxAlive, hardMaxAliveCap);

        int initialTargetCount = currentMaxAlive;

        if (!initialSpawnComplete && EnemyRegistry.AliveCount >= initialTargetCount)
        {
            initialSpawnComplete = true;
            spawnTimer = 0f;
        }

        float currentSpawnInterval;

        if (!initialSpawnComplete)
        {
            currentSpawnInterval = 0.1f;
        }
        else
        {
            currentSpawnInterval = 
                Mathf.Max(minSpawnInterval, baseSpawnInterval / difficultyMultiplier);
        }

        if (EnemyRegistry.AliveCount >= currentMaxAlive)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer < currentSpawnInterval)
        {
            return;
        }

        spawnTimer = 0f;

        Spawn(difficultyMultiplier);
    }

    public void RestartInitialSpawn()
    {
        initialSpawnComplete = false;
        spawnTimer = 0f;
    }

    private void DespawnFarEnemies()
    {
        if (enemyDespawnDistance <= 0f) 
            return;

        despawnTargets.Clear();

        float despawnDistanceSqr = enemyDespawnDistance * enemyDespawnDistance;

        foreach (EnemyController enemy in EnemyRegistry.AliveEnemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 diff = enemy.transform.position - player.position;

            diff.y = 0f;

            if (diff.sqrMagnitude >= despawnDistanceSqr)
            { 
                despawnTargets.Add(enemy);
            }
        }

        for (int i = 0; i < despawnTargets.Count; i++)
        {
            if (despawnTargets[i] == null) continue;

            ObjectPool.Return(despawnTargets[i].gameObject);
        }
    }

    private void Spawn(float difficultyMultiplier)
    {
        string selectedEnemyKey = GetRandomEnemyKey();

        if (string.IsNullOrEmpty(selectedEnemyKey))
        {
            return;
        }

        GameObject enemy = ObjectPool.Get(selectedEnemyKey);
        
        if (enemy == null) 
            return;

        Vector3 dir = Random.insideUnitSphere;

        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
        {
            dir = Vector3.right;
        }

        float spawnDistance = Random.Range(minSpawnRadius, maxSpawnRadius);

        Vector3 spawnPos = player.position + dir.normalized * spawnDistance;

        spawnPos.y = player.position.y;

        enemy.transform.position = spawnPos;

        enemy.transform.rotation = Quaternion.identity;

        EnemyStat enemyStat = enemy.GetComponent<EnemyStat>();

        if (enemyStat != null)
        { 
            enemyStat.ApplyDifficulty(difficultyMultiplier);
        }

        ApplyElite(enemy, enemyStat, difficultyMultiplier);

        EnemyController enemyController = enemy.GetComponent<EnemyController>();

        if (enemyController != null)
        {
            enemyController.SetTarget(player);

            enemyController.RefreshStat();
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();

        if (health != null) health.ResetHealth();

        EnemyVisualByDifficulty visual = enemy.GetComponent<EnemyVisualByDifficulty>();

        if (visual != null) visual.RefreshVisual();
    }

    private void ApplyElite(GameObject enemy, EnemyStat enemyStat, float difficultyMultiplier)
    {
        EliteVisual eliteVisual = enemy.GetComponent<EliteVisual>();
        
        EliteTierSetting setting = 
            difficultyMultiplier >= eliteUnlockDifficultyMultiplier 
            ? RollEliteTier(difficultyMultiplier) : null;

        if (setting == null)
        {
            if (enemyStat != null)
                enemyStat.ClearElite();

            if (eliteVisual != null)
                eliteVisual.ClearElite();

            return;
        }

        float hpMultiplier = Random.Range(setting.minHpMultiplier, setting.maxHpMultiplier);
        float damageMultiplier = Random.Range(setting.minDamageMultiplier, setting.maxDamageMultiplier);
        float scaleMultiplier = Random.Range(setting.minScaleMultiplier, setting.maxScaleMultiplier);
        float expMultiplier = Random.Range(setting.minExpMultiplier, setting.maxExpMultiplier);

        if (enemyStat != null)
        {
            enemyStat.ApplyElite(setting.tier, hpMultiplier, damageMultiplier, expMultiplier);
        }

        if (eliteVisual != null)
        {
            eliteVisual.ApplyElite(setting.auraColor, scaleMultiplier);
        }
    }

    private EliteTierSetting RollEliteTier(float difficultyMultiplier)
    {
        if (eliteTierSettings == null ||
            eliteTierSettings.Length == 0)
        {
            return null;
        }

        float difficultyIncrease =
            Mathf.Max(0f, difficultyMultiplier - eliteUnlockDifficultyMultiplier);

        float currentEliteChance = baseEliteChance + difficultyIncrease * eliteChancePerDifficulty;
        
        currentEliteChance = Mathf.Min(currentEliteChance, maxEliteChance);

        if (Random.value > currentEliteChance)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < eliteTierSettings.Length; i++)
        {
            EliteTierSetting setting = eliteTierSettings[i];

            if (setting == null || setting.weight <= 0f)
            {
                continue;
            }

            totalWeight += setting.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);

        for (int i = 0; i < eliteTierSettings.Length; i++)
        {
            EliteTierSetting setting = eliteTierSettings[i];

            if (setting == null || setting.weight <= 0f)
            {
                continue;
            }

            if (roll < setting.weight) 
                return setting;

            roll -= setting.weight;
        }

        return null;
    }

    private string GetRandomEnemyKey()
    {
        if (enemies == null || enemies.Length == 0)
        {
            return string.Empty;
        }

        int totalWeight = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;

            if (enemies[i].weight <= 0) continue;

            totalWeight += enemies[i].weight;
        }

        if (totalWeight <= 0) 
            return string.Empty;

        int randomValue = Random.Range(0, totalWeight);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;

            if (enemies[i].weight <= 0) continue;

            if (randomValue < enemies[i].weight)
            {
                return enemies[i].enemyKey;
            }

            randomValue -= enemies[i].weight;
        }

        return string.Empty;
    }

    private float GetDifficultyMultiplier()
    {
        if (GameDifficultyManager.Instance == null)
        {
            return 1f;
        }

        return GameDifficultyManager.Instance.DifficultyMultiplier;
    }

    private void OnDrawGizmosSelected()
    {
        GizmoUtility.DrawCircleXZ(transform.position, minSpawnRadius, Color.yellow);

        GizmoUtility.DrawCircleXZ(transform.position, maxSpawnRadius, Color.red);
    }
}
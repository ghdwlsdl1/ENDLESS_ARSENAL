using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    private const string DamageTextPoolKey = "DamageText";

    [Tooltip("적 피해 텍스트 색상")]
    [SerializeField] private Color enemyDamageColor = Color.white;

    [Tooltip("플레이어 피해 텍스트 색상")]
    [SerializeField] private Color playerDamageColor = Color.red;

    [Tooltip("회복 텍스트 색상")]
    [SerializeField] private Color healColor = Color.green;

    [Tooltip("치명타 텍스트 색상")]
    [SerializeField] private Color criticalDamageColor = Color.yellow;

    [Tooltip("치명타 텍스트 크기 배율")]
    [SerializeField] private float criticalScaleMultiplier = 1.2f;

    [Tooltip("데미지 텍스트가 표시될 높이")]
    [SerializeField] private float heightOffset = 1.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 적 피해 텍스트 표시
    public static void ShowEnemyDamage(float amount, Vector3 position, bool isCritical = false)
    {
        if (Instance == null)
            return;

        Color color = isCritical ? Instance.criticalDamageColor : Instance.enemyDamageColor;
        float scale = isCritical ? Instance.criticalScaleMultiplier : 1f;

        Instance.Show(amount, position, color, scale);
    }

    // 플레이어 피해 텍스트 표시
    public static void ShowPlayerDamage(float amount, Vector3 position, bool isCritical = false)
    {
        if (Instance == null)
            return;

        Color color = isCritical ? Instance.criticalDamageColor : Instance.playerDamageColor;
        float scale = isCritical ? Instance.criticalScaleMultiplier : 1f;

        Instance.Show(amount, position, color, scale);
    }

    // 회복 텍스트 표시
    public static void ShowHeal(float amount, Vector3 position)
    {
        if (Instance == null)
            return;

        Instance.Show(amount, position, Instance.healColor, 1f);
    }

    // 데미지 텍스트 생성
    private void Show(float amount, Vector3 position, Color color, float scale)
    {
        GameObject obj = ObjectPool.Get(DamageTextPoolKey);

        if (obj == null)
            return;

        obj.transform.position = position + Vector3.up * heightOffset;
        obj.transform.rotation = Quaternion.identity;

        if (!obj.TryGetComponent<DamageText>(out var damageText))
        {
            ObjectPool.Return(DamageTextPoolKey, obj);
            return;
        }

        damageText.Init(amount, DamageTextPoolKey, color, scale);
    }
}
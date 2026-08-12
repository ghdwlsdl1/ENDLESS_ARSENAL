using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    private const float MaxLifeTime = 10f;

    [Tooltip("표시할 데미지 텍스트")]
    [SerializeField] private TMP_Text damageText;

    [Tooltip("텍스트 크기")]
    [SerializeField] private float fontSize = 5f;

    [Tooltip("텍스트 이동 속도")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Tooltip("최대 이동 거리")]
    [SerializeField] private float maxDistance = 2f;

    [Tooltip("좌우 랜덤 이동 범위")]
    [SerializeField] private float randomHorizontalRange = 0.5f;

    private Camera mainCamera;

    private string poolKey;
    private float timer;
    private Vector3 startPosition;
    private Vector3 moveDirection;
    private Color startColor;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    // 데미지 텍스트 초기화
    public void Init(float amount, string poolKey, Color color, float scaleMultiplier = 1f)
    {
        this.poolKey = poolKey;

        timer = 0f;
        startPosition = transform.position;
        startColor = color;

        Vector3 randomDirection;

        if (mainCamera != null)
        {
            randomDirection =
                mainCamera.transform.up +
                mainCamera.transform.right *
                Random.Range(-randomHorizontalRange, randomHorizontalRange);
        }
        else
        {
            randomDirection = Vector3.up;
        }

        moveDirection = randomDirection.normalized;

        if (damageText != null)
        {
            damageText.text = Mathf.FloorToInt(amount).ToString();
            damageText.fontSize = fontSize * Mathf.Max(0.01f, scaleMultiplier);
            damageText.color = startColor;
        }
    }

    private void Update()
    {
        if (mainCamera != null)
            transform.forward = mainCamera.transform.forward;

        timer += Time.deltaTime;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        UpdateFade();

        if (GetMovedDistance() >= maxDistance || timer >= MaxLifeTime)
            ReturnToPool();
    }

    // 이동 거리에 따라 서서히 사라짐
    private void UpdateFade()
    {
        if (damageText == null)
            return;

        float distance = GetMovedDistance();
        float ratio = Mathf.Clamp01(distance / Mathf.Max(0.01f, maxDistance));

        Color color = startColor;
        color.a = Mathf.Lerp(1f, 0f, ratio);

        damageText.color = color;
    }

    // 시작 위치로부터 이동한 거리 계산
    private float GetMovedDistance()
    {
        return Vector3.Distance(startPosition, transform.position);
    }

    // 오브젝트 풀 반환
    private void ReturnToPool()
    {
        ObjectPool.Return(poolKey, gameObject);
    }
}
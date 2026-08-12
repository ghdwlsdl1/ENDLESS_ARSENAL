using UnityEngine;

public class CameraFovController : MonoBehaviour
{
    [InspectorLabel("대상 카메라")]
    [SerializeField] private Camera targetCamera;

    [InspectorLabel("플레이어 스탯")]
    [SerializeField] private PlayerStat playerStat;

    [InspectorLabel("변경 시 부드럽게 전환")]
    [SerializeField] private bool smoothTransition = true;

    [InspectorLabel("전환 속도")]
    [SerializeField] private float transitionSpeed;

    private float targetFov;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (playerStat == null)
        {

            playerStat = GetComponentInParent<PlayerStat>();

            if (playerStat == null)
                playerStat = FindFirstObjectByType<PlayerStat>();
        }
    }

    private void OnEnable()
    {
        if (playerStat != null)
            playerStat.OnStatChanged += RefreshTargetFov;

        RefreshTargetFov();


        if (targetCamera != null)
            targetCamera.fieldOfView = targetFov;
    }

    private void OnDisable()
    {
        if (playerStat != null)
            playerStat.OnStatChanged -= RefreshTargetFov;
    }

    private void Update()
    {
        if (targetCamera == null)
            return;

        if (!smoothTransition)
        {
            targetCamera.fieldOfView = targetFov;
            return;
        }

        targetCamera.fieldOfView = Mathf.Lerp(
            targetCamera.fieldOfView,
            targetFov,
            1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime));
    }

    private void RefreshTargetFov()
    {
        targetFov = playerStat != null ? playerStat.CameraFov : targetFov;
    }
}
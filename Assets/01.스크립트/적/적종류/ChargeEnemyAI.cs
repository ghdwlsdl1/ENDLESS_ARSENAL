using System.Collections;
using UnityEngine;

public class ChargeEnemyAI : EnemyController
{
    private enum State
    {
        Chase,
        Warning,
        Charge,
        Cooldown
    }

    [Header("이동")]

    [InspectorLabel("분리 범위")]
    [SerializeField] private float avoidRadius;

    [InspectorLabel("분리 힘")]
    [SerializeField] private float avoidWeight;

    [InspectorLabel("정지 거리")]
    [SerializeField] private float stopDistance;

    [Header("돌진")]

    [InspectorLabel("돌진 감지 거리")]
    [SerializeField] private float chargeDetectRange;

    [InspectorLabel("돌진 거리")]
    [SerializeField] private float chargeDistance;

    [InspectorLabel("돌진 속도 배율")]
    [SerializeField] private float chargeSpeedMultiplier;

    [InspectorLabel("돌진 후 대기 시간")]
    [SerializeField] private float chargeCooldown;

    [InspectorLabel("조준 오차 반경")]
    [SerializeField] private float aimErrorRadius;

    [Header("경고")]

    [InspectorLabel("경고 시간")]
    [SerializeField] private float warningTime;

    [InspectorLabel("경고 시작 위치 오프셋")]
    [SerializeField] private float warningStartOffset;

    [InspectorLabel("경고 범위 두께")]
    [SerializeField] private float warningRangeWidth;

    [InspectorLabel("경고 채움 두께")]
    [SerializeField] private float warningFillWidth;

    [InspectorLabel("경고 범위 색상")]
    [SerializeField] private Color warningRangeColor;

    [InspectorLabel("경고 채움 색상")]
    [SerializeField] private Color warningFillColor;

    private State state;
    private Vector3 chargeDirection;
    private Vector3 chargeStartPosition;
    private float chargedDistance;
    private float cooldownTimer;
    private Coroutine chargeRoutine;

    protected override void OnEnable()
    {
        base.OnEnable();

        state = State.Chase;
        chargeDirection = transform.forward;
        chargedDistance = 0f;
        cooldownTimer = 0f;
        chargeRoutine = null;
    }

    protected override void OnDisable()
    {
        if (chargeRoutine != null)
        {
            StopCoroutine(chargeRoutine);
            chargeRoutine = null;
        }

        base.OnDisable();
    }

    private void Update()
    {
        if (IsDead)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;
        
        if (target == null)
            return;

        switch (state)
        {
            case State.Chase:
                Chase();
                break;

            case State.Warning:
                break;

            case State.Charge:
                Charge();
                break;

            case State.Cooldown:
                Cooldown();
                break;
        }
    }

    private void Chase()
    {
        Vector3 currentPosition = transform.position;

        Vector3 targetPosition = target.position;
        targetPosition.y = currentPosition.y;

        Vector3 toTarget = targetPosition - currentPosition;
        toTarget.y = 0f;

        float distanceSqr = toTarget.sqrMagnitude;

        if (distanceSqr <= chargeDetectRange * chargeDetectRange)
        {
            StartChargeWarning();
            return;
        }

        if (distanceSqr <= stopDistance * stopDistance)
        {
            PlayIdleAnimation();
            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        Vector3 avoidDirection = EnemyRegistry.GetSeparationVector(this, currentPosition, avoidRadius);

        Vector3 finalDirection = moveDirection + avoidDirection * avoidWeight;
        finalDirection.y = 0f;

        if (finalDirection.sqrMagnitude <= 0.0001f)
            finalDirection = moveDirection;
        else
            finalDirection.Normalize();

        PlayMoveAnimation(finalDirection);
        transform.position = currentPosition + finalDirection * (currentMoveSpeed * Time.deltaTime);
    }

    private void StartChargeWarning()
    {
        if (state != State.Chase)
            return;

        state = State.Warning;

        Vector3 currentPosition = transform.position;

        Vector3 targetPosition = target.position;
        targetPosition.y = currentPosition.y;

        Vector2 randomCircle = Random.insideUnitCircle * aimErrorRadius;
        targetPosition += new Vector3(randomCircle.x, 0f, randomCircle.y);

        chargeDirection = targetPosition - currentPosition;
        chargeDirection.y = 0f;

        if (chargeDirection.sqrMagnitude <= 0.0001f)
            chargeDirection = transform.forward;
        else
            chargeDirection.Normalize();

        chargeRoutine = StartCoroutine(ChargeWarningRoutine());
    }

    private IEnumerator ChargeWarningRoutine()
    {
        if (WarningManager.Instance != null)
        {
            Vector3 warningOrigin =
                transform.position + chargeDirection * warningStartOffset;

            yield return WarningManager.Instance.ShowLine(
                warningOrigin,
                chargeDirection,
                chargeDistance,
                warningTime,
                warningRangeWidth,
                warningFillWidth,
                warningRangeColor,
                warningFillColor,
                WarningManager.LineWarningMode.Width);
        }
        else
        {
            yield return new WaitForSeconds(warningTime);
        }

        chargeStartPosition = transform.position;
        chargedDistance = 0f;
        state = State.Charge;
        chargeRoutine = null;
    }

    private void Charge()
    {
        float chargeMoveSpeed = currentMoveSpeed * chargeSpeedMultiplier;
        float moveDistance = chargeMoveSpeed * Time.deltaTime;

        PlayMoveAnimation(chargeDirection);
        transform.position += chargeDirection * moveDistance;
        chargedDistance = Vector3.Distance(chargeStartPosition, transform.position);

        if (chargedDistance < chargeDistance)
            return;

        cooldownTimer = chargeCooldown;
        state = State.Cooldown;
    }

    private void Cooldown()
    {
        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer > 0f)
            return;

        state = State.Chase;
    }

    private void OnDrawGizmosSelected()
    {
        GizmoUtility.DrawCircleXZ(transform.position, avoidRadius, Color.yellow);
        GizmoUtility.DrawCircleXZ(transform.position, stopDistance, Color.red);
        GizmoUtility.DrawCircleXZ(transform.position, chargeDetectRange, Color.magenta);

        Vector3 dir = Application.isPlaying ? chargeDirection : transform.forward;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = transform.forward;

        dir.y = 0f;
        dir.Normalize();

        Vector3 warningOrigin = transform.position + dir * warningStartOffset;

        GizmoUtility.DrawArrow(warningOrigin, dir * chargeDistance, Color.magenta, 4f);
    }
}
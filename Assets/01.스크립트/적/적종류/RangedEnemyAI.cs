using UnityEngine;

public class RangedEnemyAI : EnemyController
{
    [Header("이동")]

    [InspectorLabel("분리 범위")]
    [SerializeField] private float avoidRadius;

    [InspectorLabel("분리 힘")]
    [SerializeField] private float avoidWeight;

    [InspectorLabel("공격 유지 거리")]
    [SerializeField] private float attackRange;

    [InspectorLabel("도망 거리")]
    [SerializeField] private float retreatRange;

    [InspectorLabel("후퇴 속도 배율")]
    [SerializeField] private float retreatSpeedMultiplier;

    [Header("공격")]

    [InspectorLabel("투사체 무기")]
    [SerializeField] private ProjectileWeapon projectileWeapon;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (projectileWeapon == null)
            projectileWeapon = GetComponentInChildren<ProjectileWeapon>();

        if (projectileWeapon != null)
            projectileWeapon.enabled = true;
    }

    protected override void OnDisable()
    {
        if (projectileWeapon != null)
            projectileWeapon.enabled = false;

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

        Vector3 currentPosition = transform.position;

        Vector3 targetPosition = target.position;
        targetPosition.y = currentPosition.y;

        Vector3 toTarget = targetPosition - currentPosition;
        toTarget.y = 0f;

        float distanceSqr = toTarget.sqrMagnitude;

        if (distanceSqr <= retreatRange * retreatRange)
        {
            Retreat(currentPosition, toTarget);
            return;
        }

        if (distanceSqr > attackRange * attackRange)
        {
            MoveToTarget(currentPosition, toTarget);
            return;
        }
        PlayIdleAnimation();
    }

    private void MoveToTarget(Vector3 currentPosition, Vector3 toTarget)
    {
        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Vector3 moveDirection = toTarget.normalized;
        Move(currentPosition, moveDirection, currentMoveSpeed);
    }

    private void Retreat(Vector3 currentPosition, Vector3 toTarget)
    {
        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Vector3 retreatDirection = -toTarget.normalized;
        Move(currentPosition, retreatDirection, currentMoveSpeed * retreatSpeedMultiplier);
    }

    private void Move(Vector3 currentPosition, Vector3 moveDirection, float moveSpeed)
    {
        Vector3 avoidDirection = EnemyRegistry.GetSeparationVector(this, currentPosition, avoidRadius);

        Vector3 finalDirection = moveDirection + avoidDirection * avoidWeight;
        finalDirection.y = 0f;

        if (finalDirection.sqrMagnitude <= 0.0001f)
            finalDirection = moveDirection;
        else
            finalDirection.Normalize();

        PlayMoveAnimation(finalDirection);
        transform.position = currentPosition + finalDirection * (moveSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        GizmoUtility.DrawCircleXZ(transform.position, avoidRadius, Color.yellow);
        GizmoUtility.DrawCircleXZ(transform.position, attackRange, Color.red);
        GizmoUtility.DrawCircleXZ(transform.position, retreatRange, Color.cyan);
    }
}
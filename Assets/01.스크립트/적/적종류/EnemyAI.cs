using UnityEngine;

public class EnemyAI : EnemyController
{
    [InspectorLabel("분리 범위")]
    [SerializeField] private float avoidRadius;

    [InspectorLabel("분리 힘")]
    [SerializeField] private float avoidWeight;

    [InspectorLabel("정지 거리")]
    [SerializeField] private float stopDistance;

    private void Update()
    {
        if (IsDead)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;
        
        if (target == null)
            return;

        Move();
    }

    private void Move()
    {
        Vector3 currentPosition = transform.position;

        Vector3 targetPosition = target.position;
        targetPosition.y = currentPosition.y;

        Vector3 toTarget = targetPosition - currentPosition;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            return;

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

    private void OnDrawGizmosSelected()
    {
        GizmoUtility.DrawCircleXZ(transform.position, avoidRadius, Color.yellow);
        GizmoUtility.DrawCircleXZ(transform.position, stopDistance, Color.red);
    }
}
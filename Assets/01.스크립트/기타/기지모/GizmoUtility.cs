using UnityEngine;

public static class GizmoUtility
{
    // XZ 평면 원형 Gizmo
    // 몬스터 감지 범위, 공격 범위 등에 사용
    // GizmoUtility.DrawCircleXZ(transform.position, 변수명 / 2,Color.red);
    public static void DrawCircleXZ(Vector3 center, float radius, Color color, int segments = 64)
    {
        Gizmos.color = color;

        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    // 와이어 박스
    // 스폰 영역, 상호작용 범위 등에 사용
    // GizmoUtility.DrawCircleXZ(transform.position, 변수명 ,Color.red);
    public static void DrawWireBox(Vector3 center, Vector3 size, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(center, size);
    }
    
    // 회전 가능한 와이어 박스
    // GizmoUtility.DrawWireBox(center, size, rotation, Color.red);
    public static void DrawWireBox(
        Vector3 center,
        Vector3 size,
        Quaternion rotation,
        Color color)
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.color = color;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);

        Gizmos.DrawWireCube(Vector3.zero, size);

        Gizmos.matrix = oldMatrix;
    }

    // 두 지점을 잇는 선
    // GizmoUtility.DrawLine(시작위치, 끝위치 ,Color.red);
    public static void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(from, to);
    }

    // 방향 화살표
    // 이동 방향, 넉백 방향 등에 사용
    // GizmoUtility.DrawArrow(transform.position,direction * attackRange,Color.yellow,4f);
    public static void DrawArrow( Vector3 from, Vector3 direction, Color color, float headLength = 0.3f)
    {
        if (direction == Vector3.zero)
            return;

        Gizmos.color = color;

        Vector3 end = from + direction;

        // 몸통
        Gizmos.DrawLine(from, end);

        // 화살촉
        Vector3 right =
            Quaternion.LookRotation(direction) *
            Quaternion.Euler(0f, 140f, 0f) *
            Vector3.forward;

        Vector3 left =
            Quaternion.LookRotation(direction) *
            Quaternion.Euler(0f, 220f, 0f) *
            Vector3.forward;

        Gizmos.DrawLine(end, end + right * headLength);
        Gizmos.DrawLine(end, end + left * headLength);
    }
}
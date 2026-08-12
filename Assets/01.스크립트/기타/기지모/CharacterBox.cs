using UnityEngine;

public class CharacterBox : MonoBehaviour
{
    [Header("범위")]
    [SerializeField] private BoxCollider boxCollider;

    [SerializeField] private Color gizmoColor = Color.red;

    private void Reset()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnDrawGizmosSelected()
    {
        if (boxCollider == null)
            return;

        Gizmos.color = gizmoColor;

        Transform boxTransform = boxCollider.transform;

        Vector3 center = boxTransform.TransformPoint(boxCollider.center);

        // Scale까지 반영
        Vector3 size = Vector3.Scale(boxCollider.size, boxTransform.lossyScale);

        Vector3 half = new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);

        Vector3 right = boxTransform.right * half.x;
        Vector3 forward = boxTransform.forward * half.z;

        Vector3 p1 = center - right - forward;
        Vector3 p2 = center - right + forward;
        Vector3 p3 = center + right + forward;
        Vector3 p4 = center + right - forward;

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
}
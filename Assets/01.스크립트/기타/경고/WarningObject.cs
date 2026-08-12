using UnityEngine;

public class WarningObject : MonoBehaviour
{
    public SpriteRenderer[] SpriteRenderers { get; private set; }
    public MeshRenderer[] MeshRenderers { get; private set; }
    public LineRenderer LineRenderer { get; private set; }

    private void Awake()
    {
        CacheComponents();
    }

    //=================================================================================

    // 렌더러 캐싱
    private void CacheComponents()
    {
        SpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        MeshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        LineRenderer = GetComponent<LineRenderer>();
    }

    //=================================================================================

    // 경고 오브젝트 색상 설정
    public void SetColor(
        MaterialPropertyBlock propertyBlock,
        Color color)
    {
        foreach (SpriteRenderer spriteRenderer in SpriteRenderers)
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            spriteRenderer.color = color;
        }

        foreach (MeshRenderer meshRenderer in MeshRenderers)
        {
            if (meshRenderer == null)
            {
                continue;
            }

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetColor("_BaseColor", color);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        if (LineRenderer != null)
        {
            LineRenderer.startColor = color;
            LineRenderer.endColor = color;
        }
    }
}
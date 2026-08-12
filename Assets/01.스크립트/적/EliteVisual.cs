using UnityEngine;

public class EliteVisual : MonoBehaviour
{
    private class OutlineGroup
    {
        public SpriteRenderer Source;
        public SpriteRenderer Outline;
    }

    [Header("외곽선")]

    [InspectorLabel("실루엣 머테리얼")]
    [SerializeField] private Material silhouetteMaterial;

    [InspectorLabel("외곽선 확대 배율")]
    [SerializeField] private float outlineScale;

    private OutlineGroup[] outlineGroups;
    private MaterialPropertyBlock propertyBlock;

    private Vector3 baseScale;
    private bool baseScaleCached;
    private bool isElite;

    private void Awake()
    {
        CacheBaseScale();
        propertyBlock = new MaterialPropertyBlock();

        CreateOutlineGroups();
    }

    private void CacheBaseScale()
    {
        if (baseScaleCached)
            return;

        baseScale = transform.localScale;
        baseScaleCached = true;
    }

    private void CreateOutlineGroups()
    {
        if (silhouetteMaterial == null)
            return;
        
        SpriteRenderer[] sources = GetComponentsInChildren<SpriteRenderer>(true);

        outlineGroups = new OutlineGroup[sources.Length];

        for (int i = 0; i < sources.Length; i++)
        {
            SpriteRenderer source = sources[i];

            GameObject outlineObj = new GameObject("EliteOutline");
            outlineObj.transform.SetParent(source.transform, false);
            outlineObj.transform.localPosition = Vector3.zero;
            outlineObj.transform.localRotation = Quaternion.identity;
            outlineObj.transform.localScale = Vector3.one * outlineScale;

            SpriteRenderer outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
            outlineRenderer.sprite = source.sprite;
            outlineRenderer.sharedMaterial = silhouetteMaterial;
            outlineRenderer.sortingLayerID = source.sortingLayerID;
            outlineRenderer.sortingOrder = source.sortingOrder - 1;
            outlineRenderer.enabled = false;

            outlineGroups[i] = new OutlineGroup { Source = source, Outline = outlineRenderer };
        }
    }

    private void LateUpdate()
    {
        if (!isElite || outlineGroups == null)
            return;

        for (int i = 0; i < outlineGroups.Length; i++)
        {
            OutlineGroup group = outlineGroups[i];

            if (group.Source == null || group.Outline == null)
                continue;

            if (!group.Source.gameObject.activeInHierarchy)
                continue;

            Sprite currentSprite = group.Source.sprite;

            if (group.Outline.sprite != currentSprite)
                group.Outline.sprite = currentSprite;

            group.Outline.flipX = group.Source.flipX;
            group.Outline.flipY = group.Source.flipY;
        }
    }
    
    public void ClearElite()
    {
        CacheBaseScale();

        transform.localScale = baseScale;
        isElite = false;

        SetOutlineActive(false);
    }
    
    public void DisableOutline()
    {
        SetOutlineActive(false);
    }

    public void ApplyElite(Color auraColor, float scaleMultiplier)
    {
        CacheBaseScale();

        transform.localScale = baseScale * Mathf.Max(0.01f, scaleMultiplier);
        isElite = true;

        SetOutlineActive(true);
        ApplyOutlineColor(auraColor);
    }

    private void SetOutlineActive(bool active)
    {
        if (outlineGroups == null)
            return;

        for (int i = 0; i < outlineGroups.Length; i++)
        {
            if (outlineGroups[i].Outline != null)
                outlineGroups[i].Outline.enabled = active;
        }
    }

    private void ApplyOutlineColor(Color color)
    {
        if (outlineGroups == null)
            return;

        for (int i = 0; i < outlineGroups.Length; i++)
        {
            SpriteRenderer outline = outlineGroups[i].Outline;

            if (outline == null)
                continue;

            outline.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            outline.SetPropertyBlock(propertyBlock);
        }
    }
}
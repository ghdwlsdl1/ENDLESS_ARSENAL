using System.Collections;
using UnityEngine;

public class EnemyHitFlash : MonoBehaviour
{
    [InspectorLabel("플래시 유지 시간")]
    [SerializeField] private float flashDuration;

    [InspectorLabel("플래시 색상")]
    [SerializeField] private Color flashColor;

    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    private SpriteRenderer[] spriteRenderers;
    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashRoutine;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        flashRoutine = null;
        SetFlash(0f);
    }

    public void PlayFlash()
    {
        if (!isActiveAndEnabled)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;

            float amount = 1f - Mathf.Clamp01(timer / flashDuration);
            SetFlash(amount);

            yield return null;
        }

        SetFlash(0f);
        flashRoutine = null;
    }

    private void SetFlash(float amount)
    {
        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];

            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FlashAmountId, amount);
            propertyBlock.SetColor(FlashColorId, flashColor);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
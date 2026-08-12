using System.Collections;
using UnityEngine;

public class WarningManager : MonoBehaviour
{
    public enum LineWarningMode
    {
        Length,
        Width
    }
    
    [Tooltip("ObjectPool에 등록된 직선 경고 키")]
    [SerializeField] private string lineWarningKey = "LineWarning";

    [Tooltip("ObjectPool에 등록된 원형 경고 키")]
    [SerializeField] private string circleWarningKey = "CircleWarning";

    [Tooltip("ObjectPool에 등록된 박스형 경고 키")]
    [SerializeField] private string boxWarningKey = "BoxWarning";
    
    public static WarningManager Instance { get; private set; }
    private Material lineMaterial;
    private MaterialPropertyBlock propertyBlock;

    //=================================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        propertyBlock = new MaterialPropertyBlock();
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
    }
    
    //=================================================================================

    // 원형 경고 표시
    public IEnumerator ShowCircle(
        Vector3 center,
        float radius,
        float warningTime,
        Color rangeColor,
        Color fillColor,
        Quaternion? rotation = null)
    {
        Quaternion rot = rotation ?? Quaternion.identity;

        GameObject range = ObjectPool.Get(circleWarningKey, center, rot);
        GameObject fill = ObjectPool.Get(circleWarningKey, center, rot);

        if (range == null || fill == null)
        {
            ReturnWarning(range);
            ReturnWarning(fill);
            yield break;
        }

        float diameter = radius * 2f;
        Vector3 finalScale = new Vector3(diameter, 1f, diameter);
        Vector3 startScale = new Vector3(0f, 1f, 0f);

        range.transform.localScale = finalScale;
        fill.transform.localScale = startScale;

        SetColor(range, rangeColor);
        SetColor(fill, fillColor);

        StartCoroutine(CoAnimateScaleAndReturn(
            fill,
            range,
            startScale,
            finalScale,
            warningTime));

        yield return new WaitForSeconds(warningTime);
    }

    //=================================================================================

    // 박스형 경고 표시
    public IEnumerator ShowBox(
        Vector3 center,
        Vector2 size,
        float warningTime,
        Color rangeColor,
        Color fillColor,
        Quaternion? rotation = null)
    {
        Quaternion rot = rotation ?? Quaternion.identity;

        GameObject range = ObjectPool.Get(boxWarningKey, center, rot);
        GameObject fill = ObjectPool.Get(boxWarningKey, center, rot);

        if (range == null || fill == null)
        {
            ReturnWarning(range);
            ReturnWarning(fill);
            yield break;
        }

        Vector3 finalScale = new Vector3(size.x, 1f, size.y);
        Vector3 startScale = new Vector3(0f, 1f, 0f);

        range.transform.localScale = finalScale;
        fill.transform.localScale = startScale;

        SetColor(range, rangeColor);
        SetColor(fill, fillColor);

        StartCoroutine(CoAnimateScaleAndReturn(
            fill,
            range,
            startScale,
            finalScale,
            warningTime));

        yield return new WaitForSeconds(warningTime);
    }

    //=================================================================================

    // 직선 경고 표시
    public IEnumerator ShowLine(
        Vector3 origin,
        Vector3 direction,
        float lineLength,
        float warningTime,
        float rangeWidth,
        float fillWidth,
        Color rangeColor,
        Color fillColor,
        LineWarningMode warningMode = LineWarningMode.Length)
    {
        GameObject range = ObjectPool.Get(lineWarningKey, origin, Quaternion.identity);
        GameObject fill = ObjectPool.Get(lineWarningKey, origin, Quaternion.identity);

        if (range == null || fill == null)
        {
            ReturnWarning(range);
            ReturnWarning(fill);
            yield break;
        }

        LineRenderer rangeLine = GetOrAddLineRenderer(range);
        LineRenderer fillLine = GetOrAddLineRenderer(fill);

        Vector3 endPos = origin + direction.normalized * lineLength;

        SetLine(rangeLine, origin, endPos, rangeWidth, rangeColor);

        StartCoroutine(CoAnimateLineAndReturn(
            fillLine,
            range,
            fill,
            origin,
            endPos,
            fillWidth,
            fillColor,
            warningTime,
            warningMode));

        yield return new WaitForSeconds(warningTime);
    }

    //=================================================================================

    // 라인 설정
    private void SetLine(
        LineRenderer line,
        Vector3 start,
        Vector3 end,
        float width,
        Color color)
    {
        if (line == null)
        {
            return;
        }

        line.positionCount = 2;
        line.useWorldSpace = true;

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        line.startWidth = width;
        line.endWidth = width;

        line.startColor = color;
        line.endColor = color;
    }

    //=================================================================================

    // 오브젝트 크기 애니메이션
    private IEnumerator AnimateScale(
        GameObject obj,
        Vector3 startScale,
        Vector3 endScale,
        float duration)
    {
        if (obj == null)
        {
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            float ratio = time / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, endScale, ratio);

            time += Time.deltaTime;
            yield return null;
        }

        obj.transform.localScale = endScale;
    }
    //=================================================================================

    // 라인 두께 애니메이션
    private IEnumerator AnimateLineWidth(
        LineRenderer line,
        float startWidth,
        float endWidth,
        float duration)
    {
        if (line == null)
        {
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            float ratio = time / duration;
            float currentWidth = Mathf.Lerp(startWidth, endWidth, ratio);

            line.startWidth = currentWidth;
            line.endWidth = currentWidth;

            time += Time.deltaTime;
            yield return null;
        }

        line.startWidth = endWidth;
        line.endWidth = endWidth;
    }
    
    // 라인 길이 애니메이션
    private IEnumerator AnimateLineLength(
        LineRenderer line,
        Vector3 start,
        Vector3 end,
        float duration)
    {
        if (line == null)
        {
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            float ratio = time / duration;
            Vector3 currentEnd = Vector3.Lerp(start, end, ratio);

            line.SetPosition(0, start);
            line.SetPosition(1, currentEnd);

            time += Time.deltaTime;
            yield return null;
        }

        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    //=================================================================================

    // 경고 반환
    private void ReturnWarning(GameObject warning)
    {
        if (warning == null)
        {
            return;
        }

        warning.transform.localScale = Vector3.one;
        ObjectPool.Return(warning);
    }
    
    // 완료 후 경고 오브젝트를 자동으로 풀에 반환
    private IEnumerator CoAnimateScaleAndReturn(
        GameObject fill,
        GameObject range,
        Vector3 startScale,
        Vector3 finalScale,
        float warningTime)
    {
        yield return AnimateScale(
            fill,
            startScale,
            finalScale,
            warningTime);

        ReturnWarning(range);
        ReturnWarning(fill);
    }
    private IEnumerator CoAnimateLineAndReturn(
        LineRenderer fillLine,
        GameObject range,
        GameObject fill,
        Vector3 origin,
        Vector3 endPos,
        float fillWidth,
        Color fillColor,
        float warningTime,
        LineWarningMode warningMode)
    {
        if (warningMode == LineWarningMode.Width)
        {
            SetLine(fillLine, origin, endPos, 0f, fillColor);

            yield return AnimateLineWidth(
                fillLine,
                0f,
                fillWidth,
                warningTime);
        }
        else
        {
            SetLine(fillLine, origin, origin, fillWidth, fillColor);

            yield return AnimateLineLength(
                fillLine,
                origin,
                endPos,
                warningTime);
        }

        ReturnWarning(range);
        ReturnWarning(fill);
    }

    //=================================================================================

    // LineRenderer를 가져오거나 새로 생성
    private LineRenderer GetOrAddLineRenderer(GameObject obj)
    {
        if (obj.TryGetComponent(out WarningObject warningObject) &&
            warningObject.LineRenderer != null)
        {
            LineRenderer cachedLineRenderer = warningObject.LineRenderer;

            cachedLineRenderer.material = lineMaterial;
            cachedLineRenderer.loop = false;
            cachedLineRenderer.numCapVertices = 0;
            cachedLineRenderer.numCornerVertices = 0;

            return cachedLineRenderer;
        }

        if (!obj.TryGetComponent(out LineRenderer lr))
        {
            lr = obj.AddComponent<LineRenderer>();
        }

        lr.material = lineMaterial;
        lr.loop = false;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;

        return lr;
    }

    //=================================================================================

    // 경고 오브젝트 색상 설정
    private void SetColor(GameObject warning, Color color)
    {
        if (warning == null)
        {
            return;
        }

        if (warning.TryGetComponent(out WarningObject warningObject))
        {
            warningObject.SetColor(propertyBlock, color);
            return;
        }
    }
}
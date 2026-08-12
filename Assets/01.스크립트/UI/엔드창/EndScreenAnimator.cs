using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class EndScreenAnimator : MonoBehaviour
{
    [Header("게임 오버")]

    [InspectorLabel("게임 오버 텍스트")]
    [SerializeField] private TMP_Text gameOverText;

    [InspectorLabel("글자 출력 간격")]
    [SerializeField] private float characterInterval;

    [InspectorLabel("글자 박힘 시간")]
    [SerializeField] private float characterImpactDuration;

    [InspectorLabel("글자 시작 크기")]
    [SerializeField] private float characterStartScale;

    [InspectorLabel("글자 시작 높이")]
    [SerializeField] private float characterStartOffsetY;


    [Header("결과 정보")]

    [InspectorLabel("결과 정보")]
    [SerializeField] private RectTransform informationRoot;

    [InspectorLabel("결과 정보 캔버스 그룹")]
    [SerializeField] private CanvasGroup informationCanvasGroup;

    [InspectorLabel("결과 등장 지연")]
    [SerializeField] private float informationDelay;

    [InspectorLabel("결과 등장 시간")]
    [SerializeField] private float informationDuration;

    [InspectorLabel("결과 시작 위치")]
    [SerializeField] private float informationStartOffsetY;


    [Header("재시작 버튼")]

    [InspectorLabel("재시작 버튼")]
    [SerializeField] private RectTransform retryButton;

    [InspectorLabel("재시작 버튼 캔버스 그룹")]
    [SerializeField] private CanvasGroup retryCanvasGroup;

    [InspectorLabel("버튼 등장 지연")]
    [SerializeField] private float retryDelay;

    [InspectorLabel("버튼 등장 시간")]
    [SerializeField] private float retryDuration;

    [InspectorLabel("버튼 시작 크기")]
    [SerializeField] private float retryStartScale;


    [Header("닫기")]

    [InspectorLabel("닫기 시간")]
    [SerializeField] private float closeDuration;


    private Sequence currentSequence;

    private TMP_MeshInfo[] originalMeshInfo;

    private Vector2 informationOriginalPosition;
    private Vector3 retryOriginalScale;

    private bool isInitialized;


    public bool IsPlaying =>
        currentSequence != null &&
        currentSequence.IsActive() &&
        currentSequence.IsPlaying();

    public bool CanRetry { get; private set; }


    private void Awake()
    {
        Initialize();
        SetHiddenImmediately();
    }

    private void OnDisable()
    {
        KillCurrentSequence();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (informationRoot != null)
        {
            informationOriginalPosition =
                informationRoot.anchoredPosition;

            if (informationCanvasGroup == null)
            {
                informationCanvasGroup =
                    informationRoot.GetComponent<CanvasGroup>();
            }

            if (informationCanvasGroup == null)
            {
                informationCanvasGroup =
                    informationRoot.gameObject
                        .AddComponent<CanvasGroup>();
            }
        }

        if (retryButton != null)
        {
            retryOriginalScale =
                retryButton.localScale;

            if (retryCanvasGroup == null)
            {
                retryCanvasGroup =
                    retryButton.GetComponent<CanvasGroup>();
            }

            if (retryCanvasGroup == null)
            {
                retryCanvasGroup =
                    retryButton.gameObject
                        .AddComponent<CanvasGroup>();
            }
        }

        isInitialized = true;
    }

    public void PlayOpen(
        Action onComplete = null)
    {
        Initialize();
        KillCurrentSequence();
        PrepareOpenState();

        currentSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        AppendGameOverSequence();

        currentSequence.AppendInterval(
            informationDelay);

        AppendInformationSequence();

        currentSequence.AppendInterval(
            retryDelay);

        AppendRetrySequence();

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            CompleteOpenState();

            onComplete?.Invoke();
        });
    }

    public void PlayClose(
        Action onComplete)
    {
        Initialize();

        if (!CanRetry)
            return;

        KillCurrentSequence();

        CanRetry = false;

        if (retryCanvasGroup != null)
        {
            retryCanvasGroup.interactable = false;
            retryCanvasGroup.blocksRaycasts = false;
        }

        currentSequence =
            DOTween.Sequence()
                .SetUpdate(true);

        if (gameOverText != null)
        {
            currentSequence.Join(
                gameOverText
                    .DOFade(
                        0f,
                        closeDuration)
                    .SetEase(Ease.InQuad));
        }

        if (informationCanvasGroup != null)
        {
            currentSequence.Join(
                informationCanvasGroup
                    .DOFade(
                        0f,
                        closeDuration)
                    .SetEase(Ease.InQuad));
        }

        if (retryCanvasGroup != null)
        {
            currentSequence.Join(
                retryCanvasGroup
                    .DOFade(
                        0f,
                        closeDuration)
                    .SetEase(Ease.InQuad));
        }

        currentSequence.OnComplete(() =>
        {
            currentSequence = null;

            onComplete?.Invoke();
        });
    }

    public void SetHiddenImmediately()
    {
        Initialize();
        KillCurrentSequence();

        CanRetry = false;

        if (gameOverText != null)
        {
            gameOverText.alpha = 1f;
            gameOverText.maxVisibleCharacters = 0;
        }

        if (informationRoot != null)
        {
            informationRoot.anchoredPosition =
                informationOriginalPosition;
        }

        if (informationCanvasGroup != null)
            informationCanvasGroup.alpha = 0f;

        if (retryButton != null)
        {
            retryButton.localScale =
                retryOriginalScale;
        }

        if (retryCanvasGroup != null)
        {
            retryCanvasGroup.alpha = 0f;
            retryCanvasGroup.interactable = false;
            retryCanvasGroup.blocksRaycasts = false;
        }
    }

    private void PrepareOpenState()
    {
        CanRetry = false;

        if (gameOverText != null)
        {
            gameOverText.DOKill();

            gameOverText.alpha = 1f;

            gameOverText.maxVisibleCharacters =
                int.MaxValue;

            gameOverText.ForceMeshUpdate();

            originalMeshInfo =
                gameOverText.textInfo
                    .CopyMeshInfoVertexData();

            gameOverText.maxVisibleCharacters = 0;

            RestoreAllCharacterVertices();
        }

        if (informationRoot != null)
        {
            informationRoot.DOKill();

            informationRoot.anchoredPosition =
                informationOriginalPosition +
                Vector2.up *
                informationStartOffsetY;
        }

        if (informationCanvasGroup != null)
        {
            informationCanvasGroup.DOKill();
            informationCanvasGroup.alpha = 0f;
        }

        if (retryButton != null)
        {
            retryButton.DOKill();

            retryButton.localScale =
                retryOriginalScale *
                retryStartScale;
        }

        if (retryCanvasGroup != null)
        {
            retryCanvasGroup.DOKill();

            retryCanvasGroup.alpha = 0f;
            retryCanvasGroup.interactable = false;
            retryCanvasGroup.blocksRaycasts = false;
        }
    }

    private void AppendGameOverSequence()
    {
        if (gameOverText == null ||
            originalMeshInfo == null)
        {
            return;
        }

        int characterCount =
            gameOverText.textInfo.characterCount;

        float startTime = 0f;

        for (int i = 0;
             i < characterCount;
             i++)
        {
            TMP_CharacterInfo characterInfo =
                gameOverText.textInfo.characterInfo[i];

            if (!characterInfo.isVisible)
                continue;

            int characterIndex = i;
            float characterStartTime = startTime;

            currentSequence.InsertCallback(
                characterStartTime,
                () =>
                {
                    gameOverText.maxVisibleCharacters =
                        characterIndex + 1;
                });

            currentSequence.Insert(
                characterStartTime,
                CreateCharacterImpactTween(
                    characterIndex));

            startTime += characterInterval;
        }
    }

    private Tween CreateCharacterImpactTween(
        int characterIndex)
    {
        float progress = 0f;

        return DOTween
            .To(
                () => progress,
                value =>
                {
                    progress = value;

                    UpdateCharacterVertices(
                        characterIndex,
                        progress);
                },
                1f,
                characterImpactDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                RestoreCharacterVertices(
                    characterIndex);
            });
    }

    private void UpdateCharacterVertices(
        int characterIndex,
        float progress)
    {
        if (gameOverText == null ||
            originalMeshInfo == null)
        {
            return;
        }

        TMP_TextInfo textInfo =
            gameOverText.textInfo;

        if (characterIndex < 0 ||
            characterIndex >=
            textInfo.characterCount)
        {
            return;
        }

        TMP_CharacterInfo characterInfo =
            textInfo.characterInfo[characterIndex];

        if (!characterInfo.isVisible)
            return;

        int materialIndex =
            characterInfo.materialReferenceIndex;

        int vertexIndex =
            characterInfo.vertexIndex;

        Vector3[] sourceVertices =
            originalMeshInfo[materialIndex].vertices;

        Vector3[] destinationVertices =
            textInfo.meshInfo[materialIndex].vertices;

        Vector3 center =
            (
                sourceVertices[vertexIndex] +
                sourceVertices[vertexIndex + 2]
            ) * 0.5f;

        float scale =
            Mathf.Lerp(
                characterStartScale,
                1f,
                progress);

        float offsetY =
            Mathf.Lerp(
                characterStartOffsetY,
                0f,
                progress);

        Vector3 offset =
            Vector3.up * offsetY;

        for (int i = 0;
             i < 4;
             i++)
        {
            Vector3 originalVertex =
                sourceVertices[vertexIndex + i];

            Vector3 direction =
                originalVertex - center;

            destinationVertices[vertexIndex + i] =
                center +
                direction * scale +
                offset;
        }

        ApplyMesh(materialIndex);
    }

    private void RestoreCharacterVertices(
        int characterIndex)
    {
        if (gameOverText == null ||
            originalMeshInfo == null)
        {
            return;
        }

        TMP_TextInfo textInfo =
            gameOverText.textInfo;

        if (characterIndex < 0 ||
            characterIndex >=
            textInfo.characterCount)
        {
            return;
        }

        TMP_CharacterInfo characterInfo =
            textInfo.characterInfo[characterIndex];

        if (!characterInfo.isVisible)
            return;

        int materialIndex =
            characterInfo.materialReferenceIndex;

        int vertexIndex =
            characterInfo.vertexIndex;

        Vector3[] sourceVertices =
            originalMeshInfo[materialIndex].vertices;

        Vector3[] destinationVertices =
            textInfo.meshInfo[materialIndex].vertices;

        for (int i = 0;
             i < 4;
             i++)
        {
            destinationVertices[vertexIndex + i] =
                sourceVertices[vertexIndex + i];
        }

        ApplyMesh(materialIndex);
    }

    private void RestoreAllCharacterVertices()
    {
        if (gameOverText == null ||
            originalMeshInfo == null)
        {
            return;
        }

        TMP_TextInfo textInfo =
            gameOverText.textInfo;

        for (int i = 0;
             i < textInfo.meshInfo.Length;
             i++)
        {
            Vector3[] sourceVertices =
                originalMeshInfo[i].vertices;

            Vector3[] destinationVertices =
                textInfo.meshInfo[i].vertices;

            int vertexCount =
                Mathf.Min(
                    sourceVertices.Length,
                    destinationVertices.Length);

            for (int j = 0;
                 j < vertexCount;
                 j++)
            {
                destinationVertices[j] =
                    sourceVertices[j];
            }

            ApplyMesh(i);
        }
    }

    private void ApplyMesh(
        int materialIndex)
    {
        TMP_MeshInfo meshInfo =
            gameOverText.textInfo
                .meshInfo[materialIndex];

        meshInfo.mesh.vertices =
            meshInfo.vertices;

        gameOverText.UpdateGeometry(
            meshInfo.mesh,
            materialIndex);
    }

    private void AppendInformationSequence()
    {
        if (informationRoot != null)
        {
            currentSequence.Join(
                informationRoot
                    .DOAnchorPos(
                        informationOriginalPosition,
                        informationDuration)
                    .SetEase(Ease.OutCubic));
        }

        if (informationCanvasGroup != null)
        {
            currentSequence.Join(
                informationCanvasGroup
                    .DOFade(
                        1f,
                        informationDuration)
                    .SetEase(Ease.OutQuad));
        }
    }

    private void AppendRetrySequence()
    {
        if (retryButton != null)
        {
            currentSequence.Join(
                retryButton
                    .DOScale(
                        retryOriginalScale,
                        retryDuration)
                    .SetEase(Ease.OutBack));
        }

        if (retryCanvasGroup != null)
        {
            currentSequence.Join(
                retryCanvasGroup
                    .DOFade(
                        1f,
                        retryDuration)
                    .SetEase(Ease.OutQuad));
        }
    }

    private void CompleteOpenState()
    {
        if (gameOverText != null)
        {
            gameOverText.alpha = 1f;

            gameOverText.maxVisibleCharacters =
                int.MaxValue;

            RestoreAllCharacterVertices();
        }

        if (informationRoot != null)
        {
            informationRoot.anchoredPosition =
                informationOriginalPosition;
        }

        if (informationCanvasGroup != null)
            informationCanvasGroup.alpha = 1f;

        if (retryButton != null)
        {
            retryButton.localScale =
                retryOriginalScale;
        }

        if (retryCanvasGroup != null)
        {
            retryCanvasGroup.alpha = 1f;
            retryCanvasGroup.interactable = true;
            retryCanvasGroup.blocksRaycasts = true;
        }

        CanRetry = true;
    }

    private void KillCurrentSequence()
    {
        if (currentSequence != null)
        {
            currentSequence.Kill();
            currentSequence = null;
        }

        if (gameOverText != null)
            gameOverText.DOKill();

        if (informationRoot != null)
            informationRoot.DOKill();

        if (informationCanvasGroup != null)
            informationCanvasGroup.DOKill();

        if (retryButton != null)
            retryButton.DOKill();

        if (retryCanvasGroup != null)
            retryCanvasGroup.DOKill();
    }
}
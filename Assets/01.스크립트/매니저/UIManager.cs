using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI 타입
public enum UIPanelType
{
    None = 0,

    StartScreen,
    Compensation,
    EndScreen,
    SettingScreen,
    CharacterScreen,
    InventoryScreen,
    MapScreen,
    ExplanationScreen
}

public sealed class UIManager : MonoBehaviour
{
    [Header("UI 패널")]
    [SerializeField] private UIPanelBinding[] panelBindings;
    
    [Header("페이드")]
    
    [InspectorLabel("페이드 이미지")]
    [SerializeField] private Image fadeImage;

    [InspectorLabel("페이드 시간")]
    [SerializeField] private float fadeDuration = 0.5f;
    
    // UI 패널 저장
    private readonly Dictionary<UIPanelType, GameObject> panels = new();

    // 현재 열려있는 패널
    private UIPanelType currentPanel = UIPanelType.None;

    public UIPanelType CurrentPanel => currentPanel;
    
    private float currentFadeDuration;

    private void Awake()
    {
        panels.Clear();

        if (panelBindings == null)
            return;

        foreach (var binding in panelBindings)
        {
            // 패널 미등록 시 무시
            if (binding.panel == null)
                continue;

            // 중복 등록 방지
            if (panels.ContainsKey(binding.type))
                continue;

            // 패널 등록
            panels.Add(binding.type, binding.panel);
        }
    }
    
    private void Start()
    {
        SetFadeAlpha(0f);
    }
    
    // 지정한 패널만 표시
    public void ShowPanel(UIPanelType type)
    {
        currentPanel = type;

        foreach (var panel in panels)
        {
            // 파괴되었거나 비어있는 패널 무시
            if (panel.Value == null)
                continue;

            panel.Value.SetActive(panel.Key == currentPanel);
        }
    }
    
    // 기존 패널 유지하고 추가 표시
    public void ShowPopup(UIPanelType type)
    {
        if (panels.TryGetValue(type, out GameObject panel))
        {
            panel.SetActive(true);
        }
    }
    // 특정 패널 숨김
    public void HidePanel(UIPanelType type)
    {
        if (panels.TryGetValue(type, out GameObject panel))
        {
            panel.SetActive(false);
        }
    }
    
    // 모든 패널 숨김
    public void HideAllPanels()
    {
        currentPanel = UIPanelType.None;

        foreach (var panel in panels)
        {
            // 파괴되었거나 비어있는 패널 무시
            if (panel.Value == null)
                continue;

            panel.Value.SetActive(false);
        }
    }
    public IEnumerator FadeOut(float duration)
    {
        currentFadeDuration = fadeDuration;
        fadeDuration = duration;

        yield return FadeOut();

        fadeDuration = currentFadeDuration;
    }

    public IEnumerator FadeIn(float duration)
    {
        currentFadeDuration = fadeDuration;
        fadeDuration = duration;

        yield return FadeIn();

        fadeDuration = currentFadeDuration;
    }
    public IEnumerator FadeOut()
    {
        if (fadeImage == null)
            yield break;

        fadeImage.raycastTarget = true;

        Color color = fadeImage.color;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;

            color.a = Mathf.Lerp(0f, 1f, time / fadeDuration);
            fadeImage.color = color;

            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage == null)
            yield break;

        Color color = fadeImage.color;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;

            color.a = Mathf.Lerp(1f, 0f, time / fadeDuration);
            fadeImage.color = color;

            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;

        fadeImage.raycastTarget = false;
    }
    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;

        fadeImage.raycastTarget = alpha > 0f;
    }
    
    [Serializable]
    public struct UIPanelBinding
    {
        // 패널 타입
        public UIPanelType type;

        // 실제 패널 오브젝트
        public GameObject panel;
    }
}
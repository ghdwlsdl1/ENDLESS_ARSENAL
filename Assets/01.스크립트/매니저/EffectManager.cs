using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("이펙트 설정")]
    [InspectorLabel("이펙트 목록")]
    [SerializeField] private EffectSetting[] effectSettings;

    private readonly Dictionary<string, EffectSetting> effectMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RegisterEffects();
    }

    private void RegisterEffects()
    {
        effectMap.Clear();

        if (effectSettings == null)
            return;

        for (int i = 0; i < effectSettings.Length; i++)
        {
            EffectSetting setting = effectSettings[i];

            if (setting == null)
                continue;

            if (string.IsNullOrEmpty(setting.poolKey))
                continue;

            if (effectMap.ContainsKey(setting.poolKey))
                continue;

            effectMap.Add(setting.poolKey, setting);
        }
    }

    public static GameObject Play(string poolKey, Vector3 position)
    {
        return Play(poolKey, position, Quaternion.identity, Vector3.one);
    }

    public static GameObject Play(string poolKey, Vector3 position, Quaternion rotation)
    {
        return Play(poolKey, position, rotation, Vector3.one);
    }

    public static GameObject Play(string poolKey, Vector3 position, float scale)
    {
        return Play(poolKey, position, Quaternion.identity, Vector3.one * scale);
    }

    public static GameObject Play(string poolKey, Vector3 position, Quaternion rotation, float scale)
    {
        return Play(poolKey, position, rotation, Vector3.one * scale);
    }

    public static GameObject Play(string poolKey, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (Instance == null)
            return null;

        if (string.IsNullOrEmpty(poolKey))
            return null;

        GameObject effectObj = ObjectPool.Get(poolKey, position, rotation);

        if (effectObj == null)
            return null;

        float settingScale = Instance.GetSettingScale(poolKey);
        float returnDelay = Instance.GetReturnDelay(poolKey);

        effectObj.transform.localScale = scale * settingScale;

        Instance.PlayParticles(effectObj);
        Instance.StartCoroutine(Instance.ReturnAfterDelay(poolKey, effectObj, returnDelay));

        return effectObj;
    }

    private float GetSettingScale(string poolKey)
    {
        if (effectMap.TryGetValue(poolKey, out EffectSetting setting))
            return Mathf.Max(0.01f, setting.scale);

        return 1f;
    }

    private float GetReturnDelay(string poolKey)
    {
        if (effectMap.TryGetValue(poolKey, out EffectSetting setting))
            return Mathf.Max(0.01f, setting.returnDelay);

        return 1f;
    }

    private void PlayParticles(GameObject effectObj)
    {
        if (effectObj == null)
            return;

        ParticleSystem[] particles = effectObj.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Clear(true);
            particles[i].Play(true);
        }
    }

    private IEnumerator ReturnAfterDelay(string poolKey, GameObject effectObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effectObj == null)
            yield break;

        ObjectPool.Return(poolKey, effectObj);
    }

    [Serializable]
    public class EffectSetting
    {
        [InspectorLabel("풀링 키")]
        public string poolKey;

        [InspectorLabel("이펙트 크기")]
        public float scale = 1f;

        [InspectorLabel("반환 시간")]
        public float returnDelay = 0.5f;
    }
}
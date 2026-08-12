using System.Collections.Generic;
using UnityEngine;

public class MapTarget : MonoBehaviour
{
    public static readonly List<MapTarget> ActiveTargets = new();

    [InspectorLabel("타겟 타입")]
    [SerializeField] private MapTargetType targetType = MapTargetType.SupplyCrate;

    public MapTargetType TargetType => targetType;
    public Transform TargetTransform => transform;

    private void OnEnable()
    {
        if (!ActiveTargets.Contains(this))
            ActiveTargets.Add(this);
    }

    private void OnDisable()
    {
        ActiveTargets.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveTargets.Remove(this);
    }
}

public enum MapTargetType
{
    SupplyCrate,
    Shop,
    Event,
    Boss
}
using UnityEngine;

public class PlayerDistanceTracker : MonoBehaviour
{
    private Vector3 startPosition;
    public Vector3 StartPosition => startPosition;

    public float CurrentDistance { get; private set; }
    public float MaxDistance { get; private set; }

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        Vector3 diff = transform.position - startPosition;
        diff.y = 0f;

        CurrentDistance = diff.magnitude;

        if (CurrentDistance > MaxDistance)
            MaxDistance = CurrentDistance;
    }
}
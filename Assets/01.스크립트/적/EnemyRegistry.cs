using UnityEngine;
using System.Collections.Generic;

public static class EnemyRegistry
{
    private const float CellSize = 5f;
    
    private const int MaxNearestSearchRings = 12;

    private static readonly HashSet<EnemyController> alive = new HashSet<EnemyController>();
    
    private static readonly Dictionary<(int, int), List<EnemyController>> grid = new();
    private static readonly List<List<EnemyController>> cellListPool = new();
    private static int gridBuiltFrame = -1;

    public static IReadOnlyCollection<EnemyController> AliveEnemies => alive;
    public static int AliveCount => alive.Count;

    public static void Register(EnemyController enemy)
    {
        if (enemy == null)
            return;

        alive.Add(enemy);
    }

    public static void Unregister(EnemyController enemy)
    {
        if (enemy == null)
            return;

        alive.Remove(enemy);
    }

    public static EnemyController GetNearest(Vector3 position)
    {
        EnsureGridUpToDate();

        (int cx, int cz) = ToCell(position);

        EnemyController best = null;
        float bestDist = float.MaxValue;

        for (int ring = 0; ring <= MaxNearestSearchRings; ring++)
        {
            bool foundAny = ScanRing(cx, cz, ring, position, ref best, ref bestDist);
            
            if (foundAny)
            {
                ScanRing(cx, cz, ring + 1, position, ref best, ref bestDist);
                return best;
            }
        }
        
        return GetNearestBruteForce(position);
    }

    public static Vector3 GetSeparationVector(EnemyController self, Vector3 position, float radius)
    {
        if (self == null || radius <= 0f)
            return Vector3.zero;

        EnsureGridUpToDate();

        Vector3 result = Vector3.zero;
        float radiusSqr = radius * radius;

        int cellRadius = Mathf.CeilToInt(radius / CellSize);
        (int cx, int cz) = ToCell(position);

        for (int dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (int dz = -cellRadius; dz <= cellRadius; dz++)
            {
                if (!grid.TryGetValue((cx + dx, cz + dz), out List<EnemyController> cell))
                    continue;

                for (int i = 0; i < cell.Count; i++)
                {
                    EnemyController other = cell[i];

                    if (other == null || other == self || !other.isActiveAndEnabled)
                        continue;

                    Vector3 diff = position - other.transform.position;
                    diff.y = 0f;

                    float distSqr = diff.sqrMagnitude;

                    if (distSqr <= 0.0001f || distSqr >= radiusSqr)
                        continue;

                    float dist = Mathf.Sqrt(distSqr);
                    float strength = 1f - dist / radius;

                    result += diff / dist * strength;
                }
            }
        }

        return result;
    }

    private static bool ScanRing(
        int cx, int cz, int ring, Vector3 position,
        ref EnemyController best, ref float bestDist)
    {
        bool foundAny = false;

        if (ring == 0)
        {
            foundAny |= ScanCell(cx, cz, position, ref best, ref bestDist);
            return foundAny;
        }

        for (int dx = -ring; dx <= ring; dx++)
        {
            foundAny |= ScanCell(cx + dx, cz - ring, position, ref best, ref bestDist);
            foundAny |= ScanCell(cx + dx, cz + ring, position, ref best, ref bestDist);
        }

        for (int dz = -ring + 1; dz <= ring - 1; dz++)
        {
            foundAny |= ScanCell(cx - ring, cz + dz, position, ref best, ref bestDist);
            foundAny |= ScanCell(cx + ring, cz + dz, position, ref best, ref bestDist);
        }

        return foundAny;
    }

    private static bool ScanCell(
        int cx, int cz, Vector3 position,
        ref EnemyController best, ref float bestDist)
    {
        if (!grid.TryGetValue((cx, cz), out List<EnemyController> cell))
            return false;

        bool foundAny = false;

        for (int i = 0; i < cell.Count; i++)
        {
            EnemyController enemy = cell[i];

            if (enemy == null || !enemy.isActiveAndEnabled)
                continue;

            foundAny = true;

            Vector3 diff = enemy.transform.position - position;
            diff.y = 0f;

            float dist = diff.sqrMagnitude;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = enemy;
            }
        }

        return foundAny;
    }

    private static EnemyController GetNearestBruteForce(Vector3 position)
    {
        EnemyController best = null;
        float bestDist = float.MaxValue;

        foreach (EnemyController enemy in alive)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead)
                continue;

            Vector3 diff = enemy.transform.position - position;
            diff.y = 0f;

            float dist = diff.sqrMagnitude;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = enemy;
            }
        }

        return best;
    }

    private static void EnsureGridUpToDate()
    {
        if (gridBuiltFrame == Time.frameCount)
            return;

        gridBuiltFrame = Time.frameCount;

        foreach (var pair in grid)
        {
            pair.Value.Clear();
            cellListPool.Add(pair.Value);
        }

        grid.Clear();

        foreach (EnemyController enemy in alive)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead)
                continue;

            (int cx, int cz) = ToCell(enemy.transform.position);

            if (!grid.TryGetValue((cx, cz), out List<EnemyController> cell))
            {
                cell = GetPooledList();
                grid[(cx, cz)] = cell;
            }

            cell.Add(enemy);
        }
    }

    private static List<EnemyController> GetPooledList()
    {
        int lastIndex = cellListPool.Count - 1;

        if (lastIndex < 0)
            return new List<EnemyController>();

        List<EnemyController> list = cellListPool[lastIndex];
        cellListPool.RemoveAt(lastIndex);
        return list;
    }

    private static (int, int) ToCell(Vector3 position)
    {
        int cx = Mathf.FloorToInt(position.x / CellSize);
        int cz = Mathf.FloorToInt(position.z / CellSize);
        return (cx, cz);
    }
}
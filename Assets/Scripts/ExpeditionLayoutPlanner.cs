using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public sealed class ExpeditionLayoutPlan
{
    public int RequestedSeed { get; internal set; }
    public int LayoutSeed { get; internal set; }
    public int Attempt { get; internal set; }
    public Vector2 PlayerPosition { get; internal set; }
    public Vector2 ExtractionPosition { get; internal set; }
    public List<Vector2> RoutePoints { get; } = new List<Vector2>();
    public List<Vector2> TreePositions { get; } = new List<Vector2>();
    public List<Vector2> GrovePositions { get; } = new List<Vector2>();
}

public static class ExpeditionLayoutPlanner
{
    public const int DefaultHalfWidth = 46;
    public const int DefaultHalfHeight = 34;
    public const int DefaultTreeCount = 170;
    public const int MaximumGenerationAttempts = 20;

    const float ArenaInset = 3f;
    const float ExtractionMinimumDistance = 42f;
    const float ExtractionGroveRadius = 3.35f;
    const float RouteClearance = 1.65f;
    const float TreeCollisionRadius = .38f;
    const float PlayerCollisionRadius = .34f;
    const float ValidationCellSize = .5f;

    public static bool TryCreateValid(
        int requestedSeed,
        Vector2 playerPosition,
        out ExpeditionLayoutPlan plan,
        int halfWidth = DefaultHalfWidth,
        int halfHeight = DefaultHalfHeight,
        int treeCount = DefaultTreeCount)
    {
        for (int attempt = 0; attempt < MaximumGenerationAttempts; attempt++)
        {
            int layoutSeed = SeedForAttempt(requestedSeed, attempt);
            var candidate = Create(layoutSeed, requestedSeed, attempt, playerPosition, halfWidth, halfHeight, treeCount);
            if (!IsConnected(candidate, halfWidth, halfHeight)) continue;
            plan = candidate;
            return true;
        }

        plan = null;
        return false;
    }

    public static bool IsConnected(ExpeditionLayoutPlan plan, int halfWidth, int halfHeight)
    {
        float minX = -halfWidth + ArenaInset;
        float maxX = halfWidth - ArenaInset;
        float minY = -halfHeight + ArenaInset;
        float maxY = halfHeight - ArenaInset;
        int width = Mathf.FloorToInt((maxX - minX) / ValidationCellSize) + 1;
        int height = Mathf.FloorToInt((maxY - minY) / ValidationCellSize) + 1;
        var blocked = new bool[width, height];

        foreach (Vector2 obstacle in AllObstacles(plan))
            MarkObstacle(blocked, obstacle, minX, minY, width, height);

        Vector2Int start = CellFor(plan.PlayerPosition, minX, minY, width, height);
        Vector2Int goal = CellFor(plan.ExtractionPosition, minX, minY, width, height);
        blocked[start.x, start.y] = false;
        blocked[goal.x, goal.y] = false;

        var visited = new bool[width, height];
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        var directions = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            if (cell == goal) return true;
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = cell + direction;
                if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height) continue;
                if (visited[next.x, next.y] || blocked[next.x, next.y]) continue;
                visited[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }
        return false;
    }

    public static bool IsInsideArena(Vector2 point, int halfWidth, int halfHeight)
    {
        const float boundaryInset = .5f;
        return point.x >= -halfWidth + boundaryInset && point.x <= halfWidth - boundaryInset
            && point.y >= -halfHeight + boundaryInset && point.y <= halfHeight - boundaryInset;
    }

    static ExpeditionLayoutPlan Create(
        int layoutSeed,
        int requestedSeed,
        int attempt,
        Vector2 playerPosition,
        int halfWidth,
        int halfHeight,
        int treeCount)
    {
        var random = new Random(layoutSeed);
        var plan = new ExpeditionLayoutPlan
        {
            RequestedSeed = requestedSeed,
            LayoutSeed = layoutSeed,
            Attempt = attempt,
            PlayerPosition = playerPosition,
            ExtractionPosition = ChooseExtractionPosition(random, playerPosition, halfWidth, halfHeight)
        };

        BuildRoute(plan, random, halfWidth, halfHeight);
        BuildTrees(plan, random, halfWidth, halfHeight, treeCount);
        BuildExtractionGrove(plan, halfWidth, halfHeight);
        return plan;
    }

    static void BuildRoute(ExpeditionLayoutPlan plan, Random random, int halfWidth, int halfHeight)
    {
        Vector2 direct = plan.ExtractionPosition - plan.PlayerPosition;
        Vector2 perpendicular = direct.sqrMagnitude > .01f
            ? new Vector2(-direct.y, direct.x).normalized
            : Vector2.right;
        float offset = Range(random, -5f, 5f);
        Vector2 bend = Vector2.Lerp(plan.PlayerPosition, plan.ExtractionPosition, .52f) + perpendicular * offset;
        bend.x = Mathf.Clamp(bend.x, -halfWidth + ArenaInset + 1f, halfWidth - ArenaInset - 1f);
        bend.y = Mathf.Clamp(bend.y, -halfHeight + ArenaInset + 1f, halfHeight - ArenaInset - 1f);

        plan.RoutePoints.Add(plan.PlayerPosition);
        plan.RoutePoints.Add(bend);
        plan.RoutePoints.Add(plan.ExtractionPosition);
    }

    static void BuildTrees(ExpeditionLayoutPlan plan, Random random, int halfWidth, int halfHeight, int treeCount)
    {
        for (int i = 0; i < treeCount; i++)
        {
            bool found = false;
            Vector2 position = default;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                position = RandomPoint(random, halfWidth, halfHeight, 1.5f);
                if (Vector2.Distance(position, plan.PlayerPosition) < 7f) continue;
                if (Vector2.Distance(position, plan.ExtractionPosition) < 4.45f) continue;
                if (DistanceToRoute(position, plan.RoutePoints) < RouteClearance) continue;
                if (TooClose(position, plan.TreePositions, 1.05f)) continue;
                found = true;
                break;
            }
            if (found) plan.TreePositions.Add(position);
        }
    }

    static void BuildExtractionGrove(ExpeditionLayoutPlan plan, int halfWidth, int halfHeight)
    {
        for (int i = 0; i < 16; i++)
        {
            float angle = i / 16f * Mathf.PI * 2f;
            Vector2 position = plan.ExtractionPosition
                + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ExtractionGroveRadius;
            if (!IsInsideArena(position, halfWidth, halfHeight)) continue;
            // The reserved route cuts a wide entrance through the grove. The opposite
            // opening prevents the destination from becoming a single-entry trap.
            if (DistanceToRoute(position, plan.RoutePoints) < RouteClearance + .25f) continue;
            Vector2 oppositeProbe = plan.ExtractionPosition - (position - plan.ExtractionPosition);
            if (DistanceToRoute(oppositeProbe, plan.RoutePoints) < RouteClearance + .25f) continue;
            plan.GrovePositions.Add(position);
        }
    }

    static Vector2 ChooseExtractionPosition(Random random, Vector2 playerPosition, int halfWidth, int halfHeight)
    {
        float horizontalEdge = halfWidth - ExtractionGroveRadius - 1f;
        float verticalEdge = halfHeight - ExtractionGroveRadius - 1f;
        float minX = -horizontalEdge;
        float maxX = horizontalEdge;
        float minY = -verticalEdge;
        float maxY = verticalEdge;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            Vector2 candidate;
            switch (random.Next(4))
            {
                case 0: candidate = new Vector2(Range(random, minX, maxX), maxY); break;
                case 1: candidate = new Vector2(Range(random, minX, maxX), minY); break;
                case 2: candidate = new Vector2(minX, Range(random, minY, maxY)); break;
                default: candidate = new Vector2(maxX, Range(random, minY, maxY)); break;
            }
            if (Vector2.Distance(candidate, playerPosition) >= ExtractionMinimumDistance) return candidate;
        }
        return new Vector2(maxX, maxY);
    }

    static IEnumerable<Vector2> AllObstacles(ExpeditionLayoutPlan plan)
    {
        foreach (Vector2 tree in plan.TreePositions) yield return tree;
        foreach (Vector2 tree in plan.GrovePositions) yield return tree;
    }

    static void MarkObstacle(bool[,] blocked, Vector2 obstacle, float minX, float minY, int width, int height)
    {
        float radius = TreeCollisionRadius + PlayerCollisionRadius;
        int cells = Mathf.CeilToInt(radius / ValidationCellSize);
        Vector2Int center = CellFor(obstacle, minX, minY, width, height);
        for (int x = Mathf.Max(0, center.x - cells); x <= Mathf.Min(width - 1, center.x + cells); x++)
        for (int y = Mathf.Max(0, center.y - cells); y <= Mathf.Min(height - 1, center.y + cells); y++)
        {
            Vector2 point = new Vector2(minX + x * ValidationCellSize, minY + y * ValidationCellSize);
            if ((point - obstacle).sqrMagnitude <= radius * radius) blocked[x, y] = true;
        }
    }

    static Vector2Int CellFor(Vector2 point, float minX, float minY, int width, int height)
    {
        return new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt((point.x - minX) / ValidationCellSize), 0, width - 1),
            Mathf.Clamp(Mathf.RoundToInt((point.y - minY) / ValidationCellSize), 0, height - 1));
    }

    static float DistanceToRoute(Vector2 point, List<Vector2> route)
    {
        float distance = float.MaxValue;
        for (int i = 1; i < route.Count; i++)
            distance = Mathf.Min(distance, DistanceToSegment(point, route[i - 1], route[i]));
        return distance;
    }

    static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        if (segment.sqrMagnitude < .0001f) return Vector2.Distance(point, start);
        float amount = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
        return Vector2.Distance(point, start + segment * amount);
    }

    static bool TooClose(Vector2 position, List<Vector2> others, float minimumDistance)
    {
        float squaredDistance = minimumDistance * minimumDistance;
        foreach (Vector2 other in others)
            if ((position - other).sqrMagnitude < squaredDistance) return true;
        return false;
    }

    static Vector2 RandomPoint(Random random, int halfWidth, int halfHeight, float inset)
    {
        return new Vector2(
            Range(random, -halfWidth + inset, halfWidth - inset),
            Range(random, -halfHeight + inset, halfHeight - inset));
    }

    static float Range(Random random, float minimum, float maximum)
    {
        return minimum + (float)random.NextDouble() * (maximum - minimum);
    }

    static int SeedForAttempt(int requestedSeed, int attempt)
    {
        unchecked { return requestedSeed + attempt * -1640531527; }
    }
}

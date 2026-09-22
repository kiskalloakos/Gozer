using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using Random = System.Random;

public static class ExpeditionArenaGenerator
{
    const string ExpeditionScene = "ExpeditionField";
    const string GenerationMarker = "Generated Melee Level 2 Arena";
    const int BaseHalfWidth = 32;
    const int BaseHalfHeight = 24;
    const int LevelTwoHalfWidth = ExpeditionLayoutPlanner.DefaultHalfWidth;
    const int LevelTwoHalfHeight = ExpeditionLayoutPlanner.DefaultHalfHeight;
    const int LevelTwoTreeCount = ExpeditionLayoutPlanner.DefaultTreeCount;
    const float VisualGroundMargin = 4f;

    static int halfWidth = BaseHalfWidth;
    static int halfHeight = BaseHalfHeight;
    static Random random;

    public static int CurrentSeed { get; private set; }
    public static int CurrentLayoutAttempt { get; private set; }
    public static int CurrentHalfWidth => halfWidth;
    public static int CurrentHalfHeight => halfHeight;
    public static float MinX => -halfWidth + 3f;
    public static float MaxX => halfWidth - 3f;
    public static float MinY => -halfHeight + 3f;
    public static float MaxY => halfHeight - 3f;

    public static void GenerateForCurrentProgression(Scene scene)
    {
        bool proceduralPreviewRequested = ExpeditionSeedManager.HasPendingSeed;
        if (scene.name != ExpeditionScene ||
            (PlayerProgression.MeleeLevel < 2 && !proceduralPreviewRequested))
        {
            halfWidth = BaseHalfWidth;
            halfHeight = BaseHalfHeight;
            CurrentSeed = 0;
            CurrentLayoutAttempt = 0;
            if (scene.name == ExpeditionScene)
                ExpeditionRunIdentity.Begin(0, ExpeditionSeedSource.FixedField, 0);
            return;
        }

        halfWidth = LevelTwoHalfWidth;
        halfHeight = LevelTwoHalfHeight;
        if (GameObject.Find(GenerationMarker)) return;

        var generationTimer = System.Diagnostics.Stopwatch.StartNew();
        new GameObject(GenerationMarker);
        var player = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        var extraction = Object.FindAnyObjectByType<ExtractionZone>();
        CurrentSeed = ExpeditionSeedManager.BeginRun();
        if (!ExpeditionLayoutPlanner.TryCreateValid(CurrentSeed, out var layout,
                halfWidth, halfHeight, LevelTwoTreeCount))
            throw new InvalidOperationException(
                $"Expedition seed {CurrentSeed} failed after {ExpeditionLayoutPlanner.MaximumGenerationAttempts} attempts.");
        CurrentLayoutAttempt = layout.Attempt;
        ExpeditionRunIdentity.RecordLayout(layout.LayoutSeed, layout.Attempt, ExpeditionRunProgression.ThreatLevel);
        random = new Random(layout.LayoutSeed);
        long layoutMilliseconds = generationTimer.ElapsedMilliseconds;
        Debug.Log($"Expedition layout ready: seed={CurrentSeed}, attempt={CurrentLayoutAttempt}, " +
            $"layoutSeed={layout.LayoutSeed}, spawn={layout.PlayerPosition}, " +
            $"extraction={layout.ExtractionPosition}, trees={layout.TreePositions.Count}, " +
            $"grove={layout.GrovePositions.Count}, " +
            $"boundaryForest={layout.BoundaryForestPositions.Count}.");

        var stageTimer = System.Diagnostics.Stopwatch.StartNew();
        RebuildGround();
        stageTimer.Stop();
        long groundMilliseconds = stageTimer.ElapsedMilliseconds;
        ResizeBoundary();
        if (player)
        {
            player.transform.position = layout.PlayerPosition;
            var body = player.GetComponent<Rigidbody2D>();
            if (body) body.position = layout.PlayerPosition;
        }
        if (extraction) extraction.transform.position = layout.ExtractionPosition;
        stageTimer.Restart();
        RebuildForest(layout, out int reusedTrees, out int createdTrees);
        stageTimer.Stop();
        long forestMilliseconds = stageTimer.ElapsedMilliseconds;
        Physics2D.SyncTransforms();
        RedistributeEnemies(layout.PlayerPosition, layout.ExtractionPosition);
        generationTimer.Stop();
        Debug.Log($"Expedition construction profile: total={generationTimer.ElapsedMilliseconds} ms, " +
            $"layout={layoutMilliseconds} ms, groundBatch={groundMilliseconds} ms, " +
            $"forestPool={forestMilliseconds} ms, treesReused={reusedTrees}, " +
            $"treesCreated={createdTrees}.");
    }

    public static Vector2 FindOpenPosition(Transform player, Transform extraction, Camera outsideCamera = null)
    {
        if (random == null) random = new Random(Guid.NewGuid().GetHashCode());

        for (int attempt = 0; attempt < 100; attempt++)
        {
            Vector2 candidate = outsideCamera
                ? PositionJustOutsideCamera(outsideCamera)
                : RandomPoint(2f);
            if (player && Vector2.Distance(candidate, player.position) < 8f) continue;
            if (extraction && Vector2.Distance(candidate, extraction.position) < 5f) continue;
            if (outsideCamera)
            {
                Vector3 viewport = outsideCamera.WorldToViewportPoint(candidate);
                if (viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f) continue;
            }

            if (IsOpen(candidate, .65f)) return candidate;
        }

        for (float y = MinY; y <= MaxY; y += 1f)
        for (float x = MinX; x <= MaxX; x += 1f)
        {
            var candidate = new Vector2(x, y);
            if (player && Vector2.Distance(candidate, player.position) < 8f) continue;
            if (extraction && Vector2.Distance(candidate, extraction.position) < 5f) continue;
            if (IsOpen(candidate, .65f)) return candidate;
        }
        Debug.LogError("No collision-safe expedition spawn position was available; using the player spawn.");
        return player ? (Vector2)player.position : Vector2.zero;
    }

    static void RebuildGround()
    {
        var tilemap = Object.FindAnyObjectByType<Tilemap>();
        if (!tilemap) return;

        var tileChoices = new List<TileBase>();
        foreach (var position in tilemap.cellBounds.allPositionsWithin)
        {
            var tile = tilemap.GetTile(position);
            if (tile && !tileChoices.Contains(tile)) tileChoices.Add(tile);
        }
        if (tileChoices.Count == 0) return;
        tileChoices.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

        float aspect = Camera.main ? Mathf.Max(.1f, Camera.main.aspect) : 16f / 9f;
        float overviewSize = ExpeditionLoadingSequence.CalculateOverviewSize(
            halfWidth, halfHeight, aspect);
        int visualHalfWidth = Mathf.CeilToInt(
            Mathf.Max(halfWidth, overviewSize * aspect) + VisualGroundMargin);
        int visualHalfHeight = Mathf.CeilToInt(
            Mathf.Max(halfHeight, overviewSize) + VisualGroundMargin);
        int width = visualHalfWidth * 2;
        int height = visualHalfHeight * 2;
        var bounds = new BoundsInt(
            -visualHalfWidth, -visualHalfHeight, 0, width, height, 1);
        var tiles = new TileBase[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            // Keep the dominant grass tile common while allowing every expedition
            // to receive a visibly different ground pattern.
            int roll = random.Next(5);
            int tileIndex = roll < 3 ? 0 : Math.Min(roll - 2, tileChoices.Count - 1);
            tiles[x + y * width] = tileChoices[tileIndex];
        }

        tilemap.ClearAllTiles();
        tilemap.SetTilesBlock(bounds, tiles);
    }

    static void ResizeBoundary()
    {
        ResizeWall("North Boundary", new Vector2(0f, halfHeight + .5f),
            new Vector2(halfWidth * 2f + 2f, 1f));
        ResizeWall("South Boundary", new Vector2(0f, -halfHeight - .5f),
            new Vector2(halfWidth * 2f + 2f, 1f));
        ResizeWall("West Boundary", new Vector2(-halfWidth - .5f, 0f),
            new Vector2(1f, halfHeight * 2f));
        ResizeWall("East Boundary", new Vector2(halfWidth + .5f, 0f),
            new Vector2(1f, halfHeight * 2f));
    }

    static void ResizeWall(string name, Vector2 position, Vector2 size)
    {
        var wall = GameObject.Find(name);
        if (!wall) return;
        wall.transform.position = position;
        var collider = wall.GetComponent<BoxCollider2D>();
        if (collider) collider.size = size;
    }

    static void RebuildForest(ExpeditionLayoutPlan layout, out int reusedCount, out int createdCount)
    {
        reusedCount = 0;
        createdCount = 0;
        var rootObject = GameObject.Find("Trees");
        if (!rootObject || rootObject.transform.childCount == 0) return;

        Transform root = rootObject.transform;
        var pool = new List<GameObject>(root.childCount);
        for (int i = 0; i < root.childCount; i++)
            pool.Add(root.GetChild(i).gameObject);

        GameObject template = pool[0];
        int requiredCount = layout.TreePositions.Count
            + layout.GrovePositions.Count
            + layout.BoundaryForestPositions.Count;
        while (pool.Count < requiredCount)
        {
            pool.Add(Object.Instantiate(template, root));
            createdCount++;
        }

        int poolIndex = 0;
        for (int i = 0; i < layout.TreePositions.Count; i++, poolIndex++)
            ConfigureTree(pool[poolIndex], layout.TreePositions[i], $"Generated Tree {i + 1}");
        for (int i = 0; i < layout.GrovePositions.Count; i++, poolIndex++)
            ConfigureTree(pool[poolIndex], layout.GrovePositions[i], $"Generated Extraction Grove {i + 1}");
        for (int i = 0; i < layout.BoundaryForestPositions.Count; i++, poolIndex++)
            ConfigureTree(pool[poolIndex], layout.BoundaryForestPositions[i],
                $"Generated Boundary Forest {i + 1}");
        reusedCount = requiredCount - createdCount;

        // Keep surplus instances inactive so a later rebuild can reuse the same pool.
        for (int i = requiredCount; i < pool.Count; i++)
        {
            pool[i].name = $"Pooled Tree {i + 1}";
            pool[i].SetActive(false);
        }
    }

    static void ConfigureTree(GameObject tree, Vector2 position, string name)
    {
        tree.name = name;
        tree.transform.SetPositionAndRotation(position, Quaternion.identity);
        var choppable = tree.GetComponent<ChoppableTree>();
        if (!choppable) choppable = tree.AddComponent<ChoppableTree>();
        tree.SetActive(true);
        choppable.ResetForGeneration();
        var renderer = tree.GetComponent<SpriteRenderer>();
        if (renderer) renderer.sortingOrder = Mathf.RoundToInt(-position.y * 100f);
    }

    static void RedistributeEnemies(Vector2 playerPosition, Vector2 extractionPosition)
    {
        var enemies = Object.FindObjectsByType<WildernessEnemy>();
        foreach (var enemy in enemies)
        {
            Vector2 position = FindOpenPosition(null, null);
            for (int attempt = 0; attempt < 50; attempt++)
            {
                position = RandomPoint(2f);
                if (Vector2.Distance(position, playerPosition) < 9f && attempt < 49) continue;
                if (Vector2.Distance(position, extractionPosition) < 6f && attempt < 49) continue;
                if (!IsOpen(position, .65f) && attempt < 49) continue;
                break;
            }
            enemy.Relocate(position);
        }
    }

    static bool IsOpen(Vector2 position, float radius)
    {
        foreach (var collider in Physics2D.OverlapCircleAll(position, radius))
            if (!collider.isTrigger) return false;
        return true;
    }

    static Vector2 PositionJustOutsideCamera(Camera camera)
    {
        float cameraHalfHeight = camera.orthographicSize + 1.2f;
        float cameraHalfWidth = camera.orthographicSize * camera.aspect + 1.2f;
        Vector2 center = camera.transform.position;
        Vector2 candidate = NextBool()
            ? center + new Vector2(NextBool() ? -cameraHalfWidth : cameraHalfWidth,
                Range(-cameraHalfHeight, cameraHalfHeight))
            : center + new Vector2(Range(-cameraHalfWidth, cameraHalfWidth),
                NextBool() ? -cameraHalfHeight : cameraHalfHeight);

        candidate.x = Mathf.Clamp(candidate.x, MinX, MaxX);
        candidate.y = Mathf.Clamp(candidate.y, MinY, MaxY);
        return candidate;
    }

    static Vector2 RandomPoint(float inset)
    {
        return new Vector2(
            Range(-halfWidth + inset, halfWidth - inset),
            Range(-halfHeight + inset, halfHeight - inset));
    }

    static float Range(float minimum, float maximum)
    {
        return minimum + (float)random.NextDouble() * (maximum - minimum);
    }

    static bool NextBool() => random.Next(2) == 0;
}

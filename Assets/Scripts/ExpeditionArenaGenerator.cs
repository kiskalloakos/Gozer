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
    const int LevelTwoHalfWidth = 46;
    const int LevelTwoHalfHeight = 34;
    const int LevelTwoTreeCount = ExpeditionLayoutPlanner.DefaultTreeCount;

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
            return;
        }

        halfWidth = LevelTwoHalfWidth;
        halfHeight = LevelTwoHalfHeight;
        if (GameObject.Find(GenerationMarker)) return;

        new GameObject(GenerationMarker);
        var player = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        var extraction = Object.FindAnyObjectByType<ExtractionZone>();
        Vector2 playerPosition = player ? player.transform.position : new Vector2(0f, -18f);
        CurrentSeed = ExpeditionSeedManager.BeginRun();
        if (!ExpeditionLayoutPlanner.TryCreateValid(CurrentSeed, playerPosition, out var layout,
                halfWidth, halfHeight, LevelTwoTreeCount))
            throw new InvalidOperationException(
                $"Expedition seed {CurrentSeed} failed after {ExpeditionLayoutPlanner.MaximumGenerationAttempts} attempts.");
        CurrentLayoutAttempt = layout.Attempt;
        random = new Random(layout.LayoutSeed);
        Debug.Log($"Expedition layout ready: seed={CurrentSeed}, attempt={CurrentLayoutAttempt}, " +
            $"layoutSeed={layout.LayoutSeed}, trees={layout.TreePositions.Count}, grove={layout.GrovePositions.Count}.");

        RebuildGround();
        ResizeBoundary();
        if (extraction) extraction.transform.position = layout.ExtractionPosition;
        RebuildForest(layout);
        Physics2D.SyncTransforms();
        RedistributeEnemies(playerPosition, layout.ExtractionPosition);
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

        tilemap.ClearAllTiles();
        for (int x = -halfWidth; x < halfWidth; x++)
        for (int y = -halfHeight; y < halfHeight; y++)
        {
            // Keep the dominant grass tile common while allowing every expedition
            // to receive a visibly different ground pattern.
            int roll = random.Next(5);
            int tileIndex = roll < 3 ? 0 : Math.Min(roll - 2, tileChoices.Count - 1);
            tilemap.SetTile(new Vector3Int(x, y, 0), tileChoices[tileIndex]);
        }
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

    static void RebuildForest(ExpeditionLayoutPlan layout)
    {
        var rootObject = GameObject.Find("Trees");
        if (!rootObject || rootObject.transform.childCount == 0) return;

        Transform root = rootObject.transform;
        var template = root.GetChild(0).gameObject;
        for (int i = 0; i < layout.TreePositions.Count; i++)
            CreateTreeClone(template, root, layout.TreePositions[i], $"Generated Tree {i + 1}");
        for (int i = 0; i < layout.GrovePositions.Count; i++)
            CreateTreeClone(template, root, layout.GrovePositions[i], $"Generated Extraction Grove {i + 1}");

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
            if (child == template || child.name.StartsWith("Generated ")) continue;
            child.SetActive(false);
            Object.Destroy(child);
        }
        template.SetActive(false);
        Object.Destroy(template);
    }

    static void CreateTreeClone(GameObject template, Transform parent, Vector2 position, string name)
    {
        var tree = Object.Instantiate(template, position, Quaternion.identity, parent);
        tree.name = name;
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

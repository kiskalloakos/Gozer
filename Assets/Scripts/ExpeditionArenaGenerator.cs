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
    const int LevelTwoTreeCount = 170;

    static int halfWidth = BaseHalfWidth;
    static int halfHeight = BaseHalfHeight;
    static Random random;

    public static int CurrentSeed { get; private set; }
    public static float MinX => -halfWidth + 3f;
    public static float MaxX => halfWidth - 3f;
    public static float MinY => -halfHeight + 3f;
    public static float MaxY => halfHeight - 3f;

    public static void GenerateForCurrentProgression(Scene scene)
    {
        if (scene.name != ExpeditionScene || PlayerProgression.MeleeLevel < 2)
        {
            halfWidth = BaseHalfWidth;
            halfHeight = BaseHalfHeight;
            return;
        }

        halfWidth = LevelTwoHalfWidth;
        halfHeight = LevelTwoHalfHeight;
        if (GameObject.Find(GenerationMarker)) return;

        new GameObject(GenerationMarker);
        CurrentSeed = Guid.NewGuid().GetHashCode();
        random = new Random(CurrentSeed);

        var player = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        var extraction = Object.FindAnyObjectByType<ExtractionZone>();
        Vector2 playerPosition = player ? player.transform.position : new Vector2(0f, -18f);
        Vector2 extractionPosition = ChooseExtractionPosition(playerPosition);

        RebuildGround();
        ResizeBoundary();
        if (extraction) extraction.transform.position = extractionPosition;
        RebuildForest(playerPosition, extractionPosition);
        Physics2D.SyncTransforms();
        RedistributeEnemies(playerPosition, extractionPosition);
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

        return new Vector2(NextBool() ? MinX : MaxX, Range(MinY, MaxY));
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

    static Vector2 ChooseExtractionPosition(Vector2 playerPosition)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector2 candidate;
            switch (random.Next(4))
            {
                case 0: candidate = new Vector2(Range(MinX, MaxX), MaxY - Range(0f, 5f)); break;
                case 1: candidate = new Vector2(Range(MinX, MaxX), MinY + Range(0f, 5f)); break;
                case 2: candidate = new Vector2(MinX + Range(0f, 5f), Range(MinY, MaxY)); break;
                default: candidate = new Vector2(MaxX - Range(0f, 5f), Range(MinY, MaxY)); break;
            }

            if (Vector2.Distance(candidate, playerPosition) >= 42f) return candidate;
        }

        return new Vector2(MaxX - 2f, MaxY - 2f);
    }

    static void RebuildForest(Vector2 playerPosition, Vector2 extractionPosition)
    {
        var rootObject = GameObject.Find("Trees");
        if (!rootObject || rootObject.transform.childCount == 0) return;

        Transform root = rootObject.transform;
        var template = root.GetChild(0).gameObject;
        var occupied = new List<Vector2>();

        for (int i = 0; i < LevelTwoTreeCount; i++)
        {
            Vector2 position = default;
            bool found = false;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                position = RandomPoint(1.5f);
                if (Vector2.Distance(position, playerPosition) < 7f) continue;
                if (Vector2.Distance(position, extractionPosition) < 5.5f) continue;
                if (TooClose(position, occupied, 1.05f)) continue;
                found = true;
                break;
            }
            if (!found) continue;

            occupied.Add(position);
            CreateTreeClone(template, root, position, $"Generated Tree {i + 1}");
        }

        // A loose grove obscures the destination from a distance, but the large
        // protected clearing and two skipped angles always leave entrances.
        int groveIndex = 1;
        for (int i = 0; i < 14; i++)
        {
            if (i == 3 || i == 10) continue;
            float angle = i / 14f * Mathf.PI * 2f;
            Vector2 position = extractionPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5.25f;
            CreateTreeClone(template, root, position, $"Generated Extraction Grove {groveIndex++}");
        }

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

    static bool TooClose(Vector2 position, List<Vector2> others, float minimumDistance)
    {
        float squaredDistance = minimumDistance * minimumDistance;
        foreach (var other in others)
            if ((position - other).sqrMagnitude < squaredDistance) return true;
        return false;
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

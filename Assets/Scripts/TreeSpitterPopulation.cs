using System.Collections.Generic;
using UnityEngine;

public static class TreeSpitterPopulation
{
    const string SpawnMarkerName = "Tree Spitters Spawned";
    const string PrefabResourcePath = "Enemies/Enemy2/TreeSpitter";
    const float PlayerClearance = 9f;
    const float ExtractionClearance = 6f;

    public static void SpawnForExpedition(Transform player, Transform extraction)
    {
        if (GameObject.Find(SpawnMarkerName)) return;
        var prefab = Resources.Load<GameObject>(PrefabResourcePath);
        var treeComponents = Object.FindObjectsByType<ChoppableTree>();
        if (!prefab || treeComponents.Length == 0) return;

        new GameObject(SpawnMarkerName);
        var trees = new List<Transform>(treeComponents.Length);
        foreach (var tree in treeComponents) trees.Add(tree.transform);
        Shuffle(trees);

        int desiredCount = Mathf.Clamp(Mathf.CeilToInt(trees.Count / 34f), 3, 10);
        int spawned = 0;
        var chosenPositions = new List<Vector2>(desiredCount);
        foreach (Transform tree in trees)
        {
            if (!tree || spawned >= desiredCount) break;
            Vector2 position = tree.position;
            if (!TryFindNearTree(tree, player, extraction, chosenPositions, out position)) continue;

            var sentry = Object.Instantiate(prefab, position, Quaternion.identity);
            sentry.name = $"Tree Spitter {spawned + 1}";
            chosenPositions.Add(position);
            spawned++;
        }
    }

    static bool TryFindNearTree(Transform tree, Transform player, Transform extraction,
        List<Vector2> chosen, out Vector2 position)
    {
        // The tiny tree art has a wider trunk collider than its visible pixels;
        // place the sentry just outside the trunk on one of eight sides.
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float angle = (attempt + Random.value) * Mathf.PI * .25f;
            Vector2 candidate = (Vector2)tree.position
                + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(1.05f, 1.65f);
            if (player && Vector2.Distance(candidate, player.position) < PlayerClearance) continue;
            if (extraction && Vector2.Distance(candidate, extraction.position) < ExtractionClearance) continue;
            if (ExpeditionArenaGenerator.IsProceduralField
                && !ExpeditionLayoutPlanner.IsInsideArena(candidate,
                    ExpeditionArenaGenerator.CurrentHalfWidth, ExpeditionArenaGenerator.CurrentHalfHeight)) continue;
            if (Physics2D.OverlapCircle(candidate, .32f)) continue;

            bool tooClose = false;
            foreach (Vector2 existing in chosen)
                if (Vector2.Distance(candidate, existing) < 5f) { tooClose = true; break; }
            if (tooClose) continue;

            position = candidate;
            return true;
        }

        position = default;
        return false;
    }

    static void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }
}

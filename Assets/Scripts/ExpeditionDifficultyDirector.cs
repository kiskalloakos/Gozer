using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExpeditionDifficultyDirector
{
    const string ExpeditionScene = "ExpeditionField";
    const string ReinforcementPrefix = "Melee Level 2 Reinforcement";
    const string ExtractionReinforcementPrefix = "Extraction Reinforcement";
    const string ExtractionMarker = "Extraction Reinforcements Spawned";
    const int LevelTwoExtraEnemies = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AddLevelTwoEnemies(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddLevelTwoEnemies(scene);
    }

    static void AddLevelTwoEnemies(Scene scene)
    {
        if (scene.name != ExpeditionScene || PlayerProgression.MeleeLevel < 2) return;

        var enemies = Object.FindObjectsByType<WildernessEnemy>(FindObjectsSortMode.None);
        if (enemies.Length == 0) return;
        foreach (var enemy in enemies)
            if (enemy.name.StartsWith(ReinforcementPrefix)) return;

        var template = enemies[0];
        var player = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        var extraction = Object.FindAnyObjectByType<ExtractionZone>();

        for (int i = 0; i < LevelTwoExtraEnemies; i++)
        {
            Vector2 position = FindOpenPosition(player ? player.transform : null,
                extraction ? extraction.transform : null);
            var reinforcement = Object.Instantiate(
                template.gameObject,
                position,
                Quaternion.identity,
                template.transform.parent);
            reinforcement.name = $"{ReinforcementPrefix} {i + 1}";
        }
    }

    public static void SpawnExtractionReinforcements()
    {
        if (SceneManager.GetActiveScene().name != ExpeditionScene) return;
        if (GameObject.Find(ExtractionMarker)) return;

        new GameObject(ExtractionMarker);
        var enemies = Object.FindObjectsByType<WildernessEnemy>(FindObjectsSortMode.None);
        if (enemies.Length == 0) return;

        int count = PlayerProgression.MeleeLevel >= 2 ? 2 : 1;
        var template = enemies[0];
        var player = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        var extraction = Object.FindAnyObjectByType<ExtractionZone>();
        var camera = Camera.main;

        for (int i = 0; i < count; i++)
        {
            Vector2 position = FindOpenPosition(player ? player.transform : null,
                extraction ? extraction.transform : null, camera);
            var reinforcement = Object.Instantiate(
                template.gameObject,
                position,
                Quaternion.identity,
                template.transform.parent);
            reinforcement.name = $"{ExtractionReinforcementPrefix} {i + 1}";
            reinforcement.GetComponent<WildernessEnemy>().AlertFromExtraction();
        }
    }

    static Vector2 FindOpenPosition(Transform player, Transform extraction, Camera outsideCamera = null)
    {
        for (int attempt = 0; attempt < 80; attempt++)
        {
            Vector2 candidate = outsideCamera
                ? PositionJustOutsideCamera(outsideCamera)
                : new Vector2(Random.Range(-28f, 28f), Random.Range(-16f, 18f));
            if (player && Vector2.Distance(candidate, player.position) < 8f) continue;
            if (extraction && Vector2.Distance(candidate, extraction.position) < 5f) continue;
            if (outsideCamera)
            {
                Vector3 viewport = outsideCamera.WorldToViewportPoint(candidate);
                if (viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f) continue;
            }

            bool blocked = false;
            foreach (var collider in Physics2D.OverlapCircleAll(candidate, .65f))
            {
                if (!collider.isTrigger)
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked) return candidate;
        }

        // Safe edge fallback if the randomly sampled forest is unusually crowded.
        return new Vector2(Random.value < .5f ? -27f : 27f, Random.Range(-12f, 15f));
    }

    static Vector2 PositionJustOutsideCamera(Camera camera)
    {
        float halfHeight = camera.orthographicSize + 1.2f;
        float halfWidth = camera.orthographicSize * camera.aspect + 1.2f;
        Vector2 center = camera.transform.position;

        Vector2 candidate;
        if (Random.value < .5f)
            candidate = center + new Vector2(Random.value < .5f ? -halfWidth : halfWidth,
                Random.Range(-halfHeight, halfHeight));
        else
            candidate = center + new Vector2(Random.Range(-halfWidth, halfWidth),
                Random.value < .5f ? -halfHeight : halfHeight);

        candidate.x = Mathf.Clamp(candidate.x, -29f, 29f);
        candidate.y = Mathf.Clamp(candidate.y, -17f, 19f);
        return candidate;
    }
}

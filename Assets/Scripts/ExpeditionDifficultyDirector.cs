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
        ExpeditionArenaGenerator.GenerateForCurrentProgression(scene);
        if (scene.name != ExpeditionScene || PlayerProgression.MeleeLevel < 2) return;

        var enemies = Object.FindObjectsByType<WildernessEnemy>();
        if (enemies.Length == 0) return;
        foreach (var enemy in enemies)
            if (enemy.name.StartsWith(ReinforcementPrefix)) return;

        var template = enemies[0];
        var player = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        var extraction = Object.FindAnyObjectByType<ExtractionZone>();

        for (int i = 0; i < LevelTwoExtraEnemies; i++)
        {
            Vector2 position = ExpeditionArenaGenerator.FindOpenPosition(player ? player.transform : null,
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
        var enemies = Object.FindObjectsByType<WildernessEnemy>();
        if (enemies.Length == 0) return;

        int count = PlayerProgression.MeleeLevel >= 2 ? 2 : 1;
        var template = enemies[0];
        var player = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        var extraction = Object.FindAnyObjectByType<ExtractionZone>();
        var camera = Camera.main;

        for (int i = 0; i < count; i++)
        {
            Vector2 position = ExpeditionArenaGenerator.FindOpenPosition(player ? player.transform : null,
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
}

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
        if (scene.name != ExpeditionScene) return;

        var loadingPlayer = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        ExpeditionLoadingSequence.Begin(
            loadingPlayer ? loadingPlayer.transform : null,
            ExpeditionArenaGenerator.CurrentHalfWidth,
            ExpeditionArenaGenerator.CurrentHalfHeight,
            ExpeditionArenaGenerator.CurrentSeed);

        if (PlayerProgression.MeleeLevel < 2) return;

        int completedRuns = ExpeditionRunProgression.CompletedRuns;
        ExpeditionFieldOfView.Install(
            loadingPlayer ? loadingPlayer.transform : null,
            completedRuns,
            true);

        var enemies = Object.FindObjectsByType<WildernessEnemy>();
        if (enemies.Length == 0) return;
        foreach (var enemy in enemies)
            if (enemy.name.StartsWith(ReinforcementPrefix)) return;

        var template = enemies[0];
        var player = Object.FindAnyObjectByType<ExpeditionPlayerHealth>();
        var extraction = Object.FindAnyObjectByType<ExtractionZone>();

        int extraEnemyCount = LevelTwoExtraEnemies + ExpeditionRunProgression.RegularEnemyBonus;
        for (int i = 0; i < extraEnemyCount; i++)
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

        foreach (var enemy in Object.FindObjectsByType<WildernessEnemy>())
            enemy.ApplyRunDifficulty(completedRuns);
    }

    public static void SpawnExtractionReinforcements()
    {
        if (SceneManager.GetActiveScene().name != ExpeditionScene) return;
        if (GameObject.Find(ExtractionMarker)) return;

        new GameObject(ExtractionMarker);
        var enemies = Object.FindObjectsByType<WildernessEnemy>();
        if (enemies.Length == 0) return;

        int count = PlayerProgression.MeleeLevel >= 2
            ? 2 + ExpeditionRunProgression.ExtractionReinforcementBonus
            : 1;
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

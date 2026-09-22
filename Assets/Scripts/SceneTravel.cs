using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTravel
{
    private static SceneSpawnPoint pendingSpawn;

    public static void Load(GameScene destinationScene, SceneSpawnPoint spawnPoint = SceneSpawnPoint.None)
    {
        if (VillageTime.Instance)
        {
            if (VillageTime.Instance.IsSleeping) return;
            if (destinationScene == GameScene.ExpeditionField && !VillageTime.Instance.CanEnterExpedition()) return;
            VillageTime.Instance.Save();
        }
        pendingSpawn = spawnPoint;
        SceneManager.sceneLoaded -= ApplyPendingSpawn;
        SceneManager.sceneLoaded += ApplyPendingSpawn;
        SceneManager.LoadScene(GameSceneCatalog.Name(destinationScene));
    }

    public static void LoadExpeditionWithSeed(int seed)
    {
        ExpeditionSeedManager.QueueSeed(seed);
        Load(GameScene.ExpeditionField);
    }

    public static bool ReplayLastExpedition()
    {
        if (!ExpeditionSeedManager.QueueLastSeed()) return false;
        Load(GameScene.ExpeditionField);
        return true;
    }

    private static void ApplyPendingSpawn(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ApplyPendingSpawn;
        SceneSpawnPoint spawnPoint = pendingSpawn;
        pendingSpawn = SceneSpawnPoint.None;
        if (spawnPoint == SceneSpawnPoint.None) return;

        SceneReferences references = null;
        foreach (var candidate in Object.FindObjectsByType<SceneReferences>(FindObjectsSortMode.None))
            if (candidate.gameObject.scene == scene) { references = candidate; break; }
        string validationError = "SceneReferences is missing.";
        if (!references || !references.Validate(out validationError))
        {
            Debug.LogError($"Scene travel could not apply {spawnPoint} in {scene.name}: "
                + (references ? validationError : "SceneReferences is missing."));
            return;
        }
        if (!references.TryGetSpawn(spawnPoint, out Vector2 destination))
        {
            Debug.LogError($"SceneReferences has no configured {spawnPoint} spawn in {scene.name}.");
            return;
        }

        Transform player = references.Player;
        player.position = new Vector3(destination.x, destination.y, player.position.z);
        var body = player.GetComponent<Rigidbody2D>();
        if (body) body.position = destination;
        Physics2D.SyncTransforms();
    }
}

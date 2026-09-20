using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTravel
{
    public const string PlayerHomeFrontDoor = "player_home_front_door";
    public const string TownExpeditionGate = "town_expedition_gate";

    private static string pendingSpawnId;

    public static void Load(string destinationScene, string spawnId = "")
    {
        pendingSpawnId = spawnId;
        SceneManager.sceneLoaded -= ApplyPendingSpawn;
        SceneManager.sceneLoaded += ApplyPendingSpawn;
        SceneManager.LoadScene(destinationScene);
    }

    private static void ApplyPendingSpawn(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ApplyPendingSpawn;
        var spawnId = pendingSpawnId;
        pendingSpawnId = "";

        if (string.IsNullOrEmpty(spawnId)) return;

        var player = GameObject.Find("Player");
        if (!player)
        {
            Debug.LogWarning($"Could not apply scene spawn '{spawnId}': Player was not found in {scene.name}.");
            return;
        }

        if (spawnId == PlayerHomeFrontDoor)
        {
            PlaceAtPlayerHomeDoor(player);
            return;
        }

        if (spawnId == TownExpeditionGate)
        {
            PlaceAtExpeditionGate(player);
            return;
        }

        Debug.LogWarning($"Unknown scene spawn id '{spawnId}' in {scene.name}.");
    }

    private static void PlaceAtPlayerHomeDoor(GameObject player)
    {
        var home = GameObject.Find("PLAYER HOME");
        if (!home)
        {
            Debug.LogWarning("Could not place Player at the home door: PLAYER HOME was not found.");
            return;
        }

        var footprint = home.GetComponent<Collider2D>();
        float frontEdge = footprint ? footprint.bounds.min.y : home.transform.position.y;
        var destination = new Vector2(home.transform.position.x, frontEdge - .55f);

        player.transform.position = new Vector3(destination.x, destination.y, player.transform.position.z);
        var body = player.GetComponent<Rigidbody2D>();
        if (body) body.position = destination;
        Physics2D.SyncTransforms();
    }

    private static void PlaceAtExpeditionGate(GameObject player)
    {
        var gate = GameObject.Find("Town Exit Wall");
        if (!gate) gate = GameObject.Find("Expedition gate");
        if (!gate)
        {
            Debug.LogWarning("Could not place Player at the expedition gate: no town gate was found.");
            return;
        }

        var destination = new Vector2(gate.transform.position.x, gate.transform.position.y + 1.2f);
        player.transform.position = new Vector3(destination.x, destination.y, player.transform.position.z);
        var body = player.GetComponent<Rigidbody2D>();
        if (body) body.position = destination;
        Physics2D.SyncTransforms();
    }
}

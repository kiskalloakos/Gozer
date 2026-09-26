using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneSpawnPoint
{
    None,
    PlayerHomeFrontDoor,
    InfirmaryFrontDoor,
    TownExpeditionGate
}

public enum GameScene { TownHub, HomeInterior, ExpeditionField, InfirmaryInterior }

public static class GameSceneCatalog
{
    public static string Name(GameScene scene) => scene.ToString();
}

/// <summary>
/// The scene-owned registry for objects used by cross-scene gameplay.
/// Populate this component in the scene, not by naming objects to match code.
/// </summary>
public sealed class SceneReferences : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform playerHome;
    [SerializeField] Transform infirmary;
    [SerializeField] Transform townExpeditionGate;

    public Transform Player => player;

    void Awake()
    {
        if (!Validate(out string problem))
            Debug.LogError($"SceneReferences on '{gameObject.scene.name}' is invalid: {problem}", this);
    }

    public bool TryGetSpawn(SceneSpawnPoint spawnPoint, out Vector2 position)
    {
        position = default;
        switch (spawnPoint)
        {
            case SceneSpawnPoint.None:
                return true;
            case SceneSpawnPoint.PlayerHomeFrontDoor:
                if (!playerHome) return false;
                var homeDoor = playerHome.GetComponent<TownInteractable>();
                var homeBounds = homeDoor && homeDoor.interactionCollider
                    ? homeDoor.interactionCollider.bounds : new Bounds(playerHome.position, Vector3.zero);
                position = new Vector2(homeBounds.center.x, homeBounds.min.y - .55f);
                return true;
            case SceneSpawnPoint.InfirmaryFrontDoor:
                if (!infirmary) return false;
                var infirmaryDoor = infirmary.GetComponent<TownInteractable>();
                var doorBounds = infirmaryDoor && infirmaryDoor.interactionCollider
                    ? infirmaryDoor.interactionCollider.bounds : new Bounds(infirmary.position, Vector3.zero);
                position = new Vector2(doorBounds.center.x, doorBounds.min.y - .55f);
                return true;
            case SceneSpawnPoint.TownExpeditionGate:
                if (!townExpeditionGate) return false;
                position = (Vector2)townExpeditionGate.position + Vector2.up * 1.2f;
                return true;
            default:
                return false;
        }
    }

    public bool Validate(out string problem)
    {
        if (!player) { problem = "Player is not assigned."; return false; }
        if (SceneManager.GetActiveScene().name == "TownHub" && (!playerHome || !infirmary || !townExpeditionGate))
        {
            problem = "TownHub requires Player Home, Infirmary, and Expedition Gate references.";
            return false;
        }
        problem = "";
        return true;
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TownHubSetup
{
    const string ScenePath = "Assets/Scenes/TownHub.unity";
    static Sprite square;

    [MenuItem("RPG/Build Town Hub")]
    public static void Build()
    {
        // Artists arrange the top-level town objects directly in the scene.
        // Preserve those authored positions when rebuilding instead of snapping
        // everything back to this script's original prototype coordinates.
        var preservedPositions = CaptureCurrentTownPositions();

        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Square.png");
        if (!square) { Debug.LogError("Missing Square.png placeholder sprite."); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Town Hub").AddComponent<TownHubController>();

        var environment = new GameObject("Environment").transform;
        Block("Grass", Vector2.zero, new Vector2(32, 22), new Color(.20f, .36f, .22f), -10000, environment);
        Block("Main road", new Vector2(0, -1), new Vector2(5, 20), new Color(.48f, .37f, .25f), -9990, environment);
        Block("Cross road", new Vector2(0, 0), new Vector2(29, 3.5f), new Color(.48f, .37f, .25f), -9989, environment);
        var expeditionGate = Block("Expedition gate", Position(preservedPositions, "Expedition gate", new Vector2(0, -10.15f)), new Vector2(6, .35f), new Color(.92f, .55f, .18f), -4000, environment);
        var townPortal = expeditionGate.AddComponent<ScenePortal>();
        townPortal.destinationScene = "RogueTestRoom";
        townPortal.prompt = "[E]  Enter the expedition test room";

        CreateFacility("PLAYER HOME", Position(preservedPositions, "PLAYER HOME", new Vector2(-9.5f, 6.7f)), new Vector2(5.5f, 4.3f),
            new Color(.69f, .38f, .23f), TownInteractable.FacilityType.Home);
        CreateFacility("STORAGE", Position(preservedPositions, "STORAGE", new Vector2(0, 7.1f)), new Vector2(4.4f, 3.5f),
            new Color(.43f, .31f, .22f), TownInteractable.FacilityType.Storage);
        CreateWorkbench(Position(preservedPositions, "WORKBENCH", new Vector2(-4.3f, -3.4f)));

        CreateUpgradeBuilding("WATCHTOWER", "watchtower", Position(preservedPositions, "WATCHTOWER", new Vector2(9.7f, 6.3f)), new Vector2(4.3f, 4.8f),
            new Color(.38f, .30f, .22f), new[] { "Marks the safe boundary.", "Reveals danger sooner.", "Opens a safer expedition route." });
        CreateUpgradeBuilding("INFIRMARY", "infirmary", Position(preservedPositions, "INFIRMARY", new Vector2(10.2f, -.8f)), new Vector2(5.3f, 3.8f),
            new Color(.66f, .61f, .49f), new[] { "Basic first aid is available.", "Injuries recover faster.", "One serious injury can be prevented." });
        CreateUpgradeBuilding("GREENHOUSE", "greenhouse", Position(preservedPositions, "GREENHOUSE", new Vector2(-10.2f, -5.1f)), new Vector2(5.5f, 4.1f),
            new Color(.25f, .57f, .39f), new[] { "A small crop grows inside.", "Produces expedition food.", "Resists contamination events." });

        var player = new GameObject("Player");
        // Start beside the workbench so the interaction loop is visible immediately.
        player.transform.position = Position(preservedPositions, "Player", new Vector2(-2.7f, -3.4f));
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0; body.freezeRotation = true; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var collider = player.AddComponent<CapsuleCollider2D>(); collider.size = new Vector2(.55f, .75f); collider.offset = new Vector2(0, .2f);
        var visualObject = new GameObject("Visual"); visualObject.transform.SetParent(player.transform, false);
        var renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Rogue_Standing_Simplified_128.png_0001.png");
        renderer.sortingOrder = 400;
        var controller = player.AddComponent<TownPlayerController>(); controller.visual = renderer;
        player.AddComponent<TownPlayerInteractor>();

        var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = PixelArtStandard.OrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f, .12f, .09f);
        cameraObject.transform.position = new Vector3(0, -1, -10);
        cameraObject.AddComponent<AudioListener>();
        var follow = cameraObject.AddComponent<FollowCamera>();
        follow.target = player.transform;
        follow.assetsPixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        follow.referenceResolutionY = PixelArtStandard.ReferenceHeight;
        follow.snapToPixelGrid = false;

        if (AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/town_home.png"))
            TownArtSetup.ApplyToOpenTownScene();
        if (AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/TownCharacterSheet.png").OfType<Sprite>().Any())
            TownCharacterSetup.ApplyToOpenTownScene();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/RogueTestRoom.unity", true)
        };
        Selection.activeGameObject = player;
        if (SceneView.lastActiveSceneView)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.FrameSelected();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("RPG_TOWN_HUB_SUCCESS");
    }

    static Dictionary<string, Vector3> CaptureCurrentTownPositions()
    {
        var positions = new Dictionary<string, Vector3>();
        if (SceneManager.GetActiveScene().path != ScenePath) return positions;

        foreach (string objectName in new[]
        {
            "Expedition gate", "PLAYER HOME", "STORAGE", "WORKBENCH",
            "WATCHTOWER", "INFIRMARY", "GREENHOUSE", "Player"
        })
        {
            var townObject = GameObject.Find(objectName);
            if (townObject) positions[objectName] = townObject.transform.position;
        }
        return positions;
    }

    static Vector3 Position(Dictionary<string, Vector3> positions, string objectName, Vector2 fallback)
        => positions.TryGetValue(objectName, out var position)
            ? position
            : new Vector3(fallback.x, fallback.y, 0);

    static void CreateFacility(string name, Vector2 position, Vector2 size, Color color, TownInteractable.FacilityType type)
    {
        var root = new GameObject(name); root.transform.position = position;
        Block("Building", Vector2.zero, size, color, 0, root.transform, true);
        Block("Roof", new Vector2(0, size.y * .36f), new Vector2(size.x + .45f, size.y * .42f), color * .72f, 1, root.transform);
        Block("Door", new Vector2(0, -size.y * .39f), new Vector2(1.15f, size.y * .42f), new Color(.25f, .15f, .10f), 2, root.transform);
        var interactable = root.AddComponent<TownInteractable>(); interactable.facility = type; interactable.displayName = name;
    }

    static void CreateWorkbench(Vector2 position)
    {
        var root = new GameObject("WORKBENCH"); root.transform.position = position;
        Block("Bench top", new Vector2(0, .25f), new Vector2(2.8f, .55f), new Color(.53f, .30f, .16f), 10, root.transform, true);
        Block("Left leg", new Vector2(-1, -.35f), new Vector2(.35f, .8f), new Color(.29f, .16f, .10f), 9, root.transform);
        Block("Right leg", new Vector2(1, -.35f), new Vector2(.35f, .8f), new Color(.29f, .16f, .10f), 9, root.transform);
        Block("Tool glow", new Vector2(.6f, .72f), new Vector2(.65f, .22f), new Color(1f, .66f, .20f), 11, root.transform);
        var interactable = root.AddComponent<TownInteractable>(); interactable.facility = TownInteractable.FacilityType.Workbench; interactable.displayName = "WORKBENCH";
    }

    static void CreateUpgradeBuilding(string name, string id, Vector2 position, Vector2 size, Color color, string[] benefits)
    {
        var root = new GameObject(name); root.transform.position = position;
        var level1 = new GameObject("Tier 1 - Structure"); level1.transform.SetParent(root.transform, false);
        Block("Walls", Vector2.zero, size, color, 0, level1.transform, true);
        Block("Door", new Vector2(0, -size.y * .37f), new Vector2(1.05f, size.y * .45f), new Color(.22f, .14f, .09f), 2, level1.transform);

        var level2 = new GameObject("Tier 2 - Roof and chimney"); level2.transform.SetParent(root.transform, false);
        Block("Improved roof", new Vector2(0, size.y * .37f), new Vector2(size.x + .55f, size.y * .34f), color * .68f, 3, level2.transform);
        Block("Chimney", new Vector2(size.x * .28f, size.y * .68f), new Vector2(.55f, 1.15f), new Color(.34f, .25f, .22f), 2, level2.transform);

        var level3 = new GameObject("Tier 3 - Town banner"); level3.transform.SetParent(root.transform, false);
        Block("Banner pole", new Vector2(-size.x * .35f, size.y * .72f), new Vector2(.16f, 2.1f), new Color(.22f, .16f, .10f), 4, level3.transform);
        Block("Banner", new Vector2(-size.x * .17f, size.y * .95f), new Vector2(.75f, .62f), new Color(.96f, .49f, .18f), 5, level3.transform);

        var building = root.AddComponent<TownUpgradeBuilding>();
        building.displayName = name; building.buildingId = id; building.levelBenefits = benefits;
        building.tierVisuals = new[] { level1, level2, level3 };
    }

    static GameObject Block(string name, Vector2 localPosition, Vector2 size, Color color, int order, Transform parent, bool collider = false)
    {
        var block = new GameObject(name); block.transform.SetParent(parent, false); block.transform.localPosition = localPosition; block.transform.localScale = size;
        var renderer = block.AddComponent<SpriteRenderer>(); renderer.sprite = square; renderer.color = color; renderer.sortingOrder = order;
        if (collider) block.AddComponent<BoxCollider2D>();
        return block;
    }

    static void Fence(string name, Vector2 position, Vector2 size, Transform parent)
    {
        var fence = Block(name, position, size, new Color(.28f, .18f, .11f), -2000, parent, true);
        fence.transform.position = position;
    }
}

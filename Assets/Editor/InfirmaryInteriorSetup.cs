using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InfirmaryInteriorSetup
{
    const string TownPath = "Assets/Scenes/TownHub.unity";
    const string HomePath = "Assets/Scenes/HomeInterior.unity";
    const string InfirmaryPath = "Assets/Scenes/InfirmaryInterior.unity";
    const string HealingArtPath = "Assets/Art/Environment/infirmary_healing.png";

    [MenuItem("RPG/Create or Maintain Infirmary Interior")]
    public static void Build()
    {
        var importer = AssetImporter.GetAtPath(HealingArtPath) as TextureImporter;
        if (!importer) { AssetDatabase.ImportAsset(HealingArtPath); importer = AssetImporter.GetAtPath(HealingArtPath) as TextureImporter; }
        if (!importer) throw new System.Exception("Missing infirmary healing art.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(InfirmaryPath))
            if (!AssetDatabase.CopyAsset(HomePath, InfirmaryPath))
                throw new System.Exception("Could not copy HomeInterior to InfirmaryInterior.");

        var scene = EditorSceneManager.OpenScene(InfirmaryPath, OpenSceneMode.Single);
        var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var name in new[] { "WORKBENCH", "STORAGE CHEST" })
        {
            var target = objects.FirstOrDefault(item => item && item.name == name);
            if (target) Object.DestroyImmediate(target.gameObject);
        }

        var roomRoot = objects.FirstOrDefault(item => item && item.name == "Home Interior");
        if (roomRoot) roomRoot.name = "Infirmary Interior";
        var exit = objects.FirstOrDefault(item => item && item.name == "Front Door");
        if (!exit) throw new System.Exception("Infirmary has no front door.");
        var portal = exit.GetComponent<ScenePortal>();
        portal.destinationScene = GameScene.TownHub;
        portal.destinationSpawn = SceneSpawnPoint.InfirmaryFrontDoor;
        portal.prompt = "Click the front door to return to town";
        portal.interactionCollider = null;

        var healing = objects.FirstOrDefault(item => item && item.name == "Healing Station");
        if (!healing) healing = new GameObject("Healing Station").transform;
        healing.position = new Vector3(-7.2f, -2.35f, 0f);
        var renderer = healing.GetComponent<SpriteRenderer>();
        if (!renderer) renderer = healing.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealingArtPath);
        renderer.sortingOrder = 235;
        var collider = healing.GetComponent<BoxCollider2D>();
        if (!collider) collider = healing.gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.5f, 1.6f);
        collider.offset = new Vector2(0f, .4f);
        collider.isTrigger = true;
        var interactable = healing.GetComponent<InfirmaryHealing>();
        if (!interactable) interactable = healing.gameObject.AddComponent<InfirmaryHealing>();
        interactable.displayName = "HEALING STATION";
        interactable.interactionCollider = collider;
        interactable.treatmentCost = 3;
        // The copied sofa remains furniture, but sleeping belongs only in HomeInterior.

        EditorSceneManager.SaveScene(scene);

        var town = EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
        var home = GameObject.Find("PLAYER HOME")?.GetComponent<TownInteractable>();
        var infirmary = GameObject.Find("INFIRMARY")?.GetComponent<TownUpgradeBuilding>();
        if (!home || !infirmary) throw new System.Exception("Town building references are missing.");
        // Both building sprites pivot at their bottom edge. The door art sits
        // above the art child's origin, not below the building root.
        home.interactionCollider = EnsureDoorHitbox(home.transform, new Vector2(2f, -1.45f), new Vector2(1.15f, 1.55f));
        infirmary.interactionCollider = EnsureDoorHitbox(infirmary.transform, new Vector2(-.85f, -1.2f), new Vector2(1.2f, 1.5f));
        var references = Object.FindAnyObjectByType<SceneReferences>();
        if (!references) throw new System.Exception("Town SceneReferences is missing.");
        var serialized = new SerializedObject(references);
        serialized.FindProperty("infirmary").objectReferenceValue = infirmary.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(town);

        EditorBuildSettings.scenes = new[] { TownPath, HomePath, InfirmaryPath, "Assets/Scenes/ExpeditionField.unity" }
            .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("RPG_INFIRMARY_INTERIOR_SUCCESS");
    }

    static Collider2D EnsureDoorHitbox(Transform building, Vector2 position, Vector2 size)
    {
        var child = building.Find("Entrance Hitbox");
        if (!child)
        {
            child = new GameObject("Entrance Hitbox").transform;
            child.SetParent(building, false);
        }
        child.localPosition = position;
        var collider = child.GetComponent<BoxCollider2D>();
        if (!collider) collider = child.gameObject.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = true;
        return collider;
    }
}

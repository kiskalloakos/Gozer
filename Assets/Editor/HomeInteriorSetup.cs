using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HomeInteriorSetup
{
    const string ScenePath = "Assets/Scenes/HomeInterior.unity";
    const string TownScenePath = "Assets/Scenes/TownHub.unity";
    const string ArtFolder = "Assets/Art/Environment/PixelInterior";
    const string GeneratedFolder = ArtFolder + "/Generated";
    static Sprite square;

    [MenuItem("RPG/Create or Maintain Home Interior")]
    public static void Build()
    {
        Directory.CreateDirectory(GeneratedFolder);
        AssetDatabase.Refresh();
        ConfigureSourceSheets();
        CreateFurnitureCrops();
        AssetDatabase.Refresh();
        ConfigureGeneratedSprites();

        RemoveTownWorkbench();
        if (File.Exists(ScenePath))
        {
            MaintainExistingScene();
            return;
        }

        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Square.png");
        if (!square)
        {
            Debug.LogError("Missing Square.png placeholder sprite.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Home Interior").AddComponent<HomeInteriorController>();
        var room = new GameObject("Room").transform;

        Block("Floor", new Vector2(0, 0), new Vector2(19, 10), new Color(.42f, .27f, .19f), -10000, room);
        for (int x = -9; x <= 9; x++)
        for (int y = -4; y <= 4; y++)
        {
            var tint = ((x + y) & 1) == 0 ? new Color(.58f, .39f, .27f, .17f) : new Color(.32f, .20f, .15f, .12f);
            Block($"Floor detail {x},{y}", new Vector2(x, y), Vector2.one, tint, -9999, room);
        }

        Block("North wall", new Vector2(0, 4.45f), new Vector2(19, 1.1f), new Color(.76f, .67f, .51f), -9000, room, true);
        Block("North trim", new Vector2(0, 3.88f), new Vector2(19, .16f), new Color(.29f, .17f, .12f), -8999, room);
        Block("West wall", new Vector2(-9.45f, 0), new Vector2(1.1f, 10), new Color(.36f, .22f, .16f), -8900, room, true);
        Block("East wall", new Vector2(9.45f, 0), new Vector2(1.1f, 10), new Color(.36f, .22f, .16f), -8900, room, true);
        Block("South wall left", new Vector2(-5.25f, -4.45f), new Vector2(8.5f, 1.1f), new Color(.36f, .22f, .16f), -8900, room, true);
        Block("South wall right", new Vector2(5.25f, -4.45f), new Vector2(8.5f, 1.1f), new Color(.36f, .22f, .16f), -8900, room, true);

        Art("Window", "window.png", new Vector2(-5.6f, 3.1f), -310);
        Art("Fireplace", "fireplace.png", new Vector2(6.8f, 2.7f), -270);
        Art("Sofa", "sofa.png", new Vector2(-4.4f, .9f), -90, true);
        Art("Armchairs", "armchairs.png", new Vector2(3.7f, .8f), -80, true);
        Art("Rug", "rug.png", new Vector2(-1.5f, -.45f), -9950);
        Art("Dining set", "dining_set.png", new Vector2(4.7f, -1.65f), 165, true);

        var workbench = new GameObject("WORKBENCH");
        workbench.transform.position = new Vector3(-7.2f, -2.35f, 0);
        var workbenchRenderer = workbench.AddComponent<SpriteRenderer>();
        workbenchRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/workbench.png");
        workbenchRenderer.sortingOrder = 235;
        var workbenchCollider = workbench.AddComponent<BoxCollider2D>();
        workbenchCollider.size = new Vector2(2.7f, .55f);
        workbenchCollider.offset = new Vector2(0, .28f);
        var workbenchInteractable = workbench.AddComponent<TownInteractable>();
        workbenchInteractable.facility = TownInteractable.FacilityType.Workbench;
        workbenchInteractable.displayName = "WORKBENCH";

        var exit = new GameObject("Front Door");
        exit.transform.position = new Vector3(0, -3.55f, 0);
        var doorArt = Art("Door Art", "door.png", Vector2.zero, 355, false, exit.transform);
        doorArt.transform.localPosition = Vector3.zero;
        var portal = exit.AddComponent<ScenePortal>();
        portal.destinationScene = "TownHub";
        portal.destinationSpawnId = SceneTravel.PlayerHomeFrontDoor;
        portal.prompt = "Click the front door to return to town";
        var exitCollider = exit.AddComponent<BoxCollider2D>();
        exitCollider.size = new Vector2(1.75f, .45f);
        exitCollider.offset = new Vector2(0, -.75f);

        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, -1.7f, 0);
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(.5f, .38f);
        collider.offset = new Vector2(0, .19f);
        collider.direction = CapsuleDirection2D.Horizontal;
        var visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(player.transform, false);
        var renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 170;
        var controller = player.AddComponent<TownPlayerController>();
        controller.visual = renderer;
        player.AddComponent<TownPlayerInteractor>();
        TownCharacterSetup.ApplyToOpenTownScene();

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = PixelArtStandard.OrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.055f, .035f, .03f);
        cameraObject.AddComponent<AudioListener>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Selection.activeGameObject = player;
        Debug.Log("RPG_HOME_INTERIOR_SUCCESS");
    }

    static void MaintainExistingScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var sceneRoot = FindSceneObject(scene, "Home Interior");
        if (sceneRoot && !sceneRoot.GetComponent<HomeInteriorController>())
            sceneRoot.AddComponent<HomeInteriorController>();

        var player = FindSceneObject(scene, "Player");
        if (player && !player.GetComponent<TownPlayerInteractor>())
            player.AddComponent<TownPlayerInteractor>();

        var workbench = FindSceneObject(scene, "WORKBENCH");
        if (workbench)
        {
            var interactable = workbench.GetComponent<TownInteractable>() ?? workbench.AddComponent<TownInteractable>();
            interactable.facility = TownInteractable.FacilityType.Workbench;
            interactable.displayName = "WORKBENCH";
            EditorUtility.SetDirty(interactable);
        }

        var exit = FindSceneObject(scene, "Front Door");
        if (exit)
        {
            var portal = exit.GetComponent<ScenePortal>() ?? exit.AddComponent<ScenePortal>();
            portal.destinationScene = "TownHub";
            portal.destinationSpawnId = SceneTravel.PlayerHomeFrontDoor;
            portal.prompt = "Click the front door to return to town";
            EditorUtility.SetDirty(portal);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        if (player) Selection.activeGameObject = player;
        Debug.Log("RPG_HOME_INTERIOR_MAINTAINED — existing layout preserved");
    }

    static GameObject FindSceneObject(UnityEngine.SceneManagement.Scene scene, string objectName)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .FirstOrDefault(item => item.name == objectName);
    }

    static void RemoveTownWorkbench()
    {
        if (!File.Exists(TownScenePath)) return;
        var town = EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
        var workbench = town.GetRootGameObjects().FirstOrDefault(item => item.name == "WORKBENCH");
        if (workbench) Object.DestroyImmediate(workbench);
        EditorSceneManager.SaveScene(town);
    }

    static void ConfigureSourceSheets()
    {
        foreach (var path in Directory.GetFiles(ArtFolder, "*.png"))
        {
            var assetPath = path.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!importer) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }

    static void CreateFurnitureCrops()
    {
        Crop("livingroom_LRK.png", "sofa.png", 16, 16, 112, 48);
        Crop("livingroom_LRK.png", "armchairs.png", 144, 16, 176, 48);
        Crop("livingroom_LRK.png", "fireplace.png", 432, 16, 32, 48);
        Crop("livingroom_LRK.png", "rug.png", 16, 256, 112, 48);
        Crop("livingroom_LRK.png", "dining_set.png", 272, 128, 144, 80);
        Crop("doorswindowsstairs_LRK.png", "door.png", 64, 16, 32, 48);
        Crop("doorswindowsstairs_LRK.png", "window.png", 208, 16, 48, 48);
    }

    static void Crop(string sourceName, string outputName, int x, int top, int width, int height)
    {
        var source = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtFolder}/{sourceName}");
        if (!source)
        {
            Debug.LogError($"Missing interior source sheet {sourceName}.");
            return;
        }
        var pixels = source.GetPixels(x, source.height - top - height, width, height);
        var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
        output.SetPixels(pixels);
        output.Apply();
        File.WriteAllBytes($"{GeneratedFolder}/{outputName}", output.EncodeToPNG());
        Object.DestroyImmediate(output);
    }

    static void ConfigureGeneratedSprites()
    {
        foreach (var path in Directory.GetFiles(GeneratedFolder, "*.png"))
        {
            var assetPath = path.Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!importer) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }

    static GameObject Art(string name, string file, Vector2 position, int order, bool collider = false, Transform parent = null)
    {
        var gameObject = new GameObject(name);
        if (parent) gameObject.transform.SetParent(parent, false);
        gameObject.transform.position = position;
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedFolder}/{file}");
        renderer.sortingOrder = order;
        if (collider)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(renderer.bounds.size.x * .86f, Mathf.Min(.6f, renderer.bounds.size.y * .25f));
            box.offset = new Vector2(0, -renderer.bounds.size.y * .35f);
        }
        return gameObject;
    }

    static GameObject Block(string name, Vector2 position, Vector2 size, Color color, int order, Transform parent, bool collider = false)
    {
        var block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.position = position;
        block.transform.localScale = size;
        var renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (collider) block.AddComponent<BoxCollider2D>();
        return block;
    }

    static void EnsureBuildSettings()
    {
        var desired = new[]
        {
            "Assets/Scenes/TownHub.unity",
            ScenePath,
            "Assets/Scenes/ExpeditionField.unity"
        };
        EditorBuildSettings.scenes = desired
            .Where(File.Exists)
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }
}

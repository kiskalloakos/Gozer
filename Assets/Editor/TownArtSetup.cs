using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TownArtSetup
{
    const string GroundPath = "Assets/Art/Environment/ground_tiles_16px.png";
    const int GroundTilePixels = PixelArtStandard.TilePixels;
    const string HomePath = "Assets/Art/Environment/town_home.png";
    const string EnvironmentFolder = "Assets/Art/Environment";
    const string BuildingFolder = "Assets/Art/Environment/Buildings";
    const string StoragePath = BuildingFolder + "/storage_building.png";
    const string WorkbenchPath = EnvironmentFolder + "/workbench.png";
    const string GateClosedPath = EnvironmentFolder + "/expedition_gate_closed.png";
    const string GateOpenPath = EnvironmentFolder + "/expedition_gate_open.png";
    const string TownScenePath = "Assets/Scenes/TownHub.unity";
    const string TileFolder = "Assets/Art/Environment/Tiles";

    [MenuItem("RPG/Import And Apply Town Art")]
    public static void ImportAndApply()
    {
        AssetDatabase.Refresh();
        ConfigureGroundSheet();
        ConfigureSingleSprite(HomePath);
        ConfigureSingleSprite(StoragePath);
        ConfigureSingleSprite(WorkbenchPath);
        ConfigureSingleSprite(GateClosedPath);
        // Keep the unused alternate frame consistent with the project standard;
        // ScenePortal intentionally does not animate or reference it.
        ConfigureSingleSprite(GateOpenPath);
        foreach (string building in new[] { "watchtower", "infirmary", "greenhouse" })
            for (int level = 1; level <= 3; level++)
                ConfigureSingleSprite($"{BuildingFolder}/{building}_l{level}.png");
        TownDressingSetup.ConfigureAssets();
        CreateTileAssets();

        var scene = EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
        ApplyToOpenTownScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RPG_TOWN_ART_SUCCESS");
    }

    static void ConfigureGroundSheet()
    {
        var importer = AssetImporter.GetAtPath(GroundPath) as TextureImporter;
        if (!importer) { Debug.LogError($"Missing {GroundPath}"); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = GroundTilePixels;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

#pragma warning disable 0618
        importer.spritesheet = Enumerable.Range(0, 64).Select(index =>
        {
            int x = index % 8;
            int y = index / 8;
            return new SpriteMetaData
            {
                name = $"ground_{x}_{y}",
                rect = new Rect(x * GroundTilePixels, y * GroundTilePixels, GroundTilePixels, GroundTilePixels),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(.5f, .5f)
            };
        }).ToArray();
#pragma warning restore 0618
        importer.SaveAndReimport();
    }

    static void ConfigureSingleSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) { Debug.LogError($"Missing {path}"); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static void CreateTileAssets()
    {
        if (!AssetDatabase.IsValidFolder(TileFolder))
            AssetDatabase.CreateFolder("Assets/Art/Environment", "Tiles");

        var sprites = AssetDatabase.LoadAllAssetsAtPath(GroundPath).OfType<Sprite>().ToArray();
        SaveTile("Grass", sprites.First(sprite => sprite.name == "ground_0_7"));
        SaveTile("GrassFlowers", sprites.First(sprite => sprite.name == "ground_1_7"));
        SaveTile("GrassAlt", sprites.First(sprite => sprite.name == "ground_2_7"));
        SaveTile("Dirt", sprites.First(sprite => sprite.name == "ground_3_7"));
        SaveTile("DirtAlt", sprites.First(sprite => sprite.name == "ground_4_7"));
        SaveTile("Stone", sprites.First(sprite => sprite.name == "ground_6_7"));
    }

    static void SaveTile(string name, Sprite sprite)
    {
        string path = $"{TileFolder}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (!tile)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
    }

    public static void ApplyToOpenTownScene()
    {
        ApplyGroundTilemap();
        ApplyHomeArt();
        ApplyFacilityArt("STORAGE", StoragePath, -1.75f, "Storage Art");
        ApplyFacilityArt("WORKBENCH", WorkbenchPath, -.75f, "Workbench Art");
        ApplyUpgradeableBuildingArt("WATCHTOWER", "watchtower", -2.4f);
        ApplyUpgradeableBuildingArt("INFIRMARY", "infirmary", -1.9f);
        ApplyUpgradeableBuildingArt("GREENHOUSE", "greenhouse", -2.05f);
        RemoveFenceArtAndColliders();
        ApplyGateArt();
        TownDressingSetup.ApplyToOpenTownScene();
        ConfigureBuildingHitbox("PLAYER HOME", 5.2f, -2.15f, .75f);
        ConfigureBuildingHitbox("STORAGE", 4.5f, -1.75f, .75f);
        ConfigureBuildingHitbox("WATCHTOWER", 3.8f, -2.4f, .9f);
        ConfigureBuildingHitbox("INFIRMARY", 5.1f, -1.9f, .75f);
        ConfigureBuildingHitbox("GREENHOUSE", 5.2f, -2.05f, .75f);
        ApplyCameraSettings();
    }

    static void ApplyGroundTilemap()
    {
        var environment = GameObject.Find("Environment");
        if (!environment) { Debug.LogError("Town Environment object not found."); return; }

        var existing = GameObject.Find("Ground Tilemap");
        if (existing) Object.DestroyImmediate(existing);

        foreach (string placeholderName in new[] { "Grass", "Main road", "Cross road" })
        {
            var placeholder = GameObject.Find(placeholderName);
            if (placeholder && placeholder.TryGetComponent(out SpriteRenderer renderer)) renderer.enabled = false;
        }

        // The old prototype used a large stone plaza in the middle. Remove the
        // placeholder as well as its tilemap treatment so rebuilding cannot
        // bring it back.
        var oldTownSquare = GameObject.Find("Town square");
        if (oldTownSquare) Object.DestroyImmediate(oldTownSquare);

        var gridObject = new GameObject("Ground Tilemap");
        gridObject.transform.SetParent(environment.transform, false);
        var grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        var layer = new GameObject("Town Ground");
        layer.transform.SetParent(gridObject.transform, false);
        var tilemap = layer.AddComponent<Tilemap>();
        var tilemapRenderer = layer.AddComponent<TilemapRenderer>();
        tilemapRenderer.sortingOrder = -10000;

        var grass = AssetDatabase.LoadAssetAtPath<Tile>($"{TileFolder}/Grass.asset");
        var grassFlowers = AssetDatabase.LoadAssetAtPath<Tile>($"{TileFolder}/GrassFlowers.asset");
        var grassAlt = AssetDatabase.LoadAssetAtPath<Tile>($"{TileFolder}/GrassAlt.asset");
        var grasses = new[] { grass, grassAlt, grass, grassFlowers, grass };

        for (int x = -16; x < 16; x++)
        for (int y = -11; y < 11; y++)
        {
            TileBase selected = grasses[Mathf.Abs(x * 3 + y) % grasses.Length];
            tilemap.SetTile(new Vector3Int(x, y, 0), selected);
        }
    }

    static void ApplyHomeArt()
    {
        var home = GameObject.Find("PLAYER HOME");
        var homeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HomePath);
        if (!home || !homeSprite) { Debug.LogError("Player home or home sprite not found."); return; }

        var previous = home.transform.Find("Home Art");
        if (previous) Object.DestroyImmediate(previous.gameObject);
        foreach (var renderer in home.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;

        var art = new GameObject("Home Art");
        art.transform.SetParent(home.transform, false);
        art.transform.localPosition = new Vector3(0, -2.15f, 0);
        var artRenderer = art.AddComponent<SpriteRenderer>();
        artRenderer.sprite = homeSprite;
        artRenderer.sortingOrder = Mathf.RoundToInt(-home.transform.position.y * 100);
    }

    static void ApplyFacilityArt(string objectName, string spritePath, float bottomOffset, string artName)
    {
        var root = GameObject.Find(objectName);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (!root || !sprite) { Debug.LogError($"Could not apply art for {objectName}."); return; }

        var previous = root.transform.Find(artName);
        if (previous) Object.DestroyImmediate(previous.gameObject);
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;

        var art = new GameObject(artName);
        art.transform.SetParent(root.transform, false);
        art.transform.localPosition = new Vector3(0, bottomOffset, 0);
        var artRenderer = art.AddComponent<SpriteRenderer>();
        artRenderer.sprite = sprite;
        artRenderer.sortingOrder = Mathf.RoundToInt(-root.transform.position.y * 100);
    }

    static void ApplyUpgradeableBuildingArt(string objectName, string filePrefix, float bottomOffset)
    {
        var root = GameObject.Find(objectName);
        if (!root) { Debug.LogError($"Could not find {objectName}."); return; }

        var previous = root.transform.Find("Building Art");
        if (previous) Object.DestroyImmediate(previous.gameObject);
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;

        var sprites = Enumerable.Range(1, 3)
            .Select(level => AssetDatabase.LoadAssetAtPath<Sprite>($"{BuildingFolder}/{filePrefix}_l{level}.png"))
            .ToArray();
        if (sprites.Any(sprite => !sprite)) { Debug.LogError($"One or more {filePrefix} levels are missing."); return; }

        var art = new GameObject("Building Art");
        art.transform.SetParent(root.transform, false);
        art.transform.localPosition = new Vector3(0, bottomOffset, 0);
        var artRenderer = art.AddComponent<SpriteRenderer>();
        artRenderer.sprite = sprites[0];
        artRenderer.sortingOrder = Mathf.RoundToInt(-root.transform.position.y * 100);

        var building = root.GetComponent<TownUpgradeBuilding>();
        building.levelRenderer = artRenderer;
        building.levelSprites = sprites;
        building.tierVisuals = new GameObject[0];
        EditorUtility.SetDirty(building);
    }

    static void RemoveFenceArtAndColliders()
    {
        var fenceNames = new[]
        {
            "Fence Art", "North fence", "West fence", "East fence",
            "South-west fence", "South-east fence"
        };
        var objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(candidate => fenceNames.Contains(candidate.name))
            .ToArray();
        foreach (var fenceObject in objects)
            Object.DestroyImmediate(fenceObject);
    }

    static void ApplyGateArt()
    {
        var gate = GameObject.Find("Expedition gate");
        if (!gate) return;
        var previous = gate.transform.Find("Gate Art");
        if (previous) Object.DestroyImmediate(previous.gameObject);
        if (gate.TryGetComponent(out SpriteRenderer placeholder)) placeholder.enabled = false;
        // The placeholder was a scaled 1 px rectangle. Childing the real art
        // beneath that scale made the 6-unit gate 36 units wide.
        gate.transform.localScale = Vector3.one;

        var closed = AssetDatabase.LoadAssetAtPath<Sprite>(GateClosedPath);
        var art = new GameObject("Gate Art");
        art.transform.SetParent(gate.transform, false);
        art.transform.localPosition = new Vector3(0, -.35f, 0);
        var renderer = art.AddComponent<SpriteRenderer>();
        renderer.sprite = closed;
        renderer.sortingOrder = Mathf.RoundToInt(-gate.transform.position.y * 100);

        var portal = gate.GetComponent<ScenePortal>();
        EditorUtility.SetDirty(portal);

        var gateCollider = gate.GetComponent<BoxCollider2D>();
        if (!gateCollider) gateCollider = gate.AddComponent<BoxCollider2D>();
        gateCollider.size = new Vector2(5.6f, .45f);
        gateCollider.offset = new Vector2(0, .05f);
        EditorUtility.SetDirty(gateCollider);
    }

    static void ConfigureBuildingHitbox(string objectName, float width, float bottomOffset, float depth)
    {
        var root = GameObject.Find(objectName);
        if (!root) return;

        foreach (var collider in root.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;

        var footprint = root.GetComponent<BoxCollider2D>();
        if (!footprint) footprint = root.AddComponent<BoxCollider2D>();
        footprint.enabled = true;
        footprint.size = new Vector2(width, depth);
        footprint.offset = new Vector2(0, bottomOffset + depth * .5f);
        EditorUtility.SetDirty(footprint);
    }

    static void ApplyCameraSettings()
    {
        var camera = Camera.main;
        if (!camera) return;

        camera.orthographic = true;
        camera.orthographicSize = PixelArtStandard.OrthographicSize;
        var follow = camera.GetComponent<FollowCamera>();
        if (follow)
        {
            follow.assetsPixelsPerUnit = PixelArtStandard.PixelsPerUnit;
            follow.referenceResolutionY = PixelArtStandard.ReferenceHeight;
            // Rigidbody interpolation produces smooth sub-pixel motion. Snapping
            // both axes made normalized diagonal movement alternate unevenly.
            follow.snapToPixelGrid = false;
            EditorUtility.SetDirty(follow);
        }
        EditorUtility.SetDirty(camera);
    }
}

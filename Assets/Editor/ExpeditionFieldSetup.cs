using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class ExpeditionFieldSetup
{
    const string ScenePath = "Assets/Scenes/ExpeditionField.unity";
    const string TownPath = "Assets/Scenes/TownHub.unity";
    const string HomePath = "Assets/Scenes/HomeInterior.unity";
    const string TreePath = "Assets/Art/Environment/TownDressing/TREE_NIGHT.png";
    const string CharacterPath = "Assets/Art/Characters/TownCharacterSheet.png";
    const string SquarePath = "Assets/Art/Environment/Square.png";
    const string MeleeSwooshPath = "Assets/Resources/Effects/melee_swoosh.png";
    const string DemonIdlePath = "Assets/Resources/Enemies/DemonA/idle.png";
    const string DemonWalkPath = "Assets/Resources/Enemies/DemonA/walk.png";
    const string DemonAttack01Path = "Assets/Resources/Enemies/DemonA/attack01.png";
    const string DemonAttack02Path = "Assets/Resources/Enemies/DemonA/attack02.png";
    const string DemonHurtPath = "Assets/Resources/Enemies/DemonA/hurt.png";
    const string DemonDeathPath = "Assets/Resources/Enemies/DemonA/death.png";
    const string TileFolder = "Assets/Art/Environment/Tiles";
    static readonly string[] NightGrassPaths =
    {
        "Assets/Art/Environment/grass_base_1_night.png",
        "Assets/Art/Environment/grass_base_2_night.png",
        "Assets/Art/Environment/grass_base_3_night.png"
    };
    const int HalfWidth = 32;
    const int HalfHeight = 24;

    [MenuItem("RPG/Build Expedition Field")]
    public static void Build()
    {
        ConfigureNightAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var environment = new GameObject("Expedition Field").transform;

        CreateGround(environment);
        CreateBoundary(environment);
        CreateTrees(environment);
        var player = CreatePlayer();
        CreateCamera(player.transform);
        CreateExtraction(environment);
        CreateEnemies(environment);

        TownCharacterSetup.ApplyToOpenTownScene();
        EditorSceneManager.SaveScene(scene, ScenePath);
        ConnectTownGate();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TownPath, true),
            new EditorBuildSettingsScene(HomePath, true),
            new EditorBuildSettingsScene(ScenePath, true)
        };

        EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("RPG_EXPEDITION_FIELD_SUCCESS");
    }

    static void CreateGround(Transform parent)
    {
        var gridObject = new GameObject("Ground Grid");
        gridObject.transform.SetParent(parent, false);
        gridObject.AddComponent<Grid>().cellSize = Vector3.one;

        var layer = new GameObject("Grass Field");
        layer.transform.SetParent(gridObject.transform, false);
        var tilemap = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>().sortingOrder = -10000;

        var grass1 = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileFolder}/NightGrass1.asset");
        var grass2 = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileFolder}/NightGrass2.asset");
        var grass3 = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileFolder}/NightGrass3.asset");
        var choices = new[] { grass1, grass1, grass1, grass2, grass3 };

        for (int x = -HalfWidth; x < HalfWidth; x++)
        for (int y = -HalfHeight; y < HalfHeight; y++)
            tilemap.SetTile(new Vector3Int(x, y, 0), choices[Math.Abs(x * 17 + y * 31) % choices.Length]);
    }

    static void ConfigureNightAssets()
    {
        // Import source textures consistently before creating their reusable tiles.
        for (int i = 0; i < NightGrassPaths.Length; i++)
        {
            ConfigureSprite(NightGrassPaths[i], new Vector2(.5f, .5f));
            SaveTile($"NightGrass{i + 1}", AssetDatabase.LoadAssetAtPath<Sprite>(NightGrassPaths[i]));
        }
        ConfigureSprite(TreePath, new Vector2(.5f, 0f));
        ConfigureEffectTexture(MeleeSwooshPath);
        ConfigureEffectTexture(DemonIdlePath);
        ConfigureEffectTexture(DemonWalkPath);
        ConfigureEffectTexture(DemonAttack01Path);
        ConfigureEffectTexture(DemonAttack02Path);
        ConfigureEffectTexture(DemonHurtPath);
        ConfigureEffectTexture(DemonDeathPath);
    }

    static void ConfigureSprite(string path, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new InvalidOperationException($"Missing expedition art: {path}");

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool requiresReimport = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelArtStandard.PixelsPerUnit)
            || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.mipmapEnabled
            || !importer.alphaIsTransparency
            || importer.wrapMode != TextureWrapMode.Clamp
            || settings.spriteAlignment != (int)SpriteAlignment.Custom
            || settings.spritePivot != pivot;
        if (!requiresReimport) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static void ConfigureEffectTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new InvalidOperationException($"Missing melee effect art: {path}");

        bool requiresReimport = importer.textureType != TextureImporterType.Default
            || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.mipmapEnabled
            || !importer.alphaIsTransparency
            || importer.wrapMode != TextureWrapMode.Clamp;
        if (!requiresReimport) return;

        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    static void SaveTile(string name, Sprite sprite)
    {
        if (!sprite) throw new InvalidOperationException($"Could not load sprite for {name}.");
        var path = $"{TileFolder}/{name}.asset";
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

    static void CreateBoundary(Transform parent)
    {
        CreateWall("North Boundary", new Vector2(0, HalfHeight + .5f), new Vector2(HalfWidth * 2 + 2, 1), parent);
        CreateWall("South Boundary", new Vector2(0, -HalfHeight - .5f), new Vector2(HalfWidth * 2 + 2, 1), parent);
        CreateWall("West Boundary", new Vector2(-HalfWidth - .5f, 0), new Vector2(1, HalfHeight * 2), parent);
        CreateWall("East Boundary", new Vector2(HalfWidth + .5f, 0), new Vector2(1, HalfHeight * 2), parent);
    }

    static void CreateWall(string name, Vector2 position, Vector2 size, Transform parent)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.AddComponent<BoxCollider2D>().size = size;
    }

    static void CreateTrees(Transform parent)
    {
        var root = new GameObject("Trees").transform;
        root.SetParent(parent, false);
        var sprite = AssetDatabase.LoadAllAssetsAtPath(TreePath).OfType<Sprite>().FirstOrDefault();
        if (!sprite) throw new InvalidOperationException($"Could not load the night tree sprite at {TreePath}.");
        var random = new System.Random(7319);

        for (int i = 0; i < 105; i++)
        {
            var position = new Vector2(
                (float)(random.NextDouble() * (HalfWidth * 2 - 6) - HalfWidth + 3),
                (float)(random.NextDouble() * (HalfHeight * 2 - 6) - HalfHeight + 3));

            if (position.y < -13f && Mathf.Abs(position.x) < 6f) continue;
            if (Vector2.Distance(position, new Vector2(20f, 12f)) < 5f) continue;
            CreateTree(root, sprite, position, $"Tree {i + 1}");
        }

        var grove = new[]
        {
            new Vector2(16,9), new Vector2(18,9), new Vector2(20,9), new Vector2(22,9), new Vector2(24,10),
            new Vector2(15,11), new Vector2(15,13), new Vector2(16,15), new Vector2(18,16),
            new Vector2(20,16), new Vector2(22,16), new Vector2(24,15), new Vector2(25,13), new Vector2(25,11),
            new Vector2(18,12), new Vector2(22,12), new Vector2(19,14), new Vector2(23,14)
        };
        for (int i = 0; i < grove.Length; i++) CreateTree(root, sprite, grove[i], $"Extraction Grove {i + 1}");
    }

    static void CreateTree(Transform parent, Sprite sprite, Vector2 position, string name)
    {
        var tree = new GameObject(name);
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = position;
        var renderer = tree.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 100);
        var collider = tree.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Horizontal;
        collider.size = new Vector2(.7f, .38f);
        collider.offset = new Vector2(0, .19f);
        tree.AddComponent<TownOcclusionFader>();
    }

    static GameObject CreatePlayer()
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, -18, 0);
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var collider = player.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Horizontal;
        collider.size = new Vector2(.5f, .38f);
        collider.offset = new Vector2(0, .19f);

        var visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(player.transform, false);
        var visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sprite = AssetDatabase.LoadAllAssetsAtPath(CharacterPath).OfType<Sprite>()
            .FirstOrDefault(candidate => candidate.name == "town_character_down_0");
        visual.sortingOrder = 1800;
        player.AddComponent<TownPlayerController>().visual = visual;
        var health = player.AddComponent<ExpeditionPlayerHealth>();
        var combat = player.AddComponent<ExpeditionPlayerCombat>();
        combat.swooshSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(MeleeSwooshPath);
        player.AddComponent<ExpeditionHUD>().health = health;
        return player;
    }

    static void CreateCamera(Transform target)
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(target.position.x, target.position.y, -10);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = PixelArtStandard.OrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .065f, .04f);
        cameraObject.AddComponent<AudioListener>();
        var follow = cameraObject.AddComponent<FollowCamera>();
        follow.target = target;
        follow.assetsPixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        follow.referenceResolutionY = PixelArtStandard.ReferenceHeight;
    }

    static void CreateExtraction(Transform parent)
    {
        var extraction = new GameObject("Extraction Zone");
        extraction.transform.SetParent(parent, false);
        extraction.transform.localPosition = new Vector3(20, 12, 0);
        extraction.transform.localScale = new Vector3(4.3f, 2.7f, 1f);

        var renderer = extraction.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        renderer.color = new Color(.92f, .55f, .12f, .48f);
        renderer.sortingOrder = -9999;
        extraction.AddComponent<BoxCollider2D>().isTrigger = true;
        extraction.AddComponent<ExtractionZone>().extractionSeconds = 10f;
    }

    static void CreateEnemies(Transform parent)
    {
        var root = new GameObject("Enemies").transform;
        root.SetParent(parent, false);
        var positions = new[]
        {
            new Vector2(8, -13), new Vector2(-9, -8), new Vector2(11, -3),
            new Vector2(-17, 4), new Vector2(7, 8), new Vector2(14, 14),
            new Vector2(27, 7), new Vector2(-23, 15)
        };
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        var idleSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DemonIdlePath);
        var walkSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DemonWalkPath);
        var attack01Sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DemonAttack01Path);
        var attack02Sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DemonAttack02Path);
        var hurtSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DemonHurtPath);
        var deathSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DemonDeathPath);

        for (int i = 0; i < positions.Length; i++)
        {
            var enemy = new GameObject($"Wilderness Stalker {i + 1}");
            enemy.transform.SetParent(root, false);
            enemy.transform.localPosition = positions[i];
            enemy.transform.localScale = new Vector3(.8f, .8f, 1f);
            var renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = Mathf.RoundToInt(-positions[i].y * 100);
            var body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            enemy.AddComponent<CircleCollider2D>().radius = .48f;
            enemy.AddComponent<WildernessEnemy>();
            var animator = enemy.AddComponent<DemonSpriteAnimator>();
            animator.idleSheet = idleSheet;
            animator.walkSheet = walkSheet;
            animator.attack01Sheet = attack01Sheet;
            animator.attack02Sheet = attack02Sheet;
            animator.hurtSheet = hurtSheet;
            animator.deathSheet = deathSheet;
        }
    }

    static void ConnectTownGate()
    {
        var town = EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
        var portal = UnityEngine.Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.destinationScene == GameScene.ExpeditionField)
            ?? UnityEngine.Object.FindAnyObjectByType<ScenePortal>();
        if (!portal) throw new InvalidOperationException("Town expedition gate portal was not found.");

        portal.destinationScene = GameScene.ExpeditionField;
        portal.requiresExpeditionTime = true;
        portal.destinationSpawn = SceneSpawnPoint.None;
        portal.prompt = "Click to begin an expedition";
        EditorUtility.SetDirty(portal);
        EditorSceneManager.SaveScene(town);
    }
}

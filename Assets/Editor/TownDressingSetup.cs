using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TownDressingSetup
{
    const string Folder = "Assets/Art/Environment/TownDressing";
    const string PathsPath = Folder + "/town_paths.png";
    const string DetailsPath = Folder + "/town_ground_details.png";
    const string TreePath = Folder + "/town_tree_slim.png";

    enum PathKind { Vertical, Horizontal, Junction }

    public static void ConfigureAssets()
    {
        ConfigureSheet(PathsPath, "town_path", 8, 4, 16, 16, new Vector2(.5f, .5f));
        ConfigureSheet(DetailsPath, "town_detail", 8, 3, 16, 16, new Vector2(.5f, 0f));
        ConfigureSingleSprite(TreePath);
    }

    static void ConfigureSingleSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) { Debug.LogError($"Missing town dressing sprite: {path}"); return; }
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

    static void ConfigureSheet(string path, string prefix, int columns, int rows, int width, int height, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) { Debug.LogError($"Missing town dressing sheet: {path}"); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

#pragma warning disable 0618
        importer.spritesheet = Enumerable.Range(0, columns * rows).Select(index =>
        {
            int column = index % columns;
            int rowFromTop = index / columns;
            return new SpriteMetaData
            {
                name = $"{prefix}_{column}_{rowFromTop}",
                rect = new Rect(column * width, (rows - 1 - rowFromTop) * height, width, height),
                alignment = (int)SpriteAlignment.Custom,
                pivot = pivot
            };
        }).ToArray();
#pragma warning restore 0618
        importer.SaveAndReimport();
    }

    public static void ApplyToOpenTownScene()
    {
        var environment = GameObject.Find("Environment");
        if (!environment) { Debug.LogError("Town Environment object not found."); return; }

        ApplyCompactLayoutIfStillPrototype();

        var previous = environment.transform.Find("Town Dressing");
        if (previous) Object.DestroyImmediate(previous.gameObject);

        var root = new GameObject("Town Dressing").transform;
        root.SetParent(environment.transform, false);

        CreatePaths(root);
        CreateGroundDetails(root);
        CreateTrees(root);
    }

    static void CreatePaths(Transform root)
    {
        var pathRoot = NewGroup("Natural Paths", root);
        var cells = new Dictionary<Vector2Int, PathKind>();

        // One readable north-south spine connects storage to the expedition
        // gate. Short branches meet each building at its actual door rather
        // than extending a full road grid across the town.
        var storageDoor = EntranceCell("STORAGE", -1.75f);
        var gate = GameObject.Find("Expedition gate");
        int gateY = gate ? Mathf.CeilToInt(gate.transform.position.y) : -8;
        AddVertical(cells, 0, gateY, storageDoor.y);

        ConnectEntrance(cells, EntranceCell("PLAYER HOME", -2.15f), 1);
        ConnectEntrance(cells, EntranceCell("WATCHTOWER", -2.4f), 1);
        ConnectEntrance(cells, EntranceCell("GREENHOUSE", -2.05f), -3);
        ConnectEntrance(cells, EntranceCell("INFIRMARY", -1.9f), -3);

        var centerSprites = Enumerable.Range(0, 4).Select(column => Sprite(PathsPath, $"town_path_{column}_0")).ToArray();
        var verticalSprites = Enumerable.Range(4, 3).Select(column => Sprite(PathsPath, $"town_path_{column}_0")).ToArray();
        var horizontalSprites = Enumerable.Range(4, 3).Select(column => Sprite(PathsPath, $"town_path_{column}_1")).ToArray();

        foreach (var pair in cells.OrderBy(pair => pair.Key.y).ThenBy(pair => pair.Key.x))
        {
            Sprite sprite;
            if (pair.Value == PathKind.Vertical)
                sprite = verticalSprites[Mathf.Abs(pair.Key.x * 5 + pair.Key.y) % verticalSprites.Length];
            else if (pair.Value == PathKind.Horizontal)
                sprite = horizontalSprites[Mathf.Abs(pair.Key.x + pair.Key.y * 5) % horizontalSprites.Length];
            else
                sprite = centerSprites[Mathf.Abs(pair.Key.x * 3 + pair.Key.y) % centerSprites.Length];

            Place(pathRoot, $"Path {pair.Key.x},{pair.Key.y}", sprite,
                new Vector2(pair.Key.x + .5f, pair.Key.y + .5f), -9999);
        }
    }

    static Vector2Int EntranceCell(string objectName, float artBottomOffset)
    {
        var building = GameObject.Find(objectName);
        if (!building) return Vector2Int.zero;

        int targetX = Mathf.FloorToInt(building.transform.position.x);
        int targetY = Mathf.FloorToInt(building.transform.position.y + artBottomOffset - .15f);
        return new Vector2Int(targetX, targetY);
    }

    static void ConnectEntrance(Dictionary<Vector2Int, PathKind> cells, Vector2Int entrance, int branchY)
    {
        AddHorizontal(cells, branchY, 0, entrance.x);
        AddVertical(cells, entrance.x, branchY, entrance.y);
        AddPathCell(cells, new Vector2Int(0, branchY), PathKind.Junction);
        if (entrance.y != branchY)
            AddPathCell(cells, new Vector2Int(entrance.x, branchY), PathKind.Junction);
    }

    static void AddHorizontal(Dictionary<Vector2Int, PathKind> cells, int y, int fromX, int toX)
    {
        int min = Mathf.Min(fromX, toX);
        int max = Mathf.Max(fromX, toX);
        for (int x = min; x <= max; x++)
            AddPathCell(cells, new Vector2Int(x, y), PathKind.Horizontal);
    }

    static void AddVertical(Dictionary<Vector2Int, PathKind> cells, int x, int fromY, int toY)
    {
        int min = Mathf.Min(fromY, toY);
        int max = Mathf.Max(fromY, toY);
        for (int y = min; y <= max; y++)
            AddPathCell(cells, new Vector2Int(x, y), PathKind.Vertical);
    }

    static void AddPathCell(Dictionary<Vector2Int, PathKind> cells, Vector2Int cell, PathKind kind)
    {
        if (cells.TryGetValue(cell, out var existing) && existing != kind)
            cells[cell] = PathKind.Junction;
        else
            cells[cell] = kind;
    }

    static void CreateGroundDetails(Transform root)
    {
        var detailRoot = NewGroup("Ground Details", root);
        var placements = new (int column, int row, float x, float y)[]
        {
            (0,0,-14,8), (1,0,-12,3), (2,0,-7,8), (3,0,-5,-7),
            (4,0,6,8), (5,0,12,4), (6,0,7,-8), (7,0,13,-6),
            (0,1,-14,-2), (1,1,-8,2), (2,1,-5,6), (3,1,4,7),
            (4,1,8,3), (5,1,12,-3), (6,1,5,-7), (7,1,-7,-8),
            (0,2,-11,7), (1,2,-9,3), (2,2,-6,-5), (3,2,-3,8),
            (4,2,3,5), (5,2,7,2), (6,2,10,-6), (7,2,14,7),
            (2,0,-12,-7), (5,0,-3,-6), (1,1,3,-4), (4,0,10,7)
        };

        int index = 0;
        foreach (var placement in placements)
        {
            Place(detailRoot, $"Ground detail {index++}",
                Sprite(DetailsPath, $"town_detail_{placement.column}_{placement.row}"),
                new Vector2(placement.x + .5f, placement.y), -9500);
        }
    }

    static void CreateTrees(Transform root)
    {
        var treeRoot = NewGroup("Trees", root);
        var tree = AssetDatabase.LoadAssetAtPath<Sprite>(TreePath);
        var trees = new (float x, float y)[]
        {
            (-14.5f,9.5f), (-13.1f,8.9f), (-11.8f,9.6f), (-14.6f,5.7f),
            (14.4f,9.3f), (13.0f,8.8f), (11.7f,9.5f), (14.5f,5.4f),
            (-14.4f,-7.5f), (-13.0f,-8.5f), (-11.6f,-9.2f),
            (14.3f,-7.4f), (12.9f,-8.5f), (11.5f,-9.2f),
            (-9.2f,1.2f), (-8.8f,-.6f), (9.1f,1.1f), (8.7f,-.8f)
        };
        for (int i = 0; i < trees.Length; i++)
        {
            var position = new Vector2(trees[i].x, trees[i].y);
            var treeObject = Place(treeRoot, $"Tree {i + 1}", tree, position,
                DynamicOrder(position.y), new Vector2(.28f, .3f), new Vector2(0, .15f));
            if (treeObject) treeObject.AddComponent<TownOcclusionFader>();
        }
    }

    static void ApplyCompactLayoutIfStillPrototype()
    {
        var prototype = new Dictionary<string, Vector2>
        {
            { "PLAYER HOME", new Vector2(-9.5f, 6.7f) },
            { "STORAGE", new Vector2(0, 7.1f) },
            { "WORKBENCH", new Vector2(-4.3f, -3.4f) },
            { "WATCHTOWER", new Vector2(9.7f, 6.3f) },
            { "INFIRMARY", new Vector2(10.2f, -.8f) },
            { "GREENHOUSE", new Vector2(-10.2f, -5.1f) }
        };

        // If any building has already been deliberately moved, preserve the
        // artist's layout and only add dressing around it.
        if (prototype.Any(pair =>
        {
            var item = GameObject.Find(pair.Key);
            return !item || Vector2.Distance(item.transform.position, pair.Value) > .05f;
        })) return;

        var compact = new Dictionary<string, Vector2>
        {
            { "PLAYER HOME", new Vector2(-6.2f, 3.8f) },
            { "STORAGE", new Vector2(0, 4.25f) },
            { "WORKBENCH", new Vector2(-2.2f, -.8f) },
            { "WATCHTOWER", new Vector2(6.2f, 3.9f) },
            { "INFIRMARY", new Vector2(6.1f, -2.6f) },
            { "GREENHOUSE", new Vector2(-6.2f, -2.9f) },
            { "Player", new Vector2(0, -1f) },
            { "Expedition gate", new Vector2(0, -8.8f) }
        };
        foreach (var pair in compact)
        {
            var item = GameObject.Find(pair.Key);
            if (item) item.transform.position = new Vector3(pair.Value.x, pair.Value.y, item.transform.position.z);
        }
    }

    static Transform NewGroup(string name, Transform parent)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    static GameObject Place(Transform parent, string name, Sprite sprite, Vector2 position, int sortingOrder,
        Vector2? colliderSize = null, Vector2 colliderOffset = default)
    {
        if (!sprite) { Debug.LogError($"Missing sprite for {name}"); return null; }
        var item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.transform.position = new Vector3(position.x, position.y, 0);
        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        if (colliderSize.HasValue)
        {
            var collider = item.AddComponent<BoxCollider2D>();
            collider.size = colliderSize.Value;
            collider.offset = colliderOffset;
        }
        return item;
    }

    static int DynamicOrder(float y) => Mathf.RoundToInt(-y * 100);

    static Sprite Sprite(string path, string name) => AssetDatabase.LoadAllAssetsAtPath(path)
        .OfType<Sprite>()
        .FirstOrDefault(sprite => sprite.name == name);
}

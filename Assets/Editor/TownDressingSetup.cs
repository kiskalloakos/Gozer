using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TownDressingSetup
{
    const string Folder = "Assets/Art/Environment/TownDressing";
    const string PathsPath = Folder + "/town_paths.png";
    const string DetailsPath = Folder + "/town_ground_details.png";
    const string TreesPath = Folder + "/town_trees.png";
    const string BushesPath = Folder + "/town_bushes.png";
    const string PropsPath = Folder + "/town_props.png";
    const string FencesPath = Folder + "/town_fence_set_v2.png";

    enum PathKind { Vertical, Horizontal, Junction }

    public static void ConfigureAssets()
    {
        ConfigureSheet(PathsPath, "town_path", 8, 4, 16, 16, new Vector2(.5f, .5f));
        ConfigureSheet(DetailsPath, "town_detail", 8, 3, 16, 16, new Vector2(.5f, 0f));
        ConfigureSheet(TreesPath, "town_tree", 3, 1, 64, 80, new Vector2(.5f, 0f));
        ConfigureSheet(BushesPath, "town_bush", 3, 1, 32, 32, new Vector2(.5f, 0f));
        ConfigureSheet(PropsPath, "town_prop", 6, 2, 32, 32, new Vector2(.5f, 0f));
        ConfigureSheet(FencesPath, "town_fence", 7, 1, 32, 32, new Vector2(.5f, 0f));
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
        CreateVegetation(root);
        CreatePropertyProps(root);
        CreateGreenhouseFence(root);
    }

    static void CreatePaths(Transform root)
    {
        var pathRoot = NewGroup("Natural Paths", root);
        var cells = new Dictionary<Vector2Int, PathKind>();

        for (int y = -9; y <= 4; y++) AddPathCell(cells, new Vector2Int(0, y), PathKind.Vertical);
        for (int x = -6; x <= 6; x++) AddPathCell(cells, new Vector2Int(x, 0), PathKind.Horizontal);

        ConnectBuilding(cells, "PLAYER HOME", -2.15f);
        ConnectBuilding(cells, "STORAGE", -1.75f);
        ConnectBuilding(cells, "WATCHTOWER", -2.4f);
        ConnectBuilding(cells, "INFIRMARY", -1.9f);
        ConnectBuilding(cells, "GREENHOUSE", -2.05f);

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

    static void ConnectBuilding(Dictionary<Vector2Int, PathKind> cells, string objectName, float artBottomOffset)
    {
        var building = GameObject.Find(objectName);
        if (!building) return;

        int targetX = Mathf.FloorToInt(building.transform.position.x);
        int targetY = Mathf.FloorToInt(building.transform.position.y + artBottomOffset - .15f);
        int direction = targetY >= 0 ? 1 : -1;

        for (int y = 0; y != targetY + direction; y += direction)
            AddPathCell(cells, new Vector2Int(targetX, y), PathKind.Vertical);

        int horizontalDirection = targetX >= 0 ? 1 : -1;
        for (int x = 0; x != targetX + horizontalDirection; x += horizontalDirection)
            AddPathCell(cells, new Vector2Int(x, 0), PathKind.Horizontal);

        AddPathCell(cells, new Vector2Int(targetX, 0), PathKind.Junction);
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

    static void CreateVegetation(Transform root)
    {
        var treeRoot = NewGroup("Trees", root);
        var trees = new (int variant, float x, float y)[]
        {
            (0,-14.2f,9.2f), (1,-11.8f,9.6f), (2,-14.7f,4.2f),
            (1,14.2f,8.8f), (0,12.2f,9.7f), (2,14.6f,3.4f),
            (2,-14.3f,-7.2f), (0,-11.8f,-9.1f),
            (1,14.1f,-7.5f), (2,11.7f,-9.2f)
        };
        for (int i = 0; i < trees.Length; i++)
        {
            var tree = trees[i];
            Place(treeRoot, $"Tree {i + 1}", Sprite(TreesPath, $"town_tree_{tree.variant}_0"),
                new Vector2(tree.x, tree.y), DynamicOrder(tree.y), new Vector2(.55f, .35f), new Vector2(0, .18f));
        }

        var bushRoot = NewGroup("Bushes", root);
        var bushes = new (int variant, float x, float y)[]
        {
            (0,-8.1f,4.2f), (1,-6.8f,4.1f), (2,-2.8f,5.1f),
            (1,3.1f,5.2f), (0,7.7f,3.5f), (2,8.8f,3.4f),
            (2,-8.2f,-7.2f), (0,-6.9f,-7.1f), (1,7.4f,-5.8f),
            (0,8.7f,-5.7f), (2,-12.7f,0.8f), (1,12.7f,1.1f)
        };
        for (int i = 0; i < bushes.Length; i++)
        {
            var bush = bushes[i];
            Place(bushRoot, $"Bush {i + 1}", Sprite(BushesPath, $"town_bush_{bush.variant}_0"),
                new Vector2(bush.x, bush.y), DynamicOrder(bush.y), new Vector2(1.3f, .35f), new Vector2(0, .18f));
        }
    }

    static void CreatePropertyProps(Transform root)
    {
        var propsRoot = NewGroup("Property Props", root);
        Near(propsRoot, "PLAYER HOME", "Home wood pile", 3, 0, new Vector2(-3.2f, -2.1f), true);
        Near(propsRoot, "PLAYER HOME", "Home laundry", 4, 1, new Vector2(3.2f, -1.8f), false);
        Near(propsRoot, "PLAYER HOME", "Home bench", 0, 1, new Vector2(-2.2f, -2.8f), true);
        Near(propsRoot, "STORAGE", "Storage crate", 0, 0, new Vector2(-2.7f, -1.8f), true);
        Near(propsRoot, "STORAGE", "Storage barrel", 1, 0, new Vector2(2.5f, -1.8f), true);
        Near(propsRoot, "STORAGE", "Storage sacks", 2, 0, new Vector2(3.5f, -1.7f), true);
        Near(propsRoot, "STORAGE", "Storage cart", 3, 1, new Vector2(-3.6f, -2.1f), true);
        Near(propsRoot, "WATCHTOWER", "Watchtower sign", 5, 0, new Vector2(-2.5f, -2.5f), true);
        Near(propsRoot, "WATCHTOWER", "Watchtower crate", 0, 0, new Vector2(2.4f, -2.5f), true);
        Near(propsRoot, "INFIRMARY", "Infirmary well", 2, 1, new Vector2(-3.8f, -2.0f), true);
        Near(propsRoot, "INFIRMARY", "Infirmary bucket", 4, 0, new Vector2(3.1f, -2.0f), false);
        Near(propsRoot, "INFIRMARY", "Infirmary bench", 0, 1, new Vector2(4.0f, -2.0f), true);
        Near(propsRoot, "GREENHOUSE", "Greenhouse trough", 5, 1, new Vector2(3.6f, -2.1f), true);
        Near(propsRoot, "GREENHOUSE", "Greenhouse table", 1, 1, new Vector2(-3.4f, -2.1f), true);
        Near(propsRoot, "GREENHOUSE", "Greenhouse sacks", 2, 0, new Vector2(-2.3f, -2.0f), true);
    }

    static void CreateGreenhouseFence(Transform root)
    {
        var greenhouse = GameObject.Find("GREENHOUSE");
        if (!greenhouse) return;

        var fenceRoot = NewGroup("Greenhouse Garden Fence", root);
        // A small garden sits beside and below the greenhouse instead of
        // surrounding its facade. This keeps the door and walking path clear.
        Vector2 center = greenhouse.transform.position + new Vector3(4.25f, -3.1f, 0);
        Sprite horizontal = Sprite(FencesPath, "town_fence_0_0");
        Sprite vertical = Sprite(FencesPath, "town_fence_1_0");
        Sprite topLeft = Sprite(FencesPath, "town_fence_2_0");
        Sprite topRight = Sprite(FencesPath, "town_fence_3_0");
        Sprite bottomLeft = Sprite(FencesPath, "town_fence_4_0");
        Sprite bottomRight = Sprite(FencesPath, "town_fence_5_0");
        Sprite gate = Sprite(FencesPath, "town_fence_6_0");

        Place(fenceRoot, "North fence", horizontal, new Vector2(center.x, center.y + 2f), DynamicOrder(center.y + 2f), new Vector2(1.8f, .2f), new Vector2(0, .55f));
        Place(fenceRoot, "West fence", vertical, new Vector2(center.x - 3f, center.y), DynamicOrder(center.y), new Vector2(.2f, 1.8f), new Vector2(0, 1f));
        Place(fenceRoot, "East fence", vertical, new Vector2(center.x + 3f, center.y), DynamicOrder(center.y), new Vector2(.2f, 1.8f), new Vector2(0, 1f));
        Place(fenceRoot, "North-west corner", topLeft, new Vector2(center.x - 3f, center.y + 2f), DynamicOrder(center.y + 2f));
        Place(fenceRoot, "North-east corner", topRight, new Vector2(center.x + 3f, center.y + 2f), DynamicOrder(center.y + 2f));
        Place(fenceRoot, "South-west corner", bottomLeft, new Vector2(center.x - 3f, center.y - 2f), DynamicOrder(center.y - 2f));
        Place(fenceRoot, "South-east corner", bottomRight, new Vector2(center.x + 3f, center.y - 2f), DynamicOrder(center.y - 2f));
        Place(fenceRoot, "Garden gate", gate, new Vector2(center.x, center.y - 2f), DynamicOrder(center.y - 2f));
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

    static void Near(Transform parent, string anchorName, string name, int column, int row, Vector2 offset, bool collider)
    {
        var anchor = GameObject.Find(anchorName);
        if (!anchor) return;
        Vector2 position = (Vector2)anchor.transform.position + offset;
        Place(parent, name, Sprite(PropsPath, $"town_prop_{column}_{row}"), position,
            DynamicOrder(position.y), collider ? new Vector2(1.25f, .35f) : (Vector2?)null,
            collider ? new Vector2(0, .18f) : Vector2.zero);
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

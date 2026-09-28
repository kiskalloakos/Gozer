using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TreeSpitterPrefabSetup
{
    const string ArtFolder = "Assets/Art/Environment/";
    const string ResourceFolder = "Assets/Resources/Enemies/Enemy2";
    const string PrefabPath = ResourceFolder + "/TreeSpitter.prefab";

    static TreeSpitterPrefabSetup()
    {
        EditorApplication.delayCall += EnsurePrefab;
    }

    static void EnsurePrefab()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnsurePrefab;
            return;
        }
        string[] names = { "enemy2.png", "enemy2 attack.png", "enemy2blob.png" };
        foreach (string name in names)
        {
            string path = ArtFolder + name;
            if (!File.Exists(path)) return;
            ConfigureSprite(path);
        }

        var idle = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "enemy2.png");
        if (!idle) return;
        Directory.CreateDirectory(ResourceFolder);
        AssetDatabase.Refresh();

        var attack = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "enemy2 attack.png");
        var blob = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "enemy2blob.png");
        if (!attack || !blob) return;
        var sentry = new GameObject("Tree Spitter");
        sentry.transform.localScale = Vector3.one;
        var renderer = sentry.AddComponent<SpriteRenderer>();
        renderer.sprite = idle;
        renderer.sortingOrder = 0;
        var body = sentry.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        var collider = sentry.AddComponent<CircleCollider2D>();
        collider.radius = .32f;
        collider.isTrigger = true;
        var behavior = sentry.AddComponent<TreeSpitterEnemy>();
        behavior.idleSprite = idle;
        behavior.attackSprite = attack;
        behavior.blobSprite = blob;
        sentry.AddComponent<WildernessEnemy>();
        // WildernessEnemy adds the demon animator only for melee enemies.
        // Keep the ranged prefab free of it even when replacing an older prefab.
        var demonAnimator = sentry.GetComponent<DemonSpriteAnimator>();
        if (demonAnimator) Object.DestroyImmediate(demonAnimator);
        PrefabUtility.SaveAsPrefabAsset(sentry, PrefabPath);
        Object.DestroyImmediate(sentry);
        AssetDatabase.SaveAssets();
    }

    static void ConfigureSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) return;

        bool change = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelArtStandard.PixelsPerUnit)
            || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.mipmapEnabled || !importer.alphaIsTransparency
            || importer.wrapMode != TextureWrapMode.Clamp;
        if (!change) return;

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

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class RpgSetup
{
    [MenuItem("RPG/Rebuild Test Room")]
    public static void Build()
    {
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        PlayerSettings.productName = "RPG";
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 0; settings.ApplyModifiedProperties(); }
        // Temporary combat-prototype art. These 128 px sources predate the
        // shipping 16 PPU pipeline and are not a scale reference for new art.
        var names = new[] { "rogue_final_standing", "rogue_final_firing_right" };
        var sprites = new Sprite[2];
        for (int i = 0; i < names.Length; i++)
        {
            var path = "Assets/Art/Characters/" + names[i] + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 500;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spritePivot = new Vector2(.5f, .045f);
            importer.SetTextureSettings(spriteSettings);
            importer.SaveAndReimport();
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Art/Environment");
        var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply();
        const string tilePath = "Assets/Art/Environment/Square.png";
        File.WriteAllBytes(tilePath, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(tilePath);
        var ti = (TextureImporter)AssetImporter.GetAtPath(tilePath);
        ti.textureType = TextureImporterType.Sprite; ti.spritePixelsPerUnit = 1; ti.filterMode = FilterMode.Point; ti.SaveAndReimport();
        var tile = AssetDatabase.LoadAssetAtPath<Sprite>(tilePath);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var room = new GameObject("Environment");
        for (int x = -10; x <= 10; x++) for (int y = -7; y <= 7; y++)
        {
            var go = new GameObject("Floor " + x + "," + y); go.transform.parent = room.transform;
            go.transform.position = new Vector3(x, y, 0);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = tile; sr.sortingOrder = -10000;
            sr.color = (x + y) % 2 == 0 ? new Color(.095f,.045f,.17f) : new Color(.12f,.055f,.21f);
        }
        Wall("North wall", new Vector2(0,7.5f), new Vector2(22,1), tile, room.transform);
        Wall("South wall", new Vector2(0,-7.5f), new Vector2(22,1), tile, room.transform);
        Wall("West wall", new Vector2(-10.5f,0), new Vector2(1,15), tile, room.transform);
        Wall("East wall", new Vector2(10.5f,0), new Vector2(1,15), tile, room.transform);
        Wall("Crate A", new Vector2(-3,2), new Vector2(2,1), tile, room.transform);
        var extractionCrate = Wall("Crate B", new Vector2(4,-2), new Vector2(2,2), tile, room.transform);
        var extractionPortal = extractionCrate.AddComponent<ScenePortal>();
        extractionPortal.destinationScene = "TownHub";
        extractionPortal.prompt = "[E]  Extract to town";
        var player = new GameObject("Player");
        var body = player.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = player.AddComponent<BoxCollider2D>(); col.size = new Vector2(.55f,.35f); col.offset = new Vector2(0,.18f);
        var art = new GameObject("Visual"); art.transform.parent = player.transform;
        var renderer = art.AddComponent<SpriteRenderer>(); renderer.sprite = sprites[0];
        var controller = player.AddComponent<RogueController>(); controller.visual = renderer;
        controller.standing = sprites[0]; controller.firing = sprites[1];
        var cameraGo = new GameObject("Main Camera"); cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = PixelArtStandard.OrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.04f,.02f,.08f);
        cameraGo.transform.position = new Vector3(0,0,-10); cameraGo.AddComponent<AudioListener>();
        var follow = cameraGo.AddComponent<FollowCamera>();
        follow.target = player.transform;
        follow.assetsPixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        follow.referenceResolutionY = PixelArtStandard.ReferenceHeight;
        follow.snapToPixelGrid = false;
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/RogueTestRoom.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/RogueTestRoom.unity", true) };
        Selection.activeGameObject = player;
        if (SceneView.lastActiveSceneView) { SceneView.lastActiveSceneView.in2DMode = true; SceneView.lastActiveSceneView.FrameSelected(); }
        AssetDatabase.SaveAssets();
        Debug.Log("RPG_SETUP_SUCCESS");
    }
    static GameObject Wall(string name, Vector2 pos, Vector2 size, Sprite sprite, Transform parent)
    {
        var go = new GameObject(name); go.transform.parent = parent; go.transform.position = pos; go.transform.localScale = size;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = new Color(.42f,.1f,.55f); sr.sortingOrder = -5000;
        go.AddComponent<BoxCollider2D>();
        return go;
    }
}

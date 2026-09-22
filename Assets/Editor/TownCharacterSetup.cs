using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TownCharacterSetup
{
    const string SheetPath = "Assets/Art/Characters/TownCharacterSheet.png";
    const string MeleeAttackPath = "Assets/Art/UI/base character meelee attacks.png";
    const string TownScenePath = "Assets/Scenes/TownHub.unity";
    const int FrameWidth = 16;
    const int FrameHeight = 32;

    [MenuItem("RPG/Import Directional Town Character")]
    public static void ImportAndApply()
    {
        AssetDatabase.Refresh();
        ConfigureSheet();
        ConfigureMeleeAttackSheet();
        var scene = EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
        ApplyToOpenTownScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RPG_TOWN_CHARACTER_SUCCESS");
    }

    static void ConfigureSheet()
    {
        var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (!importer) { Debug.LogError($"Missing {SheetPath}"); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        string[] directions = { "down", "right", "up", "left" };
#pragma warning disable 0618
        importer.spritesheet = Enumerable.Range(0, 16).Select(index =>
        {
            int column = index % 4;
            int rowFromTop = index / 4;
            // The Pixelorama export is a clean 4 x 4 grid of 16 x 32 frames.
            int y = (3 - rowFromTop) * FrameHeight;
            return new SpriteMetaData
            {
                name = $"town_character_{directions[rowFromTop]}_{column}",
                rect = new Rect(column * FrameWidth, y, FrameWidth, FrameHeight),
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(.5f, 0f)
            };
        }).ToArray();
#pragma warning restore 0618
        importer.SaveAndReimport();
    }

    public static void ApplyToOpenTownScene()
    {
        ConfigureMeleeAttackSheet();
        var controller = Object.FindAnyObjectByType<TownPlayerController>();
        if (!controller || !controller.visual) { Debug.LogError("Town player was not found."); return; }

        var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToArray();
        controller.walkDown = Direction(sprites, "down");
        controller.walkRight = Direction(sprites, "right");
        controller.walkUp = Direction(sprites, "up");
        controller.walkLeft = Direction(sprites, "left");
        controller.walkFramesPerSecond = PixelArtStandard.WalkFramesPerSecond;
        controller.visual.sprite = controller.walkDown[0];
        controller.visual.flipX = false;

        var collider = controller.GetComponent<CapsuleCollider2D>();
        if (collider)
        {
            collider.size = new Vector2(.5f, .38f);
            collider.offset = new Vector2(0, .19f);
            collider.direction = CapsuleDirection2D.Horizontal;
            EditorUtility.SetDirty(collider);
        }

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(controller.visual);
    }

    static Sprite[] Direction(Sprite[] sprites, string direction) => Enumerable.Range(0, 4)
        .Select(frame => sprites.First(sprite => sprite.name == $"town_character_{direction}_{frame}"))
        .ToArray();

    static void ConfigureMeleeAttackSheet()
    {
        var importer = AssetImporter.GetAtPath(MeleeAttackPath) as TextureImporter;
        if (!importer) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

#pragma warning disable 0618
        importer.spritesheet = Enumerable.Range(0, 12).Select(index =>
        {
            int column = index % 3;
            int rowFromTop = index / 3;
            return new SpriteMetaData
            {
                name = $"player_melee_{rowFromTop}_{column}",
                rect = new Rect(column * 16, (3 - rowFromTop) * 32, 16, 32),
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(.5f, 0f)
            };
        }).ToArray();
#pragma warning restore 0618
        importer.SaveAndReimport();
    }
}

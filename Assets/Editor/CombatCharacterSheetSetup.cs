using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatCharacterSheetSetup
{
    const string CombatSheetPath = "Assets/Art/Characters/TownCharacterSheetCombat.png";
    const int FramesPerDirection = 3;
    static readonly string[] Directions = { "down", "right", "up", "left" };

    [MenuItem("RPG/Prepare Town Combat Sheet (3 Frames)")]
    static void PrepareCombatSheet()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        AssetDatabase.Refresh();
        var importer = AssetImporter.GetAtPath(CombatSheetPath) as TextureImporter;
        if (!importer)
        {
            Debug.LogError($"Could not find {CombatSheetPath}.");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();

        importer = AssetImporter.GetAtPath(CombatSheetPath) as TextureImporter;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelArtStandard.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
#pragma warning disable 0618
        importer.spritesheet = System.Array.Empty<SpriteMetaData>();
#pragma warning restore 0618
        importer.SaveAndReimport();

        UpdateCombatProfiles(null, clearCustomCuts: true);
        Debug.Log("Combat sheet is ready for manual 3-frame-per-direction slicing.");
    }

    [MenuItem("RPG/Prepare Town Combat Sheet (3 Frames)", true)]
    static bool CanPrepareCombatSheet() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("RPG/Apply Manually Sliced Town Combat Sprites")]
    static void ApplyManualCuts()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var sprites = AssetDatabase.LoadAllAssetsAtPath(CombatSheetPath)
            .OfType<Sprite>().ToDictionary(sprite => sprite.name);
        foreach (string direction in Directions)
        for (int frame = 0; frame < FramesPerDirection; frame++)
        {
            string expectedName = SpriteName(direction, frame);
            if (!sprites.ContainsKey(expectedName))
            {
                Debug.LogError($"Missing combat slice '{expectedName}'. Name the 12 Sprite Editor slices town_character_combat_<direction>_<frame>.");
                return;
            }
        }

        UpdateCombatProfiles(sprites, clearCustomCuts: false);
        Debug.Log("Applied the 12 manually sliced combat sprites to all attack profiles.");
    }

    [MenuItem("RPG/Apply Manually Sliced Town Combat Sprites", true)]
    static bool CanApplyManualCuts() => !EditorApplication.isPlayingOrWillChangePlaymode;

    static void UpdateCombatProfiles(System.Collections.Generic.Dictionary<string, Sprite> sprites,
        bool clearCustomCuts)
    {
        var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath).ToArray();
        int scenesChanged = 0;

        foreach (string scenePath in scenePaths)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var combat in root.GetComponentsInChildren<ExpeditionPlayerCombat>(true))
            {
                if (combat.toolProfiles == null) continue;
                foreach (var profile in combat.toolProfiles)
                {
                    if (profile == null) continue;
                    profile.characterAttackSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(CombatSheetPath);
                    profile.characterFrameCount = FramesPerDirection;
                    profile.downAttackFrameOrder = new[] { 0, 1, 2 };
                    profile.rightAttackFrameOrder = new[] { 0, 1, 2 };
                    profile.upAttackFrameOrder = new[] { 0, 1, 2 };
                    profile.leftAttackFrameOrder = new[] { 0, 1, 2 };

                    if (clearCustomCuts)
                    {
                        profile.downCharacterAttackSprites = System.Array.Empty<Sprite>();
                        profile.rightCharacterAttackSprites = System.Array.Empty<Sprite>();
                        profile.upCharacterAttackSprites = System.Array.Empty<Sprite>();
                        profile.leftCharacterAttackSprites = System.Array.Empty<Sprite>();
                    }
                    else
                    {
                        profile.downCharacterAttackSprites = DirectionSprites(sprites, "down");
                        profile.rightCharacterAttackSprites = DirectionSprites(sprites, "right");
                        profile.upCharacterAttackSprites = DirectionSprites(sprites, "up");
                        profile.leftCharacterAttackSprites = DirectionSprites(sprites, "left");
                    }
                }

                EditorUtility.SetDirty(combat);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                scenesChanged++;
            }
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Updated 3-frame combat profiles in {scenesChanged} scene(s).");
    }

    static Sprite[] DirectionSprites(System.Collections.Generic.Dictionary<string, Sprite> sprites,
        string direction)
    {
        return Enumerable.Range(0, FramesPerDirection)
            .Select(frame => sprites[SpriteName(direction, frame)])
            .ToArray();
    }

    static string SpriteName(string direction, int frame)
        => $"town_character_combat_{direction}_{frame}";
}

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HeldToolVisualSetup
{
    static readonly string[] GameplayScenes =
    {
        "Assets/Scenes/TownHub.unity",
        "Assets/Scenes/HomeInterior.unity",
        "Assets/Scenes/ExpeditionField.unity"
    };

    static readonly string[] SharedVisualProperties =
    {
        "toolToConfigure", "toolProfiles",
        "showEditModePreview", "previewFacing", "previewWalking", "previewWalkFrame",
        "previewFacingInitialized"
    };

    [MenuItem("RPG/Configure Held Tool Visuals")]
    public static void ConfigureAllGameplayScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string previouslyOpenScene = EditorSceneManager.GetActiveScene().path;
        var sourceScene = EditorSceneManager.OpenScene(GameplayScenes[0], OpenSceneMode.Single);
        var sourcePlayer = FindPlayer(sourceScene);
        if (!sourcePlayer)
        {
            Debug.LogError($"Could not find Player in master scene {GameplayScenes[0]}.");
            return;
        }

        var sourceVisual = sourcePlayer.GetComponent<ToolHeldVisual>();
        if (!sourceVisual) sourceVisual = sourcePlayer.gameObject.AddComponent<ToolHeldVisual>();
        sourceVisual.PrepareProfilesForEditor();

        var sourceSerialized = new SerializedObject(sourceVisual);
        sourceSerialized.FindProperty("showEditModePreview").boolValue = true;
        sourceSerialized.FindProperty("previewFacing").enumValueIndex =
            (int)TownPlayerController.FacingDirection.Right;
        sourceSerialized.FindProperty("previewFacingInitialized").boolValue = true;
        sourceSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sourceVisual);
        EditorSceneManager.MarkSceneDirty(sourceScene);
        EditorSceneManager.SaveScene(sourceScene);

        foreach (string scenePath in GameplayScenes.Skip(1))
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            var player = FindPlayer(scene);
            if (!player)
            {
                Debug.LogError($"Could not find Player in scene {scenePath}.");
                EditorSceneManager.CloseScene(scene, true);
                continue;
            }

            var heldVisual = player.GetComponent<ToolHeldVisual>();
            if (!heldVisual) heldVisual = player.gameObject.AddComponent<ToolHeldVisual>();

            var sourceProperties = new SerializedObject(sourceVisual);
            var targetProperties = new SerializedObject(heldVisual);
            foreach (string propertyName in SharedVisualProperties)
            {
                targetProperties.CopyFromSerializedProperty(sourceProperties.FindProperty(propertyName));
            }
            targetProperties.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(heldVisual);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }

        if (!string.IsNullOrEmpty(previouslyOpenScene) && previouslyOpenScene != GameplayScenes[0])
            EditorSceneManager.OpenScene(previouslyOpenScene, OpenSceneMode.Single);

        Debug.Log("RPG_HELD_TOOL_VISUALS_CONFIGURED — TownHub held-tool settings copied to all gameplay scenes.");
    }

    static Transform FindPlayer(UnityEngine.SceneManagement.Scene scene)
        => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => transform.name == "Player");
}

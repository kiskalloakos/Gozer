using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ExpeditionPreviewTools
{
    const string ExpeditionScenePath = "Assets/Scenes/ExpeditionField.unity";

    [MenuItem("RPG/Tests/Preview Random Expedition")]
    public static void PreviewRandomExpedition()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int seed = Guid.NewGuid().GetHashCode();
        ExpeditionSeedManager.QueueSeed(seed);
        EditorSceneManager.OpenScene(ExpeditionScenePath, OpenSceneMode.Single);
        Debug.Log($"Opening procedural expedition preview with seed {seed}.");

        // Waiting one editor tick lets scene activation settle before Play Mode
        // consumes the queued seed and constructs the production layout.
        EditorApplication.delayCall += EnterPlayMode;
    }

    [MenuItem("RPG/Tests/Preview Random Expedition", true)]
    static bool CanPreviewRandomExpedition()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    static void EnterPlayMode()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.isPlaying = true;
    }
}

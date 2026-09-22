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
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // Make the command repeatable while testing. Stop the current run,
            // then invoke this method again after Unity returns to edit mode.
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += RetryAfterPlayModeStops;
            }
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int seed = Guid.NewGuid().GetHashCode();
        ExpeditionSeedManager.QueueSeed(seed);
        PlayerPrefs.SetInt(GameSessionFlow.EditorPreviewKey, 1);
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene(ExpeditionScenePath, OpenSceneMode.Single);
        Debug.Log($"Opening procedural expedition preview with seed {seed}.");

        // Waiting one editor tick lets scene activation settle before Play Mode
        // consumes the queued seed and constructs the production layout.
        EditorApplication.delayCall += EnterPlayMode;
    }

    [MenuItem("RPG/Tests/Preview Random Expedition", true)]
    static bool CanPreviewRandomExpedition()
    {
        return true;
    }

    static void EnterPlayMode()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.isPlaying = true;
    }

    static void RetryAfterPlayModeStops()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RetryAfterPlayModeStops;
            return;
        }
        PreviewRandomExpedition();
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class HomeStorageChestPreviewCapture
{
    const string PendingKey = "RPG.HomeStorageChestPreview.Pending";
    const string StageKey = "RPG.HomeStorageChestPreview.Stage";
    const string ScenePath = "Assets/Scenes/HomeInterior.unity";
    const string OutputPath = "Artifacts/home-storage-gold-open.png";

    static double nextStepAt;

    static HomeStorageChestPreviewCapture()
    {
        EditorApplication.update -= ContinueCapture;
        EditorApplication.update += ContinueCapture;
    }

    [MenuItem("RPG/Tests/Capture Open Storage Chest")]
    public static void Capture()
    {
        Directory.CreateDirectory("Artifacts");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(PendingKey, true);
        SessionState.SetInt(StageKey, 0);
        EditorApplication.EnterPlaymode();
    }

    static void ContinueCapture()
    {
        if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying) return;

        int stage = SessionState.GetInt(StageKey, 0);
        if (stage == 0)
        {
            var chest = Object.FindAnyObjectByType<HomeStorageChest>();
            if (!chest) return;

            chest.Interact();
            SessionState.SetInt(StageKey, 1);
            nextStepAt = EditorApplication.timeSinceStartup + 1d;
            return;
        }

        if (EditorApplication.timeSinceStartup < nextStepAt) return;

        if (stage == 1)
        {
            ScreenCapture.CaptureScreenshot(OutputPath, 1);
            SessionState.SetInt(StageKey, 2);
            nextStepAt = EditorApplication.timeSinceStartup + 1d;
            return;
        }

        SessionState.SetBool(PendingKey, false);
        SessionState.EraseInt(StageKey);
        EditorApplication.ExitPlaymode();
        AssetDatabase.Refresh();
        Debug.Log($"RPG_HOME_STORAGE_CHEST_CAPTURE_SUCCESS — {OutputPath}");
    }
}

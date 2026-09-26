using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ScenePortalSetup
{
    const string TownPath = "Assets/Scenes/TownHub.unity";
    const string HomeInteriorPath = "Assets/Scenes/HomeInterior.unity";
    const string InfirmaryInteriorPath = "Assets/Scenes/InfirmaryInterior.unity";
    const string ExpeditionPath = "Assets/Scenes/ExpeditionField.unity";

    [MenuItem("RPG/Connect Town And Expedition")]
    public static void Connect()
    {
        var town = EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
        ConfigurePortal("Town Exit Wall", GameScene.ExpeditionField, "Click to begin an expedition");
        EditorSceneManager.SaveScene(town);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TownPath, true),
            new EditorBuildSettingsScene(HomeInteriorPath, true),
            new EditorBuildSettingsScene(InfirmaryInteriorPath, true),
            new EditorBuildSettingsScene(ExpeditionPath, true)
        };

        EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("RPG_SCENE_PORTALS_SUCCESS");
    }

    static void ConfigurePortal(string objectName, GameScene destination, string prompt)
    {
        var target = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == objectName);
        if (!target)
        {
            Debug.LogError($"Could not find portal object '{objectName}'.");
            return;
        }

        var portal = target.GetComponent<ScenePortal>() ?? target.AddComponent<ScenePortal>();
        portal.destinationScene = destination;
        portal.requiresExpeditionTime = destination == GameScene.ExpeditionField;
        portal.prompt = prompt;
        EditorUtility.SetDirty(portal);
    }
}

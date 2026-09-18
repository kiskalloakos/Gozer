using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ScenePortalSetup
{
    const string TownPath = "Assets/Scenes/TownHub.unity";
    const string TestRoomPath = "Assets/Scenes/RogueTestRoom.unity";

    [MenuItem("RPG/Connect Town And Test Room")]
    public static void Connect()
    {
        var town = EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
        ConfigurePortal("Expedition gate", "RogueTestRoom", "[E]  Enter the expedition test room");
        EditorSceneManager.SaveScene(town);

        var testRoom = EditorSceneManager.OpenScene(TestRoomPath, OpenSceneMode.Single);
        ConfigurePortal("Crate B", "TownHub", "[E]  Extract to town");
        EditorSceneManager.SaveScene(testRoom);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TownPath, true),
            new EditorBuildSettingsScene(TestRoomPath, true)
        };

        EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("RPG_SCENE_PORTALS_SUCCESS");
    }

    static void ConfigurePortal(string objectName, string destination, string prompt)
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
        portal.prompt = prompt;
        EditorUtility.SetDirty(portal);
    }
}

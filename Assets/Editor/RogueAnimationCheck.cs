using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RogueAnimationCheck
{
    public static void Verify()
    {
        AssetDatabase.ImportAsset("Assets/Resources/RogueAnimation/Shoot.png", ImportAssetOptions.ForceUpdate);
        EditorSceneManager.OpenScene("Assets/Scenes/RogueTestRoom.unity");
        var player = Object.FindFirstObjectByType<RogueController>();
        if (!player || !player.visual) throw new System.Exception("Player setup missing");
        var tex = Resources.Load<Texture2D>("RogueAnimation/Shoot");
        if (!tex || tex.filterMode != FilterMode.Point) throw new System.Exception("Shooting animation import invalid");
        Debug.Log("Shoot animation texture: " + tex.width + "x" + tex.height);
        Debug.Log("RPG_SHOOTING_CHECK_SUCCESS");
    }
}

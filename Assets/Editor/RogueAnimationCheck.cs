using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RogueAnimationCheck
{
    public static void Verify()
    {
        AssetDatabase.ImportAsset("Assets/Resources/RogueAnimation/Run.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Resources/RogueAnimation/Shoot.png", ImportAssetOptions.ForceUpdate);
        EditorSceneManager.OpenScene("Assets/Scenes/RogueTestRoom.unity");
        var player = Object.FindFirstObjectByType<RogueController>();
        if (!player || !player.visual) throw new System.Exception("Player setup missing");
        foreach (string name in new[] { "Run", "Shoot" })
        {
            var tex = Resources.Load<Texture2D>("RogueAnimation/" + name);
            if (!tex || tex.filterMode != FilterMode.Point) throw new System.Exception("Animation import invalid");
            Debug.Log(name + " animation texture: " + tex.width + "x" + tex.height);
        }
        Debug.Log("RPG_ANIMATION_CHECK_SUCCESS");
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ExpeditionPlayerCombat))]
public sealed class ExpeditionPlayerCombatEditor : Editor
{
    static readonly string[] CombatScenes =
    {
        "Assets/Scenes/TownHub.unity",
        "Assets/Scenes/ExpeditionField.unity"
    };
    static readonly InventoryItemId[] Tools = ItemInventory.ToolItemIds.ToArray();
    static readonly string[] Labels = Tools.Select(GetToolLabel).ToArray();
    static int selectedToolIndex;

    static string GetToolLabel(InventoryItemId item)
    {
        string name = item.ToString();
        if (item == InventoryItemId.Axe) return "Wooden Axe";
        return string.Concat(name.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? " " + character : character.ToString()));
    }

    public override void OnInspectorGUI()
    {
        var combat = (ExpeditionPlayerCombat)target;
        combat.PrepareProfilesForEditor();
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        if (Tools.Length == 0)
        {
            EditorGUILayout.HelpBox("No tools are registered in ItemInventory.ToolItemIds.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        selectedToolIndex = Mathf.Clamp(selectedToolIndex, 0, Tools.Length - 1);
        selectedToolIndex = EditorGUILayout.Popup("Combat Tool to Configure", selectedToolIndex, Labels);
        SerializedProperty profiles = serializedObject.FindProperty("toolProfiles");
        SerializedProperty profile = FindProfile(profiles, Tools[selectedToolIndex]);
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(Labels[selectedToolIndex] + " Combat", EditorStyles.boldLabel);
        if (profile == null)
            EditorGUILayout.HelpBox("Profile is missing. Reopen the Inspector to rebuild it.", MessageType.Warning);
        else
        {
            SerializedProperty iterator = profile.Copy();
            SerializedProperty end = profile.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                if (iterator.name != "item") EditorGUILayout.PropertyField(iterator, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
        combat.RefreshEditorPreview();
    }

    static SerializedProperty FindProfile(SerializedProperty profiles, InventoryItemId item)
    {
        if (profiles == null || !profiles.isArray) return null;
        for (int i = 0; i < profiles.arraySize; i++)
        {
            SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
            SerializedProperty itemProperty = profile.FindPropertyRelative("item");
            if (itemProperty != null && itemProperty.enumValueIndex == (int)item) return profile;
        }
        return null;
    }

    [MenuItem("RPG/Configure Tool Combat Across Scenes")]
    static void ConfigureCombatAcrossScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previousScene = EditorSceneManager.GetActiveScene().path;
        var sourceScene = EditorSceneManager.OpenScene(CombatScenes[0], OpenSceneMode.Single);
        var sourcePlayer = FindPlayer(sourceScene);
        if (!sourcePlayer)
        {
            Debug.LogError($"Could not find Player in {CombatScenes[0]}.");
            return;
        }
        var sourceCombat = sourcePlayer.GetComponent<ExpeditionPlayerCombat>();
        if (!sourceCombat) sourceCombat = sourcePlayer.gameObject.AddComponent<ExpeditionPlayerCombat>();
        sourceCombat.PrepareProfilesForEditor();
        EditorUtility.SetDirty(sourceCombat);
        EditorSceneManager.MarkSceneDirty(sourceScene);
        EditorSceneManager.SaveScene(sourceScene);

        var sourceProperties = new SerializedObject(sourceCombat);
        foreach (string scenePath in CombatScenes.Skip(1))
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            var player = FindPlayer(scene);
            if (!player)
            {
                Debug.LogError($"Could not find Player in {scenePath}.");
                EditorSceneManager.CloseScene(scene, true);
                continue;
            }
            var targetCombat = player.GetComponent<ExpeditionPlayerCombat>();
            if (!targetCombat) targetCombat = player.gameObject.AddComponent<ExpeditionPlayerCombat>();
            targetCombat.PrepareProfilesForEditor();
            var targetProperties = new SerializedObject(targetCombat);
            targetProperties.CopyFromSerializedProperty(sourceProperties.FindProperty("toolProfiles"));
            targetProperties.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetCombat);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }
        if (!string.IsNullOrEmpty(previousScene) && previousScene != CombatScenes[0])
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        Debug.Log("RPG_TOOL_COMBAT_CONFIGURED — TownHub combat profiles copied to combat scenes.");
    }

    static Transform FindPlayer(UnityEngine.SceneManagement.Scene scene)
        => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => transform.name == "Player");
}

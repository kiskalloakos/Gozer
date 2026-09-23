using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ToolHeldVisual))]
public sealed class ToolHeldVisualEditor : Editor
{
    static readonly InventoryItemId[] EditableTools = ItemInventory.ToolItemIds.ToArray();
    static readonly string[] ToolLabels = EditableTools
        .Select(GetToolLabel).ToArray();

    static string GetToolLabel(InventoryItemId item)
    {
        string name = item.ToString();
        if (item == InventoryItemId.Axe) return "Wooden Axe";
        return string.Concat(name.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? " " + character : character.ToString()));
    }

    public override void OnInspectorGUI()
    {
        ((ToolHeldVisual)target).PrepareProfilesForEditor();
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        SerializedProperty selector = serializedObject.FindProperty("toolToConfigure");
        if (EditableTools.Length == 0)
        {
            EditorGUILayout.HelpBox("No tools are registered in ItemInventory.ToolItemIds.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }
        int selectedIndex = System.Array.IndexOf(EditableTools, (InventoryItemId)selector.enumValueIndex);
        if (selectedIndex < 0) selectedIndex = 0;
        int newSelectedIndex = EditorGUILayout.Popup("Tool to Configure", selectedIndex, ToolLabels);
        selector.enumValueIndex = (int)EditableTools[newSelectedIndex];

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"{ToolLabels[newSelectedIndex]} Setup", EditorStyles.boldLabel);
        SerializedProperty profiles = serializedObject.FindProperty("toolProfiles");
        SerializedProperty selectedProfile = FindProfile(profiles, EditableTools[newSelectedIndex]);
        if (selectedProfile == null)
        {
            EditorGUILayout.HelpBox("The selected tool profile is missing. Reimport the component or reopen the scene to create it.",
                MessageType.Warning);
        }
        else
        {
            DrawProfileFields(selectedProfile);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Edit-mode Preview", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showEditModePreview"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("previewFacing"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("previewWalking"));
        using (new EditorGUI.DisabledScope(!serializedObject.FindProperty("previewWalking").boolValue))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewWalkFrame"));

        DrawAttackPreview((ToolHeldVisual)target, newSelectedIndex);

        EditorGUILayout.HelpBox("Play mode displays the profile matching the currently equipped quickbar item.",
            MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }

    static void DrawAttackPreview(ToolHeldVisual heldVisual, int selectedToolIndex)
    {
        var combat = heldVisual.GetComponent<ExpeditionPlayerCombat>();
        if (!combat) return;

        var combatObject = new SerializedObject(combat);
        combatObject.Update();
        combatObject.FindProperty("previewToolIndex").intValue = selectedToolIndex;
        SerializedProperty profiles = combatObject.FindProperty("toolProfiles");
        SerializedProperty profile = FindCombatProfile(profiles, EditableTools[selectedToolIndex]);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Edit-mode Attack Preview", EditorStyles.boldLabel);
        SerializedProperty showPreview = combatObject.FindProperty("showAttackPreview");
        EditorGUILayout.PropertyField(showPreview, new GUIContent("Show Preview"));
        if (showPreview.boolValue)
        {
            EditorGUILayout.PropertyField(combatObject.FindProperty("previewAttackFacing"),
                new GUIContent("Facing"));
            int frameCount = profile != null
                ? profile.FindPropertyRelative("characterFrameCount").intValue : 1;
            SerializedProperty characterFrame = combatObject.FindProperty("previewCharacterFrame");
            characterFrame.intValue = EditorGUILayout.IntSlider("Character Frame", characterFrame.intValue,
                0, Mathf.Max(0, frameCount - 1));

            SerializedProperty previewEffect = combatObject.FindProperty("previewHitEffect");
            EditorGUILayout.PropertyField(previewEffect, new GUIContent("Preview Hit Effect"));
            if (previewEffect.boolValue && profile != null)
            {
                SerializedProperty sheet = profile.FindPropertyRelative("swooshSheet");
                SerializedProperty frameHeight = profile.FindPropertyRelative("swooshFrameHeight");
                int effectCount = sheet.objectReferenceValue && frameHeight.intValue > 0
                    ? ((Texture2D)sheet.objectReferenceValue).height / frameHeight.intValue : 1;
                SerializedProperty effectFrame = combatObject.FindProperty("previewHitEffectFrame");
                effectFrame.intValue = EditorGUILayout.IntSlider("Hit Effect Frame", effectFrame.intValue,
                    0, Mathf.Max(0, effectCount - 1));
            }
        }

        combatObject.ApplyModifiedProperties();
        combat.RefreshEditorPreview();
    }

    static SerializedProperty FindCombatProfile(SerializedProperty profiles, InventoryItemId item)
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

    static void DrawProfileFields(SerializedProperty profile)
    {
        SerializedProperty iterator = profile.Copy();
        SerializedProperty end = profile.GetEndProperty();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            if (iterator.name == "item") continue;
            EditorGUILayout.PropertyField(iterator, true);
        }
    }
}

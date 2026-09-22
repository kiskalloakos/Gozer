using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Stops a build when a scene has missing cross-scene gameplay references.</summary>
public sealed class SceneReferencesValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    [MenuItem("RPG/Validate Scene References")]
    public static void ValidateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        ValidateBuildScenes();
        Debug.Log("RPG_SCENE_REFERENCES_VALID");
    }

    public void OnPreprocessBuild(BuildReport report) => ValidateBuildScenes();

    static void ValidateBuildScenes()
    {
        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled) continue;
            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            var references = UnityEngine.Object.FindObjectsByType<SceneReferences>(FindObjectsSortMode.None);
            if (references.Length != 1)
                throw new BuildFailedException($"{scene.name} must contain exactly one SceneReferences component; found {references.Length}.");
            if (!references[0].Validate(out string problem))
                throw new BuildFailedException($"{scene.name} has invalid SceneReferences: {problem}");
        }
    }
}

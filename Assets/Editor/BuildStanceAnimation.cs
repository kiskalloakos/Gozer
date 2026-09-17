using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildStanceAnimation
{
    const string SpriteSheetPath = "Assets/Art/Characters/Rogue_Standing_Simplified_128.png.png";
    const string ClipPath = "Assets/Rogue_Stance.anim";
    const string ControllerPath = "Assets/Visual.controller";

    [InitializeOnLoadMethod]
    static void BuildOnceAfterCompilation()
    {
        const string sessionKey = "RPG.BuildStanceAnimation.Completed";
        if (SessionState.GetBool(sessionKey, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling) return;
            Build();
            SessionState.SetBool(sessionKey, true);
        };
    }

    [MenuItem("RPG/Build Simplified Stance Animation")]
    public static void Build()
    {
        var frames = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.rect.x)
            .ToArray();

        if (frames.Length != 5)
        {
            Debug.LogError($"Expected five stance sprites at {SpriteSheetPath}, found {frames.Length}.");
            return;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (!clip)
        {
            clip = new AnimationClip { name = "Rogue_Stance" };
            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        clip.frameRate = 6f;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = frames.Select((sprite, index) => new ObjectReferenceKeyframe
        {
            time = index / clip.frameRate,
            value = sprite
        }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!controller)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(child => child.state)
            .FirstOrDefault(candidate => candidate.name == "Rogue_Stance")
            ?? machine.AddState("Rogue_Stance");
        state.motion = clip;
        machine.defaultState = state;
        EditorUtility.SetDirty(controller);

        var player = Object.FindAnyObjectByType<RogueController>();
        if (!player || !player.visual)
        {
            Debug.LogError("Could not find Player's RogueController and Visual SpriteRenderer in the open scene.");
            return;
        }

        var animator = player.visual.GetComponent<Animator>();
        if (!animator) animator = player.visual.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        player.stanceAnimator = animator;

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Built the five-frame Rogue_Stance animation and assigned it to Player/Visual.");
    }
}

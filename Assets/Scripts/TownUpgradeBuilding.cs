using UnityEngine;

public class TownUpgradeBuilding : TownInteractable
{
    public SpriteRenderer levelRenderer;
    public Sprite[] levelSprites;
    [Min(0)] public int treatmentCost = 3;

    // Keep the key for compatibility with existing saves.
    public static string ProgressKey(string id) => $"Town.Building.{id}.Level";

    public override string Prompt => "Click to enter the infirmary";

    void Awake()
    {
        GameState.InstallFromRuntime();
        if (levelRenderer && levelSprites != null && levelSprites.Length > 0)
            levelRenderer.sprite = levelSprites[Mathf.Clamp(GameState.Active.infirmaryLevel - 1,
                0, levelSprites.Length - 1)];
    }

    public override void Interact()
    {
        SceneTravel.Load(GameScene.InfirmaryInterior);
    }
}

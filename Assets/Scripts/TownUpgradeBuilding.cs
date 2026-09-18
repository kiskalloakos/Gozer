using UnityEngine;

public class TownUpgradeBuilding : TownInteractable
{
    public string buildingId;
    public int maxLevel = 3;
    public int baseCost = 4;
    public string[] levelBenefits;
    public GameObject[] tierVisuals;
    public SpriteRenderer levelRenderer;
    public Sprite[] levelSprites;

    public int Level { get; private set; }
    private string LevelKey => $"Town.Building.{buildingId}.Level";
    private int UpgradeCost => baseCost + (Level - 1) * 3;

    public override string Prompt => Level >= maxLevel
        ? $"[E]  Inspect {displayName} — level {Level} (MAX)"
        : $"[E]  Upgrade {displayName} — level {Level} → {Level + 1} ({UpgradeCost} supplies)";

    void Awake()
    {
        Level = Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 1), 1, maxLevel);
        RefreshVisuals();
    }

    public override void Interact()
    {
        var hub = TownHubController.Instance;
        if (!hub) return;

        if (Level >= maxLevel)
        {
            hub.ShowNotice($"{displayName} is fully upgraded. {BenefitFor(Level)}");
            return;
        }

        if (!hub.SpendSupplies(UpgradeCost))
        {
            hub.ShowNotice($"Not enough supplies. {displayName} needs {UpgradeCost}.");
            return;
        }

        Level++;
        PlayerPrefs.SetInt(LevelKey, Level);
        PlayerPrefs.Save();
        RefreshVisuals();
        hub.ShowNotice($"{displayName} reached level {Level}! {BenefitFor(Level)}", 5f);
    }

    private string BenefitFor(int level)
    {
        if (levelBenefits == null || levelBenefits.Length == 0) return "The town grows stronger.";
        return levelBenefits[Mathf.Clamp(level - 1, 0, levelBenefits.Length - 1)];
    }

    public void RefreshVisuals()
    {
        if (tierVisuals != null)
            for (int i = 0; i < tierVisuals.Length; i++)
                if (tierVisuals[i]) tierVisuals[i].SetActive(i < Level);

        if (levelRenderer && levelSprites != null && levelSprites.Length > 0)
            levelRenderer.sprite = levelSprites[Mathf.Clamp(Level - 1, 0, levelSprites.Length - 1)];
    }
}

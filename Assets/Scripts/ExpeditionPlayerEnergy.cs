using UnityEngine;

/// <summary>
/// Persistent player energy used by attacks and future food/potion items.
/// Energy remains a float so regeneration can be smooth while attacks consume
/// full, readable lightning-bolt units in the HUD.
/// </summary>
public class ExpeditionPlayerEnergy : MonoBehaviour
{
    public const string EnergyKey = "Player.Energy";
    public const float DefaultMaxEnergy = 5f;
    public const float DefaultStartingEnergy = DefaultMaxEnergy;
    public const float DefaultRegenerationPerSecond = .15f;

    [Min(.1f)] public float maxEnergy = DefaultMaxEnergy;
    [Min(0f)] public float regenerationPerSecond = DefaultRegenerationPerSecond;

    public float CurrentEnergy { get; private set; }
    public float MaxEnergy => Mathf.Max(.1f, maxEnergy);
    public bool IsFull => CurrentEnergy >= MaxEnergy - .001f;

    void Awake()
    {
        GameState.InstallFromRuntime();
        CurrentEnergy = Mathf.Clamp(GameState.Active.energy, 0f, MaxEnergy);
    }

    void Update()
    {
        if (GameSessionFlow.IsBlockingGameplay || IsFull) return;
        Restore(regenerationPerSecond * Time.deltaTime);
    }

    public bool TryConsume(float amount)
    {
        if (amount <= 0f) return true;
        if (CurrentEnergy + .0001f < amount) return false;

        SetEnergy(CurrentEnergy - amount);
        return true;
    }

    public void Restore(float amount)
    {
        if (amount <= 0f) return;
        SetEnergy(CurrentEnergy + amount);
    }

    public void RestoreFull() => SetEnergy(MaxEnergy);

    void SetEnergy(float value)
    {
        float next = Mathf.Clamp(value, 0f, MaxEnergy);
        if (Mathf.Abs(next - CurrentEnergy) < .0001f) return;
        CurrentEnergy = next;
        GameState.Active.energy = CurrentEnergy;
    }
}

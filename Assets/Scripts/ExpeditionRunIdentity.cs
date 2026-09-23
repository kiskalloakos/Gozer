using System;
using UnityEngine;

public enum ExpeditionSeedSource
{
    FixedField,
    NewRun,
    Requested,
    Replay,
    Daily,
    CommandLine,
    CoopHost
}

[Serializable]
public struct ExpeditionRunIdentity
{
    public const int CurrentSchemaVersion = 1;
    public const int CurrentGeneratorVersion = 1;
    public const string PlayerPrefsKey = "Expedition.RunIdentity";

    public int schemaVersion;
    public int generatorVersion;
    public int requestedSeed;
    public int layoutSeed;
    public int layoutAttempt;
    public int threatLevel;
    public string source;
    public string startedUtc;
    public string dailyDate;

    public static ExpeditionRunIdentity Current { get; private set; }
    public static bool HasCurrent { get; private set; }
    public static string Serialized => HasCurrent ? JsonUtility.ToJson(Current) : "";

    public static void Begin(int seed, ExpeditionSeedSource seedSource, int threatLevel = 0, string dailyDate = "")
    {
        Current = new ExpeditionRunIdentity
        {
            schemaVersion = CurrentSchemaVersion,
            generatorVersion = CurrentGeneratorVersion,
            requestedSeed = seed,
            layoutSeed = seed,
            layoutAttempt = 0,
            threatLevel = Mathf.Max(0, threatLevel),
            source = seedSource.ToString(),
            startedUtc = DateTime.UtcNow.ToString("O"),
            dailyDate = dailyDate ?? ""
        };
        HasCurrent = true;
        Persist();
        if (GameState.Active != null) GameState.Active.expeditionRunIdentity = JsonUtility.ToJson(Current);
        Debug.Log($"EXPEDITION_RUN seed={seed} source={Current.source} generator={Current.generatorVersion} "
            + $"threat={Current.threatLevel} daily={Current.dailyDate}");
    }

    public static void RecordLayout(int layoutSeed, int layoutAttempt, int threatLevel)
    {
        if (!HasCurrent) return;
        var identity = Current;
        identity.layoutSeed = layoutSeed;
        identity.layoutAttempt = Mathf.Max(0, layoutAttempt);
        identity.threatLevel = Mathf.Max(0, threatLevel);
        Current = identity;
        Persist();
        if (GameState.Active != null) GameState.Active.expeditionRunIdentity = JsonUtility.ToJson(Current);
        Debug.Log($"EXPEDITION_LAYOUT seed={Current.requestedSeed} layoutSeed={layoutSeed} "
            + $"attempt={layoutAttempt} generator={Current.generatorVersion}");
    }

    public static void RestoreSerialized(string json)
    {
        if (string.IsNullOrEmpty(json)) { Clear(); return; }
        try
        {
            Current = JsonUtility.FromJson<ExpeditionRunIdentity>(json);
            HasCurrent = Current.schemaVersion > 0;
            if (HasCurrent) Persist();
            else Clear();
            if (GameState.Active != null) GameState.Active.expeditionRunIdentity = HasCurrent ? JsonUtility.ToJson(Current) : "";
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not restore expedition run identity: {exception.Message}");
            Clear();
        }
    }

    public static void Clear()
    {
        HasCurrent = false;
        Current = default;
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        if (GameState.Active != null) GameState.Active.expeditionRunIdentity = "";
    }

    static void Persist()
    {
        PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(Current));
        PlayerPrefs.Save();
    }
}

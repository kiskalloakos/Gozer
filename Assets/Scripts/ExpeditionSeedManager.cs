using System;
using UnityEngine;

public static class ExpeditionSeedManager
{
    public const string LastSeedKey = "Expedition.LastSeed";
    const string PendingSeedKey = "Expedition.PendingSeed";
    const string HasPendingSeedKey = "Expedition.HasPendingSeed";
    const string CommandLinePrefix = "-expedition-seed=";

    static bool commandLineSeedConsumed;

    public static int LastSeed => PlayerPrefs.GetInt(LastSeedKey, 0);
    public static bool HasPendingSeed => PlayerPrefs.GetInt(HasPendingSeedKey, 0) == 1;

    public static void QueueSeed(int seed)
    {
        PlayerPrefs.SetInt(PendingSeedKey, seed);
        PlayerPrefs.SetInt(HasPendingSeedKey, 1);
        PlayerPrefs.Save();
    }

    public static bool QueueLastSeed()
    {
        if (!PlayerPrefs.HasKey(LastSeedKey)) return false;
        QueueSeed(LastSeed);
        return true;
    }

    public static int BeginRun()
    {
        int seed;
        string source;
        if (TryConsumeCommandLineSeed(out seed))
        {
            source = "command line";
        }
        else if (PlayerPrefs.GetInt(HasPendingSeedKey, 0) == 1)
        {
            seed = PlayerPrefs.GetInt(PendingSeedKey);
            PlayerPrefs.DeleteKey(PendingSeedKey);
            PlayerPrefs.DeleteKey(HasPendingSeedKey);
            source = "requested seed";
        }
        else
        {
            seed = Guid.NewGuid().GetHashCode();
            source = "new run";
        }

        PlayerPrefs.SetInt(LastSeedKey, seed);
        PlayerPrefs.Save();
        Debug.Log($"EXPEDITION_SEED={seed} ({source})");
        return seed;
    }

    static bool TryConsumeCommandLineSeed(out int seed)
    {
        seed = 0;
        if (commandLineSeedConsumed) return false;
        commandLineSeedConsumed = true;

        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (!argument.StartsWith(CommandLinePrefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(argument.Substring(CommandLinePrefix.Length), out seed)) return true;
            Debug.LogWarning($"Ignoring invalid expedition seed argument '{argument}'.");
        }
        return false;
    }
}

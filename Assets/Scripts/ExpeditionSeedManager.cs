using System;
using UnityEngine;

public static class ExpeditionSeedManager
{
    public const string LastSeedKey = "Expedition.LastSeed";
    const string PendingSeedKey = "Expedition.PendingSeed";
    const string HasPendingSeedKey = "Expedition.HasPendingSeed";
    const string PendingSourceKey = "Expedition.PendingSeedSource";
    const string PendingDailyDateKey = "Expedition.PendingDailyDate";
    const string CommandLinePrefix = "-expedition-seed=";

    static bool commandLineSeedConsumed;

    public static int LastSeed => GameState.Active != null ? GameState.Active.lastSeed : PlayerPrefs.GetInt(LastSeedKey, 0);
    public static bool HasPendingSeed => PlayerPrefs.GetInt(HasPendingSeedKey, 0) == 1;

    public static void QueueSeed(int seed)
    {
        QueueSeed(seed, ExpeditionSeedSource.Requested);
    }

    static void QueueSeed(int seed, ExpeditionSeedSource source, string dailyDate = "")
    {
        PlayerPrefs.SetInt(PendingSeedKey, seed);
        PlayerPrefs.SetInt(HasPendingSeedKey, 1);
        PlayerPrefs.SetString(PendingSourceKey, source.ToString());
        PlayerPrefs.SetString(PendingDailyDateKey, dailyDate ?? "");
        PlayerPrefs.Save();
    }

    public static bool QueueLastSeed()
    {
        if (!PlayerPrefs.HasKey(LastSeedKey)) return false;
        QueueSeed(LastSeed, ExpeditionSeedSource.Replay);
        return true;
    }

    public static void QueueDailySeed(DateTime utcDate)
    {
        string date = utcDate.ToString("yyyy-MM-dd");
        QueueSeed(StableDateSeed(date), ExpeditionSeedSource.Daily, date);
    }

    public static void QueueTodaySeed() => QueueDailySeed(DateTime.UtcNow.Date);

    /// <summary>Reserved for a future host-authoritative co-op handshake.</summary>
    public static void QueueCoopSeed(int hostSeed)
        => QueueSeed(hostSeed, ExpeditionSeedSource.CoopHost);

    public static int BeginRun()
    {
        int seed;
        ExpeditionSeedSource source;
        string dailyDate = "";
        if (TryConsumeCommandLineSeed(out seed))
        {
            source = ExpeditionSeedSource.CommandLine;
        }
        else if (PlayerPrefs.GetInt(HasPendingSeedKey, 0) == 1)
        {
            seed = PlayerPrefs.GetInt(PendingSeedKey);
            PlayerPrefs.DeleteKey(PendingSeedKey);
            PlayerPrefs.DeleteKey(HasPendingSeedKey);
            Enum.TryParse(PlayerPrefs.GetString(PendingSourceKey, ExpeditionSeedSource.Requested.ToString()), out source);
            dailyDate = PlayerPrefs.GetString(PendingDailyDateKey, "");
            PlayerPrefs.DeleteKey(PendingSourceKey);
            PlayerPrefs.DeleteKey(PendingDailyDateKey);
        }
        else
        {
            seed = Guid.NewGuid().GetHashCode();
            source = ExpeditionSeedSource.NewRun;
        }

        GameState.InstallFromRuntime();
        GameState.Active.lastSeed = seed;
        PlayerPrefs.SetInt(LastSeedKey, seed); // retained as a one-time compatibility mirror for older tools
        PlayerPrefs.Save();
        ExpeditionRunIdentity.Begin(seed, source, ExpeditionRunProgression.ThreatLevel, dailyDate);
        Debug.Log($"EXPEDITION_SEED={seed} ({source})");
        return seed;
    }

    static int StableDateSeed(string date)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (char character in date)
            {
                hash ^= character;
                hash *= 16777619u;
            }
            return (int)(hash & 0x7fffffff);
        }
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

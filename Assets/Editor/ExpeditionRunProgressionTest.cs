using System;
using UnityEditor;
using UnityEngine;

public static class ExpeditionRunProgressionTest
{
    [MenuItem("RPG/Tests/Validate Run Progression")]
    public static void RunFromMenu()
    {
        bool hadCompletedRuns = PlayerPrefs.HasKey(ExpeditionRunProgression.CompletedRunsKey);
        int previousCompletedRuns = PlayerPrefs.GetInt(ExpeditionRunProgression.CompletedRunsKey, 0);
        bool hadPendingThreat = PlayerPrefs.HasKey(ExpeditionRunProgression.PendingThreatIncreaseKey);
        int previousPendingThreat = PlayerPrefs.GetInt(ExpeditionRunProgression.PendingThreatIncreaseKey, 0);

        try
        {
            PlayerPrefs.DeleteKey(ExpeditionRunProgression.CompletedRunsKey);
            PlayerPrefs.DeleteKey(ExpeditionRunProgression.PendingThreatIncreaseKey);
            Assert(ExpeditionRunProgression.CompletedRuns == 0, "A new save must begin with zero completed runs.");
            Assert(ExpeditionRunProgression.ThreatLevel == 1, "A new save must begin at threat level 1.");
            Assert(ExpeditionRunProgression.RegularEnemyBonus == 0, "Threat 1 must not add run-based enemies.");

            ExpeditionRunProgression.RecordSuccessfulProceduralRun();
            Assert(ExpeditionRunProgression.CompletedRuns == 1, "The first success was not recorded.");
            Assert(ExpeditionRunProgression.ThreatLevel == 2, "The first success must unlock threat level 2.");
            Assert(ExpeditionRunProgression.RegularEnemyBonus == 1, "The next run must add one regular enemy.");
            Assert(ExpeditionRunProgression.ExtractionReinforcementBonus == 0,
                "The first success must not yet add an extraction reinforcement.");
            Assert(ExpeditionRunProgression.TryConsumePendingThreatIncrease(out int pendingThreat)
                && pendingThreat == 2, "The town notification must report threat level 2.");
            Assert(!ExpeditionRunProgression.TryConsumePendingThreatIncrease(out _),
                "The town notification must only be consumed once.");

            ExpeditionRunProgression.RecordSuccessfulProceduralRun();
            Assert(ExpeditionRunProgression.CompletedRuns == 2, "The second success was not recorded.");
            Assert(ExpeditionRunProgression.RegularEnemyBonus == 2, "The third run must add two regular enemies.");
            Assert(ExpeditionRunProgression.ExtractionReinforcementBonus == 1,
                "Two successes must add one extraction reinforcement.");
            Debug.Log("RPG_RUN_PROGRESSION_SUCCESS: persistent threat escalation validated.");
        }
        finally
        {
            Restore(ExpeditionRunProgression.CompletedRunsKey, hadCompletedRuns, previousCompletedRuns);
            Restore(ExpeditionRunProgression.PendingThreatIncreaseKey, hadPendingThreat, previousPendingThreat);
            PlayerPrefs.Save();
        }
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void Restore(string key, bool existed, int value)
    {
        if (existed) PlayerPrefs.SetInt(key, value);
        else PlayerPrefs.DeleteKey(key);
    }
}

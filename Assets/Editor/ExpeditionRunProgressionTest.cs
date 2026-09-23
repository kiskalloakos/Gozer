using System;
using UnityEditor;
using UnityEngine;

public static class ExpeditionRunProgressionTest
{
    [MenuItem("RPG/Tests/Validate Run Progression")]
    public static void RunFromMenu()
    {
        GameState.InstallFromRuntime();
        GameState previousState = GameState.Active;

        try
        {
            var recovered = new GameState
            {
                resultSucceeded = true,
                expeditionRunIdentity = JsonUtility.ToJson(new ExpeditionRunIdentity
                {
                    schemaVersion = ExpeditionRunIdentity.CurrentSchemaVersion,
                    source = ExpeditionSeedSource.FixedField.ToString()
                })
            };
            ExpeditionRunProgression.RecoverCompletedTutorial(recovered);
            Assert(recovered.completedRuns == 1, "A saved tutorial success must recover its missing progression.");
            ExpeditionRunProgression.RecoverCompletedTutorial(recovered);
            Assert(recovered.completedRuns == 1, "Tutorial recovery must not award progress twice.");

            GameState.Replace(new GameState());
            Assert(ExpeditionRunProgression.CompletedRuns == 0, "A new save must begin with zero completed runs.");
            Assert(ExpeditionRunProgression.ThreatLevel == 1, "A new save must begin at threat level 1.");
            Assert(!ExpeditionRunProgression.HasCompletedTutorial, "A new save must enter the fixed tutorial field.");
            Assert(ExpeditionRunProgression.RegularEnemyBonus == 0, "Threat 1 must not add run-based enemies.");

            ExpeditionRunProgression.RecordSuccessfulRun();
            Assert(ExpeditionRunProgression.CompletedRuns == 1, "The first success was not recorded.");
            Assert(ExpeditionRunProgression.ThreatLevel == 2, "The first success must unlock threat level 2.");
            Assert(ExpeditionRunProgression.HasCompletedTutorial, "The first success must unlock the seeded field.");
            ExpeditionRunResult.RecordSuccess(0, ExpeditionPlayerHealth.DefaultMaxHealthUnits);
            Assert(ExpeditionRunResult.TryConsume(out var result) && result.nextThreat == 2,
                "The tutorial result must announce Threat 2.");
            Assert(ExpeditionRunProgression.RegularEnemyBonus == 1, "The next run must add one regular enemy.");
            Assert(ExpeditionRunProgression.ExtractionReinforcementBonus == 0,
                "The first success must not yet add an extraction reinforcement.");
            Assert(ExpeditionRunProgression.TryConsumePendingThreatIncrease(out int pendingThreat)
                && pendingThreat == 2, "The town notification must report threat level 2.");
            Assert(!ExpeditionRunProgression.TryConsumePendingThreatIncrease(out _),
                "The town notification must only be consumed once.");

            ExpeditionRunProgression.RecordSuccessfulRun();
            Assert(ExpeditionRunProgression.CompletedRuns == 2, "The second success was not recorded.");
            Assert(ExpeditionRunProgression.RegularEnemyBonus == 2, "The third run must add two regular enemies.");
            Assert(ExpeditionRunProgression.ExtractionReinforcementBonus == 1,
                "Two successes must add one extraction reinforcement.");
            Debug.Log("RPG_RUN_PROGRESSION_SUCCESS: persistent threat escalation validated.");
        }
        finally
        {
            GameState.Replace(previousState);
        }
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

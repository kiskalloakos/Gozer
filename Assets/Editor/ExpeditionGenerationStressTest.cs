using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class ExpeditionGenerationStressTest
{
    const int SeedCount = 100;
    static readonly Vector2 PlayerSpawn = new Vector2(0f, -18f);

    [MenuItem("RPG/Tests/Run 100 Expedition Seeds")]
    public static void RunFromMenu()
    {
        Run(1, false);
    }

    public static void RunFromCommandLine()
    {
        Run(1, true);
    }

    public static void RunNextHundredFromCommandLine()
    {
        Run(101, true);
    }

    [MenuItem("RPG/Tests/Run Expedition Seeds 101-200")]
    public static void RunSeeds101Through200FromMenu()
    {
        Run(101, false);
    }

    [MenuItem("RPG/Tests/Run Expedition Seeds 201-300")]
    public static void RunSeeds201Through300FromMenu()
    {
        Run(201, false);
    }

    [MenuItem("RPG/Tests/Run Expedition Seeds 301-400")]
    public static void RunSeeds301Through400FromMenu()
    {
        Run(301, false);
    }

    [MenuItem("RPG/Tests/Run Expedition Seeds 401-500")]
    public static void RunSeeds401Through500FromMenu()
    {
        Run(401, false);
    }

    static void Run(int firstSeed, bool exitWhenFinished)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            int finalSeed = firstSeed + SeedCount - 1;
            for (int seed = firstSeed; seed <= finalSeed; seed++)
            {
                if (!ExpeditionLayoutPlanner.TryCreateValid(seed, PlayerSpawn, out var plan))
                    throw new InvalidOperationException($"Seed {seed} failed to produce a connected layout.");
                if (!ExpeditionLayoutPlanner.TryCreateValid(seed, PlayerSpawn, out var replay))
                    throw new InvalidOperationException($"Seed {seed} failed to produce a replay layout.");
                if (!ExpeditionLayoutPlanner.IsConnected(plan,
                        ExpeditionLayoutPlanner.DefaultHalfWidth,
                        ExpeditionLayoutPlanner.DefaultHalfHeight))
                    throw new InvalidOperationException($"Seed {seed} failed its final connectivity check.");
                if (!ExpeditionLayoutPlanner.IsInsideArena(plan.ExtractionPosition,
                        ExpeditionLayoutPlanner.DefaultHalfWidth,
                        ExpeditionLayoutPlanner.DefaultHalfHeight))
                    throw new InvalidOperationException($"Seed {seed} placed extraction outside the arena.");
                if (Vector2.Distance(PlayerSpawn, plan.ExtractionPosition) < 42f)
                    throw new InvalidOperationException($"Seed {seed} placed extraction too close to spawn.");
                if (plan.TreePositions.Count != ExpeditionLayoutPlanner.DefaultTreeCount)
                    throw new InvalidOperationException($"Seed {seed} produced {plan.TreePositions.Count} trees instead of {ExpeditionLayoutPlanner.DefaultTreeCount}.");
                if (plan.GrovePositions.Count < 8 || plan.GrovePositions.Count > 14)
                    throw new InvalidOperationException($"Seed {seed} produced an invalid {plan.GrovePositions.Count}-tree extraction grove.");
                AssertReproducible(seed, plan, replay);
                foreach (Vector2 tree in plan.TreePositions)
                {
                    if (!ExpeditionLayoutPlanner.IsInsideArena(tree,
                            ExpeditionLayoutPlanner.DefaultHalfWidth,
                            ExpeditionLayoutPlanner.DefaultHalfHeight))
                        throw new InvalidOperationException($"Seed {seed} placed a tree outside the arena.");
                    if (Vector2.Distance(tree, PlayerSpawn) < 7f)
                        throw new InvalidOperationException($"Seed {seed} placed a tree inside the spawn clearing.");
                }
                foreach (Vector2 tree in plan.GrovePositions)
                    if (!ExpeditionLayoutPlanner.IsInsideArena(tree,
                            ExpeditionLayoutPlanner.DefaultHalfWidth,
                            ExpeditionLayoutPlanner.DefaultHalfHeight))
                        throw new InvalidOperationException($"Seed {seed} placed grove scenery outside the arena.");
                AssertNoSceneryOverlaps(seed, plan);
            }

            stopwatch.Stop();
            Debug.Log($"RPG_EXPEDITION_STRESS_SUCCESS: seeds {firstSeed}-{finalSeed} " +
                $"({SeedCount} deterministic layouts) validated in {stopwatch.ElapsedMilliseconds} ms.");
            if (exitWhenFinished) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (exitWhenFinished) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void AssertReproducible(int seed, ExpeditionLayoutPlan first, ExpeditionLayoutPlan replay)
    {
        if (first.Attempt != replay.Attempt || first.LayoutSeed != replay.LayoutSeed
            || first.ExtractionPosition != replay.ExtractionPosition
            || first.TreePositions.Count != replay.TreePositions.Count
            || first.GrovePositions.Count != replay.GrovePositions.Count)
            throw new InvalidOperationException($"Seed {seed} did not reproduce its layout metadata.");

        for (int i = 0; i < first.TreePositions.Count; i++)
            if (first.TreePositions[i] != replay.TreePositions[i])
                throw new InvalidOperationException($"Seed {seed} changed tree {i} during replay.");
        for (int i = 0; i < first.GrovePositions.Count; i++)
            if (first.GrovePositions[i] != replay.GrovePositions[i])
                throw new InvalidOperationException($"Seed {seed} changed grove tree {i} during replay.");
    }

    static void AssertNoSceneryOverlaps(int seed, ExpeditionLayoutPlan plan)
    {
        const float minimumSpacing = .75f;
        float minimumSquared = minimumSpacing * minimumSpacing;
        var allScenery = new System.Collections.Generic.List<Vector2>(plan.TreePositions);
        allScenery.AddRange(plan.GrovePositions);
        for (int i = 0; i < allScenery.Count; i++)
        for (int j = i + 1; j < allScenery.Count; j++)
            if ((allScenery[i] - allScenery[j]).sqrMagnitude < minimumSquared)
                throw new InvalidOperationException($"Seed {seed} overlaps scenery objects {i} and {j}.");
    }
}

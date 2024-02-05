using Game.AttackerStats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.Random;
using static Game.AttackerStats.AttackerStats;
using Random = Utils.Random.Random;

namespace BattleSimulation.Control
{
    public class WaveGenerator : MonoBehaviour
    {
        [SerializeField] AttackerStats[] availableAttackers;
        [SerializeField] int maxWaveLengthTicks;
        [SerializeField] float parallelWaveChance;
        [SerializeField] float pathDropChance;
        [SerializeField] int maxBatchCount;
        [SerializeField] int parallelMinCount;
        // TODO: set from level settings
        public int paths;
        public Random random = new(0);
        public float baseValueRate;
        public float baseEffectiveValueBuffer;
        public float linearScaling;
        public float quadraticScaling;
        public float cubicScaling;
        public float exponentialScalingBase;
        [SerializeField] float bufferLeft;

        [Serializable]
        public class Batch
        {
            public AttackerStats?[] typePerPath;
            public int count;
            public Spacing spacing;

            public Batch(int count, Spacing spacing, AttackerStats?[] paths)
            {
                this.count = count;
                this.spacing = spacing;
                typePerPath = paths;
            }
        }

        [Serializable]
        public class Wave
        {
            public List<Batch> batches;
            public Wave(params Batch[] batches) => this.batches = new(batches);
        }

        [SerializeField] List<Wave> waves;

        public Wave GetWave(int number)
        {
            number--;
            while (number >= waves.Count)
                waves.Add(GenerateWave(waves.Count));
            return waves[number];
        }

        Wave GenerateWave(int number)
        {
            float scaling = linearScaling * number + quadraticScaling * number * number + cubicScaling * number * number * number + Mathf.Pow(exponentialScalingBase, number);
            float valueRate = baseValueRate * scaling;
            bufferLeft += baseEffectiveValueBuffer * scaling;
            print($"scaling {scaling}, rate {valueRate}, buffer {bufferLeft}");
            return random.Float() < parallelWaveChance ? GenerateParallelWave(valueRate) : GenerateSequentialWave(valueRate);
        }

        Wave GenerateSequentialWave(float valueRate)
        {
            (int selectedPathCount, bool[] selectedPaths) = SelectPaths();
            List<Batch> batches = new();
            int durationLeft = maxWaveLengthTicks;
            for (int i = 0; i < maxBatchCount; i++)
            {
                durationLeft -= Spacing.BatchSpacing.GetTicks();
                Batch? b = TryMakeBatch(selectedPathCount, selectedPaths, valueRate, ref durationLeft, i == maxBatchCount - 1);
                if (b == null)
                    break;
                batches.Add(b);
            }
            return new(batches.ToArray());
        }

        (int, bool[]) SelectPaths()
        {
            bool[] selectedPaths = new bool[paths];
            int forcedPath = random.Int(paths);
            int selectedPathCount = 0;
            for (int i = 0; i < paths; i++)
            {
                if (i != forcedPath && random.Float() < pathDropChance)
                    continue;
                selectedPaths[i] = true;
                selectedPathCount++;
            }

            return (selectedPathCount, selectedPaths);
        }

        Batch? TryMakeBatch(int pathCount, bool[] selectedPaths, float valueRate, ref int durationLeft, bool forceMaxCount)
        {
            var dur = durationLeft;
            var filteredAttackers = availableAttackers.Where(a =>
                a.MaxValueRate(pathCount) > valueRate
                && a.MinEffectiveValue(valueRate, pathCount) < bufferLeft
                && a.MaxEffectiveValue(valueRate, pathCount, dur) >= bufferLeft / 2
                );
            WeightedRandomSet<AttackerStats> selection = new(filteredAttackers.Select(a => (a, a.weight)), random.NewSeed());
            while (true)
            {
                if (selection.Count == 0)
                    return null;
                var selected = selection.PopRandom();
                var lastUnitValue = selected.MinEffectiveValue(valueRate, pathCount);
                bufferLeft -= lastUnitValue;
                var minSpacing = selected.MinFeasibleSpacing(valueRate, pathCount, bufferLeft);
                var maxSpacing = selected.MaxFeasibleSpacing(valueRate, pathCount, durationLeft);
                if (minSpacing > maxSpacing)
                    continue;
                var spacing = (Spacing)random.Int((int)minSpacing, (int)maxSpacing + 1);
                var unitValue = selected.GetEffectiveValue(spacing, pathCount, valueRate);
                var maxExtraCount = Mathf.Min(Mathf.FloorToInt(Mathf.Min(bufferLeft / unitValue, 1000)), durationLeft / spacing.GetTicks());
                var extraCount = (forceMaxCount || selection.Count == 0) ? maxExtraCount : random.Int(maxExtraCount + 1);
                durationLeft -= extraCount * spacing.GetTicks();
                bufferLeft -= extraCount * unitValue;
                var types = new AttackerStats?[paths];
                for (int i = 0; i < paths; i++)
                {
                    if (selectedPaths[i])
                        types[i] = selected;
                }

                return new(extraCount + 1, spacing, types);
            }
        }

        Wave GenerateParallelWave(float valueRate)
        {
            var filteredAttackers = availableAttackers.Where(a => a.MinEffectiveValue(valueRate, 1) * parallelMinCount < bufferLeft);
            WeightedRandomSet<AttackerStats> selection = new(filteredAttackers.Select(a => (a, a.weight)), random.NewSeed());
            if (selection.Count == 0)
                throw new();
            int[] pathSelection = Enumerable.Range(0, paths).ToArray();
            random.Shuffle(pathSelection);
            for (int r = 0; r < 5; r++)
            {
                var types = new AttackerStats?[paths];
                AttackerStats mockStats = ScriptableObject.CreateInstance<AttackerStats>();
                for (int i = 0; i < paths; i++)
                {
                    int p = pathSelection[i];
                    var selected = selection.PopRandom();
                    selection.Add(selected, selected.weight);
                    AttackerStats newMockStats = ScriptableObject.CreateInstance<AttackerStats>();
                    newMockStats.baseValue = mockStats.baseValue + selected.baseValue;
                    newMockStats.speed = (mockStats.speed * mockStats.baseValue + selected.speed * selected.baseValue) / newMockStats.baseValue;
                    newMockStats.minSpacing = (Spacing)Mathf.Max((int)mockStats.minSpacing, (int)selected.minSpacing);
                    if (newMockStats.GetEffectiveValue(Spacing.Max, 1, valueRate) * parallelMinCount > bufferLeft)
                        break;
                    mockStats = newMockStats;
                    types[p] = selected;
                }
                if (mockStats.MaxValueRate(1) > valueRate && mockStats.GetEffectiveValue(mockStats.minSpacing, paths, valueRate) * (maxWaveLengthTicks / mockStats.minSpacing.GetTicks()) >= bufferLeft * 0.8f)
                {
                    var minSpacing = mockStats.MinFeasibleSpacing(valueRate, 1, bufferLeft);
                    var maxSpacing = mockStats.MaxFeasibleSpacing(valueRate, 1, maxWaveLengthTicks);
                    if (minSpacing > maxSpacing)
                        continue;
                    var spacing = (Spacing)random.Int((int)minSpacing, (int)maxSpacing + 1);
                    var unitValue = mockStats.GetEffectiveValue(spacing, 1, valueRate);
                    var count = Mathf.Min(Mathf.FloorToInt(Mathf.Min(bufferLeft / unitValue, 1000)), maxWaveLengthTicks / spacing.GetTicks());
                    bufferLeft -= count * unitValue;
                    return new(new Batch(count, spacing, types));
                }
            }

            return GenerateSequentialWave(valueRate);
        }
    }
    internal static class AttackerStatsCalculations
    {
        public static float MinValueRate(this AttackerStats stats) => stats.GetValueRate(Spacing.Max, 1);
        public static float MaxValueRate(this AttackerStats stats, int paths) => stats.GetValueRate(stats.minSpacing, paths);
        public static float GetValueRate(this AttackerStats stats, Spacing spacing, int paths) => paths * stats.baseValue / Mathf.Sqrt(spacing.GetSeconds());
        public static float MinEffectiveValue(this AttackerStats stats, float valueRate, int paths) => stats.GetEffectiveValue(Spacing.BatchSpacing, paths, valueRate);
        public static float MaxEffectiveValue(this AttackerStats stats, float valueRate, int paths, int maxDuration) => stats.MinEffectiveValue(valueRate, paths) + stats.GetEffectiveValue(stats.minSpacing, paths, valueRate) * (maxDuration / stats.minSpacing.GetTicks());
        public static float GetEffectiveValue(this AttackerStats stats, Spacing spacing, int paths, float valueRate)
        {
            // how much value will the attacker have left, assuming it gets sqrt(spacing) seconds of damage
            // sqrt is used, because even with faster spacing, splash damage can deal with multiple attackers at once
            float valueLeft = stats.baseValue * paths - valueRate * Mathf.Sqrt(spacing.GetSeconds());
            // the remaining value gets multiplied by sqrt(speed) to get effective value
            // this reflects that faster attackers are harder to damage
            return valueLeft * Mathf.Sqrt(stats.speed);
        }

        public static Spacing MinFeasibleSpacing(this AttackerStats stats, float valueRate, int paths, float maxValue)
        {
            for (Spacing s = stats.minSpacing; s < Spacing.Max; s++)
            {
                if (stats.GetEffectiveValue(s, paths, valueRate) <= maxValue)
                    return s;
            }
            return Spacing.Max;
        }

        public static Spacing MaxFeasibleSpacing(this AttackerStats stats, float valueRate, int paths, int maxTicks)
        {
            for (Spacing s = Spacing.Max; s > stats.minSpacing; s--)
            {
                if (s.GetTicks() <= maxTicks && stats.GetValueRate(s, paths) > valueRate)
                    return s;
            }
            return stats.minSpacing;
        }
    }
}

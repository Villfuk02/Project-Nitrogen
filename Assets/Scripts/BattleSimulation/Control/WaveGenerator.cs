using System;
using System.Collections.Generic;
using System.Linq;
using Game.AttackerStats;
using UnityEngine;
using Utils.Random;
using static Game.AttackerStats.AttackerStats;
using Random = Utils.Random.Random;

namespace BattleSimulation.Control
{
    public class WaveGenerator : MonoBehaviour
    {
        [Header("Settings")]
        public bool overrideRunSettings;
        [SerializeField] AttackerStats[] availableAttackers;
        [SerializeField] int maxWaveLengthTicks;
        [SerializeField] float parallelWaveChance;
        [SerializeField] float pathDropChance;
        [SerializeField] int maxBatchCount;
        [SerializeField] int parallelMinCount;
        [Header("Settings - auto-assigned")]
        public float baseValueRate;
        public float baseEffectiveValueBuffer;
        public float linearScaling;
        public float quadraticScaling;
        public float cubicScaling;
        public float exponentialScalingBase;
        public int paths;
        public Random random;
        [Header("Runtime variables")]
        [SerializeField] float bufferLeft;
        [SerializeField] List<Wave> waves;
        readonly HashSet<AttackerStats> usedAttackers_ = new();
        AttackerStats? newAttacker_;
        [SerializeField] int currentWaveMaxLength;
        [SerializeField] int currentWaveMaxBatches;

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
            public AttackerStats? newAttacker;
            public List<Batch> batches;

            public Wave(AttackerStats? newAttacker, params Batch[] batches)
            {
                this.newAttacker = newAttacker;
                this.batches = new(batches);
            }
        }

        public Wave GetWave(int number)
        {
            number--;
            while (number >= waves.Count)
                waves.Add(GenerateWave(waves.Count));
            return waves[number];
        }

        Wave GenerateWave(int number)
        {
            newAttacker_ = null;
            currentWaveMaxLength = maxWaveLengthTicks * Mathf.Clamp(number + 1, 2, 8) / 8;
            currentWaveMaxBatches = Mathf.Min(1 + number / 2, maxBatchCount);
            float scaling = linearScaling * number + quadraticScaling * number * number + cubicScaling * number * number * number + Mathf.Pow(exponentialScalingBase, number);
            float valueRate = baseValueRate * scaling;
            bufferLeft += baseEffectiveValueBuffer * scaling;
            print($"scaling {scaling}, rate {valueRate}, buffer {bufferLeft}");
            if (random.Bool(parallelWaveChance))
            {
                Wave? w = TryGenerateParallelWave(valueRate);
                if (w is not null)
                    return w;
            }

            return GenerateSequentialWave(valueRate);
        }

        Wave GenerateSequentialWave(float valueRate)
        {
            SelectPaths(out var selectedPathCount, out var selectedPaths);
            List<Batch> batches = new();
            int durationLeft = currentWaveMaxLength;
            for (int i = 0; i < currentWaveMaxBatches; i++)
            {
                durationLeft -= Spacing.BatchSpacing.GetTicks();
                Batch? b = TryMakeBatch(selectedPathCount, selectedPaths, valueRate, ref durationLeft, i == currentWaveMaxBatches - 1);
                if (b == null)
                    break;
                batches.Add(b);
            }

            return new(newAttacker_, batches.ToArray());
        }

        void SelectPaths(out int count, out bool[] selectedPaths)
        {
            selectedPaths = new bool[paths];
            int forcedPath = random.Int(paths);
            count = 0;
            for (int i = 0; i < paths; i++)
            {
                if (i != forcedPath && random.Bool(pathDropChance))
                    continue;
                selectedPaths[i] = true;
                count++;
            }
        }

        Batch? TryMakeBatch(int pathCount, bool[] selectedPaths, float valueRate, ref int durationLeft, bool forceMaxCount)
        {
            var selection = PrepareBatchAttackerSelection(pathCount, valueRate, durationLeft);
            while (true)
            {
                if (selection.Count == 0)
                    return null;
                var selected = selection.PopRandom();
                var batch = TryMakeBatchOf(selected, pathCount, selectedPaths, valueRate, ref durationLeft, forceMaxCount || selection.Count == 0);

                if (batch == null)
                    continue;
                if (usedAttackers_.Add(selected))
                    newAttacker_ = selected;
                return batch;
            }
        }

        Batch? TryMakeBatchOf(AttackerStats selected, int pathCount, bool[] selectedPaths, float valueRate, ref int durationLeft, bool forceMaxCount)
        {
            if (!TryPickBatchSpacing(selected, pathCount, valueRate, durationLeft, out var spacing, out float lastUnitValue))
                return null;
            bufferLeft -= lastUnitValue;
            PickBatchAttackerCount(selected, pathCount, valueRate, durationLeft, forceMaxCount, spacing, out var extraCount, out float unitValue);
            durationLeft -= extraCount * spacing.GetTicks();
            bufferLeft -= extraCount * unitValue;
            var types = Enumerable.Range(0, paths).Select(i => selectedPaths[i] ? selected : null).ToArray();
            return new(extraCount + 1, spacing, types);
        }

        void PickBatchAttackerCount(AttackerStats selected, int pathCount, float valueRate, int durationLeft, bool forceMaxCount, Spacing spacing, out int extraCount, out float unitValue)
        {
            unitValue = selected.GetEffectiveValue(spacing, pathCount, valueRate);
            var maxExtraCount = 200;
            maxExtraCount = Mathf.Min(maxExtraCount, durationLeft / spacing.GetTicks());
            maxExtraCount = Mathf.Min(maxExtraCount, Mathf.FloorToInt(bufferLeft / unitValue));
            extraCount = forceMaxCount ? maxExtraCount : random.Int(maxExtraCount + 1);
        }

        bool TryPickBatchSpacing(AttackerStats selected, int pathCount, float valueRate, int durationLeft, out Spacing spacing, out float lastUnitValue)
        {
            lastUnitValue = selected.MinEffectiveValue(valueRate, pathCount);
            var minSpacing = selected.MinFeasibleSpacing(valueRate, pathCount, bufferLeft - lastUnitValue);
            var maxSpacing = selected.MaxFeasibleSpacing(valueRate, pathCount, durationLeft);
            spacing = (Spacing)random.Int((int)minSpacing, (int)maxSpacing + 1);
            return minSpacing <= maxSpacing;
        }

        WeightedRandomSet<AttackerStats> PrepareBatchAttackerSelection(int pathCount, float valueRate, int durationLeft)
        {
            IEnumerable<AttackerStats> selection = newAttacker_ != null ? usedAttackers_ : availableAttackers;
            var filteredAttackers = selection.Where(a =>
                a.MaxValueRate(pathCount) > valueRate
                && a.MinEffectiveValue(valueRate, pathCount) < bufferLeft
                && a.MaxEffectiveValue(valueRate, pathCount, durationLeft) >= bufferLeft / 2
            );
            return new(filteredAttackers.Select(a => (a, a.weight)), random.NewSeed());
        }

        Wave? TryGenerateParallelWave(float valueRate)
        {
            var selection = PrepareParallelAttackerSelection(valueRate);
            if (selection.Count == 0)
                return null;
            int[] pathSelection = Enumerable.Range(0, paths).ToArray();
            random.Shuffle(pathSelection);
            for (int r = 0; r < 5; r++)
            {
                var b = TryMakeParallelBatchOnce(valueRate, pathSelection, selection);
                if (b is null)
                    continue;

                if (newAttacker_ != null)
                    usedAttackers_.Add(newAttacker_);
                return new(newAttacker_, b);
            }

            return null;
        }

        Batch? TryMakeParallelBatchOnce(float valueRate, int[] pathSelection, WeightedRandomSet<AttackerStats> selection)
        {
            newAttacker_ = null;
            PickParallelAttackerTypes(valueRate, pathSelection, new(selection), out var types, out var mockStats);

            if (mockStats.MaxValueRate(1) <= valueRate)
                return null;
            float maxEffectiveValue = mockStats.GetEffectiveValue(mockStats.minSpacing, paths, valueRate) * (currentWaveMaxLength / mockStats.minSpacing.GetTicks());
            if (maxEffectiveValue < bufferLeft * 0.8f)
                return null;

            if (!TryPickParallelSpacing(valueRate, mockStats, out var spacing))
                return null;

            var unitValue = mockStats.GetEffectiveValue(spacing, 1, valueRate);
            int count = PickParallelAttackerCount(spacing, unitValue);
            bufferLeft -= count * unitValue;
            return new(count, spacing, types);
        }

        int PickParallelAttackerCount(Spacing spacing, float unitValue)
        {
            var count = 200;
            count = Mathf.Min(count, currentWaveMaxLength / spacing.GetTicks());
            count = Mathf.Min(count, Mathf.FloorToInt(bufferLeft / unitValue));
            return count;
        }

        bool TryPickParallelSpacing(float valueRate, AttackerStats mockStats, out Spacing spacing)
        {
            var minSpacing = mockStats.MinFeasibleSpacing(valueRate, 1, bufferLeft);
            var maxSpacing = mockStats.MaxFeasibleSpacing(valueRate, 1, currentWaveMaxLength);
            spacing = (Spacing)random.Int((int)minSpacing, (int)maxSpacing + 1);
            return minSpacing <= maxSpacing;
        }

        void PickParallelAttackerTypes(float valueRate, int[] pathSelection, WeightedRandomSet<AttackerStats> selection, out AttackerStats[] types, out AttackerStats mockStats)
        {
            types = new AttackerStats[paths];
            mockStats = ScriptableObject.CreateInstance<AttackerStats>();
            foreach (int p in pathSelection)
            {
                AttackerStats selected;
                while (true)
                {
                    selected = selection.PopRandom();
                    if (newAttacker_ == null || newAttacker_ == selected || usedAttackers_.Contains(selected))
                    {
                        selection.AddOrUpdate(selected, selected.weight);
                        break;
                    }
                }

                var newMockStats = UpdateMockStats(mockStats, selected);
                if (newMockStats.GetEffectiveValue(Spacing.Max, 1, valueRate) * parallelMinCount > bufferLeft)
                    return;
                mockStats = newMockStats;
                types[p] = selected;
                if (!usedAttackers_.Contains(selected))
                    newAttacker_ = selected;
            }
        }

        static AttackerStats UpdateMockStats(AttackerStats mockStats, AttackerStats selected)
        {
            AttackerStats newMockStats = ScriptableObject.CreateInstance<AttackerStats>();
            newMockStats.baseValue = mockStats.baseValue + selected.baseValue;
            newMockStats.speed = (mockStats.speed * mockStats.baseValue + selected.speed * selected.baseValue) / newMockStats.baseValue;
            newMockStats.minSpacing = (Spacing)Mathf.Max((int)mockStats.minSpacing, (int)selected.minSpacing);
            return newMockStats;
        }

        WeightedRandomSet<AttackerStats> PrepareParallelAttackerSelection(float valueRate)
        {
            var filteredAttackers = availableAttackers.Where(a => a.MinEffectiveValue(valueRate, 1) * parallelMinCount < bufferLeft);
            WeightedRandomSet<AttackerStats> selection = new(filteredAttackers.Select(a => (a, a.weight)), random.NewSeed());
            return selection;
        }
    }

    internal static class AttackerStatsCalculations
    {
        public static float MaxValueRate(this AttackerStats stats, int paths) => stats.GetValueRate(stats.minSpacing, paths);
        public static float GetValueRate(this AttackerStats stats, Spacing spacing, int paths) => paths * stats.baseValue / Mathf.Sqrt(spacing.GetSeconds());
        public static float MinEffectiveValue(this AttackerStats stats, float valueRate, int paths) => stats.GetEffectiveValue(Spacing.BatchSpacing, paths, valueRate);

        public static float MaxEffectiveValue(this AttackerStats stats, float valueRate, int paths, int maxDuration)
        {
            return stats.MinEffectiveValue(valueRate, paths) + stats.GetEffectiveValue(stats.minSpacing, paths, valueRate) * (maxDuration / stats.minSpacing.GetTicks());
        }

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
                if (stats.GetEffectiveValue(s, paths, valueRate) <= maxValue)
                    return s;

            return Spacing.Max;
        }

        public static Spacing MaxFeasibleSpacing(this AttackerStats stats, float valueRate, int paths, int maxTicks)
        {
            for (Spacing s = Spacing.Max; s > stats.minSpacing; s--)
                if (s.GetTicks() <= maxTicks && stats.GetValueRate(s, paths) > valueRate)
                    return s;

            return stats.minSpacing;
        }
    }
}
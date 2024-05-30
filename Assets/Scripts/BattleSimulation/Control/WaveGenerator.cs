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
        [SerializeField] int maxAttackersPerWave;
        [SerializeField] float parallelWaveChance;
        [SerializeField] float pathDropChance;
        [SerializeField] int maxBatchCount;
        [SerializeField] int parallelMinCount;
        [SerializeField] List<Wave> tutorialWaves;
        [Header("Settings - auto-assigned")]
        public float baseValueRate;
        public float baseEffectiveValueBuffer;
        public float linearScaling;
        public float quadraticScaling;
        public float cubicScaling;
        public float exponentialScalingBase;
        public int paths;
        public Random random;
        public bool tutorial;
        [Header("Runtime variables")]
        [SerializeField] float bufferLeft;
        [SerializeField] List<Wave> waves;
        readonly HashSet<AttackerStats> usedAttackers_ = new();
        AttackerStats? newAttacker_;
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

            if (tutorial)
                return tutorialWaves.Count > number ? tutorialWaves[number] : new(null);

            while (number >= waves.Count)
                waves.Add(GenerateWave(waves.Count));
            return waves[number];
        }

        Wave GenerateWave(int number)
        {
            newAttacker_ = null;
            currentWaveMaxBatches = Mathf.Min(1 + number / 2, maxBatchCount);
            float scaling = linearScaling * number + quadraticScaling * number * number + cubicScaling * number * number * number + Mathf.Pow(exponentialScalingBase, number);
            float valueRate = baseValueRate * scaling;
            bufferLeft += baseEffectiveValueBuffer * scaling;

            print($"Generating wave {number + 1}: scaling {scaling}, rate {valueRate}, buffer {bufferLeft}");

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
            SelectPathsForSequentialWave(out var selectedPathCount, out var selectedPaths);
            List<Batch> batches = new();
            int durationLeft = maxWaveLengthTicks;
            int attackersLeft = maxAttackersPerWave;
            for (int i = 0; i < currentWaveMaxBatches; i++)
            {
                if (attackersLeft < selectedPathCount)
                    break;
                durationLeft -= Spacing.BatchSpacing.GetTicks();
                Batch? b = TryMakeSequentialBatch(selectedPathCount, selectedPaths, valueRate, ref durationLeft, ref attackersLeft, i == currentWaveMaxBatches - 1, batches.SelectMany(b => b.typePerPath).Where(a => a != null));
                if (b == null)
                    break;
                batches.Add(b);
            }

            return new(newAttacker_, batches.ToArray());
        }

        void SelectPathsForSequentialWave(out int count, out bool[] selectedPaths)
        {
            selectedPaths = new bool[paths];
            // always select at least one path
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

        Batch? TryMakeSequentialBatch(int pathCount, bool[] selectedPaths, float valueRate, ref int durationLeft, ref int attackersLeft, bool forceMaxCount, IEnumerable<AttackerStats> usedAttackers)
        {
            var selection = PrepareSequentialBatchAttackerSelection(pathCount, valueRate, durationLeft, attackersLeft);
            foreach (var usedAttacker in usedAttackers)
                selection.Remove(usedAttacker);
            while (true)
            {
                if (selection.Count == 0)
                    return null;
                var selected = selection.PopRandom();
                var batch = TryMakeSequentialBatchOf(selected, pathCount, selectedPaths, valueRate, ref durationLeft, ref attackersLeft, forceMaxCount || selection.Count == 0);

                if (batch == null)
                    continue;
                if (usedAttackers_.Add(selected))
                    newAttacker_ = selected;
                return batch;
            }
        }

        WeightedRandomSet<AttackerStats> PrepareSequentialBatchAttackerSelection(int pathCount, float valueRate, int durationLeft, int attackersLeft)
        {
            IEnumerable<AttackerStats> selection = newAttacker_ != null ? usedAttackers_ : availableAttackers;
            var filteredAttackers = selection.Where(a =>
                a.MaxValueRate(pathCount) > valueRate
                && a.MinEffectiveValue(valueRate, pathCount) < bufferLeft
                && a.MaxEffectiveValue(valueRate, pathCount, durationLeft, attackersLeft) >= bufferLeft / 2
            );
            return new(filteredAttackers.Select(a => (a, a.weight)), random.NewSeed());
        }

        Batch? TryMakeSequentialBatchOf(AttackerStats selected, int pathCount, bool[] selectedPaths, float valueRate, ref int durationLeft, ref int attackersLeft, bool forceMaxCount)
        {
            if (!TryPickSequentialBatchSpacing(selected, pathCount, valueRate, durationLeft, attackersLeft, forceMaxCount, out var spacing, out float totalValue, out int count))
                return null;
            bufferLeft -= totalValue;
            durationLeft -= (count - 1) * spacing.GetTicks();
            attackersLeft -= count * pathCount;
            var types = Enumerable.Range(0, paths).Select(i => selectedPaths[i] ? selected : null).ToArray();
            return new(count, spacing, types);
        }

        bool TryPickSequentialBatchSpacing(AttackerStats selected, int pathCount, float valueRate, int durationLeft, int maxCount, bool useUpBuffer, out Spacing spacing, out float totalValue, out int count)
        {
            spacing = default;
            totalValue = 0;
            count = 0;

            var lastUnitValue = selected.MinEffectiveValue(valueRate, pathCount);
            var minSpacing = selected.MinFeasibleSpacing(valueRate, pathCount, bufferLeft - lastUnitValue);
            var maxSpacing = selected.MaxFeasibleSpacing(valueRate, pathCount, durationLeft);
            if (minSpacing > maxSpacing)
                return false;

            if (!useUpBuffer)
            {
                spacing = (Spacing)random.Int((int)minSpacing, (int)maxSpacing + 1);
                PickSequentialBatchAttackerCount(selected, pathCount, valueRate, durationLeft, maxCount, bufferLeft - lastUnitValue, false, spacing, out var extraCount, out float unitValue);
                totalValue = lastUnitValue + extraCount * unitValue;
                count = extraCount + 1;
                return totalValue > 0;
            }

            for (Spacing s = minSpacing; s <= maxSpacing; s++)
            {
                PickSequentialBatchAttackerCount(selected, pathCount, valueRate, durationLeft, maxCount, bufferLeft - lastUnitValue, true, s, out var extraCount, out float unitValue);
                float value = lastUnitValue + extraCount * unitValue;

                if (value > totalValue)
                {
                    totalValue = value;
                    count = extraCount + 1;
                    spacing = s;
                }
            }

            return totalValue > 0;
        }

        void PickSequentialBatchAttackerCount(AttackerStats selected, int pathCount, float valueRate, int durationLeft, int maxCount, float bufferLeft, bool forceMaxCount, Spacing spacing, out int extraCount, out float unitValue)
        {
            unitValue = selected.GetEffectiveValue(spacing, pathCount, valueRate);
            var maxExtraCount = 200;
            maxExtraCount = Mathf.Min(maxExtraCount, durationLeft / spacing.GetTicks());
            maxExtraCount = Mathf.Min(maxExtraCount, Mathf.FloorToInt(bufferLeft / unitValue));
            maxExtraCount = Mathf.Min(maxExtraCount, maxCount / pathCount - 1);
            extraCount = forceMaxCount ? maxExtraCount : random.Int(maxExtraCount + 1);
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

        WeightedRandomSet<AttackerStats> PrepareParallelAttackerSelection(float valueRate)
        {
            var filteredAttackers = availableAttackers.Where(a => a.MinEffectiveValue(valueRate, 1) * parallelMinCount < bufferLeft);
            WeightedRandomSet<AttackerStats> selection = new(filteredAttackers.Select(a => (a, a.weight)), random.NewSeed());
            return selection;
        }

        Batch? TryMakeParallelBatchOnce(float valueRate, int[] pathSelection, WeightedRandomSet<AttackerStats> selection)
        {
            newAttacker_ = null;
            PickParallelAttackerTypes(valueRate, pathSelection, new(selection), out var types, out var mockStats);

            if (mockStats.MaxValueRate(1) <= valueRate)
                return null;
            int pathCount = types.Count(t => t is not null);
            int maxCount = Mathf.Min(maxWaveLengthTicks / mockStats.minSpacing.GetTicks(), maxAttackersPerWave / pathCount);
            float maxEffectiveValue = mockStats.GetEffectiveValue(mockStats.minSpacing, paths, valueRate) * maxCount;
            if (maxEffectiveValue < bufferLeft * 0.8f)
                return null;

            if (!TryPickParallelSpacing(valueRate, mockStats, pathCount, out var spacing, out var unitValue, out int count))
                return null;

            bufferLeft -= count * unitValue;
            return new(count, spacing, types);
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

        bool TryPickParallelSpacing(float valueRate, AttackerStats mockStats, int pathCount, out Spacing spacing, out float unitValue, out int count)
        {
            spacing = default;
            unitValue = 0;
            count = 0;

            var minSpacing = mockStats.MinFeasibleSpacing(valueRate, 1, bufferLeft);
            var maxSpacing = mockStats.MaxFeasibleSpacing(valueRate, 1, maxWaveLengthTicks);
            if (minSpacing > maxSpacing)
                return false;

            float best = 0;
            for (Spacing s = minSpacing; s <= maxSpacing; s++)
            {
                var uv = mockStats.GetEffectiveValue(s, 1, valueRate);
                var c = PickParallelAttackerCount(s, uv, pathCount);
                var value = uv * c;
                if (value > best)
                {
                    best = value;
                    spacing = s;
                    unitValue = uv;
                    count = c;
                }
            }

            return true;
        }

        int PickParallelAttackerCount(Spacing spacing, float unitValue, int pathCount)
        {
            var count = 200;
            count = Mathf.Min(count, maxWaveLengthTicks / spacing.GetTicks());
            count = Mathf.Min(count, Mathf.FloorToInt(bufferLeft / unitValue));
            count = Mathf.Min(count, maxAttackersPerWave / pathCount);
            return count;
        }
    }

    internal static class AttackerStatsCalculations
    {
        /// <summary>
        /// Calculates the value rate of a given attacker type spawning with the given spacing on the given number of paths at once.
        /// Value rate represents the damage per second the towers would need to deal to kill one attacker on each path in the time it takes before the next one spawns.
        /// However, the value of an attacker is not equal to its health.
        /// Some attackers might have greater value, because they are harder to kill, or because they have some powerful abilities.
        /// Also, in the calculation, the sqrt of spacing time is used instead of the spacing time itself, because attackers closer together can be damaged at once by towers that deal damage in an area.
        /// </summary>
        public static float GetValueRate(this AttackerStats stats, Spacing spacing, int paths) => paths * stats.baseValue / Mathf.Sqrt(spacing.GetSeconds());

        /// <summary>
        /// Calculates the maximum value rate (see <see cref="GetValueRate"/>) for the given attacker type.
        /// </summary>
        public static float MaxValueRate(this AttackerStats stats, int paths) => stats.GetValueRate(stats.minSpacing, paths);

        /// <summary>
        /// Calculates the effective value an attacker will have left after being exposed to valueRate of damage for spacing of time.
        /// First, it calculates how much value will the attacker have left, assuming it gets sqrt(spacing) seconds of damage.
        /// Sqrt is used, because even with faster spacing, some can damage multiple attackers at once.
        /// Effective value is this value that's left multiplied by the attacker's sqrt(speed).
        /// This reflects that faster attackers will get to the hub sooner, so the towers will have less time to deal with them.
        /// The relation is not linear, because most abilities don't care about attacker speed.
        /// </summary>
        public static float GetEffectiveValue(this AttackerStats stats, Spacing spacing, int paths, float valueRate)
        {
            float valueLeft = stats.baseValue * paths - valueRate * Mathf.Sqrt(spacing.GetSeconds());
            return valueLeft * Mathf.Sqrt(stats.speed);
        }

        /// <summary>
        /// The minimum possible effective value (see <see cref="GetEffectiveValue"/>) if this attacker is selected.
        /// </summary>
        public static float MinEffectiveValue(this AttackerStats stats, float valueRate, int paths) => stats.GetEffectiveValue(Spacing.BatchSpacing, paths, valueRate);

        /// <summary>
        /// The maximum possible effective value (see <see cref="GetEffectiveValue"/>) if this attacker is selected.
        /// </summary>
        public static float MaxEffectiveValue(this AttackerStats stats, float valueRate, int paths, int maxDuration, int maxAttackers)
        {
            int maximumCount = Mathf.Min(maxDuration / stats.minSpacing.GetTicks(), maxAttackers / paths);
            return stats.MinEffectiveValue(valueRate, paths) + stats.GetEffectiveValue(stats.minSpacing, paths, valueRate) * maximumCount;
        }

        /// <summary>
        /// The minimum feasible spacing given that the effective value (see <see cref="GetEffectiveValue"/>) cannot go over maxValue.
        /// </summary>
        public static Spacing MinFeasibleSpacing(this AttackerStats stats, float valueRate, int paths, float maxValue)
        {
            for (Spacing s = stats.minSpacing; s < Spacing.Max; s++)
                if (stats.GetEffectiveValue(s, paths, valueRate) <= maxValue)
                    return s;

            return Spacing.Max;
        }

        /// <summary>
        /// The maximum feasible spacing given that the rate (see <see cref="GetValueRate"/>) must be at least valueRate, but the spacing must be at most maxTicks.
        /// </summary>
        public static Spacing MaxFeasibleSpacing(this AttackerStats stats, float valueRate, int paths, int maxTicks)
        {
            for (Spacing s = Spacing.Max; s > stats.minSpacing; s--)
                if (s.GetTicks() <= maxTicks && stats.GetValueRate(s, paths) > valueRate)
                    return s;

            return stats.minSpacing;
        }
    }
}
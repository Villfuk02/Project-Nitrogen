using System;
using System.Collections.Generic;
using System.Linq;
using Game.AttackerStats;
using UnityEngine;
using Utils;
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
        [SerializeField] float splashDamageMultiplier;
        [SerializeField] int fullSplashDamageMultiplierWave;
        [SerializeField] float splashDamageBase;
        [SerializeField] float globalCapacityPortion;
        [SerializeField] List<Wave> tutorialWaves;
        [Header("Settings - auto-assigned")]
        public float baseRate;
        public float baseCapacity;
        public float linearScaling;
        public float quadraticScaling;
        public float cubicScaling;
        public int pathCount;
        public Random random;
        public bool tutorial;
        [Header("Runtime variables")]
        [SerializeField] float totalCapacity;
        [SerializeField] float totalRate;
        [SerializeField] float[] capacityPerPath;
        [SerializeField] float[] capacityLeftPerPath;
        [SerializeField] float[] ratePerPath;
        [SerializeField] float globalCapacity;
        [SerializeField] float globalCapacityLeft;
        [SerializeField] List<Wave> waves;
        [SerializeField] int currentWaveNumber;
        readonly HashSet<AttackerStats> usedAttackers_ = new();
        AttackerStats? newAttacker_;
        [SerializeField] int currentMaxBatches;
        [SerializeField] List<int> currentPaths;
        readonly HashSet<AttackerStats> currentUsedAttackers_ = new();
        int currentTicksLeft_;
        int currentAttackersLeft_;
        [SerializeField] float currentSplashDamageMultiplier;

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
                waves.Add(GenerateWave());
            return waves[number];
        }

        Wave GenerateWave()
        {
            currentWaveNumber = waves.Count;
            if (currentWaveNumber == 0)
            {
                capacityLeftPerPath = new float[pathCount];
                capacityPerPath = new float[pathCount];
                ratePerPath = new float[pathCount];
            }

            newAttacker_ = null;
            currentUsedAttackers_.Clear();
            currentMaxBatches = Mathf.Min(1 + currentWaveNumber / 2, maxBatchCount);
            currentSplashDamageMultiplier = Mathf.Lerp(0, splashDamageMultiplier, currentWaveNumber / (float)fullSplashDamageMultiplierWave);
            SelectPaths();
            UpdateTotalRatesAndCapacities(out float newRate, out float newCapacity);

            print($"Generating wave {currentWaveNumber + 1}: total rate {totalRate}, total capacity {totalCapacity} (global {globalCapacityLeft})");


            // first wave can never be parallel
            if (currentWaveNumber > 0 && random.Bool(parallelWaveChance))
                return GenerateParallelWave(newRate, newCapacity);

            return GenerateSequentialWave(newRate, newCapacity);
        }

        void SelectPaths()
        {
            currentPaths.Clear();
            // always select at least one path
            int forcedPath = random.Int(pathCount);
            for (int i = 0; i < pathCount; i++)
            {
                if (i != forcedPath && random.Bool(pathDropChance))
                    continue;
                currentPaths.Add(i);
            }
        }

        void UpdateTotalRatesAndCapacities(out float newRate, out float newCapacity)
        {
            float scaling = 1 +
                            linearScaling * currentWaveNumber +
                            quadraticScaling * currentWaveNumber * currentWaveNumber +
                            cubicScaling * currentWaveNumber * currentWaveNumber * currentWaveNumber;
            newRate = baseRate * scaling - totalRate;
            newCapacity = baseCapacity * scaling - totalCapacity;
            totalRate = baseRate * scaling;
            totalCapacity = baseCapacity * scaling;

            globalCapacity += newCapacity * globalCapacityPortion;
            globalCapacityLeft += globalCapacity;
            newCapacity *= 1 - globalCapacityPortion;
        }

        void DistributeNewRateAndCapacityFairly(float newRate, float newCapacity)
        {
            var sortedPaths = currentPaths.Select(p => (p, rate: ratePerPath[p])).OrderBy(p => p.rate).Select(p => p.p);
            List<int> equalPaths = new();
            int representative = -1;
            foreach (int path in sortedPaths)
            {
                if (newRate <= 0)
                    break;
                if (equalPaths.Count == 0)
                {
                    equalPaths.Add(path);
                    representative = path;
                    continue;
                }

                float rateDifference = ratePerPath[path] - ratePerPath[representative];
                rateDifference = Mathf.Min(newRate / equalPaths.Count, rateDifference);
                float capacityDifference = capacityPerPath[path] - capacityPerPath[representative];
                capacityDifference = Mathf.Min(newCapacity / equalPaths.Count, capacityDifference);

                foreach (int equalPath in equalPaths)
                {
                    ratePerPath[equalPath] += rateDifference;
                    capacityPerPath[equalPath] += capacityDifference;
                }

                newRate -= rateDifference * equalPaths.Count;
                newCapacity -= capacityDifference * equalPaths.Count;

                equalPaths.Add(path);
            }

            foreach (int path in currentPaths)
            {
                ratePerPath[path] += newRate / currentPaths.Count;
                capacityPerPath[path] += newCapacity / currentPaths.Count;
                capacityLeftPerPath[path] += capacityPerPath[path];
            }
        }

        Wave GenerateSequentialWave(float newRate, float newCapacity)
        {
            DistributeNewRateAndCapacityFairly(newRate, newCapacity);

            currentTicksLeft_ = maxWaveLengthTicks + Spacing.BatchSpacing.GetTicks();
            currentAttackersLeft_ = maxAttackersPerWave;

            List<Batch> batches = new();
            for (int i = 0; i < currentMaxBatches; i++)
            {
                if (currentAttackersLeft_ < currentPaths.Count)
                    break;
                currentTicksLeft_ -= Spacing.BatchSpacing.GetTicks();

                bool isFirstBatch = i == 0;
                bool isLastBatch = i == currentMaxBatches - 1;
                Batch? b = TryMakeSequentialBatch(isFirstBatch, isLastBatch);
                if (b == null)
                    break;
                batches.Add(b);

                var selected = b.typePerPath.First(a => a is not null);
                currentUsedAttackers_.Add(selected);
                if (usedAttackers_.Add(selected))
                    newAttacker_ = selected;
            }

            return new(newAttacker_, batches.ToArray());
        }


        Batch? TryMakeSequentialBatch(bool isFirstBatch, bool isLastBatch)
        {
            var selection = PrepareSequentialBatchAttackerSelection(isFirstBatch, isLastBatch);

            if (selection.Count == 0 && isLastBatch)
                selection = PrepareSequentialBatchAttackerSelection(isFirstBatch, false);

            if (selection.Count == 0)
                return null;

            var selected = selection.PopRandom();

            if (isLastBatch || selection.Count == 0)
                return MakeSequentialLastBatchOf(selected.stats, selected.spacings, isFirstBatch);
            return MakeSequentialBatchOf(selected.stats, selected.spacings, isFirstBatch);
        }

        WeightedRandomSet<(AttackerStats stats, BitSet32 spacings)> PrepareSequentialBatchAttackerSelection(bool isFirstBatch, bool isLastBatch)
        {
            IEnumerable<AttackerStats> selection = newAttacker_ != null ? usedAttackers_ : availableAttackers;

            selection = selection.Where(s => !currentUsedAttackers_.Contains(s));

            var withSpacings = selection.Select(a =>
            {
                BitSet32 set = new();

                // at least one attacker must fit into the remaining capacity
                if (OvershootsCapacity(a, Spacing.Min, 1, isFirstBatch))
                    return (a, set);

                // find all valid spacings
                for (Spacing s = a.minSpacing; s <= Spacing.Max; s++)
                {
                    // if last batch, there must be enough space left in the wave to fill up the capacity
                    if (isLastBatch)
                    {
                        int maxCount = AttackerStatsCalculations.MaxAttackerCount(s, currentPaths.Count, currentTicksLeft_, currentAttackersLeft_);
                        if (!OvershootsCapacity(a, s, maxCount + 1, isFirstBatch))
                            continue;
                    }

                    set.SetBit((int)s);
                }

                return (a, set);
            });

            var filtered = withSpacings.Where(p => !p.set.IsEmpty);

            return new(filtered.Select(p => (p, p.a.weight)), random.NewSeed());
        }

        bool OvershootsCapacity(AttackerStats stats, Spacing spacing, int count, bool isFirstBatch)
        {
            var rates = ratePerPath.PickOut(currentPaths);
            var capacity = capacityLeftPerPath.PickOut(currentPaths);
            float global = globalCapacityLeft;
            stats.GetRemainingCapacity(spacing, currentSplashDamageMultiplier, splashDamageBase, rates, count, isFirstBatch, ref capacity, ref global);
            return global < 0;
        }

        Batch MakeSequentialBatchOf(AttackerStats selected, BitSet32 spacings, bool isFirstBatch)
        {
            // select random spacing
            var spacingsArray = spacings.GetBits().Select(i => (Spacing)i).ToArray();
            Spacing spacing = spacingsArray[random.Int(spacingsArray.Length)];

            // select random count
            int maxCount = MaxCountSequentialBatch(selected, spacing, isFirstBatch, false);
            int count = random.Int(1, maxCount + 1);

            // update remaining room in the wave
            currentTicksLeft_ -= (count - 1) * spacing.GetTicks();
            currentAttackersLeft_ -= count * currentPaths.Count;

            // update capacity
            var rates = ratePerPath.PickOut(currentPaths);
            var capacities = capacityLeftPerPath.PickOut(currentPaths);
            selected.GetRemainingCapacity(spacing, currentSplashDamageMultiplier, splashDamageBase, rates, count, isFirstBatch, ref capacities, ref globalCapacityLeft);
            for (int i = 0; i < currentPaths.Count; i++)
                capacityLeftPerPath[currentPaths[i]] = capacities[i];

            // prepare result
            var types = new AttackerStats[pathCount];
            foreach (int path in currentPaths)
                types[path] = selected;
            return new(count, spacing, types);
        }

        Batch MakeSequentialLastBatchOf(AttackerStats selected, BitSet32 spacings, bool isFirstBatch)
        {
            var rates = ratePerPath.PickOut(currentPaths);

            // find best spacing and count
            float bestGlobalCapacity = float.PositiveInfinity;
            float[] bestCapacities = null;
            Spacing bestSpacing = default;
            int bestCount = 0;
            foreach (var spacing in spacings.GetBits().Select(i => (Spacing)i))
            {
                int count = MaxCountSequentialBatch(selected, spacing, isFirstBatch, false);
                var capacities = capacityLeftPerPath.PickOut(currentPaths);
                float global = globalCapacityLeft;
                selected.GetRemainingCapacity(spacing, currentSplashDamageMultiplier, splashDamageBase, rates, count, isFirstBatch, ref capacities, ref global);

                if (global < bestGlobalCapacity)
                {
                    bestGlobalCapacity = global;
                    bestCapacities = capacities;
                    bestSpacing = spacing;
                    bestCount = count;
                }
            }

            // update remaining room in the wave
            currentTicksLeft_ -= (bestCount - 1) * bestSpacing.GetTicks();
            currentAttackersLeft_ -= bestCount * currentPaths.Count;

            // update capacities
            for (int i = 0; i < currentPaths.Count; i++)
                capacityLeftPerPath[currentPaths[i]] = bestCapacities![i];
            globalCapacityLeft = bestGlobalCapacity;

            // prepare result
            var types = new AttackerStats[pathCount];
            foreach (int path in currentPaths)
                types[path] = selected;
            return new(bestCount, bestSpacing, types);
        }

        int MaxCountSequentialBatch(AttackerStats stats, Spacing spacing, bool isFirstBatch, bool isLastBatch)
        {
            int max = AttackerStatsCalculations.MaxAttackerCount(spacing, currentPaths.Count, currentTicksLeft_, currentAttackersLeft_);
            // if the current upper bound doesn't overshoot the limit, we are done
            // if this isn't the last batch, we need to leave room for the last batch to use up the limit as much as possible
            if (!OvershootsCapacity(stats, spacing, max, isFirstBatch))
                return isLastBatch ? max : max / 2;

            // do a binary search to find the limit
            max--;
            int min = 1;
            while (min < max)
            {
                int mid = (min + max + 1) / 2;
                if (OvershootsCapacity(stats, spacing, mid, isFirstBatch))
                    max = mid - 1;
                else
                    min = mid;
            }

            return max;
        }

        Wave GenerateParallelWave(float newRate, float newCapacity)
        {
            currentTicksLeft_ = maxWaveLengthTicks;
            currentAttackersLeft_ = maxAttackersPerWave;
            foreach (int path in currentPaths)
                capacityLeftPerPath[path] += capacityPerPath[path];

            Spacing spacing = (Spacing)random.Int((int)Spacing.Max + 1);

            var validAttackers = GetValidParallelAttackers(spacing);

            var selectedAttackers = PickInitialParallelAttackers(validAttackers);

            while (true)
            {
                int minCount = MinCountRequiredToUseAllPathCapacity(spacing, selectedAttackers);

                if (!FitsWithinBudget(spacing, selectedAttackers, minCount, newRate, newCapacity, out int mostExpensive))
                {
                    selectedAttackers[mostExpensive] = null;
                    selectedAttackers[mostExpensive] = GetRandomAttacker(validAttackers[mostExpensive], selectedAttackers);
                    continue;
                }

                int maxCount = MaxCountParallelWave(spacing, selectedAttackers, newRate, newCapacity, minCount, out bool canUseCapacity);

                if (!canUseCapacity)
                {
                    int cheapest = CheapestAttacker(spacing, selectedAttackers, maxCount, newAttacker_ == null);
                    selectedAttackers[cheapest] = null;
                    selectedAttackers[cheapest] = GetRandomAttacker(validAttackers[cheapest], selectedAttackers);
                    continue;
                }

                DistributeNewRateAndCapacityParallelWave(spacing, selectedAttackers, newRate, newCapacity, maxCount);

                var attackerPerPath = new AttackerStats[pathCount];
                for (int i = 0; i < currentPaths.Count; i++)
                    attackerPerPath[currentPaths[i]] = selectedAttackers[i];
                if (newAttacker_ is not null)
                    usedAttackers_.Add(newAttacker_);
                return new(newAttacker_, new Batch(maxCount, spacing, attackerPerPath));
            }
        }

        WeightedRandomSet<AttackerStats>[] GetValidParallelAttackers(Spacing spacing)
        {
            int maxCount = AttackerStatsCalculations.MaxAttackerCount(spacing, currentPaths.Count, currentTicksLeft_, currentAttackersLeft_);

            var result = new WeightedRandomSet<AttackerStats>[currentPaths.Count];

            for (int i = 0; i < currentPaths.Count; i++)
            {
                int path = currentPaths[i];

                var rate = new[] { ratePerPath[path] };
                var pathCapacity = capacityLeftPerPath[path];

                var selection = availableAttackers.Where(stats =>
                {
                    // comply with minimum spacing
                    if (stats.minSpacing > spacing)
                        return false;

                    // at least minCount attackers must fit into the capacity
                    var value = stats.AttackersValue(spacing, currentSplashDamageMultiplier, splashDamageBase, rate, parallelMinCount, true)[0];
                    if (value > pathCapacity + globalCapacityLeft)
                        return false;

                    // it must be possible to use up the path capacity
                    value = stats.AttackersValue(spacing, currentSplashDamageMultiplier, splashDamageBase, rate, maxCount, true)[0];
                    return value > pathCapacity;
                });

                result[i] = new(selection.Select(s => (s, s.weight)), random.NewSeed());
            }

            return result;
        }

        AttackerStats[] PickInitialParallelAttackers(WeightedRandomSet<AttackerStats>[] validAttackers)
        {
            var selectedAttackers = new AttackerStats[currentPaths.Count];
            var randomOrder = Enumerable.Range(0, currentPaths.Count).ToArray();
            random.Shuffle(randomOrder);
            foreach (int i in randomOrder)
                selectedAttackers[i] = GetRandomAttacker(validAttackers[i], selectedAttackers);
            return selectedAttackers;
        }

        AttackerStats GetRandomAttacker(WeightedRandomSet<AttackerStats> validAttackers, IEnumerable<AttackerStats?> currentlyUsedAttackers)
        {
            newAttacker_ = null;
            currentUsedAttackers_.Clear();
            foreach (var a in currentlyUsedAttackers)
            {
                if (a is null)
                    continue;

                if (currentUsedAttackers_.Add(a) && !usedAttackers_.Contains(a))
                    newAttacker_ = a;
            }

            if (newAttacker_ == null)
            {
                var selected = validAttackers.PopRandom();
                if (currentUsedAttackers_.Add(selected) && !usedAttackers_.Contains(selected))
                    newAttacker_ = selected;
                return selected;
            }

            List<AttackerStats> invalid = new();
            while (true)
            {
                var selected = validAttackers.PopRandom();
                if (!usedAttackers_.Contains(selected) && newAttacker_ != selected)
                {
                    invalid.Add(selected);
                    continue;
                }

                foreach (var a in invalid)
                    validAttackers.AddOrUpdate(a, a.weight);

                return selected;
            }
        }

        int MinCountRequiredToUseAllPathCapacity(Spacing spacing, AttackerStats[] selectedAttackers)
        {
            int maxCount = AttackerStatsCalculations.MaxAttackerCount(spacing, currentPaths.Count, currentTicksLeft_, currentAttackersLeft_);

            for (int count = 1; count < maxCount; count++)
            {
                bool allValid = true;
                for (int i = 0; i < selectedAttackers.Length; i++)
                {
                    int path = currentPaths[i];
                    float[] rate = { ratePerPath[path] };
                    var value = selectedAttackers[i].AttackersValue(spacing, currentSplashDamageMultiplier, splashDamageBase, rate, count, true)[0];
                    if (value < capacityLeftPerPath[path])
                    {
                        allValid = false;
                        break;
                    }
                }

                if (allValid)
                    return count;
            }

            return maxCount;
        }

        bool FitsWithinBudget(Spacing spacing, AttackerStats[] selectedAttackers, int count, float newRate, float newCapacity, out int mostExpensive)
        {
            float capacity = globalCapacityLeft + newCapacity + newRate * spacing.GetSeconds() * (count - 1);
            mostExpensive = 0;
            float mostExpensiveValue = 0;
            for (int i = 0; i < selectedAttackers.Length; i++)
            {
                int path = currentPaths[i];
                float[] rate = { ratePerPath[path] };
                var value = selectedAttackers[i].AttackersValue(spacing, currentSplashDamageMultiplier, splashDamageBase, rate, count, true)[0];
                value -= capacityLeftPerPath[path];

                if (value > mostExpensiveValue)
                {
                    mostExpensiveValue = value;
                    mostExpensive = i;
                }

                capacity -= value;
            }

            return capacity >= 0;
        }

        int MaxCountParallelWave(Spacing spacing, AttackerStats[] selectedAttackers, float newRate, float newCapacity, int minCount, out bool canUseCapacity)
        {
            int max = AttackerStatsCalculations.MaxAttackerCount(spacing, currentPaths.Count, currentTicksLeft_, currentAttackersLeft_);

            if (FitsWithinBudget(spacing, selectedAttackers, max + 1, newRate, newCapacity, out _))
            {
                canUseCapacity = false;
                return 0;
            }

            int min = minCount;
            while (min < max)
            {
                int mid = (min + max) / 2;
                if (FitsWithinBudget(spacing, selectedAttackers, mid + 1, newRate, newCapacity, out _))
                    min = mid + 1;
                else
                    max = mid;
            }

            canUseCapacity = true;
            return max;
        }

        int CheapestAttacker(Spacing spacing, AttackerStats[] selectedAttackers, int count, bool onlyConsiderNewAttackers)
        {
            int cheapest = 0;
            float cheapestValue = float.PositiveInfinity;

            for (int i = 0; i < selectedAttackers.Length; i++)
            {
                AttackerStats a = selectedAttackers[i];
                if (onlyConsiderNewAttackers && a != newAttacker_)
                    continue;

                int path = currentPaths[i];
                float[] rate = { ratePerPath[path] };
                var value = a.AttackersValue(spacing, currentSplashDamageMultiplier, splashDamageBase, rate, count, true)[0];
                value -= capacityLeftPerPath[path];

                if (value < cheapestValue)
                {
                    cheapestValue = value;
                    cheapest = i;
                }
            }

            return cheapest;
        }

        void DistributeNewRateAndCapacityParallelWave(Spacing spacing, AttackerStats[] selectedAttackers, float newRate, float newCapacity, int count)
        {
            float[] values = new float[currentPaths.Count];
            float totalValue = 0;
            for (int i = 0; i < selectedAttackers.Length; i++)
            {
                int path = currentPaths[i];
                float[] rate = { ratePerPath[path] };
                var value = selectedAttackers[i].AttackersValue(spacing, currentSplashDamageMultiplier, splashDamageBase, rate, count, true)[0];
                value -= capacityLeftPerPath[path];

                values[i] = value;
                totalValue += value;
            }

            for (int i = 0; i < selectedAttackers.Length; i++)
            {
                int path = currentPaths[i];

                // distribute proportionally to the extra value
                float portion = values[i] / totalValue;
                ratePerPath[path] += newRate * portion;
                capacityPerPath[path] += newCapacity * portion;

                // subtract from capacities
                values[i] -= portion * (newCapacity + newRate * spacing.GetSeconds() * (count - 1));
                capacityLeftPerPath[path] = 0;
                globalCapacityLeft -= values[i];
            }
        }
    }

    internal static class AttackerStatsCalculations
    {
        public static float NthAttackerValue(this AttackerStats stats, Spacing spacing, float splashDamageMultiplier, float splashDamageBase, float rate, int n)
        {
            float b = Mathf.Pow(splashDamageBase, spacing.GetSeconds() * stats.speed);
            float a = splashDamageMultiplier;

            float numerator = 1 - a - b + a * b + a * Mathf.Pow(b * (1 - a), n);
            float denominator = (1 - a) * (1 - b + a * b);
            float multiplier = numerator / denominator;

            float value = stats.baseValue * multiplier;
            value -= rate * spacing.GetSeconds();
            if (value < 0)
                value = 0;

            return value * stats.speed;
        }

        public static float AttackerValueLimit(this AttackerStats stats, Spacing spacing, float splashDamageMultiplier, float splashDamageBase, float rate)
        {
            float beta = Mathf.Pow(splashDamageBase, spacing.GetSeconds() * stats.speed);
            float limitMultiplier = (1 - beta) / (1 + (splashDamageMultiplier - 1) * beta);

            float value = stats.baseValue * limitMultiplier;
            value -= rate * spacing.GetSeconds();
            if (value < 0)
                value = 0;
            return value * stats.speed;
        }

        public static float[] AttackersValue(this AttackerStats stats, Spacing spacing, float splashDamageMultiplier, float splashDamageBase, float[] rates, int count, bool isFirstBatch)
        {
            float[] result = new float[rates.Length];

            float b = Mathf.Pow(splashDamageBase, spacing.GetSeconds() * stats.speed);
            float ib = 1 - b;
            float a = splashDamageMultiplier;

            float sqrtDenominator = 1 + (a - 1) * b;
            float denominator = sqrtDenominator * sqrtDenominator;

            for (int i = 0; i < rates.Length; i++)
            {
                int n = stats.ContributingAttackers(spacing, splashDamageMultiplier, splashDamageBase, rates[i], count);

                if (n == 0)
                    continue;

                float numerator = n * ib * ib - a * b * (Mathf.Pow(b * (1 - a), n) - n * ib - 1);
                float multiplier = numerator / denominator;
                float value = stats.baseValue * multiplier;

                float firingTime = spacing.GetSeconds() * (n - 1);
                if (!isFirstBatch)
                    firingTime++;

                value -= rates[i] * firingTime;
                if (value < 0)
                    value = 0;
                result[i] = value * stats.speed;
            }

            return result;
        }

        public static int MaxAttackerCount(Spacing spacing, int pathCount, int ticksLeft, int attackersLeft)
        {
            return Mathf.Min(ticksLeft / spacing.GetTicks() + 1, attackersLeft / pathCount);
        }

        public static int ContributingAttackers(this AttackerStats stats, Spacing spacing, float splashDamageMultiplier, float splashDamageBase, float rate, int count)
        {
            if (stats.AttackerValueLimit(spacing, splashDamageMultiplier, splashDamageBase, rate) > 0.001f)
                return count;

            for (int i = 1; i <= count; i++)
                if (stats.NthAttackerValue(spacing, splashDamageMultiplier, splashDamageBase, rate, i) < 0.001f)
                    return i - 1;

            return count;
        }

        public static void GetRemainingCapacity(this AttackerStats stats, Spacing spacing, float splashDamageMultiplier, float splashDamageBase, float[] rates, int count, bool isFirstBatch, ref float[] pathCapacities, ref float globalCapacity)
        {
            if (count <= 0)
                return;

            var value = stats.AttackersValue(spacing, splashDamageMultiplier, splashDamageBase, rates, count, isFirstBatch);

            for (int i = 0; i < rates.Length; i++)
            {
                pathCapacities[i] -= value[i];
                if (pathCapacities[i] < 0)
                {
                    globalCapacity += pathCapacities[i];
                    pathCapacities[i] = 0;
                }
            }
        }
    }
}
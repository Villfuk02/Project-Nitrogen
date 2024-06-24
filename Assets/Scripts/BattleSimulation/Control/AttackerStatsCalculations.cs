using Game.AttackerStats;
using UnityEngine;
using static Game.AttackerStats.AttackerStats;

namespace BattleSimulation.Control
{
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
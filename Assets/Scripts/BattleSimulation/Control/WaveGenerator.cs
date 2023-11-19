using System;
using System.Collections.Generic;
using UnityEngine;
using static BattleSimulation.Control.WaveGenerator;

namespace BattleSimulation.Control
{
    public class WaveGenerator : MonoBehaviour
    {
        public enum Spacing { Minimal, Tiny, VerySmall, Small, Medium, Sec, SecAndHalf, TwoSecs }

        [Serializable]
        public class Batch
        {
            public bool[] paths;
            public int count;
            public Spacing spacing;
            // public AttackerRecord attacker;
            public Batch(int count, Spacing spacing, params bool[] paths)
            {
                this.count = count;
                this.spacing = spacing;
                this.paths = paths;
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
            if (number == 0)
                return new(new Batch(1, Spacing.Sec, true, false, false));
            if (number == 1)
                return new(new Batch(2, Spacing.Sec, true, false, false));
            if (number < 5)
                return new(new Batch(number - 1, Spacing.Medium, true, true, true));
            return new(new Batch((number - 4) * (number - 3) / 2, Spacing.Medium, true, true, true), new Batch(number - 2, Spacing.Tiny, true, true, true));
        }
    }

    public static class SpacingExtensions
    {
        static readonly (int ticks, float dispSpacing, int dispAmount)[] SpacingParams = { (1, 0.25f, 7), (3, 0.375f, 5), (5, 0.5f, 4), (10, 0.5f, 4), (15, 0.75f, 3), (20, 1, 2), (30, 1.25f, 2), (40, 1.5f, 2) };
        public static int GetTicks(this Spacing spacing) => SpacingParams[(int)spacing].ticks;
        public static float GetDisplaySpacing(this Spacing spacing) => SpacingParams[(int)spacing].dispSpacing;
        public static int GetDisplayAmount(this Spacing spacing) => SpacingParams[(int)spacing].dispAmount;
    }
}

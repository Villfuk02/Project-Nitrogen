using System.Collections.Generic;
using UnityEngine;
using Utils;
using static Game.AttackerStats.AttackerStats;

namespace Game.AttackerStats
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "New Attacker Stats", menuName = "Attacker Stats")]
    public class AttackerStats : ScriptableObject
    {
        static readonly string[] HumanReadableSizeNames = { "Small", "Large", "Boss" };
        static readonly TextUtils.Icon[] SizeIcons = { TextUtils.Icon.Small, TextUtils.Icon.Large, TextUtils.Icon.Boss };
        public enum Size { Small, Large, Boss }
        public enum Spacing { Minimal, Tiny, VerySmall, Small, Medium, Sec, SecAndHalf, TwoSecs, Min = Minimal, Max = TwoSecs, BatchSpacing = Sec }

        public new string name;
        public GameObject prefab;
        public Sprite icon;
        public Size size;
        public Spacing minSpacing;
        public float baseValue;
        public float weight;
        public float speed;
        public int maxHealth;
        public List<string> descriptions;

        public AttackerStats Clone()
        {
            AttackerStats copy = CreateInstance<AttackerStats>();

            copy.name = name;
            copy.prefab = prefab;
            copy.icon = icon;
            copy.size = size;
            copy.minSpacing = minSpacing;
            copy.baseValue = baseValue;
            copy.speed = speed;
            copy.maxHealth = maxHealth;
            copy.descriptions = new(descriptions);

            return copy;
        }

        public static string HumanReadableSize(Size s, bool icons) => $"{(icons ? SizeIcons[(int)s].Sprite() : "")}{HumanReadableSizeNames[(int)s]}";
    }

    public static class SpacingExtensions
    {
        static readonly (int ticks, float dispSpacing, int dispAmount)[] SpacingParams = { (1, 0.25f, 7), (3, 0.375f, 5), (5, 0.5f, 4), (10, 0.5f, 4), (15, 0.75f, 3), (20, 1, 2), (30, 1.25f, 2), (40, 1.5f, 2) };
        public static int GetTicks(this Spacing spacing) => SpacingParams[(int)spacing].ticks;
        public static float GetSeconds(this Spacing spacing) => spacing.GetTicks() * 0.05f;
        public static float GetDisplaySpacing(this Spacing spacing) => SpacingParams[(int)spacing].dispSpacing;
        public static int GetDisplayAmount(this Spacing spacing) => SpacingParams[(int)spacing].dispAmount;
        public static Spacing MinSpacing => Spacing.Minimal;
        public static Spacing MaxSpacing => Spacing.TwoSecs;
    }
}

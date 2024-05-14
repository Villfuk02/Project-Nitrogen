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
        public enum Spacing { Tiny, Small, Medium, Sec, TwoSecs, Min = Tiny, Max = TwoSecs, BatchSpacing = Sec }

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
        static readonly int[] TicksSpacing = { 2, 5, 10, 20, 40 };
        static readonly float[] DisplaySpacing = { 0.375f, 0.5f, 0.75f, 1, 1.5f };
        static readonly int[] MaxDisplayCount = { 5, 4, 3, 2, 2 };
        public static int GetTicks(this Spacing spacing) => TicksSpacing[(int)spacing];
        public static float GetSeconds(this Spacing spacing) => spacing.GetTicks() * TimeUtils.SECS_PER_TICK;
        public static float GetDisplaySpacing(this Spacing spacing) => DisplaySpacing[(int)spacing];
        public static int GetMaxDisplayCount(this Spacing spacing) => MaxDisplayCount[(int)spacing];
    }
}

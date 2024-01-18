using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Game.AttackerStats
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "New Attacker Stats", menuName = "Attacker Stats")]
    public class AttackerStats : ScriptableObject
    {
        static readonly string[] HumanReadableSizeNames = { "Small", "Large", "Boss" };
        static readonly TextUtils.Icon[] SizeIcons = { TextUtils.Icon.Small, TextUtils.Icon.Large, TextUtils.Icon.Boss };
        public enum Size { Small, Large, Boss }

        public new string name;
        public GameObject prefab;
        public Sprite icon;
        public Size size;
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
            copy.speed = speed;
            copy.maxHealth = maxHealth;
            copy.descriptions = new(descriptions);

            return copy;
        }

        public static string HumanReadableSize(Size s, bool icons) => $"{(icons ? SizeIcons[(int)s].Sprite() : "")}{HumanReadableSizeNames[(int)s]}";
    }
}

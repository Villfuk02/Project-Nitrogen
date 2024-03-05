using System.Collections.Generic;
using UnityEngine;

namespace Game.Blueprint
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "New Blueprint", menuName = "Blueprint")]
    public class Blueprint : ScriptableObject
    {
        public enum Rarity { Starter, Common, Rare, Legendary, Special }
        public enum Type { DefensiveBuilding, EconomicBuilding, SpecialBuilding, Ability, Upgrade }

        public new string name;
        public GameObject prefab;
        public Sprite icon;
        public Rarity rarity;
        public Type type;
        public int energyCost;
        public int materialCost;
        public int startingCooldown;
        public int cooldown;
        public float range = -1;
        public int damage = -1;
        public Damage.Damage.Type damageType = 0;
        public int interval = -1;
        public float radius = -1;
        public int delay = -1;
        public int durationTicks = -1;
        public int durationWaves = -1;
        public int fuelProduction = -1;
        public int materialProduction = -1;
        public int energyProduction = -1;
        public int magic1 = -1;
        public List<string> descriptions;

        public bool HasRange => range >= 0;
        public bool HasDamage => damage >= 0;
        public bool HasDamageType => damageType > 0;
        public bool HasInterval => interval >= 0;
        public bool HasRadius => radius >= 0;
        public bool HasDelay => delay >= 0;
        public bool HasDurationTicks => durationTicks >= 0;
        public bool HasDurationWaves => durationWaves >= 0;
        public bool HasFuelProduction => fuelProduction >= 0;
        public bool HasMaterialProduction => materialProduction >= 0;
        public bool HasEnergyProduction => energyProduction >= 0;
        public float BaseDps => damage * 2000 / interval * 0.01f;

        public Blueprint Clone()
        {
            Blueprint copy = CreateInstance<Blueprint>();

            copy.name = name;
            copy.prefab = prefab;
            copy.icon = icon;
            copy.rarity = rarity;
            copy.type = type;
            copy.energyCost = energyCost;
            copy.materialCost = materialCost;
            copy.startingCooldown = startingCooldown;
            copy.cooldown = cooldown;
            copy.range = range;
            copy.damage = damage;
            copy.damageType = damageType;
            copy.interval = interval;
            copy.radius = radius;
            copy.delay = delay;
            copy.durationTicks = durationTicks;
            copy.durationWaves = durationWaves;
            copy.fuelProduction = fuelProduction;
            copy.materialProduction = materialProduction;
            copy.energyProduction = energyProduction;
            copy.magic1 = magic1;
            copy.descriptions = new(descriptions);

            return copy;
        }
    }
}

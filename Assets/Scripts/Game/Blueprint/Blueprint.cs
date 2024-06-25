using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Game.Blueprint
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "New Blueprint", menuName = "Blueprint")]
    public class Blueprint : ScriptableObject, IBlueprintProvider
    {
        public enum Rarity { Starter, Common, Rare, Legendary, Special }

        public enum Type { Tower, EconomicBuilding, SpecialBuilding, Ability }

        public new string name;
        public static readonly ModifiableQuery<IBlueprintProvider, string> Name = new(b => b.GetBaseBlueprint().name);

        public GameObject prefab;

        public Sprite icon;

        public Rarity rarity;

        public Type type;

        public int energyCost;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> EnergyCost = new(b => b.GetBaseBlueprint().energyCost, v => (int)v);

        public int materialCost;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> MaterialCost = new(b => b.GetBaseBlueprint().materialCost, v => (int)v);

        public int startingCooldown;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> StartingCooldown = new(b => b.GetBaseBlueprint().startingCooldown, v => (int)v);

        public int cooldown;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> Cooldown = new(b => b.GetBaseBlueprint().cooldown, v => (int)v);

        public float range = -1;
        public bool HasRange => range >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float> Range = new(b => b.GetBaseBlueprint().range);

        public int damage = -1;
        public bool HasDamage => damage >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> Damage = new(b => b.GetBaseBlueprint().damage, v => (int)v);

        public Damage.Type damageType = 0;
        public bool HasDamageType => damageType > 0;
        public static readonly ModifiableQuery<IBlueprintProvider, Damage.Type> DamageType = new(b => b.GetBaseBlueprint().damageType);

        public int interval = -1;
        public bool HasInterval => interval >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> Interval = new(b => b.GetBaseBlueprint().interval, v => (int)v);

        public float radius = -1;
        public bool HasRadius => radius >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float> Radius = new(b => b.GetBaseBlueprint().radius);

        public int delay = -1;
        public bool HasDelay => delay >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> Delay = new(b => b.GetBaseBlueprint().delay, v => (int)v);

        public int durationTicks = -1;
        public bool HasDurationTicks => durationTicks >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> DurationTicks = new(b => b.GetBaseBlueprint().durationTicks, v => (int)v);

        public int durationWaves = -1;
        public bool HasDurationWaves => durationWaves >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> DurationWaves = new(b => b.GetBaseBlueprint().durationWaves, v => (int)v);

        public int fuelProduction = -1;
        public bool HasFuelProduction => fuelProduction >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> FuelProduction = new(b => b.GetBaseBlueprint().fuelProduction, v => (int)v);

        public int materialProduction = -1;
        public bool HasMaterialProduction => materialProduction >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> MaterialProduction = new(b => b.GetBaseBlueprint().materialProduction, v => (int)v);

        public int energyProduction = -1;
        public bool HasEnergyProduction => energyProduction >= 0;
        public static readonly ModifiableQuery<IBlueprintProvider, float, int> EnergyProduction = new(b => b.GetBaseBlueprint().energyProduction, v => (int)v);

        public List<string> statsToDisplay;
        public List<string> descriptions;

        public float BaseDps => Utils.Damage.CalculateDps(damage, interval);
        public static float Dps(IBlueprintProvider provider) => Utils.Damage.CalculateDps(Damage.Query(provider), Interval.Query(provider));

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
            copy.statsToDisplay = new(statsToDisplay);
            copy.descriptions = new(descriptions);

            return copy;
        }

        public static Blueprint CloneModifiedValues(IBlueprintProvider source)
        {
            Blueprint original = source.GetBaseBlueprint();

            Blueprint copy = CreateInstance<Blueprint>();

            copy.name = Name.Query(source);
            copy.prefab = original.prefab;
            copy.icon = original.icon;
            copy.rarity = original.rarity;
            copy.type = original.type;
            copy.energyCost = EnergyCost.Query(source);
            copy.materialCost = MaterialCost.Query(source);
            copy.startingCooldown = StartingCooldown.Query(source);
            copy.cooldown = Cooldown.Query(source);
            copy.range = Range.Query(source);
            copy.damage = Damage.Query(source);
            copy.damageType = DamageType.Query(source);
            copy.interval = Interval.Query(source);
            copy.radius = Radius.Query(source);
            copy.delay = Delay.Query(source);
            copy.durationTicks = DurationTicks.Query(source);
            copy.durationWaves = DurationWaves.Query(source);
            copy.fuelProduction = FuelProduction.Query(source);
            copy.materialProduction = MaterialProduction.Query(source);
            copy.energyProduction = EnergyProduction.Query(source);
            copy.statsToDisplay = new(original.statsToDisplay);
            copy.descriptions = new(original.descriptions);

            return copy;
        }

        public Blueprint GetBaseBlueprint() => this;

        public (Type, int, int) GetOrderIndex() => (type, energyCost, materialCost);
    }
}
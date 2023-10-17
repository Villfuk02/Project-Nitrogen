using UnityEngine;

namespace Game.Blueprint
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "New Blueprint", menuName = "Blueprint")]
    public class Blueprint : ScriptableObject
    {
        public enum Rarity { Starter, Common, Rare, Legendary }

        //public new string name;
        public GameObject prefab;
        public Rarity rarity;
        public int cost;
        public int startingCooldown = 0;
        public int cooldown;
        public float range = -1;
        public int damage = -1;
        public Damage.Damage.Type damageType = 0;
        public int shotInterval = -1;
        public int materialGeneration = -1;
        public int energyGeneration = -1;
        public int fuelGeneration = -1;

        public bool HasRange => range >= 0;
        public bool HasDamage => damage >= 0;
        public bool HasDamageType => damageType > 0;
        public bool HasShotInterval => shotInterval >= 0;
        public bool HasMaterialGeneration => materialGeneration >= 0;
        public bool HasEnergyGeneration => energyGeneration >= 0;
        public bool HasFuelGeneration => fuelGeneration >= 0;

        public Blueprint Clone()
        {
            Blueprint copy = CreateInstance<Blueprint>();

            copy.prefab = prefab;
            copy.rarity = rarity;
            copy.cost = cost;
            copy.startingCooldown = startingCooldown;
            copy.cooldown = cooldown;
            copy.range = range;
            copy.damage = damage;
            copy.damageType = damageType;
            copy.shotInterval = shotInterval;
            copy.materialGeneration = materialGeneration;
            copy.energyGeneration = energyGeneration;
            copy.fuelGeneration = fuelGeneration;

            return copy;
        }
    }
}

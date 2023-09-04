namespace Game.Blueprint
{
    [System.Serializable]
    public struct Blueprint
    {
        public enum Rarity { Starter, Common, Rare, Legendary }

        public string name;
        public Rarity rarity;
        public int? cost;
        public int? cooldown;
        public int? range;
        public int? damage;
        public Damage.Damage.Type? damageType;
        public int? shotInterval;

        public Blueprint(string name, Rarity rarity)
        {
            this.name = name;
            this.rarity = rarity;
            cost = null;
            cooldown = null;
            range = null;
            damage = null;
            damageType = null;
            shotInterval = null;
        }
    }
}

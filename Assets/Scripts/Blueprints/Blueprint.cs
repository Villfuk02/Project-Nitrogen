using System.Collections.Generic;
using static Blueprints.Blueprint;

namespace Blueprints
{
    public record Blueprint(string Name, BlueprintRarity Rarity, Dictionary<Stat, int> Stats)
    {
        public enum Stat { Cooldown, Cost, Range, Damage, ShotInterval }
        public enum BlueprintRarity { Starter, Common, Rare, Legendary }
    }
}

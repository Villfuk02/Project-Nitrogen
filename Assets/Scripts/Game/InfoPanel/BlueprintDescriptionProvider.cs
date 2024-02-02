using BattleSimulation.Buildings;
using System.Collections.Generic;
using System.Text;

namespace Game.InfoPanel
{
    public class BlueprintDescriptionProvider : DescriptionProvider
    {
        readonly Blueprint.Blueprint blueprint_;
        readonly Blueprint.Blueprint original_;
        readonly Building? building_;
        readonly DescriptionFormatter<(Blueprint.Blueprint, Blueprint.Blueprint)> descriptionFormatter_;
        public BlueprintDescriptionProvider(Building building) : this(building.Blueprint, building.OriginalBlueprint)
        {
            building_ = building;
        }

        public BlueprintDescriptionProvider(Blueprint.Blueprint blueprint, Blueprint.Blueprint original)
        {
            blueprint_ = blueprint;
            original_ = original;
            descriptionFormatter_ = DescriptionFormat.Blueprint(blueprint, original);
        }

        protected override string GenerateDescription() => descriptionFormatter_.Format(GenerateRawDescription());

        string GenerateRawDescription()
        {
            StringBuilder sb = new();

            string? extra = building_ != null ? building_.GetExtraStats() : null;
            if (extra != null)
            {
                sb.Append(extra);
                sb.Append("[BRK]");
            }

            List<string> stats = new();
            if (blueprint_.HasRange)
                stats.Add("Range [RNG]");
            if (blueprint_.HasDamage)
                stats.Add("Damage [DMG]");
            if (blueprint_.HasDamageType)
                stats.Add("Damage Type [DMT]");
            if (blueprint_.HasShotInterval)
                stats.Add("Shot Interval [SHI]");
            if (blueprint_.HasDamage && blueprint_.HasShotInterval)
                stats.Add("Base Damage/s [DPS]");

            if (stats.Count > 0)
            {
                sb.AppendJoin('\n', stats);
                sb.Append("[BRK]");
            }
            foreach (string desc in blueprint_.descriptions)
            {
                sb.Append(desc);
                sb.Append("[BRK]");
            }

            return sb.ToString();
        }
    }
}

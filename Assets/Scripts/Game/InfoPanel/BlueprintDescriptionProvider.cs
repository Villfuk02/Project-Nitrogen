using System.Collections.Generic;
using System.Text;

namespace Game.InfoPanel
{
    public class BlueprintDescriptionProvider : DescriptionProvider
    {
        readonly Blueprint.Blueprint blueprint_;
        readonly Blueprint.Blueprint original_;
        readonly DescriptionFormatter<(Blueprint.Blueprint, Blueprint.Blueprint)> descriptionFormatter_;

        public BlueprintDescriptionProvider(Blueprint.Blueprint blueprint, Blueprint.Blueprint original)
        {
            blueprint_ = blueprint;
            original_ = original;
            descriptionFormatter_ = DescriptionFormat.Blueprint(blueprint, original);
        }

        protected override string GenerateDescription() => descriptionFormatter_.Format(GenerateRawDescription());

        string GenerateRawDescription()
        {
            List<string> stats = new();

            if (blueprint_.HasRange)
                stats.Add("Range [RNG]");
            if (blueprint_.HasDamage)
                stats.Add("Damage [DMG]");
            if (blueprint_.HasDamageType)
                stats.Add("Damage Type [DMT]");
            if (blueprint_.HasShotInterval)
                stats.Add("Shot Interval [SHI]");
            if (blueprint_.HasEnergyGeneration || blueprint_.HasMaterialGeneration || blueprint_.HasFuelGeneration)
                stats.Add("Generation [GEN]");

            StringBuilder sb = new();
            sb.AppendJoin('\n', stats);
            sb.Append("[BRK]");
            foreach (string desc in blueprint_.descriptions)
            {
                sb.Append(desc);
                sb.Append("[BRK]");
            }

            return sb.ToString();
        }
    }
}

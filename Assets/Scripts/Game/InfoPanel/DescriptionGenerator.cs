using System.Collections.Generic;
using System.Text;

namespace Game.InfoPanel
{
    public static class DescriptionGenerator
    {
        public static string GenerateBlueprintDescription(Blueprint.Blueprint blueprint)
        {
            List<string> stats = new();

            if (blueprint.HasRange)
                stats.Add("Range [RNG]");
            if (blueprint.HasDamage)
                stats.Add("Damage [DMG]");
            if (blueprint.HasDamageType)
                stats.Add("Damage Type [DMT]");
            if (blueprint.HasShotInterval)
                stats.Add("Shot Interval [SHI]");
            if (blueprint.HasEnergyGeneration || blueprint.HasMaterialGeneration || blueprint.HasFuelGeneration)
                stats.Add("Generation [GEN]");

            StringBuilder sb = new();
            sb.AppendJoin('\n', stats);
            sb.Append("[BRK]");
            foreach (string desc in blueprint.descriptions)
            {
                sb.Append(desc);
                sb.Append("[BRK]");
            }

            return sb.ToString();
        }
    }
}
